using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model.Pet;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Pet;

namespace NexusForever.Game.Tests.Pet;

public class ClientPetMiniCommandHandlerTests
{
    [Fact]
    public void HandlerUsesCurrentPlayerBindingAndDoesNotAffectOtherBots()
    {
        var f = new PetMiniBarTestFixture(); var pet = f.Pet(); var other = f.Pet(101, 42684);
        f.Manager.TryBind(pet); f.Manager.TryBind(other);
        var session = RecordingDispatchProxy<IWorldSession>.Create(out var proxy);
        proxy.SetProperty(nameof(IWorldSession.Player), f.Owner);
        var handler = new ClientPetMiniCommandHandler();
        handler.HandleMessage(session, Message("0200000000000000"));
        handler.HandleMessage(session, Message("0200000000000000"));
        Assert.Equal(new uint[] { 100 }, f.Removals);
        Assert.Same(other, Assert.Single(f.Factory.GetSummonCreatures(42684)));
    }

    [Fact]
    public void AbsentOrLegacyPlayerCapabilityIsIgnored()
    {
        var session = RecordingDispatchProxy<IWorldSession>.Create(out var proxy);
        var handler = new ClientPetMiniCommandHandler();
        handler.HandleMessage(session, Message("0200000000000000"));
        var player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        proxy.SetProperty(nameof(IWorldSession.Player), player);
        handler.HandleMessage(session, Message("0200000000000000"));
        Assert.Empty(playerProxy.Invocations);
    }

    private static ClientPetMiniCommand Message(string raw)
    {
        using var reader = new GamePacketReader(new MemoryStream(Convert.FromHexString(raw)));
        var message = new ClientPetMiniCommand(); message.Read(reader); return message;
    }
}
