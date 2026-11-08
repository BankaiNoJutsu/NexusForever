using System.Numerics;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Entitlement;
using NexusForever.Game.Abstract.Entity;
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

// Agent-written native read, not re-derived (Decomp/Analysis/PREREQUISITE_239_226_244_18_NATIVE_READ_20261108.md; the lead cross-checked the server side, see RESTORATION_GAP2_REMAINING_PREREQUISITES_20261108.md):
// * 226 Entitlement (FUN_1404a1620): player units only; n = the character amount plus the account amount of Entitlement ObjectId capped at the row's MaxCount (the count type 273 uses);
//   Equal is n != 0, NotEqual is n == 0, other comparisons false; Value is not read.
// * 244 (named OwnsAccountItem before, really HasBindPoint; FUN_14049ca70 -> FUN_1401ed460): player units only; whether the player's current bind point exists; Equal is true when it does, NotEqual
//   when it does not; ObjectId and Value are not read.
// * 239 ActionBar (FUN_14049ca10): player units only; bit 0 of the per-action-set unlock dword for set Value (0 to 3), which the client fills from ServerActionSet.Unlocked; Equal is the flag,
//   NotEqual its negation. The server announces every set (0 to MaxActionSets - 1) as unlocked, so that is the flag here.
public class EntitlementBindPointActionBarPrerequisiteTests
{
    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 3u, 0u, 0u, true)]
    [InlineData(PrerequisiteComparison.Equal, 0u, 2u, 0u, true)]
    [InlineData(PrerequisiteComparison.Equal, 0u, 0u, 0u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 0u, 0u, 0u, true)]
    [InlineData(PrerequisiteComparison.NotEqual, 1u, 0u, 0u, false)]
    [InlineData(PrerequisiteComparison.Equal, 9u, 9u, 5u, true)]   // character and account amounts add up to 18, capped at 5: still non-zero
    [InlineData(PrerequisiteComparison.GreaterThan, 1u, 0u, 0u, false)]
    public void EntitlementIsWhetherTheCappedCountIsNonZero(PrerequisiteComparison comparison, uint character, uint account, uint maxCount, bool expected)
    {
        var check = new PrerequisiteCheckEntitlement(Tables(maxCount));

        Assert.Equal(expected, check.Meets(PlayerWithEntitlements(character, account), comparison, 77u, 31u, new PrerequisiteParameters()));
    }

    [Fact]
    public void EntitlementIgnoresValueAndTreatsAnAbsentEntitlementAsZero()
    {
        var check = new PrerequisiteCheckEntitlement(Tables(0u));

        Assert.True(check.Meets(PlayerWithEntitlements(null, null), PrerequisiteComparison.NotEqual, 0u, 31u, new PrerequisiteParameters()));
        Assert.False(check.Meets(PlayerWithEntitlements(null, null), PrerequisiteComparison.Equal, 12345u, 31u, new PrerequisiteParameters()));
        Assert.True(check.Meets(PlayerWithEntitlements(1u, null), PrerequisiteComparison.Equal, 0u, 31u, new PrerequisiteParameters()));
    }

    [Fact]
    public void EntitlementOfANonPlayerSubjectIsFalseForEveryComparison()
    {
        IPrerequisiteCheck check = new PrerequisiteCheckEntitlement(Tables(0u));
        IUnitEntity creature = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IUnitEntity.Type), EntityType.NonPlayer);

        Assert.False(check.MeetsUnit(creature, PrerequisiteComparison.Equal, 0u, 31u, new PrerequisiteParameters()));
        Assert.False(check.MeetsUnit(creature, PrerequisiteComparison.NotEqual, 0u, 31u, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(true, PrerequisiteComparison.Equal, true)]
    [InlineData(true, PrerequisiteComparison.NotEqual, false)]
    [InlineData(false, PrerequisiteComparison.Equal, false)]
    [InlineData(false, PrerequisiteComparison.NotEqual, true)]
    [InlineData(true, PrerequisiteComparison.GreaterThan, false)]
    public void HasBindPointIsWhetherThePlayerHasABindPoint(bool hasBindPoint, PrerequisiteComparison comparison, bool expected)
    {
        var check = new PrerequisiteCheckHasBindPoint();

        Assert.Equal(expected, check.Meets(PlayerWithBindPoint(hasBindPoint), comparison, 123u, 456u, new PrerequisiteParameters()));
    }

    [Fact]
    public void HasBindPointOfANonPlayerSubjectIsFalseForEveryComparison()
    {
        IPrerequisiteCheck check = new PrerequisiteCheckHasBindPoint();
        IUnitEntity creature = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IUnitEntity.Type), EntityType.NonPlayer);

        Assert.False(check.MeetsUnit(creature, PrerequisiteComparison.Equal, 0u, 0u, new PrerequisiteParameters()));
        Assert.False(check.MeetsUnit(creature, PrerequisiteComparison.NotEqual, 0u, 0u, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(0u, PrerequisiteComparison.Equal, true)]
    [InlineData(3u, PrerequisiteComparison.Equal, true)]
    [InlineData(4u, PrerequisiteComparison.Equal, false)]   // there is no fifth action set, so its flag is never set
    [InlineData(0u, PrerequisiteComparison.NotEqual, false)]
    [InlineData(4u, PrerequisiteComparison.NotEqual, true)]
    [InlineData(1u, PrerequisiteComparison.GreaterThan, false)]
    public void ActionBarIsWhetherTheActionSetIsUnlocked(uint actionSet, PrerequisiteComparison comparison, bool expected)
    {
        var check = new PrerequisiteCheckActionBar();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

        Assert.Equal(expected, check.Meets(player, comparison, actionSet, 999u, new PrerequisiteParameters()));
    }

    [Fact]
    public void ActionBarOfANonPlayerSubjectIsFalseForEveryComparison()
    {
        IPrerequisiteCheck check = new PrerequisiteCheckActionBar();
        IUnitEntity creature = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IUnitEntity.Type), EntityType.NonPlayer);

        Assert.False(check.MeetsUnit(creature, PrerequisiteComparison.Equal, 0u, 0u, new PrerequisiteParameters()));
        Assert.False(check.MeetsUnit(creature, PrerequisiteComparison.NotEqual, 0u, 0u, new PrerequisiteParameters()));
    }

    private static IGameTableManager Tables(uint maxCount)
    {
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.Entitlement), PlayerBindPointTests.Table(new EntitlementEntry { Id = 31, MaxCount = maxCount }));
        return tables;
    }

    private static IPlayer PlayerWithBindPoint(bool has)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetProperty(nameof(IPlayer.Type), EntityType.Player);
        proxy.SetMethodHandler(nameof(IPlayer.TryGetBindPoint), args =>
        {
            args[0] = has ? (ushort)1229 : (ushort)0;
            args[1] = Vector3.Zero;
            return has;
        });
        return player;
    }

    private static IPlayer PlayerWithEntitlements(uint? character, uint? account)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Type), EntityType.Player);

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
