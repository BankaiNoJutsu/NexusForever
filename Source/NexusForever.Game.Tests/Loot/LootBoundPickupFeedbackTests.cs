using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable.Model;
using NexusForever.Script;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Collection;
using NexusForever.Game.Loot;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Loot;

namespace NexusForever.Game.Tests.Loot;

public sealed partial class LootBoundPickupTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void InventoryEnqueueFailureCannotRepeatAnyPartOfAConfirmedPickup(int rejectAt)
    {
        using var f = new Fixture(4);
        var loot = f.Loot(15);
        Assert.False(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
        int adds = 0;
        var accepted = new List<object>();
        var session = (RecordingDispatchProxy<IGameSession>)(object)f.Actor.Player.Session;
        session.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerItemAdd && ++adds == rejectAt) throw new IOException("Owned loot ItemAdd refusal.");
            accepted.Add(args[0]); return null;
        });
        try { loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id); } catch (IOException) { }
        loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id);
        for (int i = 0; i < 8; ++i) { f.Actor.Player.Inventory.Update(.01); f.Actor.Events.Update(.01); }
        Assert.Equal(15u, f.Actor.Player.Inventory.GetItemCount(Fixture.ItemId));
        Assert.True(loot.Item.Delivered);
        Assert.Equal(2, accepted.OfType<ServerItemAdd>().Count());
        Assert.Equal(2, accepted.OfType<ServerItemAdd>().Select(row => row.InventoryItem.Item.ItemGuid).Distinct().Count());
        Assert.Single(accepted.OfType<ServerLootGrant>());
    }

    [Fact]
    public void PublicationCannotReenterTheSameLootThroughAReplacementInventory()
    {
        using var f = new Fixture(2);
        using var other = new Fixture(2);
        var loot = f.Loot(1);
        Assert.False(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
        IInventory original = f.Actor.Player.Inventory;
        bool entered = false, nested = false;
        var session = (RecordingDispatchProxy<IGameSession>)(object)f.Actor.Player.Session;
        session.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerItemAdd && !entered)
            {
                entered = true;
                f.Actor.Set(nameof(NexusForever.Game.Entity.Player.Inventory), other.Actor.Player.Inventory);
                try { nested = loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id); }
                finally { f.Actor.Set(nameof(NexusForever.Game.Entity.Player.Inventory), original); }
            }
            return null;
        });
        Assert.True(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
        Assert.True(entered); Assert.False(nested);
        Assert.Equal(1u, original.GetItemCount(Fixture.ItemId));
        Assert.Equal(0u, other.Actor.Player.Inventory.GetItemCount(Fixture.ItemId));
    }

    [Fact]
    public void RetriedStackNoticeRebuildsItsUnmodifiedAcceptedQuantity()
    {
        using var f = new Fixture(1);
        f.Add(3, true);
        var loot = f.Loot(2);
        Assert.False(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
        bool refuse = true;
        var accepted = new List<object>();
        var session = (RecordingDispatchProxy<IGameSession>)(object)f.Actor.Player.Session;
        session.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerItemStackCountUpdate stack && refuse)
            {
                refuse = false; stack.StackCount = 999; throw new IOException("Owned stack notice refusal.");
            }
            accepted.Add(args[0]); return null;
        });
        Assert.True(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
        Assert.False(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
        f.Actor.Player.Inventory.Update(.01);
        Assert.Equal(5u, f.Actor.Player.Inventory.GetItemCount(Fixture.ItemId));
        Assert.Equal(5u, Assert.Single(accepted.OfType<ServerItemStackCountUpdate>()).StackCount);
        Assert.Single(accepted.OfType<ServerLootGrant>());
    }

    [Fact]
    public void RefusedGrantDoesNotRepeatItemsOrReuseTheRejectedPacket()
    {
        using var f = new Fixture(1);
        var loot = f.Loot(1);
        Assert.False(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
        bool refuse = true;
        var accepted = new List<object>();
        var session = (RecordingDispatchProxy<IGameSession>)(object)f.Actor.Player.Session;
        session.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerLootGrant grant && refuse)
            {
                refuse = false; grant.LootItem.Amount = 999; throw new IOException("Owned grant refusal.");
            }
            accepted.Add(args[0]); return null;
        });
        Assert.True(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
        f.Actor.Player.Inventory.Update(.01);
        Assert.False(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
        Assert.Single(accepted.OfType<ServerItemAdd>());
        Assert.Equal(1u, Assert.Single(accepted.OfType<ServerLootGrant>()).LootItem.Amount);
        Assert.Equal(1u, f.Actor.Player.Inventory.GetItemCount(Fixture.ItemId));
    }

    [Fact]
    public void RemovingAnItemWhileItsAddIsRefusedDoesNotPublishAGhostOnRetry()
    {
        using var f = new Fixture(1);
        var loot = f.Loot(1);
        Assert.False(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
        bool refuse = true;
        var accepted = new List<object>();
        var session = (RecordingDispatchProxy<IGameSession>)(object)f.Actor.Player.Session;
        session.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerItemAdd && refuse) throw new IOException("Owned delayed add.");
            accepted.Add(args[0]); return null;
        });
        Assert.True(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
        f.Actor.Player.Inventory.ItemDelete(Fixture.ItemId, 1);
        refuse = false; f.Actor.Player.Inventory.Update(.01);
        Assert.Empty(accepted.OfType<ServerItemAdd>());
        Assert.Single(accepted.OfType<ServerLootGrant>());
        Assert.Equal(0u, f.Actor.Player.Inventory.GetItemCount(Fixture.ItemId));
        Assert.False(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
    }

    [Fact]
    public async Task ARefusedPacketDoesNotPreventTheCompletePhysicalAwardFromSavingAndReloading()
    {
        using var f = new Fixture(2);
        var loot = f.Loot(15);
        Assert.False(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
        var session = (RecordingDispatchProxy<IGameSession>)(object)f.Actor.Player.Session;
        session.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerItemAdd) throw new IOException("Owned unavailable item channel.");
            return null;
        });
        Assert.True(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
        await f.Persist();
        using var fresh = f.Reload();
        Assert.Equal(15u, fresh.Player.Inventory.GetItemCount(Fixture.ItemId));
        Assert.All(fresh.Player.Inventory.Single(), item => Assert.True(item.Soulbound));
        Assert.False(loot.Instance.GiveLoot(fresh.Player, loot.Item.Id));
    }

    [Fact]
    public void MovingMapsDuringAnItemNoticeStopsTheOriginalLootGrant()
    {
        using var f = new Fixture(1);
        var loot = f.Loot(1);
        Assert.False(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
        var accepted = new List<object>();
        var session = (RecordingDispatchProxy<IGameSession>)(object)f.Actor.Player.Session;
        session.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            accepted.Add(args[0]);
            if (args[0] is ServerItemAdd)
                f.Actor.Set(nameof(NexusForever.Game.Entity.Player.Map), RecordingDispatchProxy<IBaseMap>.Create(out _));
            return null;
        });
        Assert.True(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
        f.Actor.Player.Inventory.Update(.01);
        Assert.Single(accepted.OfType<ServerItemAdd>());
        Assert.Empty(accepted.OfType<ServerLootGrant>());
        Assert.Equal(1u, f.Actor.Player.Inventory.GetItemCount(Fixture.ItemId));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RetriedLootFeedbackCannotCrossANewMapAdmissionOrUnitGuid(bool newAdmission)
    {
        using var f = new Fixture(1);
        IBaseMap originalMap = RecordingDispatchProxy<IBaseMap>.Create(out _);
        f.Actor.Set(nameof(NexusForever.Game.Entity.Player.Map), originalMap);
        var loot = f.Loot(1);
        Assert.False(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
        bool refuse = true;
        var accepted = new List<object>();
        var session = (RecordingDispatchProxy<IGameSession>)(object)f.Actor.Player.Session;
        session.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerItemAdd && refuse) throw new IOException("Owned pre-transfer refusal.");
            accepted.Add(args[0]); return null;
        });
        Assert.True(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
        if (newAdmission)
        {
            IPlayerMapAdmission admission = f.Actor.Player;
            ulong old = admission.MapAdmissionGeneration;
            f.Actor.Set(nameof(NexusForever.Game.Entity.Player.Map), RecordingDispatchProxy<IBaseMap>.Create(out _));
            admission.BeginMapAdmission();
            f.Actor.Set(nameof(NexusForever.Game.Entity.Player.Map), originalMap);
            admission.BeginMapAdmission();
            Assert.NotEqual(old, admission.MapAdmissionGeneration);
            f.Actor.Set(nameof(NexusForever.Game.Entity.Player.IsLoading), false);
        }
        else f.Actor.Set(nameof(NexusForever.Game.Entity.Player.Guid), 4243u);
        refuse = false; f.Actor.Player.Inventory.Update(.01);
        Assert.Empty(accepted.OfType<ServerItemAdd>());
        Assert.Empty(accepted.OfType<ServerLootGrant>());
        Assert.Equal(1u, f.Actor.Player.Inventory.GetItemCount(Fixture.ItemId));
    }

    [Fact]
    public async Task RefusingACollectQuestPacketCannotLeaveItsCompletionHalfApplied()
    {
        using var f = new Fixture(1);
        var prepared = PrepareCollectQuest(f);
        var quest = prepared.Quest;
        var loot = f.Loot(1);
        Assert.False(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
        bool refuse = true;
        var accepted = new List<object>();
        var session = (RecordingDispatchProxy<IGameSession>)(object)f.Actor.Player.Session;
        session.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerQuestObjectiveUpdate update && refuse)
            { refuse = false; update.Completed = 999; throw new IOException("Owned collect-quest notice refusal."); }
            accepted.Add(args[0]); return null;
        });
        Assert.True(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
        f.Actor.Player.Inventory.Update(.01);
        Assert.Equal(QuestState.Achieved, quest.State);
        Assert.True(quest.Flags.HasFlag(QuestStateFlags.Objective0Complete));
        Assert.Equal(1u, Assert.Single(quest).Progress);
        Assert.Equal(1, prepared.ObjectiveCalls()); Assert.Equal(1, prepared.AchievedCalls());
        Assert.Equal(1u, Assert.Single(accepted.OfType<ServerQuestObjectiveUpdate>()).Completed);
        await f.Persist();
        using var fresh = f.Reload();
        Assert.Equal(QuestState.Achieved, fresh.Quest.State);
        Assert.True(fresh.Quest.Flags.HasFlag(QuestStateFlags.Objective0Complete));
        Assert.Single(accepted.OfType<ServerLootGrant>());
        Assert.Equal(1u, f.Actor.Player.Inventory.GetItemCount(Fixture.ItemId));
    }

    [Fact]
    public async Task RefusedInitialItemNoticeCannotSuppressCollectCreditAcrossTransfer()
    {
        using var f = new Fixture(1);
        var prepared = PrepareCollectQuest(f);
        var loot = f.Loot(1);
        Assert.False(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
        bool refuse = true;
        var accepted = new List<object>();
        var session = (RecordingDispatchProxy<IGameSession>)(object)f.Actor.Player.Session;
        session.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerItemAdd && refuse) throw new IOException("Owned initial-add refusal.");
            accepted.Add(args[0]); return null;
        });
        Assert.True(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
        Assert.Equal(QuestState.Achieved, prepared.Quest.State);
        Assert.Equal(1, prepared.ObjectiveCalls()); Assert.Equal(1, prepared.AchievedCalls());
        ((IPlayerMapAdmission)f.Actor.Player).BeginMapAdmission();
        f.Actor.Set(nameof(NexusForever.Game.Entity.Player.IsLoading), false);
        refuse = false; f.Actor.Player.Inventory.Update(.01);
        Assert.Empty(accepted.OfType<ServerItemAdd>());
        Assert.Empty(accepted.OfType<ServerLootGrant>());
        await f.Persist();
        using var fresh = f.Reload();
        Assert.Equal(1u, fresh.Player.Inventory.GetItemCount(Fixture.ItemId));
        Assert.Equal(QuestState.Achieved, fresh.Quest.State);
        Assert.Equal(1u, Assert.Single(fresh.Quest).Progress);
        Assert.True(fresh.Quest.Flags.HasFlag(QuestStateFlags.Objective0Complete));
        Assert.False(loot.Instance.GiveLoot(fresh.Player, loot.Item.Id));
    }

    [Fact]
    public void NestedInventoryAdmissionDoesNotWaitForAnotherThreadsMapGate()
    {
        using var f = new Fixture(1);
        var loot = f.Loot(1);
        Assert.False(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
        object mapGate = ((IPlayerMapAdmission)f.Actor.Player).MapAdmissionSyncRoot;
        object inventoryGate = typeof(NexusForever.Game.Entity.Inventory)
            .GetProperty("MutationGate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(f.Actor.Player.Inventory)!;
        using var mapHeld = new ManualResetEventSlim(); using var releaseMap = new ManualResetEventSlim();
        using var inventoryHeld = new ManualResetEventSlim(); using var finished = new ManualResetEventSlim();
        bool delivered = false; Exception failure = null;
        var holder = new Thread(() => { lock (mapGate) { mapHeld.Set(); releaseMap.Wait(TimeSpan.FromSeconds(5)); } });
        var caller = new Thread(() =>
        {
            try { lock (inventoryGate) { inventoryHeld.Set(); delivered = loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id); } }
            catch (Exception error) { failure = error; }
            finally { finished.Set(); }
        });
        holder.Start(); bool fast = false;
        try
        {
            Assert.True(mapHeld.Wait(TimeSpan.FromSeconds(5)));
            caller.Start(); Assert.True(inventoryHeld.Wait(TimeSpan.FromSeconds(5)));
            fast = finished.Wait(TimeSpan.FromMilliseconds(500));
        }
        finally { releaseMap.Set(); holder.Join(); if (caller.ThreadState != ThreadState.Unstarted) caller.Join(); }
        Assert.True(fast, "Nested Inventory admission waited for the map gate.");
        Assert.Null(failure); Assert.False(delivered);
        Assert.Equal(0u, f.Actor.Player.Inventory.GetItemCount(Fixture.ItemId));
        Assert.True(loot.Instance.GiveLoot(f.Actor.Player, loot.Item.Id));
        Assert.Equal(1u, f.Actor.Player.Inventory.GetItemCount(Fixture.ItemId));
    }

    private static (NexusForever.Game.Quest.Quest Quest, Func<int> ObjectiveCalls, Func<int> AchievedCalls) PrepareCollectQuest(Fixture f)
    {
        var definition = (RecordingDispatchProxy<IQuestInfo>)(object)f.Storage.Info;
        definition.SetProperty(nameof(IQuestInfo.Objectives), ImmutableList.Create<IQuestObjectiveInfo>(new QuestObjectiveInfo(
            new QuestObjectiveEntry { Id = 90010, Type = (uint)QuestObjectiveType.CollectItem, Data = Fixture.ItemId, Count = 1 })));
        var script = RecordingDispatchProxy<IQuestScript>.Create(out var scriptProxy);
        int objectiveCalls = 0, achievedCalls = 0;
        scriptProxy.SetMethodHandler(nameof(IQuestScript.OnObjectiveUpdate), _ => { ++objectiveCalls; return null; });
        scriptProxy.SetMethodHandler(nameof(IQuestScript.OnQuestStateChange), args =>
        { if ((QuestState)args[0] == QuestState.Achieved) ++achievedCalls; return null; });
        var collection = RecordingDispatchProxy<IScriptCollection>.Create(out var collections);
        collections.SetMethodHandler(nameof(IScriptCollection.Invoke), (method, args) =>
        {
            if (method.IsGenericMethod && method.GetGenericArguments()[0] == typeof(IQuestScript)) ((Action<IQuestScript>)args[0])(script);
            return null;
        });
        ((RecordingDispatchProxy<IScriptManager>)(object)f.Storage.Scripts).SetMethodReturn(nameof(IScriptManager.InitialiseOwnedScripts), collection);
        using var context = f.Storage.Context();
        context.CharacterQuest.Add(new()
        {
            Id = 42, QuestId = 9001, State = (byte)QuestState.Accepted,
            QuestObjective = [new() { Id = 42, QuestId = 9001, Index = 0, Progress = 0 }]
        });
        context.SaveChanges();
        var model = context.CharacterQuest.AsNoTracking().Include(row => row.QuestObjective).Single();
        var quest = new NexusForever.Game.Quest.Quest(f.Actor.Player, f.Storage.Info, model,
            globalQuestManager: f.Storage.Global, scriptManager: f.Storage.Scripts, gameTableManager: f.Storage.Source.Tables);
        var active = (IDictionary<ushort, IQuest>)typeof(NexusForever.Game.Entity.QuestManager)
            .GetField("activeQuests", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(f.Actor.Quests)!;
        active.Add(quest.Id, quest);
        return (quest, () => objectiveCalls, () => achievedCalls);
    }
}
