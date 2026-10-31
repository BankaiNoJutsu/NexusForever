using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Static.Quest;
using NexusForever.Network.Message;

internal static class ContractAcceptanceCommand
{
    internal static void ValidateBoundary(LoginWorldOptions options, int quest, bool allowed)
    {
        if (!allowed || quest is < 1 or > 0x7FFF || !Regex.IsMatch(options.User, @"^loadtest[0-9]+@example\.local$", RegexOptions.CultureInvariant)
            || new[] { options.StsHost, options.AuthHost, options.WorldHost }.Any(host => host is not ("127.0.0.1" or "localhost")))
            throw new InvalidOperationException("Contract acceptance requires an explicit owned loopback loadtest fixture and a mapped Quest identity.");
        var auth = new MySqlConnectionStringBuilder(options.AuthConnectionString);
        var character = new MySqlConnectionStringBuilder(options.CharacterConnectionString);
        if (new[] { auth, character }.Any(b => b.Server is not ("127.0.0.1" or "localhost"))
            || !Regex.IsMatch(auth.Database, @"^nexus_assessment_[a-zA-Z0-9_]+_auth$", RegexOptions.CultureInvariant)
            || character.Database != auth.Database[..^5] + "_character" || auth.Port != character.Port)
            throw new InvalidOperationException("Contract acceptance requires the same owned local assessment Auth/Character stack.");
    }

    internal static bool ValidAcceptance(string? value) => value is { Length: 32 }
        && Guid.TryParseExact(value, "N", out var token) && token != Guid.Empty && token.ToString("N") == value;

    internal static async Task<int> Run(CliArgs args)
    {
        var options = new LoginWorldOptions
        {
            User = args.GetString("user", ""), Password = args.GetString("password", Defaults.Password),
            StsHost = args.GetString("sts-host", "127.0.0.1"), AuthHost = args.GetString("auth-host", "127.0.0.1"), WorldHost = args.GetString("world-host", "127.0.0.1"),
            StsPort = args.GetInt("sts-port", 6600), AuthPort = args.GetInt("auth-port", 23115), WorldPort = args.GetInt("world-port", 24009),
            AuthConnectionString = args.GetString("auth-connection", Defaults.AuthConnectionString), CharacterConnectionString = args.GetString("character-connection", Defaults.CharacterConnectionString),
            ClientEnteredWorldDelayMs = 250
        };
        int quest = args.GetInt("quest", 0);
        ValidateBoundary(options, quest, args.Has("allow-contract-acceptance"));
        string output = Path.GetFullPath(args.GetString("output", Path.Combine("artifacts", "load-tests", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-contract-acceptance")));
        if (Directory.Exists(output)) throw new InvalidOperationException("Refusing to overwrite Contract evidence.");
        Directory.CreateDirectory(output);
        var started = DateTime.UtcNow; var sessions = new List<object>(); bool sent = false;
        ContractSqlState? before = null, final = null; string? token = null;
        try
        {
            var owner = await LoginWorldCommand.LookupAccountCharacter(options, options.User);
            await WaitOffline(options, owner, (ushort)quest);
            before = await Read(options, owner, (ushort)quest);
            if (before.State is QuestState.Completed or QuestState.Botched or QuestState.Abandoned or QuestState.Ignored)
                throw new InvalidOperationException("Terminal fixture Quest requires a separate lifecycle scenario.");
            bool resume = before.State is QuestState.Accepted or QuestState.Achieved;
            if (resume && !ValidAcceptance(before.Acceptance)) throw new InvalidOperationException("Historical active Contract is intentionally not backfilled.");
            if (resume) token = before.Acceptance;
            for (int phase = 0; phase < 2; ++phase)
            {
                var observer = new ContractSocketObserver();
                var sample = new LoginWorldSample { User = options.User, Label = "contract-acceptance-relog", StartedUtc = DateTime.UtcNow };
                var connected = await LoginWorldCommand.ConnectForScenario(options, sample, observer.Receive);
                await using (var session = new ReceivingWorld(connected.World))
                {
                    if (connected.Character.CharacterId != owner.CharacterId) throw new InvalidOperationException("Fixture owner changed.");
                    if (phase == 0 && !resume)
                    {
                        await session.World.SendEncrypted(GameMessageOpcode.ClientQuestAccept, writer => ContractProtocol.WriteAccept(writer, (ushort)quest));
                        sent = true;
                    }
                    await Until(() => { session.Check(); return Task.FromResult(observer.State((ushort)quest) is QuestState.Accepted or QuestState.Achieved); }, "Contract acceptance/relog publication");
                    sessions.Add(new { Phase = phase, State = observer.State((ushort)quest), observer.Changes, sample.Steps });
                }
                await WaitOffline(options, owner, (ushort)quest);
                final = await Read(options, owner, (ushort)quest);
                if (final.State is not (QuestState.Accepted or QuestState.Achieved) || !ValidAcceptance(final.Acceptance))
                    throw new InvalidOperationException("Normal cleanup did not retain the accepted Contract identity.");
                token ??= final.Acceptance;
                if (final.Acceptance != token || final.EconomySHA256 != before.EconomySHA256)
                    throw new InvalidOperationException("Acceptance identity changed or acceptance unexpectedly altered rewards.");
                await File.WriteAllTextAsync(Path.Combine(output, $"phase-{phase}.json"), JsonSerializer.Serialize(final, Json));
            }
            await Report(true, null); Console.WriteLine("Contract acceptance socket PASS: normal accept/cleanup/relog; stable identity and unchanged rewards."); return 0;
        }
        catch (Exception error)
        {
            string message = error.GetType().Name + ": " + error.Message;
            foreach (var secret in new[] { options.Password, new MySqlConnectionStringBuilder(options.AuthConnectionString).Password, new MySqlConnectionStringBuilder(options.CharacterConnectionString).Password }.Where(v => !string.IsNullOrEmpty(v)).Distinct())
                message = message.Replace(secret, "[REDACTED]", StringComparison.Ordinal);
            await Report(false, message); Console.Error.WriteLine(message); return 1;
        }
        async Task Report(bool passed, string? error) => await File.WriteAllTextAsync(Path.Combine(output, "contract-acceptance.json"), JsonSerializer.Serialize(new
        {
            Passed = passed, StartedUtc = started, CompletedUtc = DateTime.UtcNow, options.User, QuestId = quest, SentAccept = sent, Before = before, Sessions = sessions, Final = final, Error = error,
            Scope = "Normal socket login/Quest accept/logout/relog; read-only SQL observation. No placement, Quest/token/reward SQL writes, forced completion, Contract credit/claim or native UI."
        }, Json));
    }

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private sealed record ContractSqlState(bool Online, bool Closed, ulong Epoch, QuestState? State, string? Acceptance, string EconomySHA256);
    private static async Task<ContractSqlState> Read(LoginWorldOptions options, AccountCharacter owner, ushort quest)
    {
        await using var db = new CharacterContext(new DatabaseConnectionString { Provider = DatabaseProvider.MySql, ConnectionString = options.CharacterConnectionString });
        var player = await db.Character.AsNoTracking().SingleAsync(row => row.Id == owner.CharacterId && row.AccountId == owner.AccountId);
        if (!player.Name.StartsWith("Load", StringComparison.Ordinal)) throw new InvalidOperationException("Owned Load fixture required.");
        var row = await db.CharacterQuest.AsNoTracking().SingleOrDefaultAsync(row => row.Id == owner.CharacterId && row.QuestId == quest);
        var authBuilder = new MySqlConnectionStringBuilder(options.AuthConnectionString); string friendship = authBuilder.Database[..^5] + "_friendship";
        await using var auth = new MySqlConnection(authBuilder.ConnectionString); await auth.OpenAsync();
        await using var command = auth.CreateCommand();
        command.CommandText = $"SELECT w.epoch,COALESCE(f.presenceSessionEpoch=w.epoch AND f.presenceClosed=1 AND f.activeCharacterId IS NULL AND f.activeRealmId IS NULL,0) FROM account_world_session w LEFT JOIN `{friendship}`.account f ON f.accountId=w.accountId WHERE w.accountId=@id";
        command.Parameters.AddWithValue("@id", owner.AccountId); ulong epoch = 0; bool closed = true;
        await using (var reader = await command.ExecuteReaderAsync()) if (await reader.ReadAsync()) { epoch = reader.GetUInt64(0); closed = reader.GetBoolean(1); }
        await using var connection = new MySqlConnection(options.CharacterConnectionString); await connection.OpenAsync();
        var values = new List<string>();
        foreach (string table in new[] { "character_currency", "character_challenge_reward_track", "character_challenge_reward_slot", "character_challenge_reward_claim", "character_challenge_medal_receipt", "item" })
        {
            await using var select = connection.CreateCommand(); select.CommandText = $"SELECT * FROM `{table}` WHERE `{(table == "item" ? "ownerId" : "id")}`=@id"; select.Parameters.AddWithValue("@id", owner.CharacterId);
            await using var reader = await select.ExecuteReaderAsync();
            while (await reader.ReadAsync()) values.Add(table + ":" + string.Join(':', Enumerable.Range(0, reader.FieldCount).Select(i => reader.IsDBNull(i) ? "NULL" : Convert.ToHexString(Encoding.UTF8.GetBytes(Convert.ToString(reader.GetValue(i), CultureInfo.InvariantCulture)!)))));
        }
        values.Sort(StringComparer.Ordinal);
        return new(player.IsOnline, closed, epoch, row == null ? null : (QuestState)row.State, row?.ContractAcceptanceId, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', values)))));
    }
    private static Task WaitOffline(LoginWorldOptions options, AccountCharacter owner, ushort quest)
        => Until(async () => { var row = await Read(options, owner, quest); return !row.Online && row.Closed; }, "Normal offline cleanup/presence");
    private static async Task Until(Func<Task<bool>> condition, string label)
    {
        var watch = Stopwatch.StartNew();
        while (!await condition()) { if (watch.Elapsed > TimeSpan.FromSeconds(30)) throw new TimeoutException(label); await Task.Delay(100); }
    }
    private sealed class ReceivingWorld : IAsyncDisposable
    {
        internal GameProtocolClient World { get; }
        private readonly CancellationTokenSource cancellation = new();
        private readonly Task receiver;
        internal ReceivingWorld(GameProtocolClient world) { World = world; receiver = Receive(); }
        private async Task Receive() { while (!cancellation.IsCancellationRequested) _ = await World.ReadInnerFrame(cancellation.Token); }
        internal void Check() { if (receiver.IsCompleted) receiver.GetAwaiter().GetResult(); }
        public async ValueTask DisposeAsync()
        {
            cancellation.Cancel(); await World.DisposeAsync();
            try { await receiver; } catch (Exception error) when (error is OperationCanceledException or IOException or ObjectDisposedException) { }
            cancellation.Dispose();
        }
    }
}
