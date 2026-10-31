namespace NexusForever.Game.Tests.Network;

// These real-TCP tests deliberately exercise short protocol deadlines. Keep
// unrelated parallel fixture work from consuming the observation window.
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class LoadHarnessWorldBootstrapCollection
{
    public const string Name = "Load harness world bootstrap deadlines";
}
