using System.Numerics;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Entitlement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Prerequisite;

// Native evidence (Decomp/Analysis/PREREQUISITE_TYPE_COVERAGE_20261107.md, "Bounds, entitlement count and PvP flag"):
//  - 141 IsOutOfBounds, FUN_14049e540: looks up the WorldLocation2 row ObjectId (FUN_14024b980); no row or a different world gives false for every comparison; otherwise
//    `distance(unit, row position) < row.Radius` with Equal returning that result, NotEqual its negation and any other comparison false.
//  - 273 EntitlementCount, FUN_1404a1670: player units only; the count is FUN_1403d2140(ObjectId) = min(MaxCount (0 = no cap), character amount + account amount) and is compared
//    with Value (compare helper argument order: comparison, count, value).
//  - 217 PvpFlag, FUN_14049db90: compares FUN_1403b48b0(unit) (1 when the unit's PvP flag bit is set) with Value; Equal and NotEqual only.
public class BoundsEntitlementPvpPrerequisiteTests
{
    // ---- 141 ----

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 3f, 4f, 0f, true)]      // distance 5 < radius 10
    [InlineData(PrerequisiteComparison.NotEqual, 3f, 4f, 0f, false)]
    [InlineData(PrerequisiteComparison.Equal, 10f, 0f, 0f, false)]    // distance 10 is not < 10
    [InlineData(PrerequisiteComparison.NotEqual, 10f, 0f, 0f, true)]
    [InlineData(PrerequisiteComparison.Equal, 6f, 8f, 1f, false)]     // distance sqrt(101) > 10, vertical offset counts
    [InlineData(PrerequisiteComparison.GreaterThan, 3f, 4f, 0f, false)]
    [InlineData(PrerequisiteComparison.LessThan, 30f, 4f, 0f, false)]
    public void IsOutOfBoundsComparesThe3dDistanceWithTheLocationRadius(PrerequisiteComparison comparison, float x, float y, float z, bool expected)
    {
        var check = new PrerequisiteCheckIsOutOfBounds(Tables(new WorldLocation2Entry { Id = 5947, WorldId = 1, Radius = 10f, Position0 = 0f, Position1 = 0f, Position2 = 0f }));

        Assert.Equal(expected, check.Meets(Player(new Vector3(x, y, z), world: 1), comparison, 0u, 5947u, new PrerequisiteParameters()));
    }

    [Fact]
    public void IsOutOfBoundsFailsForEveryComparisonWithoutARowOrInAnotherWorld()
    {
        var check = new PrerequisiteCheckIsOutOfBounds(Tables(new WorldLocation2Entry { Id = 5947, WorldId = 1, Radius = 10f }));

        foreach (PrerequisiteComparison comparison in new[] { PrerequisiteComparison.Equal, PrerequisiteComparison.NotEqual })
        {
            Assert.False(check.Meets(Player(Vector3.Zero, world: 1), comparison, 0u, 123u, new PrerequisiteParameters()));  // no such row
            Assert.False(check.Meets(Player(Vector3.Zero, world: 2), comparison, 0u, 5947u, new PrerequisiteParameters())); // other world
        }
    }

    // ---- 273 ----

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 5u, 3u, 2u, 5u, true)]           // 3 character + 2 account
    [InlineData(PrerequisiteComparison.LessThan, 6u, 3u, 2u, 0u, true)]        // MaxCount 0 = uncapped
    [InlineData(PrerequisiteComparison.LessThan, 5u, 3u, 2u, 0u, false)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 4u, 3u, 2u, 0u, true)]
    [InlineData(PrerequisiteComparison.Equal, 4u, 3u, 2u, 4u, true)]           // capped by MaxCount 4
    [InlineData(PrerequisiteComparison.Equal, 5u, 3u, 2u, 4u, false)]
    [InlineData(PrerequisiteComparison.Equal, 0u, 0u, 0u, 4u, true)]
    public void EntitlementCountComparesTheCappedCharacterPlusAccountAmount(PrerequisiteComparison comparison, uint value, uint character, uint account, uint maxCount, bool expected)
    {
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.Entitlement), PlayerBindPointTests.Table(new EntitlementEntry { Id = 31, MaxCount = maxCount }));
        var check = new PrerequisiteCheckEntitlementCount(tables);

        Assert.Equal(expected, check.Meets(PlayerWithEntitlements(character, account), comparison, value, 31u, new PrerequisiteParameters()));
    }

    [Fact]
    public void EntitlementCountOfAnUnknownEntitlementIsUncappedAndAnAbsentOneIsZero()
    {
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out _);
        var check = new PrerequisiteCheckEntitlementCount(tables);

        Assert.True(check.Meets(PlayerWithEntitlements(7u, 0u), PrerequisiteComparison.Equal, 7u, 99u, new PrerequisiteParameters()));
        Assert.True(check.Meets(PlayerWithEntitlements(null, null), PrerequisiteComparison.Equal, 0u, 99u, new PrerequisiteParameters()));
    }

    // ---- 217 ----

    [Theory]
    [InlineData(PvPFlag.Enabled, PrerequisiteComparison.Equal, 1u, true)]
    [InlineData(PvPFlag.Forced, PrerequisiteComparison.Equal, 1u, true)]
    [InlineData(PvPFlag.Enabled | PvPFlag.Forced, PrerequisiteComparison.Equal, 1u, true)]
    [InlineData(PvPFlag.Disabled, PrerequisiteComparison.Equal, 1u, false)]
    [InlineData(PvPFlag.Disabled, PrerequisiteComparison.Equal, 0u, true)]
    [InlineData(PvPFlag.Enabled, PrerequisiteComparison.Equal, 0u, false)]
    [InlineData(PvPFlag.Disabled, PrerequisiteComparison.NotEqual, 1u, true)]
    [InlineData(PvPFlag.Enabled, PrerequisiteComparison.NotEqual, 1u, false)]
    [InlineData(PvPFlag.Enabled, PrerequisiteComparison.GreaterThan, 0u, false)]
    [InlineData(PvPFlag.Enabled, PrerequisiteComparison.LessThanOrEqual, 1u, false)]
    public void PvpFlagComparesWhetherThePlayerIsFlaggedWithValue(PvPFlag flag, PrerequisiteComparison comparison, uint value, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetProperty(nameof(IPlayer.PvPFlag), flag);

        Assert.Equal(expected, new PrerequisiteCheckPvpFlag().Meets(player, comparison, value, 0u, new PrerequisiteParameters()));
    }

    [Fact]
    public void PvpFlagEvaluatesTheTargetWhenOneIsSupplied()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.PvPFlag), PvPFlag.Disabled);
        IPlayer target = RecordingDispatchProxy<IPlayer>.Create(out var targetProxy);
        targetProxy.SetProperty(nameof(IPlayer.PvPFlag), PvPFlag.Enabled);

        var check = new PrerequisiteCheckPvpFlag();
        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 1u, 0u, new PrerequisiteParameters { Target = target }));
        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 1u, 0u, new PrerequisiteParameters()));
    }

    private static IGameTableManager Tables(WorldLocation2Entry location)
    {
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
        proxy.SetProperty(nameof(IGameTableManager.WorldLocation2), PlayerBindPointTests.Table(location));
        return tables;
    }

    private static IPlayer Player(Vector3 position, uint world)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetProperty(nameof(IPlayer.Position), position);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.Entry), new WorldEntry { Id = world });
        proxy.SetProperty(nameof(IPlayer.Map), map);
        return player;
    }

    private static IPlayer PlayerWithEntitlements(uint? character, uint? account)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);

        ICharacterEntitlementManager characterManager = RecordingDispatchProxy<ICharacterEntitlementManager>.Create(out var characterManagerProxy);
        characterManagerProxy.SetMethodHandler(nameof(ICharacterEntitlementManager.GetEntitlement), _ =>
        {
            if (character == null)
                return null;

            ICharacterEntitlement entitlement = RecordingDispatchProxy<ICharacterEntitlement>.Create(out var entitlementProxy);
            entitlementProxy.SetProperty(nameof(ICharacterEntitlement.Amount), character.Value);
            return entitlement;
        });
        playerProxy.SetProperty(nameof(IPlayer.EntitlementManager), characterManager);

        IAccount accountModel = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
        IAccountEntitlementManager accountManager = RecordingDispatchProxy<IAccountEntitlementManager>.Create(out var accountManagerProxy);
        accountManagerProxy.SetMethodHandler(nameof(IAccountEntitlementManager.GetEntitlement), _ =>
        {
            if (account == null)
                return null;

            IAccountEntitlement entitlement = RecordingDispatchProxy<IAccountEntitlement>.Create(out var entitlementProxy);
            entitlementProxy.SetProperty(nameof(IAccountEntitlement.Amount), account.Value);
            return entitlement;
        });
        accountProxy.SetProperty(nameof(IAccount.EntitlementManager), accountManager);
        playerProxy.SetProperty(nameof(IPlayer.Account), accountModel);

        return player;
    }
}
