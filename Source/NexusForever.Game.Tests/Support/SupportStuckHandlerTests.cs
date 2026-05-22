using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Map.Lock;
using NexusForever.Game.Housing;
using NexusForever.Game.Static.Housing;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Static.Support;
using NexusForever.Game.Support;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Support;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Support;

namespace NexusForever.Game.Tests.Support;

public class SupportStuckHandlerTests
{
    public SupportStuckHandlerTests()
    {
        RetailStuckCooldownTracker.ClearForTests();
    }

    [Fact]
    public void FreeSuicide_WhenPlayerAliveDealsCurrentHealthDamage()
    {
        var handler = CreateHandler();
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy, out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IUnitEntity.IsAlive), true);
        playerProxy.SetProperty(nameof(IWorldEntity.Health), 123u);

        handler.HandleMessage(session, CreateStuck(UnstickType.FreeSuicide, 9001u));

        RecordingDispatchProxy<IPlayer>.Invocation invocation =
            Assert.Single(playerProxy.GetInvocations(nameof(IUnitEntity.ModifyHealth)));
        Assert.Equal(123u, invocation.Arguments[0]);
        Assert.Equal(DamageType.Physical, invocation.Arguments[1]);
        Assert.Same(session.Player, invocation.Arguments[2]);
        Assert.Empty(GetEncryptedMessages(sessionProxy));
    }

    [Fact]
    public void FreeSuicide_WhenPlayerDeadSendsCasterCannotBeDeadResult()
    {
        var handler = CreateHandler();
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy, out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IUnitEntity.IsAlive), false);

        handler.HandleMessage(session, CreateStuck(UnstickType.FreeSuicide, 9002u));

        Assert.Empty(playerProxy.GetInvocations(nameof(IUnitEntity.ModifyHealth)));
        AssertStuckCastResult(sessionProxy, 9002u, SupportStuckSpell4Ids.FreeSuicide, CastResult.CasterCannotBeDead);
    }

    [Fact]
    public void FreeSuicide_WithZeroHealthStillDealsAtLeastOneDamage()
    {
        var handler = CreateHandler();
        IWorldSession session = CreateSession(out _, out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IUnitEntity.IsAlive), true);
        playerProxy.SetProperty(nameof(IWorldEntity.Health), 0u);

        handler.HandleMessage(session, CreateStuck(UnstickType.FreeSuicide, 9003u));

        RecordingDispatchProxy<IPlayer>.Invocation invocation =
            Assert.Single(playerProxy.GetInvocations(nameof(IUnitEntity.ModifyHealth)));
        Assert.Equal(1u, invocation.Arguments[0]);
    }

    [Fact]
    public void RecallTransmat_WhenTeleportBlockedSendsPendingSpellCastResult()
    {
        var handler = CreateHandler();
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy, out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetMethodReturn(nameof(IPlayer.CanTeleport), false);

        handler.HandleMessage(session, CreateStuck(UnstickType.RecallTransmat, 9004u));

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
        AssertStuckCastResult(sessionProxy, 9004u, SupportStuckSpell4Ids.RecallTransmat, CastResult.PendingSpellCast);
    }

    [Fact]
    public void RecallTransmat_WhenZoneExitMissingSendsSpellPreRequisitesResult()
    {
        var handler = CreateHandler(out RecordingDispatchProxy<IGameTableManager> tableProxy);
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy, out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetMethodReturn(nameof(IPlayer.CanTeleport), true);
        playerProxy.SetProperty(nameof(IWorldEntity.Zone), new WorldZoneEntry { WorldLocation2IdExit = 77u });
        tableProxy.SetProperty(nameof(IGameTableManager.WorldLocation2), CreateWorldLocationTable(null));

        handler.HandleMessage(session, CreateStuck(UnstickType.RecallTransmat, 9006u));

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
        AssertStuckCastResult(sessionProxy, 9006u, SupportStuckSpell4Ids.RecallTransmat, CastResult.SpellPreRequisites);
    }

    [Fact]
    public void RecallTransmat_WhenZoneExitResolvedTeleportsToWorldLocation()
    {
        var handler = CreateHandler(out RecordingDispatchProxy<IGameTableManager> tableProxy);
        IWorldSession session = CreateSession(out _, out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetMethodReturn(nameof(IPlayer.CanTeleport), true);
        playerProxy.SetProperty(nameof(IWorldEntity.Zone), new WorldZoneEntry { WorldLocation2IdExit = 77u });

        var location = new WorldLocation2Entry
        {
            Id        = 77u,
            WorldId   = 42u,
            Position0 = 1f,
            Position1 = 2f,
            Position2 = 3f
        };
        tableProxy.SetProperty(nameof(IGameTableManager.WorldLocation2), CreateWorldLocationTable(location));

        handler.HandleMessage(session, CreateStuck(UnstickType.RecallTransmat, 9007u));

        RecordingDispatchProxy<IPlayer>.Invocation invocation =
            Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
        Assert.Equal((ushort)42u, invocation.Arguments[0]);
        Assert.Equal(1f, invocation.Arguments[1]);
        Assert.Equal(2f, invocation.Arguments[2]);
        Assert.Equal(3f, invocation.Arguments[3]);
    }

    [Fact]
    public void RecallHouse_WhenTeleportBlockedSendsPendingSpellCastResult()
    {
        var handler = CreateHandler();
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy, out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetMethodReturn(nameof(IPlayer.CanTeleport), false);

        handler.HandleMessage(session, CreateStuck(UnstickType.RecallHouse, 9008u));

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
        AssertStuckCastResult(sessionProxy, 9008u, SupportStuckSpell4Ids.RecallHouse, CastResult.PendingSpellCast);
    }

    [Fact]
    public void RecallHouse_ResolvesResidenceThroughGlobalManagerAndTeleportsToEntrance()
    {
        var handler = CreateHandler(
            out _,
            out RecordingDispatchProxy<IGlobalResidenceManager> residenceProxy,
            out RecordingDispatchProxy<IMapLockManager> mapLockProxy);
        IWorldSession session = CreateSession(out _, out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Name), "Tester");
        playerProxy.SetMethodReturn(nameof(IPlayer.CanTeleport), true);

        IResidence residence = RecordingDispatchProxy<IResidence>.Create(out RecordingDispatchProxy<IResidence> residenceInstanceProxy);
        residenceInstanceProxy.SetProperty(nameof(IResidence.PropertyInfoId), PropertyInfoId.Residence);
        residenceInstanceProxy.SetProperty(nameof(IResidence.Parent), null);

        IResidenceEntrance entrance = RecordingDispatchProxy<IResidenceEntrance>.Create(out RecordingDispatchProxy<IResidenceEntrance> entranceProxy);
        entranceProxy.SetProperty(nameof(IResidenceEntrance.Entry), new WorldEntry { Id = 9u });
        entranceProxy.SetProperty(nameof(IResidenceEntrance.Position), new Vector3(4f, 5f, 6f));
        entranceProxy.SetProperty(nameof(IResidenceEntrance.Rotation), Quaternion.Identity);

        IResidenceMapLock mapLock = RecordingDispatchProxy<IResidenceMapLock>.Create(out _);
        residenceProxy.SetMethodReturn(nameof(IGlobalResidenceManager.GetResidenceByOwner), residence);
        residenceProxy.SetMethodReturn(nameof(IGlobalResidenceManager.GetResidenceEntrance), entrance);
        mapLockProxy.SetMethodReturn(nameof(IMapLockManager.GetResidenceLock), mapLock);

        handler.HandleMessage(session, CreateStuck(UnstickType.RecallHouse, 9009u));

        Assert.Single(residenceProxy.GetInvocations(nameof(IGlobalResidenceManager.GetResidenceByOwner)));
        Assert.Empty(residenceProxy.GetInvocations(nameof(IGlobalResidenceManager.CreateResidence)));
        Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
    }

    [Fact]
    public void RecallHouse_WhenEntranceMissingSendsSpellPreRequisitesResult()
    {
        var handler = CreateHandler(
            out _,
            out RecordingDispatchProxy<IGlobalResidenceManager> residenceProxy,
            out _);
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy, out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Name), "Tester");
        playerProxy.SetMethodReturn(nameof(IPlayer.CanTeleport), true);

        IResidence residence = RecordingDispatchProxy<IResidence>.Create(out RecordingDispatchProxy<IResidence> residenceInstanceProxy);
        residenceInstanceProxy.SetProperty(nameof(IResidence.PropertyInfoId), PropertyInfoId.Residence);
        residenceProxy.SetMethodReturn(nameof(IGlobalResidenceManager.GetResidenceByOwner), residence);
        residenceProxy.SetMethodReturnFactory(nameof(IGlobalResidenceManager.GetResidenceEntrance),
            () => throw new HousingException("missing entrance"));

        handler.HandleMessage(session, CreateStuck(UnstickType.RecallHouse, 9010u));

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
        AssertStuckCastResult(sessionProxy, 9010u, SupportStuckSpell4Ids.RecallHouse, CastResult.SpellPreRequisites);
    }

    [Fact]
    public void InvalidUnstickTypeDoesNotMutatePlayerAndSendsSpellUnknownResult()
    {
        var handler = CreateHandler();
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy, out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IUnitEntity.IsAlive), true);
        playerProxy.SetMethodReturn(nameof(IPlayer.CanTeleport), true);

        handler.HandleMessage(session, CreateStuck((UnstickType)7u, 9005u));

        Assert.Empty(playerProxy.GetInvocations(nameof(IUnitEntity.ModifyHealth)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
        AssertStuckCastResult(sessionProxy, 9005u, 0u, CastResult.SpellUnknown);
    }

    private static ClientStuckHandler CreateHandler()
    {
        return CreateHandler(out _, out _, out _);
    }

    private static ClientStuckHandler CreateHandler(out RecordingDispatchProxy<IGameTableManager> tableProxy)
    {
        return CreateHandler(out tableProxy, out _, out _);
    }

    private static ClientStuckHandler CreateHandler(
        out RecordingDispatchProxy<IGameTableManager> tableProxy,
        out RecordingDispatchProxy<IGlobalResidenceManager> residenceProxy,
        out RecordingDispatchProxy<IMapLockManager> mapLockProxy)
    {
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out tableProxy);
        IGlobalResidenceManager globalResidenceManager = RecordingDispatchProxy<IGlobalResidenceManager>.Create(out residenceProxy);
        IMapLockManager mapLockManager = RecordingDispatchProxy<IMapLockManager>.Create(out mapLockProxy);

        return new ClientStuckHandler(
            NullLogger<ClientStuckHandler>.Instance,
            gameTableManager,
            globalResidenceManager,
            mapLockManager);
    }

    private static IWorldSession CreateSession(
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        return session;
    }

    private static ClientStuck CreateStuck(UnstickType type, uint contextToken)
    {
        var stuck = (ClientStuck)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ClientStuck));
        typeof(ClientStuck).GetProperty(nameof(ClientStuck.UnstickingType))!.SetValue(stuck, type);
        typeof(ClientStuck).GetProperty(nameof(ClientStuck.ContextToken))!.SetValue(stuck, contextToken);
        return stuck;
    }

    private static void AssertStuckCastResult(
        RecordingDispatchProxy<IWorldSession> sessionProxy,
        uint contextToken,
        uint spell4Id,
        CastResult castResult)
    {
        ServerSpellCastResult result = GetEncryptedMessages(sessionProxy)
            .OfType<ServerSpellCastResult>()
            .Single();

        Assert.Equal(contextToken, result.Unknown0);
        Assert.Equal(spell4Id, result.Spell4Id);
        Assert.Equal(castResult, result.CastResult);
    }

    private static IEnumerable<object> GetEncryptedMessages(RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        return sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0]);
    }

    private static GameTable<WorldLocation2Entry> CreateWorldLocationTable(WorldLocation2Entry entry)
    {
        return entry == null
            ? CreateGameTable<WorldLocation2Entry>()
            : CreateGameTable(entry);
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
}
