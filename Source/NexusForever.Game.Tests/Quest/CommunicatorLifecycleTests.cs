using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Challenges;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Entity;
using NexusForever.Game.Quest;
using NexusForever.Game.Static;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Story;
using NexusForever.Script;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Quest;

namespace NexusForever.Game.Tests.Quest;

public class CommunicatorLifecycleTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void CommunicatorHandlerRejectsForeignSessionOrAccount(bool foreignAccount)
    {
        using var f = new Fixture(new CommunicatorMessagesEntry { Id = 10, QuestIdDelivered = 200, Quests = [], States = [] });
        IWorldSession caller;
        if (foreignAccount)
        {
            caller = (IWorldSession)f.Player.Session;
            f.SessionProxy.SetProperty(nameof(IWorldSession.Account), RecordingDispatchProxy<IAccount>.Create(out _));
        }
        else
        {
            caller = RecordingDispatchProxy<IWorldSession>.Create(out var cp);
            cp.SetProperty(nameof(IWorldSession.Player), f.Player); cp.SetProperty(nameof(IWorldSession.Account), f.Player.Account);
        }
        var report = new ClientCommunicatorAction();
        typeof(ClientCommunicatorAction).GetProperty(nameof(ClientCommunicatorAction.QuestId))!.SetValue(report, 200u);
        typeof(ClientCommunicatorAction).GetProperty(nameof(ClientCommunicatorAction.Engaged))!.SetValue(report, true);
        new ClientCommunicatorActionHandler(NullLogger<ClientCommunicatorActionHandler>.Instance).HandleMessage(caller, report);
        Assert.Null(f.Manager.GetQuestState(200));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void PublicationReentrancyCannotMentionAQuestAfterItsOwnerChanges(bool busy)
    {
        using var f = new Fixture(CommunicatorSchedulingTests.Row(10, 1, 200));
        f.Manager.QuestAdd(f.Infos[1]);
        f.SessionProxy.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerCommunicatorMessage)
            {
                if (busy) f.InventoryProxy.SetProperty(nameof(IInventory.IsSaveBlocked), true);
                else f.PlayerProxy.SetProperty(nameof(IPlayer.Session), RecordingDispatchProxy<IGameSession>.Create(out _));
            }
            return null;
        });
        f.Manager.Update(0);
        Assert.Null(f.Manager.GetQuestState(200)); Assert.True(f.Manager.RequiresAuthoritativeReload);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ClientReportMentionRetainsItsSaveIntentThroughRollbackAndRelog(bool rollbackFirst)
    {
        var row = new CommunicatorMessagesEntry { Id = 10, QuestIdDelivered = 200, Quests = [], States = [] };
        using var f = new Fixture(row);
        f.Manager.QuestCommunicatorAction(200, true); Assert.Equal(QuestState.Mentioned, f.Manager.GetQuestState(200));
        using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<CharacterContext>().UseSqlite(connection).Options;
        using (var seed = new CharacterContext(options))
        {
            await seed.Database.EnsureCreatedAsync();
            seed.Character.Add(new CharacterModel { Id = 100, AccountId = 12, Name = "Communicator", OriginalName = "Communicator", CreateTime = DateTime.UtcNow, PathActivatedTimestamp = DateTime.UtcNow });
            await seed.SaveChangesAsync();
        }
        if (rollbackFirst)
        {
            using var attempt = new CharacterContext(options); using var transaction = await attempt.Database.BeginTransactionAsync();
            f.Manager.Save(attempt); await attempt.SaveChangesAsync(); await transaction.RollbackAsync();
        }
        using (var commit = new CharacterContext(options)) { f.Manager.Save(commit); await commit.SaveChangesAsync(); }
        using var read = new CharacterContext(options);
        var restored = read.Character.AsNoTracking().Include(c => c.Quest).ThenInclude(q => q.QuestObjective).Single();
        Assert.Equal((byte)QuestState.Mentioned, Assert.Single(restored.Quest).State);
        f.Reload(restored); Assert.Equal(QuestState.Mentioned, f.Manager.GetQuestState(200));
        f.Manager.QuestCommunicatorAction(200, true); f.Manager.Update(1);
        Assert.Empty(f.Packets<ServerCommunicatorMessage>());
    }

    [Theory]
    [InlineData(0u)] [InlineData(1u)]
    public void RealQuestStateQueuesCallsUntilItIsFiledAndQuestPacketsAreSent(uint flags)
    {
        using var f = new Fixture(CommunicatorSchedulingTests.Row(10, 1, 200, flags));
        f.Manager.QuestAdd(f.Infos[1]);
        Assert.Null(f.Manager.GetQuestState(200)); Assert.Empty(f.Packets<ServerCommunicatorMessage>());
        f.Manager.Update(0);
        Assert.Equal(QuestState.Mentioned, f.Manager.GetQuestState(200));
        Assert.Equal(flags == 1 ? 1 : 0, f.Packets<ServerCommunicatorMessage>().Length);
        var packets = f.Packets<object>();
        if (flags == 1)
        {
            Assert.True(Array.FindIndex(packets, p => p is ServerQuestStateChange { QuestId: 1 }) < Array.FindIndex(packets, p => p is ServerCommunicatorMessage));
            Assert.True(Assert.Single(f.Packets<ServerCommunicatorMessage>()).CheckConditions);
        }
        f.Manager.Update(1); Assert.Equal(flags == 1 ? 1 : 0, f.Packets<ServerCommunicatorMessage>().Length);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void RealObjectiveCompletionQueuesOneCallAndExportsItsCompletionBits(bool byType)
    {
        using var f = new Fixture(CommunicatorSchedulingTests.Row(10, 5));
        f.Manager.QuestAdd(f.Infos[1]); f.Manager.Update(0); Assert.Empty(f.Packets<ServerCommunicatorMessage>());
        if (byType) f.Manager.ObjectiveUpdate(QuestObjectiveType.KillCreature, 77, 1);
        else f.Manager.ObjectiveUpdate(101u, 1);
        Assert.Empty(f.Packets<ServerCommunicatorMessage>());
        Assert.Equal((byte)1, f.Manager.GetCommunicatorSnapshot(1)!.Value.CompletedObjectives);
        f.Manager.Update(0); Assert.Single(f.Packets<ServerCommunicatorMessage>());
        f.Manager.ObjectiveUpdate(101u, 1); f.Manager.Update(1); Assert.Single(f.Packets<ServerCommunicatorMessage>());
    }

    [Fact]
    public void CompletingRequiredObjectivesAlsoArmsTheOptionalObjectiveCall()
    {
        using var f = new Fixture(CommunicatorSchedulingTests.Row(10, 6));
        f.Infos[1].Objectives[1].Entry.Flags = (uint)QuestObjectiveFlags.Optional;
        f.Manager.QuestAdd(f.Infos[1]); f.Manager.ObjectiveUpdate(101u, 1);
        Assert.Equal(QuestState.Achieved, f.Manager.GetQuestState(1));
        Assert.Equal((byte)3, f.Manager.GetCommunicatorSnapshot(1)!.Value.CompletedObjectives);
        Assert.Empty(f.Packets<ServerCommunicatorMessage>());
        f.Manager.Update(0); Assert.Single(f.Packets<ServerCommunicatorMessage>());
    }

    [Theory]
    [InlineData("loading")] [InlineData("inventory")] [InlineData("reward")] [InlineData("normal-save")]
    public void ProvisionalOwnersHoldCallsUntilTheirBarrierEnds(string barrier)
    {
        using var f = new Fixture(CommunicatorSchedulingTests.Row(10, 1));
        f.Manager.QuestAdd(f.Infos[1]); IDisposable normal = null;
        if (barrier == "loading") f.PlayerProxy.SetProperty(nameof(IPlayer.IsLoading), true);
        if (barrier == "inventory") f.InventoryProxy.SetProperty(nameof(IInventory.IsSaveBlocked), true);
        if (barrier == "reward") f.ChallengeProxy.SetProperty(nameof(IChallengeManager.HasPendingRewardOperation), true);
        if (barrier == "normal-save") normal = f.Manager.CaptureNormalSave("communicator-lifecycle");
        f.Manager.Update(1); Assert.Empty(f.Packets<ServerCommunicatorMessage>());
        f.PlayerProxy.SetProperty(nameof(IPlayer.IsLoading), false); f.InventoryProxy.SetProperty(nameof(IInventory.IsSaveBlocked), false);
        f.ChallengeProxy.SetProperty(nameof(IChallengeManager.HasPendingRewardOperation), false); normal?.Dispose();
        f.Manager.Update(0); Assert.Single(f.Packets<ServerCommunicatorMessage>());
    }

    [Theory]
    [InlineData("session")] [InlineData("account")] [InlineData("realm")] [InlineData("manager")] [InlineData("invalidated")]
    public void ReplacedOrInvalidatedActorsCannotPublishQueuedCalls(string replacement)
    {
        using var f = new Fixture(CommunicatorSchedulingTests.Row(10, 1)); f.Manager.QuestAdd(f.Infos[1]);
        if (replacement == "session") f.PlayerProxy.SetProperty(nameof(IPlayer.Session), RecordingDispatchProxy<IGameSession>.Create(out _));
        if (replacement == "account") ((RecordingDispatchProxy<IAccount>)(object)f.Player.Account).SetProperty(nameof(IAccount.Id), 99u);
        if (replacement == "realm") f.PlayerProxy.SetProperty(nameof(IPlayer.Identity), new Identity { RealmId = 3, Id = 100 });
        if (replacement == "manager") f.PlayerProxy.SetProperty(nameof(IPlayer.QuestManager), RecordingDispatchProxy<IQuestManager>.Create(out _));
        if (replacement == "invalidated") f.Manager.InvalidateRewardOperations();
        f.Manager.Update(1); Assert.Empty(f.Packets<ServerCommunicatorMessage>());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ClientReportRecordsOnlyAnEngagedCurrentlyOfferedQuest(bool engaged)
    {
        var row = new CommunicatorMessagesEntry { Id = 10, QuestIdDelivered = 200, Quests = [200], States = [11], WorldZoneId = 2 };
        using var f = new Fixture(row);
        var message = new ClientCommunicatorAction();
        typeof(ClientCommunicatorAction).GetProperty(nameof(ClientCommunicatorAction.QuestId))!.SetValue(message, 200u);
        typeof(ClientCommunicatorAction).GetProperty(nameof(ClientCommunicatorAction.Engaged))!.SetValue(message, engaged);
        new ClientCommunicatorActionHandler(NullLogger<ClientCommunicatorActionHandler>.Instance).HandleMessage((IWorldSession)f.Player.Session, message);
        Assert.Equal(engaged ? QuestState.Mentioned : null, f.Manager.GetQuestState(200));
        if (engaged)
        {
            f.PlayerProxy.SetProperty(nameof(IPlayer.Zone), new WorldZoneEntry { Id = 9 });
            f.Manager.QuestCommunicatorAction(200, true);
            f.Manager.QuestAdd(200, null); Assert.Equal(QuestState.Accepted, f.Manager.GetQuestState(200));
        }
        Assert.Empty(f.Packets<ServerCommunicatorMessage>());
    }

    [Theory]
    [InlineData("race")] [InlineData("disabled")] [InlineData("flag")] [InlineData("world")] [InlineData("busy")]
    public void ClientReportCannotInventAnUnavailableQuest(string gate)
    {
        var row = new CommunicatorMessagesEntry { Id = 10, QuestIdDelivered = 200, Quests = [], States = [] };
        if (gate == "race") row.RaceId = 2;
        if (gate == "world") row.WorldId = 99;
        using var f = new Fixture(row);
        if (gate == "disabled") f.DisableProxy.SetMethodReturn(nameof(IDisableManager.IsDisabled), true);
        if (gate == "flag") f.Infos[200].Entry.Flags = 0x10000;
        if (gate == "busy") f.InventoryProxy.SetProperty(nameof(IInventory.IsSaveBlocked), true);
        f.Manager.QuestCommunicatorAction(200, true); Assert.Null(f.Manager.GetQuestState(200));
    }

    private sealed class Fixture : IDisposable
    {
        internal IPlayer Player;
        internal RecordingDispatchProxy<IPlayer> PlayerProxy;
        internal RecordingDispatchProxy<IInventory> InventoryProxy;
        internal RecordingDispatchProxy<IChallengeManager> ChallengeProxy;
        internal RecordingDispatchProxy<IDisableManager> DisableProxy;
        internal RecordingDispatchProxy<IWorldSession> SessionProxy;
        internal Dictionary<ushort, IQuestInfo> Infos = [];
        internal QuestManager Manager;
        private IGlobalQuestManager global;
        private IGameTableManager tables;
        private IDisableManager disables;
        private IPrerequisiteManager prerequisites;
        internal Fixture(params CommunicatorMessagesEntry[] rows)
        {
            global = RecordingDispatchProxy<IGlobalQuestManager>.Create(out var gp);
            tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tp);
            var objectives = new[] { new QuestObjectiveEntry { Id = 101, Count = 1, Type = (uint)QuestObjectiveType.KillCreature, Data = 77 },
                new QuestObjectiveEntry { Id = 102, Count = 1, Type = (uint)QuestObjectiveType.KillCreature, Data = 78 } };
            tp.SetProperty(nameof(IGameTableManager.QuestObjective), PlayerBindPointTests.Table(objectives));
            foreach (ushort id in new ushort[] { 1, 200 })
                Infos[id] = new QuestInfo(new Quest2Entry { Id = id, Flags = 4, QuestPlayerFactionEnum = 0, Objectives = [101, 102], PushedItemIds = [], PushedItemCounts = [] }, global, tables);
            var index = CommunicatorIndex.Build(rows, id => Infos.TryGetValue(id, out var info) ? new CommunicatorQuestInfo((byte)info.Objectives.Count, false, info.Entry.Flags) : null);
            gp.SetProperty(nameof(IGlobalQuestManager.CommunicatorIndex), index);
            gp.SetMethodHandler(nameof(IGlobalQuestManager.GetQuestInfo), a => Infos.GetValueOrDefault((ushort)a[0]));
            gp.SetMethodHandler(nameof(IGlobalQuestManager.GetQuestCommunicatorMessages), a => index.Messages.Values.Where(m => m.QuestId == (ushort)a[0]).ToArray());
            gp.SetMethodReturn(nameof(IGlobalQuestManager.GetQuestGivers), Array.Empty<uint>());
            gp.SetMethodReturn(nameof(IGlobalQuestManager.GetQuestReceivers), Array.Empty<uint>());
            var session = RecordingDispatchProxy<IWorldSession>.Create(out SessionProxy);
            var account = RecordingDispatchProxy<IAccount>.Create(out var ap); ap.SetProperty(nameof(IAccount.Id), 12u); ap.SetProperty(nameof(IAccount.Session), session);
            var inventory = RecordingDispatchProxy<IInventory>.Create(out InventoryProxy); InventoryProxy.SetMethodReturn(nameof(IInventory.GetInventorySlotsRemaining), 20u);
            var challenges = RecordingDispatchProxy<IChallengeManager>.Create(out ChallengeProxy);
            Player = RecordingDispatchProxy<IPlayer>.Create(out PlayerProxy);
            SessionProxy.SetProperty(nameof(IWorldSession.Account), account); SessionProxy.SetProperty(nameof(IWorldSession.Player), Player);
            PlayerProxy.SetProperty(nameof(IPlayer.CharacterId), 100ul); PlayerProxy.SetProperty(nameof(IPlayer.Identity), new Identity { RealmId = 2, Id = 100 });
            PlayerProxy.SetProperty(nameof(IPlayer.Session), session); PlayerProxy.SetProperty(nameof(IPlayer.Account), account); PlayerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
            PlayerProxy.SetProperty(nameof(IPlayer.ChallengeManager), challenges); PlayerProxy.SetProperty(nameof(IPlayer.Level), 10u); PlayerProxy.SetProperty(nameof(IPlayer.Faction1), Faction.Exile);
            var map = RecordingDispatchProxy<IBaseMap>.Create(out var mp); mp.SetProperty(nameof(IBaseMap.Entry), new WorldEntry { Id = 1 }); PlayerProxy.SetProperty(nameof(IPlayer.Map), map);
            PlayerProxy.SetProperty(nameof(IPlayer.Zone), new WorldZoneEntry { Id = 2 });
            PlayerProxy.SetMethodReturn(nameof(IPlayer.GetVisibleCreature), Array.Empty<WorldEntity>());
            disables = RecordingDispatchProxy<IDisableManager>.Create(out DisableProxy);
            prerequisites = RecordingDispatchProxy<IPrerequisiteManager>.Create(out var pp); pp.SetMethodReturn(nameof(IPrerequisiteManager.Meets), true);
            Reload(new CharacterModel());
        }
        internal void Reload(CharacterModel model)
        {
            Manager?.Dispose();
            Manager = new QuestManager(Player, model, disables, RecordingDispatchProxy<IAssetManager>.Create(out _), prerequisites, global,
                RecordingDispatchProxy<IScriptManager>.Create(out _), tables);
            PlayerProxy.SetProperty(nameof(IPlayer.QuestManager), Manager);
        }
        internal T[] Packets<T>() => SessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Select(c => c.Arguments[0]).OfType<T>().ToArray();
        public void Dispose() => Manager.Dispose();
    }
}
