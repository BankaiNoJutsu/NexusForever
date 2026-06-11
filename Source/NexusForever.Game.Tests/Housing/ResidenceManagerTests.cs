using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Housing;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Housing;

public class ResidenceManagerTests
{
    [Fact]
    public void SendHousingBasics_WithoutResidence_OnlySendsHousingBasics()
    {
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
}
