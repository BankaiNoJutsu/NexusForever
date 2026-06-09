namespace NexusForever.GameTable
{
    public sealed class MissingGameDataDiagnostic
    {
        public MissingGameDataDiagnostic(
            MissingGameDataDiagnosticKind kind,
            MissingGameDataSeverity severity,
            string tableName,
            string staticId,
            string context,
            string detail,
            long count)
        {
            Kind      = kind;
            Severity  = severity;
            TableName = tableName;
            StaticId  = staticId;
            Context   = context;
            Detail    = detail;
            Count     = count;
        }

        public MissingGameDataDiagnosticKind Kind { get; }
        public MissingGameDataSeverity Severity { get; }
        public string TableName { get; }
        public string StaticId { get; }
        public string Context { get; }
        public string Detail { get; }
        public long Count { get; }
    }
}
