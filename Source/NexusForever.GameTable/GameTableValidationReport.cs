namespace NexusForever.GameTable
{
    public sealed class GameTableValidationReport
    {
        public GameTableValidationReport(int requiredTableCount, IReadOnlyList<string> missingRequiredTables)
        {
            RequiredTableCount     = requiredTableCount;
            MissingRequiredTables = missingRequiredTables;
        }

        public int RequiredTableCount { get; }
        public IReadOnlyList<string> MissingRequiredTables { get; }
        public bool HasMissingRequiredTables => MissingRequiredTables.Count != 0;
    }
}
