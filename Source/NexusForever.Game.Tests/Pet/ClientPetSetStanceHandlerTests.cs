using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Static.Pet;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Pet;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Pet;

namespace NexusForever.Game.Tests.Pet;

public class ClientPetSetStanceHandlerTests
{
    [Theory]
    [InlineData(PetStance.Aggressive, 1)]
    [InlineData(PetStance.Defensive, 2)]
    [InlineData(PetStance.Passive, 3)]
    [InlineData(PetStance.Assist, 4)]
    [InlineData(PetStance.Stay, 5)]
    public void PetStance_MatchesRetailLuaOrdinal(PetStance stance, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)stance);
    }

    [Theory]
    [InlineData(PetStance.Assist, 0x01)]
    [InlineData(PetStance.Stay, 0x02)]
    [InlineData(PetStance.Passive, 0x04)]
    [InlineData(PetStance.Defensive, 0x08)]
    [InlineData(PetStance.Aggressive, 0x10)]
    public void PetStanceEncoding_MapsRetailWireBitmask(PetStance stance, uint expectedMask)
    {
        Assert.Equal(expectedMask, PetStanceEncoding.ToWireMask(stance));
        Assert.Equal(stance, PetStanceEncoding.FromWireMask(expectedMask));
    }

    [Fact]
    public void ServerPetSpawned_WriteEncodesCurrentStanceAsRetailWireBitmask()
    {
        using GamePacketReader reader = WritePacket(new ServerPetSpawned
        {
            PetUnitId         = 333u,
            SummoningSpell4Id = 27002u,
            ValidStances      = 0b11101u,
            Stance            = PetStance.Assist
        });

        Assert.Equal(333u, reader.ReadUInt());
        Assert.Equal(27002u, reader.ReadUInt(18u));
        Assert.Equal(0b11101u, reader.ReadUInt(5u));
        Assert.Equal(0x01u, reader.ReadUInt(5u));
    }

    [Fact]
    public void ServerPetStanceChanged_WriteEncodesStanceAsRetailWireBitmask()
    {
        using GamePacketReader reader = WritePacket(new ServerPetStanceChanged
        {
            PetUnitId = 333u,
            Stance    = PetStance.Aggressive
        });

        Assert.Equal(333u, reader.ReadUInt());
        Assert.Equal(0x10u, reader.ReadUInt(5u));
    }

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
    public void HandleMessage_WithZeroPetIdAndActiveEngineerCombatBot_AcknowledgesCommandSurfaceAndConcreteEngineerBots()
    {
        IWorldSession session = CreateSession(1001u, 222u, out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IPetEntity vanityPet = CreatePet(222u, 1001u);
        AttachMap(session.Player, vanityPet);
        IWorldEntity firstBot = CreateSummon(333u, 1001u, 42683u);
        IWorldEntity secondBot = CreateSummon(444u, 1001u, 42683u);
        AttachSummonFactory(session.Player, 42683u, firstBot, secondBot);
        var handler = new ClientPetSetStanceHandler(NullLogger<ClientPetSetStanceHandler>.Instance);

        handler.HandleMessage(session, ReadSetStance(0u, PetStance.Defensive));

        Assert.Equal(PetStance.Assist, vanityPet.Stance);
        Assert.Equal(PetStance.Defensive, firstBot.SummonCommandStance);
        Assert.Equal(PetStance.Defensive, secondBot.SummonCommandStance);
        ServerPetStanceChanged[] responses = sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => Assert.IsType<ServerPetStanceChanged>(invocation.Arguments[0]))
            .ToArray();
        Assert.Equal([0u, 333u, 444u], responses.Select(response => response.PetUnitId));
        Assert.All(responses, response => Assert.Equal(PetStance.Defensive, response.Stance));
    }

    [Fact]
    public void HandleMessage_WithOwnedSummonedCombatBot_AcknowledgesServerStance()
    {
        IWorldSession session = CreateSession(1001u, 222u, out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IWorldEntity summoned = RecordingDispatchProxy<IWorldEntity>.Create(out RecordingDispatchProxy<IWorldEntity> summonedProxy);
        summonedProxy.SetProperty(nameof(IWorldEntity.Guid), 333u);
        summonedProxy.SetProperty(nameof(IWorldEntity.SummonerGuid), 1001u);
        AttachMap(session.Player, summoned);
        var handler = new ClientPetSetStanceHandler(NullLogger<ClientPetSetStanceHandler>.Instance);

        handler.HandleMessage(session, ReadSetStance(333u, PetStance.Defensive));

        Assert.Equal(PetStance.Defensive, summoned.SummonCommandStance);
        RecordingDispatchProxy<IWorldSession>.Invocation invocation =
            Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        ServerPetStanceChanged response = Assert.IsType<ServerPetStanceChanged>(invocation.Arguments[0]);
        Assert.Equal(333u, response.PetUnitId);
        Assert.Equal(PetStance.Defensive, response.Stance);
    }

    [Theory]
    [InlineData(PetStance.Passive, true)]
    [InlineData(PetStance.Stay, false)]
    public void HandleMessage_WithStopStyleSummonedCombatBotStance_ClearsCombatAndSetsFollowRequest(PetStance stance, bool expectedFollowRequest)
    {
        IWorldSession session = CreateSession(1001u, 222u, out _);
        IUnitEntity summoned = CreateUnitSummon(333u, 1001u, out RecordingDispatchProxy<IUnitEntity> summonedProxy, out RecordingDispatchProxy<IMovementManager> movementProxy);
        AttachMap(session.Player, summoned);
        var handler = new ClientPetSetStanceHandler(NullLogger<ClientPetSetStanceHandler>.Instance);

        handler.HandleMessage(session, ReadSetStance(333u, stance));

        Assert.Equal(stance, summoned.SummonCommandStance);
        Assert.Equal(expectedFollowRequest, summoned.SummonCommandFollowRequested);
        Assert.Single(summonedProxy.GetInvocations(nameof(IUnitEntity.SetTarget)));
        Assert.Single(movementProxy.GetInvocations(nameof(IMovementManager.Finalise)));
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
            handler.HandleMessage(session, ReadSetStanceMask(222u, 31u)));
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

    private static void AttachMap(IPlayer player, IWorldEntity summoned)
    {
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetMethodHandler(nameof(IBaseMap.GetEntity), (method, _) =>
        {
            Type entityType = method.GetGenericArguments()[0];
            return entityType == typeof(IWorldEntity) ? summoned : null;
        });

        RecordingDispatchProxy<IPlayer> playerProxy = (RecordingDispatchProxy<IPlayer>)(object)player;
        playerProxy.SetProperty(nameof(IPlayer.Map), map);
    }

    private static void AttachSummonFactory(IPlayer player, uint activeCreatureId, params IWorldEntity[] activeSummons)
    {
        IEntitySummonFactory summonFactory = RecordingDispatchProxy<IEntitySummonFactory>.Create(
            out RecordingDispatchProxy<IEntitySummonFactory> summonFactoryProxy);
        summonFactoryProxy.SetMethodHandler(nameof(IEntitySummonFactory.GetSummonCreatureCount), args =>
            (uint)args[0] == activeCreatureId ? (uint)activeSummons.Length : 0u);
        summonFactoryProxy.SetMethodHandler(nameof(IEntitySummonFactory.GetSummonCreatures), args =>
            (uint)args[0] == activeCreatureId ? activeSummons : []);

        RecordingDispatchProxy<IPlayer> playerProxy = (RecordingDispatchProxy<IPlayer>)(object)player;
        playerProxy.SetProperty(nameof(IPlayer.SummonFactory), summonFactory);
    }

    private static IWorldEntity CreateSummon(uint guid, uint summonerGuid, uint creatureId)
    {
        IWorldEntity summon = RecordingDispatchProxy<IWorldEntity>.Create(out RecordingDispatchProxy<IWorldEntity> summonProxy);
        summonProxy.SetProperty(nameof(IWorldEntity.Guid), guid);
        summonProxy.SetProperty(nameof(IWorldEntity.SummonerGuid), summonerGuid);
        summonProxy.SetProperty(nameof(IWorldEntity.CreatureId), creatureId);
        summonProxy.SetProperty(nameof(IWorldEntity.SummonCommandStance), PetStance.Assist);
        summonProxy.SetProperty(nameof(IWorldEntity.SummonCommandFollowRequested), false);
        return summon;
    }

    private static IUnitEntity CreateUnitSummon(
        uint guid,
        uint summonerGuid,
        out RecordingDispatchProxy<IUnitEntity> summonProxy,
        out RecordingDispatchProxy<IMovementManager> movementProxy)
    {
        IUnitEntity summon = RecordingDispatchProxy<IUnitEntity>.Create(out summonProxy);
        IMovementManager movementManager = RecordingDispatchProxy<IMovementManager>.Create(out movementProxy);
        summonProxy.SetProperty(nameof(IUnitEntity.Guid), guid);
        summonProxy.SetProperty(nameof(IUnitEntity.SummonerGuid), summonerGuid);
        summonProxy.SetProperty(nameof(IUnitEntity.CreatureId), 42683u);
        summonProxy.SetProperty(nameof(IUnitEntity.SummonCommandStance), PetStance.Assist);
        summonProxy.SetProperty(nameof(IUnitEntity.SummonCommandFollowRequested), false);
        summonProxy.SetProperty(nameof(IUnitEntity.MovementManager), movementManager);
        return summon;
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
        writer.Write(PetStanceEncoding.ToWireMask(stance), 5u);
        writer.FlushBits();

        byte[] payload = stream.ToArray();
        using var reader = new GamePacketReader(new MemoryStream(payload));
        var message = new ClientPetSetStance();
        message.Read(reader);
        return message;
    }

    private static ClientPetSetStance ReadSetStanceMask(uint petUnitId, uint stanceMask)
    {
        using var stream = new MemoryStream();
        var writer = new GamePacketWriter(stream);
        writer.Write(petUnitId);
        writer.Write(stanceMask, 5u);
        writer.FlushBits();

        byte[] payload = stream.ToArray();
        using var reader = new GamePacketReader(new MemoryStream(payload));
        var message = new ClientPetSetStance();
        message.Read(reader);
        return message;
    }

    private static GamePacketReader WritePacket(IWritable packet)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            packet.Write(writer);
            writer.FlushBits();
        }

        return new GamePacketReader(new MemoryStream(stream.ToArray()));
    }
}
