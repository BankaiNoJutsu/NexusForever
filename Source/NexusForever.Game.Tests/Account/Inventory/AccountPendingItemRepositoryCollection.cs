namespace NexusForever.Game.Tests.Account.Inventory;

// These fixtures intentionally use the process-wide fallback repository and
// fixed account IDs. Their clear/seed/assert sequences must not interleave.
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class AccountPendingItemRepositoryCollection
{
    public const string Name = "Account pending item repository";
}
