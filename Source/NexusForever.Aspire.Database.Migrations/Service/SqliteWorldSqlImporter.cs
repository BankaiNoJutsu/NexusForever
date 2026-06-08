using System.Data.Common;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using NexusForever.Database.World;

namespace NexusForever.Aspire.Database.Migrations.Service
{
    public sealed class SqliteWorldSqlImporter
    {
        private static readonly Regex SetLiteralRegex = new(@"^\s*SET\s+@(?<name>[A-Za-z0-9_]+)\s*(?:=|:=)\s*(?<value>-?\d+(?:\.\d+)?)\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex SetMaxIdRegex = new(@"^\s*SET\s+@(?<name>[A-Za-z0-9_]+)\s*(?:=|:=)\s*\(\s*SELECT\s+IFNULL\s*\(\s*MAX\s*\(\s*`?(?<column>[A-Za-z0-9_]+)`?\s*\)\s*,\s*0\s*\)\s+FROM\s+`?(?<table>[A-Za-z0-9_]+)`?\s*\)\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex VariableRegex = new(@"@(?<name>[A-Za-z0-9_]+)", RegexOptions.Compiled);
        private static readonly Regex ConvertHexRegex = new(@"CONVERT\s*\(\s*0x(?<hex>[0-9A-Fa-f]+)\s+USING\s+utf8mb4\s*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex InsertRegex = new(@"^\s*INSERT\s+(?:IGNORE\s+)?INTO\s+`?(?<table>[A-Za-z0-9_]+)`?\s*\((?<columns>.*?)\)\s*(?<body>.*)$", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex OnDuplicateRegex = new(@"\s+ON\s+DUPLICATE\s+KEY\s+UPDATE\s+", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex ValuesFunctionRegex = new(@"VALUES\s*\(\s*(?<column>`?[A-Za-z0-9_]+`?)\s*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly ILogger _log;
        private readonly WorldContext _context;
        private readonly Dictionary<string, TableConflictMetadata> _tableMetadata;
        private readonly Dictionary<string, string> _variables = new(StringComparer.OrdinalIgnoreCase);

        public SqliteWorldSqlImporter(ILogger log, WorldContext context)
        {
            _log           = log;
            _context       = context;
            _tableMetadata = BuildTableMetadata(context);
        }

        public async Task ImportFileAsync(string filePath, CancellationToken cancellationToken)
        {
            string content = await File.ReadAllTextAsync(filePath, cancellationToken);
            IReadOnlyList<string> statements = SplitStatements(RemoveComments(content));
            bool disableForeignKeys = UsesDisabledForeignKeyChecks(statements);

            if (disableForeignKeys)
            {
                await _context.Database.OpenConnectionAsync(cancellationToken);
                await SetForeignKeysAsync(false, cancellationToken);
            }

            try
            {
                await using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    foreach (string rawStatement in statements)
                    {
                        string statement = rawStatement.Trim();
                        if (statement.Length == 0 || await TryHandleDirective(statement, cancellationToken))
                            continue;

                        statement = TranslateStatement(statement);
                        if (statement.Length == 0)
                            continue;

                        try
                        {
                            await ExecuteSqlAsync(statement, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _log.LogError(ex, "SQLite import failed in {FilePath}. Statement: {Statement}", filePath, Abbreviate(statement, 600));
                            throw;
                        }
                    }

                    await transaction.CommitAsync(cancellationToken);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            }
            finally
            {
                if (disableForeignKeys)
                {
                    try
                    {
                        await SetForeignKeysAsync(true, cancellationToken);
                    }
                    finally
                    {
                        await _context.Database.CloseConnectionAsync();
                    }
                }
            }
        }

        private async Task ExecuteSqlAsync(string statement, CancellationToken cancellationToken)
        {
            DbConnection connection = _context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync(cancellationToken);

            await using DbCommand command = connection.CreateCommand();
            if (_context.Database.CurrentTransaction != null)
                command.Transaction = _context.Database.CurrentTransaction.GetDbTransaction();

            command.CommandText = statement;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static bool UsesDisabledForeignKeyChecks(IEnumerable<string> statements)
        {
            return statements.Any(s => Regex.IsMatch(s.Trim(), @"^SET\s+FOREIGN_KEY_CHECKS\s*=\s*0\s*$", RegexOptions.IgnoreCase));
        }

        private async Task SetForeignKeysAsync(bool enabled, CancellationToken cancellationToken)
        {
            DbConnection connection = _context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync(cancellationToken);

            await using DbCommand command = connection.CreateCommand();
            command.CommandText = $"PRAGMA foreign_keys = {(enabled ? "ON" : "OFF")}";
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task<bool> TryHandleDirective(string statement, CancellationToken cancellationToken)
        {
            if (StartsWith(statement, "USE ")
                || StartsWith(statement, "START TRANSACTION")
                || StartsWith(statement, "COMMIT")
                || StartsWith(statement, "SET FOREIGN_KEY_CHECKS")
                || StartsWith(statement, "SET @NF_OLD_FOREIGN_KEY_CHECKS")
                || StartsWith(statement, "DROP TEMPORARY TABLE")
                || StartsWith(statement, "CREATE TEMPORARY TABLE")
                || StartsWith(statement, "INSERT INTO tmp_nf_required_runtime_tables")
                || StartsWith(statement, "SET @nf_missing_runtime_tables")
                || StartsWith(statement, "SET @nf_missing_runtime_tables_sql")
                || StartsWith(statement, "PREPARE ")
                || StartsWith(statement, "EXECUTE ")
                || StartsWith(statement, "DEALLOCATE "))
            {
                return true;
            }

            Match literalMatch = SetLiteralRegex.Match(statement);
            if (literalMatch.Success)
            {
                _variables[literalMatch.Groups["name"].Value] = literalMatch.Groups["value"].Value;
                return true;
            }

            Match maxIdMatch = SetMaxIdRegex.Match(statement);
            if (maxIdMatch.Success)
            {
                string table  = maxIdMatch.Groups["table"].Value;
                string column = maxIdMatch.Groups["column"].Value;
                object result = await ExecuteScalarAsync($"SELECT IFNULL(MAX(`{column}`), 0) FROM `{table}`", cancellationToken);
                _variables[maxIdMatch.Groups["name"].Value] = Convert.ToString(result, CultureInfo.InvariantCulture) ?? "0";
                return true;
            }

            if (StartsWith(statement, "SET @"))
                throw new NotSupportedException($"Unsupported SQLite import variable directive: {statement}");

            return false;
        }

        private string TranslateStatement(string statement)
        {
            statement = ReplaceVariables(statement);
            statement = NormalizeStringEscapes(statement);
            statement = ConvertHexRegex.Replace(statement, match => ToSqlString(match.Groups["hex"].Value));
            statement = TranslateDeleteJoin(statement);
            statement = TranslateInsert(statement);
            return statement;
        }

        private string ReplaceVariables(string statement)
        {
            return VariableRegex.Replace(statement, match =>
            {
                string name = match.Groups["name"].Value;
                if (!_variables.TryGetValue(name, out string value))
                    throw new NotSupportedException($"SQLite import statement references unknown variable @{name}: {statement}");

                return value;
            });
        }

        private string TranslateDeleteJoin(string statement)
        {
            if (!Regex.IsMatch(statement, @"^\s*DELETE\s+li\s+FROM\s+loot_item\s+li\s+JOIN\s+loot_group\s+lg\s+ON\s+lg\.id\s*=\s*li\.id\s+WHERE\s+lg\.comment\s+LIKE\s+'DataMapping %'\s*$", RegexOptions.IgnoreCase | RegexOptions.Singleline))
                return statement;

            return """
                DELETE FROM loot_item
                WHERE id IN (
                    SELECT li.id
                    FROM loot_item li
                    JOIN loot_group lg ON lg.id = li.id
                    WHERE lg.comment LIKE 'DataMapping %'
                )
                """;
        }

        private string TranslateInsert(string statement)
        {
            Match insertMatch = InsertRegex.Match(statement);
            if (!insertMatch.Success)
                return statement;

            Match onDuplicateMatch = OnDuplicateRegex.Match(statement);
            if (!onDuplicateMatch.Success)
            {
                string translatedSql = Regex.Replace(statement, @"^\s*INSERT\s+IGNORE\s+INTO\s+", "INSERT OR IGNORE INTO ", RegexOptions.IgnoreCase);
                return Regex.Replace(translatedSql, @"\s+VALUE\s+", " VALUES ", RegexOptions.IgnoreCase);
            }

            string table = insertMatch.Groups["table"].Value;
            string[] insertColumns = SplitColumns(insertMatch.Groups["columns"].Value);
            string conflictTarget = GetConflictTarget(table, insertColumns);

            string insertSql = statement[..onDuplicateMatch.Index];
            insertSql = Regex.Replace(insertSql, @"^\s*INSERT\s+IGNORE\s+INTO\s+", "INSERT INTO ", RegexOptions.IgnoreCase);
            insertSql = Regex.Replace(insertSql, @"\s+VALUE\s+", " VALUES ", RegexOptions.IgnoreCase);

            string updateSql = statement[(onDuplicateMatch.Index + onDuplicateMatch.Length)..].Trim();
            updateSql = ValuesFunctionRegex.Replace(updateSql, match => $"excluded.{match.Groups["column"].Value}");

            return $"{insertSql} ON CONFLICT ({conflictTarget}) DO UPDATE SET {updateSql}";
        }

        private string GetConflictTarget(string table, string[] insertColumns)
        {
            if (!_tableMetadata.TryGetValue(table, out TableConflictMetadata metadata))
                throw new NotSupportedException($"SQLite import cannot find EF metadata for table '{table}'.");

            HashSet<string> insertColumnSet = insertColumns.Select(UnquoteIdentifier).ToHashSet(StringComparer.OrdinalIgnoreCase);
            string[] conflictColumns = metadata.UniqueColumnSets.FirstOrDefault(c => c.All(insertColumnSet.Contains));
            if (conflictColumns == null)
                throw new NotSupportedException($"SQLite import cannot infer an ON CONFLICT target for table '{table}' with columns: {string.Join(", ", insertColumns)}.");

            return string.Join(", ", conflictColumns.Select(QuoteIdentifier));
        }

        private async Task<object> ExecuteScalarAsync(string sql, CancellationToken cancellationToken)
        {
            DbConnection connection = _context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync(cancellationToken);

            await using DbCommand command = connection.CreateCommand();
            command.CommandText = sql;
            if (_context.Database.CurrentTransaction != null)
                command.Transaction = _context.Database.CurrentTransaction.GetDbTransaction();

            return await command.ExecuteScalarAsync(cancellationToken);
        }

        private static Dictionary<string, TableConflictMetadata> BuildTableMetadata(WorldContext context)
        {
            var result = new Dictionary<string, TableConflictMetadata>(StringComparer.OrdinalIgnoreCase);
            foreach (IEntityType entityType in context.Model.GetEntityTypes())
            {
                string table = entityType.GetTableName();
                if (table == null)
                    continue;

                var storeObject = StoreObjectIdentifier.Table(table, entityType.GetSchema());
                var uniqueColumnSets = new List<string[]>();

                IKey primaryKey = entityType.FindPrimaryKey();
                if (primaryKey != null)
                    uniqueColumnSets.Add(primaryKey.Properties.Select(p => p.GetColumnName(storeObject)).Where(c => c != null).ToArray());

                foreach (IIndex index in entityType.GetIndexes().Where(i => i.IsUnique))
                    uniqueColumnSets.Add(index.Properties.Select(p => p.GetColumnName(storeObject)).Where(c => c != null).ToArray());

                result[table] = new TableConflictMetadata(uniqueColumnSets.Where(c => c.Length != 0).ToArray());
            }

            return result;
        }

        private static IReadOnlyList<string> SplitStatements(string sql)
        {
            var statements = new List<string>();
            var current = new StringBuilder();
            bool inSingleQuote = false;
            bool inDoubleQuote = false;

            for (int i = 0; i < sql.Length; i++)
            {
                char c = sql[i];

                if (c == '\'' && !inDoubleQuote)
                {
                    current.Append(c);
                    if (inSingleQuote && i + 1 < sql.Length && sql[i + 1] == '\'')
                    {
                        current.Append(sql[++i]);
                        continue;
                    }

                    if (!IsEscaped(sql, i))
                        inSingleQuote = !inSingleQuote;
                    continue;
                }

                if (c == '"' && !inSingleQuote)
                {
                    inDoubleQuote = !inDoubleQuote;
                    current.Append(c);
                    continue;
                }

                if (c == ';' && !inSingleQuote && !inDoubleQuote)
                {
                    statements.Add(current.ToString());
                    current.Clear();
                    continue;
                }

                current.Append(c);
            }

            if (current.Length != 0)
                statements.Add(current.ToString());

            return statements;
        }

        private static string RemoveComments(string sql)
        {
            var builder = new StringBuilder(sql.Length);
            bool inSingleQuote = false;
            bool inDoubleQuote = false;
            bool inLineComment = false;
            bool inBlockComment = false;

            for (int i = 0; i < sql.Length; i++)
            {
                char c = sql[i];
                char next = i + 1 < sql.Length ? sql[i + 1] : '\0';

                if (inLineComment)
                {
                    if (c is '\r' or '\n')
                    {
                        inLineComment = false;
                        builder.Append(c);
                    }
                    continue;
                }

                if (inBlockComment)
                {
                    if (c == '*' && next == '/')
                    {
                        inBlockComment = false;
                        i++;
                    }
                    continue;
                }

                if (!inSingleQuote && !inDoubleQuote && c == '-' && next == '-')
                {
                    inLineComment = true;
                    i++;
                    continue;
                }

                if (!inSingleQuote && !inDoubleQuote && c == '/' && next == '*')
                {
                    inBlockComment = true;
                    i++;
                    continue;
                }

                if (c == '\'' && !inDoubleQuote && !IsEscaped(sql, i))
                    inSingleQuote = !inSingleQuote;
                else if (c == '"' && !inSingleQuote)
                    inDoubleQuote = !inDoubleQuote;

                builder.Append(c);
            }

            return builder.ToString();
        }

        private static string NormalizeStringEscapes(string sql)
        {
            var builder = new StringBuilder(sql.Length);
            bool inSingleQuote = false;

            for (int i = 0; i < sql.Length; i++)
            {
                char c = sql[i];
                if (c == '\'')
                {
                    builder.Append(c);
                    if (inSingleQuote && i + 1 < sql.Length && sql[i + 1] == '\'')
                    {
                        builder.Append(sql[++i]);
                        continue;
                    }

                    inSingleQuote = !inSingleQuote;
                    continue;
                }

                if (inSingleQuote && c == '\\' && i + 1 < sql.Length)
                {
                    char next = sql[++i];
                    builder.Append(next == '\'' ? "''" : next);
                    continue;
                }

                builder.Append(c);
            }

            return builder.ToString();
        }

        private static bool IsEscaped(string text, int index)
        {
            int slashCount = 0;
            for (int i = index - 1; i >= 0 && text[i] == '\\'; i--)
                slashCount++;

            return slashCount % 2 == 1;
        }

        private static string[] SplitColumns(string columns)
        {
            return columns.Split(',')
                .Select(c => c.Trim())
                .Where(c => c.Length != 0)
                .Select(UnquoteIdentifier)
                .ToArray();
        }

        private static string UnquoteIdentifier(string identifier)
        {
            identifier = identifier.Trim();
            if (identifier.Length >= 2 && identifier[0] == '`' && identifier[^1] == '`')
                return identifier[1..^1];

            return identifier;
        }

        private static string QuoteIdentifier(string identifier)
        {
            return $"`{identifier.Replace("`", "``")}`";
        }

        private static string ToSqlString(string hex)
        {
            byte[] bytes = Convert.FromHexString(hex);
            return $"'{Encoding.UTF8.GetString(bytes).Replace("'", "''")}'";
        }

        private static bool StartsWith(string value, string prefix)
        {
            return value.TrimStart().StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        private static string Abbreviate(string value, int maxLength)
        {
            return value.Length <= maxLength ? value : value[..maxLength] + "...";
        }

        private sealed record TableConflictMetadata(IReadOnlyList<string[]> UniqueColumnSets);
    }
}
