using NexusForever.Game;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Loot;
using NexusForever.Game.Static.Loot;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Loot;

public class LootEligibilityTests
{
    [Fact]
    public void CanTryRoll_RequiresRollAndEligibleMember()
    {
        Identity winner = new() { RealmId = 1, Id = 42ul };
        Identity loser  = new() { RealmId = 1, Id = 43ul };
        var lootItem = new LootInstanceItem(100u, LootItemType.StaticItem, 1u);
        lootItem.ConfigureRoll([winner, loser]);

        Assert.True(lootItem.CanTryRoll(winner.Id));
        Assert.True(lootItem.CanTryRoll(loser.Id));
        Assert.False(lootItem.CanTryRoll(99ul));
    }

    [Fact]
    public void TryRecordRoll_RejectsIneligiblePlayer()
    {
        Identity winner = new() { RealmId = 1, Id = 42ul };
        var lootItem = new LootInstanceItem(100u, LootItemType.StaticItem, 1u);
        lootItem.ConfigureRoll([winner]);

        IPlayer outsider = RecordingDispatchProxy<IPlayer>.Create(out var outsiderProxy);
        outsiderProxy.SetProperty(nameof(IPlayer.CharacterId), 99ul);
        outsiderProxy.SetProperty(nameof(IPlayer.Identity), new Identity { RealmId = 1, Id = 99ul });

        Assert.False(lootItem.TryRecordRoll(outsider, LootRollAction.Need));
    }

    [Fact]
    public void CanTryMasterAssign_RequiresMasterAuthorityAndEligibleAssignee()
    {
        Identity master   = new() { RealmId = 1, Id = 52ul };
        Identity assignee = new() { RealmId = 1, Id = 53ul };
        Identity outsider = new() { RealmId = 1, Id = 54ul };
        var lootItem = new LootInstanceItem(100u, LootItemType.StaticItem, 1u);
        lootItem.ConfigureMaster([master], [master, assignee], [master, assignee]);

        Assert.True(lootItem.CanTryMasterAssign(master.Id, assignee));
        Assert.False(lootItem.CanTryMasterAssign(master.Id, outsider));
        Assert.False(lootItem.CanTryMasterAssign(assignee.Id, assignee));
    }

    [Fact]
    public void AssignMasterLoot_RejectsNonMasterAssigner()
    {
        Identity master   = new() { RealmId = 1, Id = 52ul };
        Identity assignee = new() { RealmId = 1, Id = 53ul };
        var lootInstance = new LootInstance(
            ownerUnitId: 99u,
            looterIds: new Dictionary<ulong, uint>
            {
                [master.Id]   = 5252u,
                [assignee.Id] = 5353u
            },
            looterType: LooterType.Group,
            lootEntityType: LootEntityType.Creature);

        LootInstanceItem lootItem = lootInstance.AddLootItem(100u, LootItemType.StaticItem, 1u);
        lootItem.ConfigureMaster([master], [master, assignee], [master, assignee]);

        IPlayer assigneePlayer = RecordingDispatchProxy<IPlayer>.Create(out var assigneeProxy);
        assigneeProxy.SetProperty(nameof(IPlayer.CharacterId), assignee.Id);

        Assert.False(lootInstance.AssignMasterLoot(assigneePlayer, lootItem.Id, assignee));
    }
}
