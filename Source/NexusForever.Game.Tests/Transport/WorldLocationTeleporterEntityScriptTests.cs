using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Script.Main.Transport;

namespace NexusForever.Game.Tests.Transport;

public class WorldLocationTeleporterEntityScriptTests
{
    public static IEnumerable<object[]> DirectCreatureDestinationData()
    {
        yield return [32269u, (ushort)51, new Vector3(881.554f, -909.42f, -3130.06f), Vector3.Zero];
        yield return [45366u, (ushort)870, new Vector3(-8267.476f, -995.66176f, -239.02145f), new Vector3(-1.8304919f, 0f, 0f)];
        yield return [70174u, (ushort)870, new Vector3(-8267.476f, -995.66176f, -239.02145f), new Vector3(-1.8304919f, 0f, 0f)];
        yield return [70172u, (ushort)990, new Vector3(-771.823f, -904.285f, -2269.56f), new Vector3(-1.1214001f, 0f, 0f)];
        yield return [30352u, (ushort)1387, new Vector3(-3835.34f, -980.217f, -6050.52f), new Vector3(-0.45682f, 0f, 0f)];
        yield return [70173u, (ushort)1387, new Vector3(-3835.34f, -980.217f, -6050.52f), new Vector3(-0.45682f, 0f, 0f)];
    }

    [Theory]
    [InlineData(70783u, 50181u)]
    [InlineData(70382u, 49902u)]
    [InlineData(67491u, 43968u)]
    [InlineData(67492u, 36743u)]
    [InlineData(46182u, 37776u)]
    [InlineData(51202u, 40705u)]
    [InlineData(62464u, 46311u)]
    [InlineData(67494u, 32543u)]
    [InlineData(47505u, 21660u)]
    [InlineData(70782u, 50182u)]
    [InlineData(70383u, 49901u)]
    [InlineData(67480u, 43971u)]
    [InlineData(70385u, 49900u)]
    [InlineData(67487u, 36742u)]
    [InlineData(46183u, 37777u)]
    [InlineData(51207u, 40707u)]
    [InlineData(47459u, 21659u)]
    [InlineData(70678u, 46631u)]
    [InlineData(70681u, 46631u)]
    [InlineData(27196u, 9801u)]
    [InlineData(59189u, 19279u)]
    [InlineData(46980u, 20541u)]
    [InlineData(59199u, 24083u)]
    [InlineData(70781u, 50187u)]
    [InlineData(46184u, 37775u)]
    [InlineData(51204u, 40704u)]
    [InlineData(62465u, 46314u)]
    [InlineData(45102u, 1594u)]
    [InlineData(70663u, 1594u)]
    [InlineData(46185u, 37772u)]
    public void CreatureWorldLocationIds_ContainsBranchWorldLocationTransporters(uint creatureId, uint worldLocationId)
    {
        Assert.True(WorldLocationTeleporterEntityScript.CreatureWorldLocationIds.TryGetValue(creatureId, out uint actualWorldLocationId));
        Assert.Equal(worldLocationId, actualWorldLocationId);
    }

    [Theory]
    [MemberData(nameof(DirectCreatureDestinationData))]
    public void DirectCreatureDestinations_ContainsBranchCoordinateTransporters(
        uint creatureId,
        ushort worldId,
        Vector3 position,
        Vector3 rotation)
    {
        Assert.True(WorldLocationTeleporterEntityScript.DirectCreatureDestinations.TryGetValue(creatureId, out WorldLocationTeleporterEntityScript.DirectTransportDestination destination));
        Assert.Equal(worldId, destination.WorldId);
        Assert.Equal(position, destination.Position);
        Assert.Equal(rotation, destination.Rotation);
    }

    [Theory]
    [InlineData(28454u, 21760u)]
    [InlineData(25886u, 19282u)]
    public void RangeCreatureWorldLocationIds_ContainsBranchRangeTransporters(uint creatureId, uint expectedWorldLocationId)
    {
        Assert.True(WorldLocationTeleporterEntityScript.RangeCreatureWorldLocationIds.TryGetValue(creatureId, out uint worldLocationId));
        Assert.Equal(expectedWorldLocationId, worldLocationId);
    }

    [Fact]
    public void OnActivateSuccess_WithKnownDestination_TeleportsAndAppliesFacing()
    {
        var destination = new WorldLocation2Entry
        {
            Id        = 49900u,
            WorldId   = 1550u,
            Position0 = 11f,
            Position1 = 22f,
            Position2 = 33f,
            Facing3   = 1f
        };

        WorldLocationTeleporterEntityScript script = CreateScript(70385u, CreateGameTable(destination));
        IPlayer player = CreatePlayer(true, out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetMethodHandler(nameof(IPlayer.TeleportTo), args =>
        {
            Assert.Equal((ushort)1550, (ushort)args[0]);
            Assert.Equal(11f, (float)args[1]);
            Assert.Equal(22f, (float)args[2]);
            Assert.Equal(33f, (float)args[3]);
            return null;
        });

        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPlayer>.Invocation rotationInvocation = Assert.Single(playerProxy.GetInvocations("set_" + nameof(IWorldEntity.Rotation)));
        Assert.Equal(Vector3.Zero, Assert.IsType<Vector3>(rotationInvocation.Arguments[0]));
        Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
    }

    [Fact]
    public void OnActivateSuccess_WhenDestinationMissing_DoesNotTeleport()
    {
        WorldLocationTeleporterEntityScript script = CreateScript(70783u, CreateGameTable());
        IPlayer player = CreatePlayer(true, out RecordingDispatchProxy<IPlayer> playerProxy);

        script.OnActivateSuccess(player);

        Assert.Empty(playerProxy.GetInvocations("set_" + nameof(IWorldEntity.Rotation)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
    }

    [Fact]
    public void OnActivateSuccess_WhenTeleportUnavailable_DoesNotApplyFacingOrTeleport()
    {
        var destination = new WorldLocation2Entry
        {
            Id        = 1594u,
            WorldId   = 870u,
            Position0 = 1f,
            Position1 = 2f,
            Position2 = 3f,
            Facing3   = 1f
        };

        WorldLocationTeleporterEntityScript script = CreateScript(45102u, CreateGameTable(destination));
        IPlayer player = CreatePlayer(false, out RecordingDispatchProxy<IPlayer> playerProxy);

        script.OnActivateSuccess(player);

        Assert.Empty(playerProxy.GetInvocations("set_" + nameof(IWorldEntity.Rotation)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
    }

    [Fact]
    public void OnActivateSuccess_WithKnownDirectDestination_TeleportsToBranchCoordinate()
    {
        WorldLocationTeleporterEntityScript script = CreateScript(
            70173u,
            CreateGameTable(),
            CreateWorldTable(new WorldEntry { Id = 1387u }));
        IPlayer player = CreatePlayer(true, out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetMethodHandler(nameof(IPlayer.TeleportTo), args =>
        {
            Assert.Equal((ushort)1387, (ushort)args[0]);
            Assert.Equal(-3835.34f, (float)args[1]);
            Assert.Equal(-980.217f, (float)args[2]);
            Assert.Equal(-6050.52f, (float)args[3]);
            Assert.Null(args[4]);
            Assert.Equal(TeleportReason.Relocate, args[5]);
            return null;
        });

        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPlayer>.Invocation rotationInvocation = Assert.Single(playerProxy.GetInvocations("set_" + nameof(IWorldEntity.Rotation)));
        Assert.Equal(new Vector3(-0.45682f, 0f, 0f), Assert.IsType<Vector3>(rotationInvocation.Arguments[0]));
        Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
    }

    [Fact]
    public void OnActivateSuccess_WithDirectDestinationMissingWorld_DoesNotTeleport()
    {
        WorldLocationTeleporterEntityScript script = CreateScript(
            70173u,
            CreateGameTable(),
            CreateWorldTable());
        IPlayer player = CreatePlayer(true, out RecordingDispatchProxy<IPlayer> playerProxy);

        script.OnActivateSuccess(player);

        Assert.Empty(playerProxy.GetInvocations("set_" + nameof(IWorldEntity.Rotation)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
    }

    [Fact]
    public void OnActivateSuccess_WithDirectDestinationAndTeleportUnavailable_DoesNotTeleport()
    {
        WorldLocationTeleporterEntityScript script = CreateScript(
            70173u,
            CreateGameTable(),
            CreateWorldTable(new WorldEntry { Id = 1387u }));
        IPlayer player = CreatePlayer(false, out RecordingDispatchProxy<IPlayer> playerProxy);

        script.OnActivateSuccess(player);

        Assert.Empty(playerProxy.GetInvocations("set_" + nameof(IWorldEntity.Rotation)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
    }

    [Theory]
    [InlineData(28454u)]
    [InlineData(25886u)]
    public void OnAddToMap_ForRangeTransporter_ArmsBranchRangeCheck(uint creatureId)
    {
        WorldLocationTeleporterEntityScript script = CreateScript(creatureId, CreateGameTable(), out RecordingDispatchProxy<ICreatureEntity> ownerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out _);

        script.OnAddToMap(map);

        RecordingDispatchProxy<ICreatureEntity>.Invocation rangeCheck = Assert.Single(ownerProxy.GetInvocations(nameof(ICreatureEntity.SetInRangeCheck)));
        Assert.Equal(2f, rangeCheck.Arguments[0]);
    }

    [Fact]
    public void OnEnterRange_WithKnownRangeDestination_TeleportsAndAppliesFacing()
    {
        var destination = new WorldLocation2Entry
        {
            Id        = 21760u,
            WorldId   = 870u,
            Position0 = -1f,
            Position1 = -2f,
            Position2 = -3f,
            Facing3   = 1f
        };

        WorldLocationTeleporterEntityScript script = CreateScript(28454u, CreateGameTable(destination), new Vector3(0f, 10f, 0f));
        IPlayer player = CreatePlayer(true, new Vector3(0f, 10f, 0f), out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetMethodHandler(nameof(IPlayer.TeleportTo), args =>
        {
            Assert.Equal((ushort)870, (ushort)args[0]);
            Assert.Equal(-1f, (float)args[1]);
            Assert.Equal(-2f, (float)args[2]);
            Assert.Equal(-3f, (float)args[3]);
            return null;
        });

        script.OnEnterRange(player);

        RecordingDispatchProxy<IPlayer>.Invocation rotationInvocation = Assert.Single(playerProxy.GetInvocations("set_" + nameof(IWorldEntity.Rotation)));
        Assert.Equal(Vector3.Zero, Assert.IsType<Vector3>(rotationInvocation.Arguments[0]));
        Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
    }

    [Fact]
    public void OnEnterRange_WithExoLab22RangeDestination_TeleportsToClientWorldLocation()
    {
        var destination = new WorldLocation2Entry
        {
            Id        = 19282u,
            WorldId   = 870u,
            Position0 = -7079.58f,
            Position1 = -1045.62f,
            Position2 = -1037.51f,
            Facing2   = 0.644289f,
            Facing3   = 0.764782f
        };

        WorldLocationTeleporterEntityScript script = CreateScript(25886u, CreateGameTable(destination), new Vector3(-7048.574f, -997.1969f, -1021.181f));
        IPlayer player = CreatePlayer(true, new Vector3(-7048.574f, -997.1969f, -1021.181f), out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetMethodHandler(nameof(IPlayer.TeleportTo), args =>
        {
            Assert.Equal((ushort)870, (ushort)args[0]);
            Assert.Equal(-7079.58f, (float)args[1]);
            Assert.Equal(-1045.62f, (float)args[2]);
            Assert.Equal(-1037.51f, (float)args[3]);
            return null;
        });

        script.OnEnterRange(player);

        Assert.IsType<Vector3>(Assert.Single(playerProxy.GetInvocations("set_" + nameof(IWorldEntity.Rotation))).Arguments[0]);
        Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
    }

    [Fact]
    public void OnEnterRange_WhenPlayerBelowRangeTriggerPlane_DoesNotTeleport()
    {
        var destination = new WorldLocation2Entry
        {
            Id        = 21760u,
            WorldId   = 870u,
            Position0 = -1f,
            Position1 = -2f,
            Position2 = -3f,
            Facing3   = 1f
        };

        WorldLocationTeleporterEntityScript script = CreateScript(28454u, CreateGameTable(destination), new Vector3(0f, 10f, 0f));
        IPlayer player = CreatePlayer(true, new Vector3(0f, 8.5f, 0f), out RecordingDispatchProxy<IPlayer> playerProxy);

        script.OnEnterRange(player);

        Assert.Empty(playerProxy.GetInvocations("set_" + nameof(IWorldEntity.Rotation)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
    }

    private static WorldLocationTeleporterEntityScript CreateScript(uint creatureId, GameTable<WorldLocation2Entry> worldLocationTable)
    {
        return CreateScript(creatureId, worldLocationTable, Vector3.Zero);
    }

    private static WorldLocationTeleporterEntityScript CreateScript(
        uint creatureId,
        GameTable<WorldLocation2Entry> worldLocationTable,
        GameTable<WorldEntry> worldTable)
    {
        return CreateScript(creatureId, worldLocationTable, Vector3.Zero, worldTable, out _);
    }

    private static WorldLocationTeleporterEntityScript CreateScript(uint creatureId, GameTable<WorldLocation2Entry> worldLocationTable, Vector3 ownerPosition)
    {
        return CreateScript(creatureId, worldLocationTable, ownerPosition, out _);
    }

    private static WorldLocationTeleporterEntityScript CreateScript(
        uint creatureId,
        GameTable<WorldLocation2Entry> worldLocationTable,
        out RecordingDispatchProxy<ICreatureEntity> ownerProxy)
    {
        return CreateScript(creatureId, worldLocationTable, Vector3.Zero, out ownerProxy);
    }

    private static WorldLocationTeleporterEntityScript CreateScript(
        uint creatureId,
        GameTable<WorldLocation2Entry> worldLocationTable,
        Vector3 ownerPosition,
        out RecordingDispatchProxy<ICreatureEntity> ownerProxy)
    {
        return CreateScript(creatureId, worldLocationTable, ownerPosition, null, out ownerProxy);
    }

    private static WorldLocationTeleporterEntityScript CreateScript(
        uint creatureId,
        GameTable<WorldLocation2Entry> worldLocationTable,
        Vector3 ownerPosition,
        GameTable<WorldEntry> worldTable,
        out RecordingDispatchProxy<ICreatureEntity> ownerProxy)
    {
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> gameTableManagerProxy);
        gameTableManagerProxy.SetProperty(nameof(IGameTableManager.WorldLocation2), worldLocationTable);
        if (worldTable != null)
            gameTableManagerProxy.SetProperty(nameof(IGameTableManager.World), worldTable);

        ICreatureEntity owner = RecordingDispatchProxy<ICreatureEntity>.Create(out ownerProxy);
        ownerProxy.SetProperty(nameof(IWorldEntity.Guid), 900u);
        ownerProxy.SetProperty(nameof(IWorldEntity.CreatureId), creatureId);
        ownerProxy.SetProperty(nameof(IGridEntity.Position), ownerPosition);

        var script = new WorldLocationTeleporterEntityScript(
            NullLogger<WorldLocationTeleporterEntityScript>.Instance,
            gameTableManager);
        script.OnLoad(owner);

        return script;
    }

    private static IPlayer CreatePlayer(bool canTeleport, out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        return CreatePlayer(canTeleport, Vector3.Zero, out playerProxy);
    }

    private static IPlayer CreatePlayer(bool canTeleport, Vector3 position, out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 42u);
        playerProxy.SetProperty(nameof(IGridEntity.Position), position);
        playerProxy.SetMethodReturn(nameof(IPlayer.CanTeleport), canTeleport);
        return player;
    }

    private static GameTable<WorldLocation2Entry> CreateGameTable(params WorldLocation2Entry[] entries)
    {
        var table = (GameTable<WorldLocation2Entry>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<WorldLocation2Entry>));

        typeof(GameTable<WorldLocation2Entry>)
            .GetField("<Entries>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(table, entries);

        uint maxId = entries.Length == 0 ? 0u : entries.Max(e => e.Id) + 1u;
        typeof(GameTable<WorldLocation2Entry>)
            .GetField("header", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(table, new GameTableHeader { MaxId = maxId });

        int[] lookup = Enumerable.Repeat(-1, (int)maxId).ToArray();
        for (int i = 0; i < entries.Length; i++)
            lookup[entries[i].Id] = i;

        typeof(GameTable<WorldLocation2Entry>)
            .GetField("lookup", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(table, lookup);

        return table;
    }

    private static GameTable<WorldEntry> CreateWorldTable(params WorldEntry[] entries)
    {
        var table = (GameTable<WorldEntry>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<WorldEntry>));

        typeof(GameTable<WorldEntry>)
            .GetField("<Entries>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(table, entries);

        uint maxId = entries.Length == 0 ? 0u : entries.Max(e => e.Id) + 1u;
        typeof(GameTable<WorldEntry>)
            .GetField("header", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(table, new GameTableHeader { MaxId = maxId });

        int[] lookup = Enumerable.Repeat(-1, (int)maxId).ToArray();
        for (int i = 0; i < entries.Length; i++)
            lookup[entries[i].Id] = i;

        typeof(GameTable<WorldEntry>)
            .GetField("lookup", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(table, lookup);

        return table;
    }
}
