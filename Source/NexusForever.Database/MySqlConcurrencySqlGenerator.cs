using System.Text;
using Microsoft.EntityFrameworkCore.Update;
using Microting.EntityFrameworkCore.MySql.Infrastructure.Internal;
using Microting.EntityFrameworkCore.MySql.Update.Internal;

namespace NexusForever.Database
{
    // The pinned provider's generator preserves its DML/affected-row behavior;
    // only string concurrency predicates need ordinal rather than collation equality.
#pragma warning disable EF1001
    internal sealed class MySqlConcurrencySqlGenerator : MySqlUpdateSqlGenerator
    {
        public MySqlConcurrencySqlGenerator(UpdateSqlGeneratorDependencies dependencies, IMySqlOptions options)
            : base(dependencies, options)
        {
        }

        protected override void AppendWhereCondition(StringBuilder commandStringBuilder,
            IColumnModification columnModification, bool useOriginalValue)
        {
            // A PAD SPACE collation otherwise treats an empty CSV and a space as
            // equal, allowing a stale DELETE to erase a competing committed row.
            if (columnModification.Property?.IsConcurrencyToken == true
                && columnModification.TypeMapping?.ClrType == typeof(string))
                commandStringBuilder.Append("BINARY ");

            base.AppendWhereCondition(commandStringBuilder, columnModification, useOriginalValue);
        }
    }
#pragma warning restore EF1001
}
