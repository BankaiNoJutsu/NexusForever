using System.Reflection;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Currency;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.PrimalMatrix;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Entity;

/// <summary>
/// Primal Matrix allocation rules (Decomp/Analysis/PRIMAL_MATRIX_RULES_20261110.md). The fixture is a small piece of the retail layout: the start node at (0, 0) (row 1, flag 1),
/// colour nodes around a free cluster centre, and a bridge node that costs purple essence.
/// </summary>
public class PrimalMatrixTests
{
    private const uint Start = 1u, RedNode = 2u, Centre = 7u, Bridge = 20u, Far = 30u;

    [Fact]
    public void NeighboursAreTheSixAxialHexNeighbours()
    {
        GameTable<PrimalMatrixNodeEntry> nodes = Nodes();

        uint[] neighbours = PrimalMatrixRules.GetNeighbours(nodes.GetEntry(Start), nodes).Select(n => n.Id).OrderBy(id => id).ToArray();

        // (1, 0) and (0, 1) are next to (0, 0); (1, 1) is not on this lattice, (1, -1) is
        Assert.Equal(new[] { RedNode, Centre, 9u }, neighbours);
    }

    [Fact]
    public void TheStartNodeIsOpenAndOpensItsNeighbours()
    {
        GameTable<PrimalMatrixNodeEntry> nodes = Nodes();
        var saved = new Dictionary<uint, byte>();

        Assert.Equal(PrimalMatrixRules.Result.Locked, PrimalMatrixRules.TryApply(saved, [(RedNode, 1)], nodes, out _, out _));
        Assert.Equal(PrimalMatrixRules.Result.Ok, PrimalMatrixRules.TryApply(saved, [(Start, 1)], nodes, out Dictionary<uint, byte> afterStart, out _));
        Assert.Equal(PrimalMatrixRules.Result.Ok, PrimalMatrixRules.TryApply(afterStart, [(RedNode, 1)], nodes, out _, out _));
    }

    [Fact]
    public void ANodeMayBeOpenedByAnotherNodeOfTheSameRequestInAnyOrder()
    {
        GameTable<PrimalMatrixNodeEntry> nodes = Nodes();

        PrimalMatrixRules.Result result = PrimalMatrixRules.TryApply(new Dictionary<uint, byte>(), [(Bridge, 1), (RedNode, 2), (Start, 1)], nodes,
            out Dictionary<uint, byte> allocations, out PrimalMatrixRules.Cost cost);

        Assert.Equal(PrimalMatrixRules.Result.Ok, result);
        Assert.Equal((byte)2, allocations[RedNode]);
        // cost is per allocation: two red allocations at 10000 and one purple at 7500; the start node carries its own red cost
        Assert.Equal(new PrimalMatrixRules.Cost(Red: 5000ul + 20000ul, Blue: 0ul, Green: 0ul, Purple: 7500ul), cost);
    }

    [Theory]
    [InlineData(RedNode, (byte)6, PrimalMatrixRules.Result.OverMaximum)]     // maximum 5
    [InlineData(999u, (byte)1, PrimalMatrixRules.Result.UnknownNode)]
    [InlineData(Far, (byte)1, PrimalMatrixRules.Result.Locked)]             // no allocated neighbour
    [InlineData(RedNode, (byte)0, PrimalMatrixRules.Result.EmptyRequest)]
    public void RequestsThatBreakARuleAreRefused(uint nodeId, byte increment, PrimalMatrixRules.Result expected)
    {
        var saved = new Dictionary<uint, byte> { [Start] = 1 };

        Assert.Equal(expected, PrimalMatrixRules.TryApply(saved, [(nodeId, increment)], Nodes(), out Dictionary<uint, byte> allocations, out _));
        Assert.Equal((byte)1, Assert.Single(allocations).Value);    // nothing changed
    }

    [Fact]
    public void ANodeThatAlreadyHoldsAnAllocationCanTakeMoreWithoutBeingReopened()
    {
        var saved = new Dictionary<uint, byte> { [Start] = 1, [RedNode] = 2 };

        Assert.Equal(PrimalMatrixRules.Result.Ok, PrimalMatrixRules.TryApply(saved, [(RedNode, 3)], Nodes(), out Dictionary<uint, byte> allocations, out PrimalMatrixRules.Cost cost));
        Assert.Equal((byte)5, allocations[RedNode]);
        Assert.Equal(30000ul, cost.Red);
    }

    // PrimalMatrixRewardType, the client's named enum: AbilityPoint 2, AMPPoint 3, AbilityUnlock 4, UnitProperty 5.
    [Fact]
    public void RewardsAreReadPerClassWithTheirClientTypes()
    {
        GameTable<PrimalMatrixNodeEntry> nodes = Nodes();
        GameTable<PrimalMatrixRewardEntry> rewards = Rewards();

        PrimalMatrixRules.Reward[] warrior = PrimalMatrixRules.GetRewards(nodes.GetEntry(RedNode), Class.Warrior, rewards).ToArray();
        Assert.Equal(new PrimalMatrixRules.Reward(PrimalMatrixRules.RewardType.UnitProperty, 34u, 0u, 30f), warrior[0]);
        Assert.Equal(new PrimalMatrixRules.Reward(PrimalMatrixRules.RewardType.UnitProperty, 153u, 0u, 25f), warrior[1]);    // Heroism

        Assert.Equal(PrimalMatrixRules.RewardType.AbilityUnlock, Assert.Single(PrimalMatrixRules.GetRewards(nodes.GetEntry(Bridge), Class.Medic, rewards)).Type);
        Assert.Empty(PrimalMatrixRules.GetRewards(nodes.GetEntry(Centre), Class.Warrior, rewards));    // a cluster centre has no reward row
    }

    [Fact]
    public void ASaveSpendsEssenceRecordsTheAllocationsAndAddsThePropertyRewardsPerAllocation()
    {
        (PrimalMatrixManager manager, RecordingDispatchProxy<IAccountCurrencyManager> wallet, RecordingDispatchProxy<IPlayer> player) = Manager(affordable: true);

        Assert.True(manager.TryAllocate([(Start, 1), (RedNode, 2)]));

        Assert.Equal((byte)2, manager.GetAllocations(RedNode));
        Assert.Equal((byte)0, manager.GetAllocations(Far));
        Assert.Contains(wallet.GetInvocations(nameof(IAccountCurrencyManager.CurrencySubtractAmount)),
            i => (AccountCurrencyType)i.Arguments[0] == AccountCurrencyType.CrimsonEssence && (ulong)i.Arguments[1] == 25000ul);

        var totals = (IReadOnlyDictionary<Property, float>)player.GetInvocations(nameof(IPlayer.SetPrimalMatrixProperties)).Last().Arguments[0];
        // the start node and the red node share one reward row here: 30 per allocation over three allocations, and 25 Heroism each
        Assert.Equal(90f, totals[(Property)34]);
        Assert.Equal(75f, totals[Property.Heroism]);
    }

    // An AMPPoint node (reward type 3) adds to the AMP power the matrix contributes: a term of its own in the snapshot and on the action sets.
    [Fact]
    public void AnAmpPointNodeRaisesTheMatrixAmpPower()
    {
        (PrimalMatrixManager manager, _, RecordingDispatchProxy<IPlayer> player) = Manager(affordable: true);
        RecordingDispatchProxy<ISpellManager> spells = spellManagerProxy;
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out var sessionProxy);
        player.SetProperty(nameof(IPlayer.Session), session);

        Assert.True(manager.TryAllocate([(Start, 1), (RedNode, 1), (Bridge, 1)]));

        Assert.Equal((ushort)1, (ushort)spells.GetInvocations(nameof(ISpellManager.SetPrimalMatrixAmpPower)).Last().Arguments[0]);

        manager.SendSnapshot();
        var snapshot = Assert.IsType<ServerPrimalMatrixSnapshot>(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Last().Arguments[0]);
        Assert.Equal(1u, snapshot.MatrixAmpPower);
        Assert.Equal(new uint[] { Start, RedNode, Bridge }, snapshot.NodeIds);
        Assert.Equal(new byte[] { 1, 1, 1 }, snapshot.SavedAllocationCounts);
    }

    [Fact]
    public void ACharacterWithoutAllocationsIsSentNoSnapshotUnlessOneIsForced()
    {
        (PrimalMatrixManager manager, _, RecordingDispatchProxy<IPlayer> player) = Manager(affordable: true);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out var sessionProxy);
        player.SetProperty(nameof(IPlayer.Session), session);

        manager.SendSnapshot();
        Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));

        manager.SendSnapshot(force: true);
        Assert.Empty(Assert.IsType<ServerPrimalMatrixSnapshot>(Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))).Arguments[0]).NodeIds);
    }

    [Fact]
    public void ASaveThatCannotBeAffordedChangesNothing()
    {
        (PrimalMatrixManager manager, RecordingDispatchProxy<IAccountCurrencyManager> wallet, _) = Manager(affordable: false);

        Assert.False(manager.TryAllocate([(Start, 1)]));

        Assert.Equal((byte)0, manager.GetAllocations(Start));
        Assert.Empty(wallet.GetInvocations(nameof(IAccountCurrencyManager.CurrencySubtractAmount)));
    }

    [Fact]
    public async Task SavePersistsNewNodesUpdatesChangedOnesAndLoadsThemBack()
    {
        using var f = new Fixture(null); f.Seed();
        (PrimalMatrixManager manager, _, _) = Manager(affordable: true);
        Assert.True(manager.TryAllocate([(Start, 1), (RedNode, 2)]));

        await using (var context = f.Context())
        {
            manager.Save(context);
            await context.SaveChangesAsync();
        }

        // more allocations on a node that is already stored: an update, not a second row
        Assert.True(manager.TryAllocate([(RedNode, 3)]));
        await using (var context = f.Context())
        {
            manager.Save(context);
            await context.SaveChangesAsync();
        }

        // nothing changed since: nothing staged
        await using (var context = f.Context())
        {
            manager.Save(context);
            Assert.Empty(context.ChangeTracker.Entries<CharacterPrimalMatrixNodeModel>());
        }

        using var read = f.Context();
        CharacterPrimalMatrixNodeModel[] rows = read.CharacterPrimalMatrixNode.AsNoTracking().OrderBy(row => row.NodeId).ToArray();
        Assert.Equal(new[] { ((ushort)Start, (byte)1), ((ushort)RedNode, (byte)5) }, rows.Select(row => (row.NodeId, row.Allocations)));

        // a manager built from the stored rows knows them and hands the property totals to the player again
        (PrimalMatrixManager loaded, _, RecordingDispatchProxy<IPlayer> player) = Manager(affordable: true, rows);
        Assert.Equal((byte)5, loaded.GetAllocations(RedNode));
        var totals = (IReadOnlyDictionary<Property, float>)player.GetInvocations(nameof(IPlayer.SetPrimalMatrixProperties)).Last().Arguments[0];
        Assert.Equal(180f, totals[(Property)34]);
    }

    [ThreadStatic]
    private static RecordingDispatchProxy<ISpellManager> spellManagerProxy;

    private static (PrimalMatrixManager, RecordingDispatchProxy<IAccountCurrencyManager>, RecordingDispatchProxy<IPlayer>) Manager(bool affordable, params CharacterPrimalMatrixNodeModel[] stored)
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.PrimalMatrixNode), Nodes());
        tableProxy.SetProperty(nameof(IGameTableManager.PrimalMatrixReward), Rewards());

        IAccountCurrencyManager wallet = RecordingDispatchProxy<IAccountCurrencyManager>.Create(out var walletProxy);
        walletProxy.SetMethodReturn(nameof(IAccountCurrencyManager.CanAfford), affordable);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
        accountProxy.SetProperty(nameof(IAccount.CurrencyManager), wallet);

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Class), Class.Warrior);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);
        playerProxy.SetProperty(nameof(IPlayer.SpellManager), RecordingDispatchProxy<ISpellManager>.Create(out spellManagerProxy));

        var model = new CharacterModel { Id = 42ul };
        foreach (CharacterPrimalMatrixNodeModel row in stored)
            model.PrimalMatrixNode.Add(row);

        return (new PrimalMatrixManager(player, model, tables), walletProxy, playerProxy);
    }

    private static GameTable<PrimalMatrixNodeEntry> Nodes() => CreateGameTable(
        new PrimalMatrixNodeEntry { Id = Start, PositionX = 0u, PositionY = 0u, Flags = 1u, MaxAllocations = 1u, CostRedEssence = 5000u, PrimalMatrixRewardIdWarrior = 1u },
        new PrimalMatrixNodeEntry { Id = RedNode, PositionX = 1u, PositionY = 0u, MaxAllocations = 5u, CostRedEssence = 10000u, PrimalMatrixRewardIdWarrior = 1u },
        new PrimalMatrixNodeEntry { Id = Centre, PositionX = 0u, PositionY = 1u, PrimalMatrixNodeTypeEnum = 1u, MaxAllocations = 1u },
        // (1, -1) with the coordinate stored as an unsigned word, as the table does
        new PrimalMatrixNodeEntry { Id = 9u, PositionX = 1u, PositionY = unchecked((uint)-1), MaxAllocations = 5u, CostBlueEssence = 10000u },
        new PrimalMatrixNodeEntry { Id = Bridge, PositionX = 2u, PositionY = 0u, PrimalMatrixNodeTypeEnum = 2u, MaxAllocations = 1u, CostPurpleEssence = 7500u, PrimalMatrixRewardIdMedic = 2u, PrimalMatrixRewardIdWarrior = 3u },
        new PrimalMatrixNodeEntry { Id = Far, PositionX = 5u, PositionY = 5u, MaxAllocations = 5u, CostGreenEssence = 10000u });

    private static GameTable<PrimalMatrixRewardEntry> Rewards() => CreateGameTable(
        new PrimalMatrixRewardEntry { Id = 1u, PrimalMatrixRewardTypeEnum0 = 5u, ObjectId0 = 34u, Value0 = 30f, PrimalMatrixRewardTypeEnum1 = 5u, ObjectId1 = 153u, Value1 = 25f },
        new PrimalMatrixRewardEntry { Id = 2u, PrimalMatrixRewardTypeEnum0 = 4u, ObjectId0 = 87964u },
        new PrimalMatrixRewardEntry { Id = 3u, PrimalMatrixRewardTypeEnum0 = 3u, Value0 = 1f });

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        typeof(GameTable<T>).GetField($"<{nameof(GameTable<T>.Entries)}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(table, entries);
        FieldInfo idField = typeof(T).GetField("Id", BindingFlags.Instance | BindingFlags.Public)!;
        uint maxId = entries.Length == 0 ? 0u : entries.Max(e => (uint)idField.GetValue(e)!);
        typeof(GameTable<T>).GetField("header", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(table, new GameTableHeader { MaxId = maxId + 1u });
        int[] lookup = Enumerable.Repeat(-1, (int)maxId + 1).ToArray();
        for (int i = 0; i < entries.Length; i++)
            lookup[(uint)idField.GetValue(entries[i])!] = i;
        typeof(GameTable<T>).GetField("lookup", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(table, lookup);
        return table;
    }
}
