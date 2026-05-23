using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Configuration.Model;
using NexusForever.Game.Housing;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared;
using NexusForever.Shared.Configuration;

namespace NexusForever.Game.Tests.Housing;

[Collection(LegacyServiceProviderCollection.Name)]
public class ResidenceManagerTests
{
    [Fact]
    public void SendHousingBasics_WithoutResidence_OnlySendsHousingBasics()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        using ServiceProvider provider = BuildLegacyProvider();

        try
        {
            LegacyServiceProvider.Provider = provider;

            IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out var sessionProxy);
            IPlayer player = TestPlayerBuilder.Create()
                .WithCharacterId(0x0F0E0D0C0B0A0908ul)
                .WithSession(session)
                .Build();

            var manager = new ResidenceManager(player);
            manager.SendHousingBasics();

            IReadOnlyList<RecordingDispatchProxy<IGameSession>.Invocation> calls = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted));
            RecordingDispatchProxy<IGameSession>.Invocation call = Assert.Single(calls);

            var packet = Assert.IsType<ServerHousingBasics>(call.Arguments[0]);
            Assert.Equal(0ul, packet.ResidenceId);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    private static ServiceProvider BuildLegacyProvider()
    {
        var configuration = new SharedConfiguration(new ConfigurationBuilder()
            .AddInMemoryCollection([])
            .Build());
        configuration.Initialise<TestConfiguration>();

        return new ServiceCollection()
            .AddSingleton(configuration)
            .AddSingleton<GlobalResidenceManager>()
            .BuildServiceProvider();
    }

    private sealed class TestConfiguration
    {
        public WorldConfig World { get; set; }
    }
}