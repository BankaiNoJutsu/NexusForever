using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Prerequisite;

// Prerequisite 219 (native FUN_1404a15d0 -> FUN_1404a80b0, Decomp/Analysis/PREREQUISITE_24_25_219_NATIVE_READ_20261108.md): only a Player/Ghost subject is evaluated (others give false for every
// comparison); the volume is complete when popcount(volume progress word) >= the number of datacubes the volume has for the player's faction, with need 0 meaning never complete; Value is
// not read; Equal returns the flag, NotEqual its negation. The datacube count is built at load: a datacube is kept only when its WorldZone row exists, the first kept datacube fixes the
// volume's faction (DatacubeFactionEnum: 0 both, 1 Exile only, 2 Dominion only) and later datacubes with a different faction are dropped.
public class VolumeCompletionPrerequisiteTests
{
    private const uint VolumeId = 5u;

    [Theory]
    [InlineData(0b011u, false)]
    [InlineData(0b101u, false)]
    [InlineData(0b111u, true)]
    [InlineData(0b1111u, true)] // more bits than datacubes still counts
    [InlineData(0u, false)]
    public void ACompleteVolumeNeedsOneBitPerKeptDatacube(uint progress, bool complete)
    {
        var check = new PrerequisiteCheckVolume(Tables(Volume(VolumeId, 1, 2, 3), Cube(1, 0), Cube(2, 0), Cube(3, 0)));
        IPlayer player = Player(Faction.Exile, progress);

        Assert.Equal(complete, check.Meets(player, PrerequisiteComparison.Equal, 0u, VolumeId, new PrerequisiteParameters()));
        Assert.Equal(!complete, check.Meets(player, PrerequisiteComparison.NotEqual, 0u, VolumeId, new PrerequisiteParameters()));
        Assert.False(check.Meets(player, PrerequisiteComparison.GreaterThan, 0u, VolumeId, new PrerequisiteParameters()));
        Assert.Equal(complete, check.Meets(player, PrerequisiteComparison.Equal, 7u, VolumeId, new PrerequisiteParameters())); // Value is ignored
    }

    [Fact]
    public void AVolumeWhoseDatacubesHaveNoWorldZoneIsNeverComplete()
    {
        // retail volume 1: datacubes 53, 54 and 55 all have WorldZoneId 0, so the loader keeps none and never registers it
        var check = new PrerequisiteCheckVolume(Tables(Volume(VolumeId, 1, 2, 3), Cube(1, 0, zone: 0u), Cube(2, 0, zone: 0u), Cube(3, 0, zone: 0u)));
        IPlayer player = Player(Faction.Exile, uint.MaxValue);

        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 0u, VolumeId, new PrerequisiteParameters()));
        Assert.True(check.Meets(player, PrerequisiteComparison.NotEqual, 0u, VolumeId, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(Faction.Exile, 1u, true)]
    [InlineData(Faction.Dominion, 1u, false)]
    [InlineData(Faction.Dominion, 2u, true)]
    [InlineData(Faction.Exile, 2u, false)]
    [InlineData(Faction.Exile, 0u, true)]
    [InlineData(Faction.Dominion, 0u, true)]
    public void AVolumeCountsOnlyForTheFactionItsDatacubeBelongsTo(Faction player, uint datacubeFaction, bool expected)
    {
        var check = new PrerequisiteCheckVolume(Tables(Volume(VolumeId, 1), Cube(1, datacubeFaction)));

        Assert.Equal(expected, check.Meets(Player(player, 1u), PrerequisiteComparison.Equal, 0u, VolumeId, new PrerequisiteParameters()));
    }

    [Fact]
    public void LaterDatacubesOfAnotherFactionAreDropped()
    {
        // the first kept datacube fixes the volume faction (1, Exile only); the Dominion one is dropped, so one bit completes it
        var check = new PrerequisiteCheckVolume(Tables(Volume(VolumeId, 1, 2), Cube(1, 1), Cube(2, 2)));

        Assert.True(check.Meets(Player(Faction.Exile, 1u), PrerequisiteComparison.Equal, 0u, VolumeId, new PrerequisiteParameters()));
    }

    [Fact]
    public void AMissingVolumeOrProgressRecordIsNotComplete()
    {
        var check = new PrerequisiteCheckVolume(Tables(Volume(VolumeId, 1), Cube(1, 0)));

        Assert.False(check.Meets(Player(Faction.Exile, 1u), PrerequisiteComparison.Equal, 0u, VolumeId + 1u, new PrerequisiteParameters()));
        Assert.False(check.Meets(Player(Faction.Exile, progress: null), PrerequisiteComparison.Equal, 0u, VolumeId, new PrerequisiteParameters()));
        Assert.True(check.Meets(Player(Faction.Exile, progress: null), PrerequisiteComparison.NotEqual, 0u, VolumeId, new PrerequisiteParameters()));
    }

    [Fact]
    public void ANonPlayerSubjectIsFalseForEveryComparison()
    {
        IPrerequisiteCheck check = new PrerequisiteCheckVolume(Tables(Volume(VolumeId, 1), Cube(1, 0)));
        IUnitEntity creature = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IUnitEntity.Type), EntityType.NonPlayer);

        Assert.False(check.MeetsUnit(creature, PrerequisiteComparison.Equal, 0u, VolumeId, new PrerequisiteParameters()));
        Assert.False(check.MeetsUnit(creature, PrerequisiteComparison.NotEqual, 0u, VolumeId, new PrerequisiteParameters()));
    }

    [Fact]
    public void TheProgressComesFromTheJournalDatacubeType()
    {
        var check = new PrerequisiteCheckVolume(Tables(Volume(VolumeId, 1), Cube(1, 0)));
        IPlayer player = Player(Faction.Exile, 1u, out var datacubes);

        check.Meets(player, PrerequisiteComparison.Equal, 0u, VolumeId, new PrerequisiteParameters());

        Assert.Equal(DatacubeType.Journal, Assert.Single(datacubes.GetInvocations(nameof(IDatacubeManager.GetDatacube))).Arguments[1]);
    }

    private static DatacubeVolumeEntry Volume(uint id, params uint[] datacubes)
    {
        var entry = new DatacubeVolumeEntry { Id = id };
        var fields = typeof(DatacubeVolumeEntry).GetFields().Where(f => f.Name.StartsWith("DatacubeId")).OrderBy(f => f.Name).ToArray();
        for (int i = 0; i < datacubes.Length; i++)
            fields[i].SetValue(entry, datacubes[i]);
        return entry;
    }

    private static DatacubeEntry Cube(uint id, uint faction, uint zone = 10u)
        => new() { Id = id, DatacubeFactionEnum = faction, WorldZoneId = zone };

    private static IGameTableManager Tables(DatacubeVolumeEntry volume, params DatacubeEntry[] cubes)
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
        proxy.SetProperty(nameof(IGameTableManager.DatacubeVolume), PlayerBindPointTests.Table(volume));
        proxy.SetProperty(nameof(IGameTableManager.Datacube), PlayerBindPointTests.Table(cubes));
        proxy.SetProperty(nameof(IGameTableManager.WorldZone), PlayerBindPointTests.Table(new WorldZoneEntry { Id = 10u }));
        return tables;
    }

    private static IPlayer Player(Faction faction, uint? progress)
        => Player(faction, progress, out _);

    private static IPlayer Player(Faction faction, uint? progress, out RecordingDispatchProxy<IDatacubeManager> datacubeProxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Type), EntityType.Player);
        playerProxy.SetProperty(nameof(IPlayer.Faction1), faction);
        IDatacubeManager manager = RecordingDispatchProxy<IDatacubeManager>.Create(out datacubeProxy);
        IDatacube cube = null;
        if (progress is uint value)
        {
            cube = RecordingDispatchProxy<IDatacube>.Create(out var cubeProxy);
            cubeProxy.SetProperty(nameof(IDatacube.Progress), value);
        }
        datacubeProxy.SetMethodReturn(nameof(IDatacubeManager.GetDatacube), cube);
        playerProxy.SetProperty(nameof(IPlayer.DatacubeManager), manager);
        return player;
    }
}
