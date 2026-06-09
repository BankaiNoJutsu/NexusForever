using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Static.Pet;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Pet;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Pet;

namespace NexusForever.Game.Tests.Pet;

public class ClientPetSetStanceHandlerTests
{
    [Fact]
    public void HandleMessage_WithOwnedPet_UpdatesServerStanceWithoutBlockedResponse()
    {
        IWorldSession session = CreateSession(1001u, 222u, out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IPetEntity pet = CreatePet(222u, 1001u);
        AttachMap(session.Player, pet);
        var handler = new ClientPetSetStanceHandler(NullLogger<ClientPetSetStanceHandler>.Instance);

        handler.HandleMessage(session, ReadSetStance(222u, PetStance.Aggressive));

        Assert.Equal(PetStance.Aggressive, pet.Stance);
        Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
    }

    [Fact]
    public void HandleMessage_WithZeroPetId_UsesActiveVanityPet()
    {
        IWorldSession session = CreateSession(1001u, 222u, out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IPetEntity pet = CreatePet(222u, 1001u);
        AttachMap(session.Player, pet);
        var handler = new ClientPetSetStanceHandler(NullLogger<ClientPetSetStanceHandler>.Instance);

        handler.HandleMessage(session, ReadSetStance(0u, PetStance.Stay));

        Assert.Equal(PetStance.Stay, pet.Stance);
        Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
    }

    [Fact]
    public void HandleMessage_WithForeignPet_DoesNotMutateStance()
    {
        IWorldSession session = CreateSession(1001u, 222u, out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IPetEntity pet = CreatePet(222u, 2002u);
        AttachMap(session.Player, pet);
        var handler = new ClientPetSetStanceHandler(NullLogger<ClientPetSetStanceHandler>.Instance);

        handler.HandleMessage(session, ReadSetStance(222u, PetStance.Aggressive));

        Assert.Equal(PetStance.Assist, pet.Stance);
        Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
    }

    [Fact]
    public void HandleMessage_WithInvalidStance_ThrowsInvalidPacketValue()
    {
        IWorldSession session = CreateSession(1001u, 222u, out _);
        AttachMap(session.Player, CreatePet(222u, 1001u));
        var handler = new ClientPetSetStanceHandler(NullLogger<ClientPetSetStanceHandler>.Instance);

        Assert.Throws<InvalidPacketValueException>(() =>
            handler.HandleMessage(session, ReadSetStance(222u, (PetStance)31)));
    }

    private static IWorldSession CreateSession(
        uint playerGuid,
        uint? vanityPetGuid,
        out RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);

        playerProxy.SetProperty(nameof(IPlayer.Guid), playerGuid);
        playerProxy.SetProperty(nameof(IPlayer.VanityPetGuid), vanityPetGuid);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        return session;
    }

    private static void AttachMap(IPlayer player, IPetEntity pet)
    {
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), _ => pet);

        RecordingDispatchProxy<IPlayer> playerProxy = (RecordingDispatchProxy<IPlayer>)(object)player;
        playerProxy.SetProperty(nameof(IPlayer.Map), map);
    }

    private static IPetEntity CreatePet(uint guid, uint ownerGuid)
    {
        IPetEntity pet = RecordingDispatchProxy<IPetEntity>.Create(out RecordingDispatchProxy<IPetEntity> petProxy);
        petProxy.SetProperty(nameof(IPetEntity.Guid), guid);
        petProxy.SetProperty(nameof(IPetEntity.OwnerGuid), ownerGuid);
        petProxy.SetProperty(nameof(IPetEntity.Stance), PetStance.Assist);
        return pet;
    }

    private static ClientPetSetStance ReadSetStance(uint petUnitId, PetStance stance)
    {
        using var stream = new MemoryStream();
        var writer = new GamePacketWriter(stream);
        writer.Write(petUnitId);
        writer.Write(stance, 5u);
        writer.FlushBits();

        byte[] payload = stream.ToArray();
        using var reader = new GamePacketReader(new MemoryStream(payload));
        var message = new ClientPetSetStance();
        message.Read(reader);
        return message;
    }
}
