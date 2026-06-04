using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Packet;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Entity.Player;

namespace NexusForever.Game.Tests.Transport;

public class TransportHandlerTests
{
    private const uint RapidTransportSpellGameFormulaId = 0x051Bu;
    private const uint RapidTransportSpell4Id         = 82922u;

    [Fact]
    public void RapidTransport_RejectsUnknownDestinationNode()
    {
        ClientRapidTransportHandler handler = CreateRapidTransportHandler(
            out RecordingDispatchProxy<IGameTableManager> tableProxy,
            out IPlayer player,
            out _,
            out _,
            out _,
            out _);
        tableProxy.SetProperty(nameof(IGameTableManager.TaxiNode), CreateGameTable<TaxiNodeEntry>());
        tableProxy.SetProperty(nameof(IGameTableManager.GameFormula), CreateGameTable(new GameFormulaEntry
        {
            Id        = RapidTransportSpellGameFormulaId,
            Dataint0  = RapidTransportSpell4Id
        }));

        TransportTestSession session = CreateSession(player, out RecordingDispatchProxy<IPlayer> playerProxy);

        handler.HandleMessage(session, BuildRapidTransport(destinationNodeId: 999));

        Assert.Empty(playerProxy.GetInvocations(nameof(IUnitEntity.CastSpell)));
        ServerSpellCastResult result = GetEncryptedMessages(session)
            .OfType<ServerSpellCastResult>()
            .Single();
        Assert.Equal(0u, result.Spell4Id);
        Assert.Equal(CastResult.RapidTransportInvalid, result.CastResult);
    }

    [Fact]
    public void RapidTransport_RejectsInsufficientCredits()
    {
        ClientRapidTransportHandler handler = CreateRapidTransportHandler(
            out _,
            out IPlayer player,
            out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
            out _,
            out _,
            out _);
        currencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), false);

        TransportTestSession session = CreateSession(player, out RecordingDispatchProxy<IPlayer> playerProxy);
        handler.HandleMessage(session, BuildRapidTransport(destinationNodeId: 20));

        Assert.Empty(playerProxy.GetInvocations(nameof(IUnitEntity.CastSpell)));
        ServerSpellCastResult result = GetEncryptedMessages(session)
            .OfType<ServerSpellCastResult>()
            .Single();
        Assert.Equal(RapidTransportSpell4Id, result.Spell4Id);
        Assert.Equal(CastResult.CasterVitalCostMoney, result.CastResult);
    }

    [Fact]
    public void RapidTransport_RejectsWhenTaxiRouteTableUnavailable()
    {
        ClientRapidTransportHandler handler = CreateRapidTransportHandler(
            out RecordingDispatchProxy<IGameTableManager> tableProxy,
            out IPlayer player,
            out _,
            out _,
            out _,
            out _);
        tableProxy.SetProperty(nameof(IGameTableManager.TaxiRoute), null);

        TransportTestSession session = CreateSession(player, out RecordingDispatchProxy<IPlayer> playerProxy);
        handler.HandleMessage(session, BuildRapidTransport(destinationNodeId: 20));

        Assert.Empty(playerProxy.GetInvocations(nameof(IUnitEntity.CastSpell)));
        ServerSpellCastResult result = GetEncryptedMessages(session)
            .OfType<ServerSpellCastResult>()
            .Single();
        Assert.Equal(RapidTransportSpell4Id, result.Spell4Id);
        Assert.Equal(CastResult.RapidTransportInvalid, result.CastResult);
    }

    [Fact]
    public void RapidTransport_ChargesNearestRouteAndCastsRapidTransportSpell()
    {
        ClientRapidTransportHandler handler = CreateRapidTransportHandler(
            out _,
            out IPlayer player,
            out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
            out _,
            out _,
            out _);

        TransportTestSession session = CreateSession(player, out RecordingDispatchProxy<IPlayer> playerProxy);
        handler.HandleMessage(session, BuildRapidTransport(destinationNodeId: 20, contextToken: 0xAABBCCDDu));

        RecordingDispatchProxy<ICurrencyManager>.Invocation debit =
            Assert.Single(currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
        Assert.Equal(CurrencyType.Credits, debit.Arguments[0]);
        Assert.Equal(25ul, debit.Arguments[1]);

        RecordingDispatchProxy<IPlayer>.Invocation cast =
            Assert.Single(playerProxy.GetInvocations(nameof(IUnitEntity.CastSpell)));
        Assert.Equal(RapidTransportSpell4Id, cast.Arguments[0]);
        ISpellParameters parameters = Assert.IsAssignableFrom<ISpellParameters>(cast.Arguments[1]);
        Assert.Equal((ushort)20, parameters.TaxiNode);
        Assert.Equal(0xAABBCCDDu, parameters.ClientContextToken);
        Assert.Equal(nameof(ClientRapidTransport), parameters.ClientRequestSource);
    }

    [Fact]
    public void FlightPathPurchase_RejectsNonContiguousRouteChain()
    {
        ClientFlightPathPurchaseHandler handler = CreateFlightPathHandler(
            out RecordingDispatchProxy<IGameTableManager> tableProxy,
            out IPlayer player,
            out _,
            out _);
        tableProxy.SetProperty(nameof(IGameTableManager.TaxiRoute), CreateGameTable(
            new TaxiRouteEntry { Id = 1u, TaxiNodeIdSource = 10u, TaxiNodeIdDestination = 20u, Price = 5u },
            new TaxiRouteEntry { Id = 2u, TaxiNodeIdSource = 99u, TaxiNodeIdDestination = 30u, Price = 7u }));

        TransportTestSession session = CreateSession(player, out RecordingDispatchProxy<IPlayer> playerProxy);
        handler.HandleMessage(session, BuildFlightPathPurchase(1u, 2u));

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
        RecordingDispatchProxy<IPlayer>.Invocation error =
            Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.SendGenericError)));
        Assert.Equal(GenericError.EmbarkNoSplineForTaxi, error.Arguments[0]);
    }

    [Fact]
    public void FlightPathPurchase_RejectsWhenTaxiRouteTableUnavailable()
    {
        ClientFlightPathPurchaseHandler handler = CreateFlightPathHandler(
            out RecordingDispatchProxy<IGameTableManager> tableProxy,
            out IPlayer player,
            out _,
            out _);
        tableProxy.SetProperty(nameof(IGameTableManager.TaxiRoute), null);

        TransportTestSession session = CreateSession(player, out RecordingDispatchProxy<IPlayer> playerProxy);
        handler.HandleMessage(session, BuildFlightPathPurchase(1u));

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
        RecordingDispatchProxy<IPlayer>.Invocation error =
            Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.SendGenericError)));
        Assert.Equal(GenericError.EmbarkNoSplineForTaxi, error.Arguments[0]);
    }

    [Fact]
    public void FlightPathPurchase_RejectsInsufficientCredits()
    {
        ClientFlightPathPurchaseHandler handler = CreateFlightPathHandler(
            out _,
            out IPlayer player,
            out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
            out _);
        currencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), false);

        TransportTestSession session = CreateSession(player, out RecordingDispatchProxy<IPlayer> playerProxy);
        handler.HandleMessage(session, BuildFlightPathPurchase(1u));

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
        RecordingDispatchProxy<IPlayer>.Invocation error =
            Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.SendGenericError)));
        Assert.Equal(GenericError.VendorNotEnoughCash, error.Arguments[0]);
    }

    [Fact]
    public void FlightPathPurchase_ChargesContiguousRoutesAndTeleportsToDestination()
    {
        ClientFlightPathPurchaseHandler handler = CreateFlightPathHandler(
            out _,
            out IPlayer player,
            out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
            out _);

        TransportTestSession session = CreateSession(player, out RecordingDispatchProxy<IPlayer> playerProxy);
        handler.HandleMessage(session, BuildFlightPathPurchase(1u, 2u));

        RecordingDispatchProxy<ICurrencyManager>.Invocation debit =
            Assert.Single(currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
        Assert.Equal(CurrencyType.Credits, debit.Arguments[0]);
        Assert.Equal(12ul, debit.Arguments[1]);

        RecordingDispatchProxy<IPlayer>.Invocation teleport =
            Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
        Assert.Equal((ushort)7u, teleport.Arguments[0]);
        Assert.Equal(40f, teleport.Arguments[1]);
        Assert.Equal(50f, teleport.Arguments[2]);
        Assert.Equal(60f, teleport.Arguments[3]);
    }

    private static ClientRapidTransportHandler CreateRapidTransportHandler(
        out RecordingDispatchProxy<IGameTableManager> tableProxy,
        out IPlayer player,
        out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
        out RecordingDispatchProxy<ISpellManager> spellProxy,
        out RecordingDispatchProxy<IBaseMap> mapProxy,
        out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.GameFormula), CreateGameTable(new GameFormulaEntry
        {
            Id       = RapidTransportSpellGameFormulaId,
            Dataint0 = RapidTransportSpell4Id
        }));
        tableProxy.SetProperty(nameof(IGameTableManager.TaxiNode), CreateGameTable(
            new TaxiNodeEntry { Id = 10u, AutoUnlockLevel = 1u, WorldLocation2Id = 1001u },
            new TaxiNodeEntry { Id = 20u, AutoUnlockLevel = 1u, WorldLocation2Id = 1002u }));
        tableProxy.SetProperty(nameof(IGameTableManager.TaxiRoute), CreateGameTable(
            new TaxiRouteEntry { Id = 1u, TaxiNodeIdSource = 10u, TaxiNodeIdDestination = 20u, Price = 25u },
            new TaxiRouteEntry { Id = 2u, TaxiNodeIdSource = 99u, TaxiNodeIdDestination = 20u, Price = 99u }));
        tableProxy.SetProperty(nameof(IGameTableManager.WorldLocation2), CreateGameTable(
            new WorldLocation2Entry
            {
                Id        = 1001u,
                WorldId   = 5u,
                Position0 = 0f,
                Position1 = 0f,
                Position2 = 0f
            },
            new WorldLocation2Entry
            {
                Id        = 1002u,
                WorldId   = 5u,
                Position0 = 10f,
                Position1 = 0f,
                Position2 = 0f
            }));

        ICurrencyManager currencyManager = RecordingDispatchProxy<ICurrencyManager>.Create(out currencyProxy);
        currencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);

        ISpellManager spellManager = RecordingDispatchProxy<ISpellManager>.Create(out spellProxy);
        spellProxy.SetMethodReturn(nameof(ISpellManager.GetSpellCooldown), 0d);

        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 5u });

        player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Level), 50u);
        playerProxy.SetProperty(nameof(IPlayer.Position), Vector3.Zero);
        playerProxy.SetProperty(nameof(IPlayer.Map), map);
        playerProxy.SetProperty(nameof(IPlayer.CurrencyManager), currencyManager);
        playerProxy.SetProperty(nameof(IPlayer.SpellManager), spellManager);

        return new ClientRapidTransportHandler(NullLogger<ClientRapidTransportHandler>.Instance, gameTableManager);
    }

    private static ClientFlightPathPurchaseHandler CreateFlightPathHandler(
        out RecordingDispatchProxy<IGameTableManager> tableProxy,
        out IPlayer player,
        out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
        out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.TaxiRoute), CreateGameTable(
            new TaxiRouteEntry { Id = 1u, TaxiNodeIdSource = 10u, TaxiNodeIdDestination = 20u, Price = 5u },
            new TaxiRouteEntry { Id = 2u, TaxiNodeIdSource = 20u, TaxiNodeIdDestination = 30u, Price = 7u }));
        tableProxy.SetProperty(nameof(IGameTableManager.TaxiNode), CreateGameTable(
            new TaxiNodeEntry { Id = 30u, WorldLocation2Id = 3003u }));
        tableProxy.SetProperty(nameof(IGameTableManager.WorldLocation2), CreateGameTable(
            new WorldLocation2Entry
            {
                Id        = 3003u,
                WorldId   = 7u,
                Position0 = 40f,
                Position1 = 50f,
                Position2 = 60f
            }));

        ICurrencyManager currencyManager = RecordingDispatchProxy<ICurrencyManager>.Create(out currencyProxy);
        currencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);

        player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        playerProxy.SetMethodReturn(nameof(IPlayer.CanTeleport), true);
        playerProxy.SetProperty(nameof(IPlayer.CurrencyManager), currencyManager);

        return new ClientFlightPathPurchaseHandler(NullLogger<ClientFlightPathPurchaseHandler>.Instance, gameTableManager);
    }

    private static TransportTestSession CreateSession(
        IPlayer player,
        out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        playerProxy = (RecordingDispatchProxy<IPlayer>)(object)player;
        return new TransportTestSession { Player = player };
    }

    private static ClientRapidTransport BuildRapidTransport(ushort destinationNodeId, uint contextToken = 0u)
    {
        var message = (ClientRapidTransport)RuntimeHelpers.GetUninitializedObject(typeof(ClientRapidTransport));
        SetPrivateProperty(message, nameof(ClientRapidTransport.TaxiNode), destinationNodeId);
        SetPrivateProperty(message, nameof(ClientRapidTransport.ContextToken), contextToken);
        return message;
    }

    private static ClientFlightPathPurchase BuildFlightPathPurchase(params uint[] routeIds)
    {
        var message = new ClientFlightPathPurchase();
        message.RouteIds.AddRange(routeIds);
        return message;
    }

    private static IEnumerable<object> GetEncryptedMessages(TransportTestSession session)
    {
        return session.EncryptedMessages;
    }

    private sealed class TransportTestSession : IWorldSession
    {
        public IAccount Account => null;
        public IPlayer Player { get; set; }
        public bool HasSentCharacterListPackets { get; set; }
        public bool HasSentPregameAccountPackets { get; set; }
        public List<CharacterModel> Characters { get; } = [];
        public bool? IsQueued { get; set; }
        public bool CanProcessIncomingPackets { get; set; }
        public bool CanProcessOutgoingPackets { get; set; }
        public string Id { get; private set; } = "transport-handler-test";
        public EventQueue Events { get; } = new();
        public SocketHeartbeat Heartbeat { get; } = new();
        public List<IWritable> EncryptedMessages { get; } = [];

        public void EnqueueMessage(IWritable message)
        {
        }

        public void EnqueueMessageEncrypted(IWritable message)
        {
            EncryptedMessages.Add(message);
        }

        public void EnqueueMessageEncrypted(uint opcode, string hex)
        {
        }

        public void HandlePacket(ClientGamePacket packet)
        {
        }

        public void FlushPackets()
        {
        }

        public bool CanDispose() => false;

        public void ForceDisconnect()
        {
        }

        public void OnAccept(Socket newSocket)
        {
        }

        public void UpdateId(string id) => Id = id;

        public void Update(double lastTick)
        {
        }

        public void Initialise(AccountModel account)
        {
        }

        public void SetEncryptionKey(byte[] sessionKey)
        {
        }

        public void ArmNextClientSpellEvidenceCapture(bool emitDiagnosticSpellBroadcasts = false)
        {
        }

        public bool TryConsumeNextClientSpellEvidenceCapture(out bool emitDiagnosticSpellBroadcasts)
        {
            emitDiagnosticSpellBroadcasts = false;
            return false;
        }

        public void ArmNextLootEvidenceCapture()
        {
        }

        public bool TryConsumeNextLootEvidenceCapture() => false;

        public void ArmNextAccountRuntimeEvidenceCapture()
        {
        }

        public bool TryConsumeNextAccountRuntimeEvidenceCapture() => false;

        public void ArmNextRewardRotationEvidenceCapture()
        {
        }

        public bool TryConsumeNextRewardRotationEvidenceCapture() => false;
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        SetPrivateField(table, "header", new GameTableHeader
        {
            MaxId = entries.Length == 0 ? 0u : entries.Max(GetEntryId) + 1u
        });
        SetPrivateField(table, "lookup", BuildLookup(entries));
        return table;
    }

    private static int[] BuildLookup<T>(IReadOnlyList<T> entries)
    {
        if (entries.Count == 0)
            return [];

        int[] lookup = Enumerable.Repeat(-1, (int)(entries.Max(GetEntryId) + 1u)).ToArray();
        for (int i = 0; i < entries.Count; i++)
            lookup[GetEntryId(entries[i])] = i;

        return lookup;
    }

    private static uint GetEntryId<T>(T entry)
    {
        FieldInfo idField = typeof(T).GetField("Id")!;
        return (uint)idField.GetValue(entry)!;
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(instance, value);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(instance, value);
    }

    private static void SetPrivateProperty<T>(object instance, string propertyName, T value)
    {
        PropertyInfo property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
        property.SetValue(instance, value);
    }
}
