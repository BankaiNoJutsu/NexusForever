using NexusForever.Shared;

namespace NexusForever.Game.Tests.TestSupport;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class LegacyServiceProviderCollection
{
    public const string Name = "LegacyServiceProvider";
}

internal sealed class LegacyServiceProviderScope : IDisposable
{
    private readonly IServiceProvider previousProvider;

    public LegacyServiceProviderScope(IServiceProvider provider)
    {
        Provider = provider;
        previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = provider;
    }

    public IServiceProvider Provider { get; }

    public void Dispose()
    {
        LegacyServiceProvider.Provider = previousProvider;
    }
}
