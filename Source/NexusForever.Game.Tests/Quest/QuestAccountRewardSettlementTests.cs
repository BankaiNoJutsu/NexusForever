using System.Collections.Immutable;
using System.Collections.Concurrent;
using System.Collections;
using System.Data.Common;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Cryptography;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Character;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Account.Currency;
using NexusForever.Game.Account.Inventory;
using NexusForever.Game.Crafting;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Tutorial;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.Packet;
using NexusForever.Network.World.Message.Model;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Quest;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using Store = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Quest;

public class QuestAccountRewardSettlementTests
{
    [Theory]
    [InlineData("direct", "commit")] [InlineData("direct", "rollback")]
    [InlineData("tutorial", "commit")] [InlineData("tutorial", "rollback")]
    [InlineData("tutorial", "auth-refusal")] [InlineData("tutorial", "replacement")]
    [InlineData("mixed", "commit")] [InlineData("mixed", "rollback")]
    [InlineData("mixed", "auth-refusal")] [InlineData("mixed", "replacement")]
    [InlineData("selected", "commit")] [InlineData("selected", "rollback")]
    [InlineData("selected", "auth-refusal")] [InlineData("selected", "replacement")]
    [InlineData("auto", "commit")] [InlineData("auto", "rollback")]
    [InlineData("auto", "auth-refusal")] [InlineData("auto", "replacement")]
    [InlineData("ordinary", "rollback-retry")] [InlineData("ordinary", "sql-rollback")]
    [InlineData("ordinary", "character-lost-ack")] [InlineData("unpaired", "reject")]
    public Task CompletionVariantsShareTheirCharacterAndAccountBoundary(string variant, string outcome)
        => Run(outcome, variant: variant);

    [Theory]
    [InlineData("commit")] [InlineData("rollback")] [InlineData("auth-refusal")]
    [InlineData("replacement")] [InlineData("unready")]
    public Task PublicTurnInCannotCreditAuthBeforeItsCharacterQuestCommits(string outcome) => Run(outcome);

    internal static async Task Run(string outcome, IGameTableManager actualTables = null, Quest2RewardEntry[] actualAwards = null,
        string variant = "ordinary")
    {
        bool rejectCharacter = outcome is "rollback" or "rollback-retry" or "sql-rollback";
        var awards = actualAwards ?? [new Quest2RewardEntry { Id = 70001, Quest2Id = 9001,
            Quest2RewardTypeId = (uint)QuestRewardType.AccountCurrency, ObjectId = 14, ObjectAmount = 10 }];
        if (variant == "tutorial") awards[0].Quest2Id = StarterTutorialDefinition.ExileMovementQuestId;
        ushort questId = checked((ushort)awards[0].Quest2Id);
        var amounts = awards.GroupBy(row => row.ObjectId).ToDictionary(group => group.Key,
            group => group.Aggregate(0ul, (sum, row) => checked(sum + row.ObjectAmount)));
        if (variant == "mixed") awards = [..awards, new() { Id = 70002, Quest2Id = questId,
            Quest2RewardTypeId = (uint)QuestRewardType.Item, ObjectId = 7001, ObjectAmount = 1 }];
        if (variant == "selected")
        {
            awards[0].Flags = 1;
            awards = [..awards, new() { Id = 70002, Quest2Id = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.AccountCurrency, ObjectId = 9, ObjectAmount = 99, Flags = 1 }];
        }
        ushort selection = variant == "selected" ? (ushort)1 : (ushort)0;
        using var store = new Store(null); store.Seed();
        var info = (RecordingDispatchProxy<IQuestInfo>)(object)store.Info;
        info.SetProperty(nameof(IQuestInfo.Entry), new Quest2Entry { Id = questId, QuestPlayerFactionEnum = 2, PushedItemIds = [], PushedItemCounts = [] });
        info.SetProperty(nameof(IQuestInfo.Objectives), ImmutableList<IQuestObjectiveInfo>.Empty);
        info.SetProperty(nameof(IQuestInfo.Rewards), awards.ToImmutableDictionary(row => row.Id));
        info.SetMethodReturn(nameof(IQuestInfo.IsCommunicatorReceived), true);
        info.SetMethodReturn(nameof(IQuestInfo.IsAutoComplete), variant == "auto");
        info.SetProperty(nameof(IQuestInfo.PrerequisiteQuests), ImmutableList<Quest2Entry>.Empty);
        ((RecordingDispatchProxy<IGlobalQuestManager>)(object)store.Global).SetMethodHandler(nameof(IGlobalQuestManager.GetQuestInfo),
            args => (ushort)args[0] == questId ? store.Info : null);
        ((RecordingDispatchProxy<IGlobalQuestManager>)(object)store.Global).SetMethodReturn(nameof(IGlobalQuestManager.GetQuestReceivers), Array.Empty<uint>());
        ((RecordingDispatchProxy<IGlobalQuestManager>)(object)store.Global).SetMethodReturn(nameof(IGlobalQuestManager.GetQuestGivers), Array.Empty<uint>());
        var call = RecordingDispatchProxy<ICommunicatorMessage>.Create(out var offer);
        offer.SetProperty(nameof(ICommunicatorMessage.Reachable), true);
        offer.SetMethodReturn(nameof(ICommunicatorMessage.Evaluate), CommunicatorResult.Eligible);
        ((RecordingDispatchProxy<IGlobalQuestManager>)(object)store.Global).SetMethodReturn(nameof(IGlobalQuestManager.GetQuestCommunicatorMessages), new[] { call });
        var tables = (RecordingDispatchProxy<IGameTableManager>)(object)store.Source.Tables;
        tables.SetProperty(nameof(IGameTableManager.AccountCurrencyType), actualTables?.AccountCurrencyType
            ?? PlayerBindPointTests.Table(new AccountCurrencyTypeEntry { Id = 14 }));
        using var source = store.Context();
        var connection = source.Database.GetDbConnection().ConnectionString;
        (await source.Character.SingleAsync()).AccountId = 12;
        source.CharacterQuestObjective.RemoveRange(await source.CharacterQuestObjective.ToArrayAsync());
        source.CharacterQuest.RemoveRange(await source.CharacterQuest.ToArrayAsync());
        await source.SaveChangesAsync();
        if (variant != "auto") source.CharacterQuest.Add(new() { Id = 42, QuestId = questId, State = (byte)QuestState.Achieved });
        await source.SaveChangesAsync();
        var auth = new AwardDatabase { RejectAwards = outcome == "auth-refusal" };
        auth.Initialise(new DatabaseConnectionString { Provider = DatabaseProvider.Sqlite,
            ConnectionString = $"Data Source={Path.Combine(Path.GetDirectoryName(source.Database.GetDbConnection().DataSource)!, "quest-account-auth.sqlite")};Pooling=False" });
        var model = new AccountModel { Id = 12, Email = "quest-12@example.test", SessionKey = "00112233445566778899AABBCCDDEEFF" };
        using (var seed = auth.CreateContext())
        {
            await seed.Database.EnsureCreatedAsync(); seed.Account.Add(model);
            seed.AccountWorldSession.Add(new() { AccountId = 12, Epoch = 1 }); await seed.SaveChangesAsync();
        }
        var messages = RecordingDispatchProxy<IMessageManager>.Create(out var mp);
        mp.SetMethodHandler(nameof(IMessageManager.GetOpcode), args => args[0].GetType().GetCustomAttribute<MessageAttribute>()?.Opcode);
        var session = new WorldSession(messages, null, null, null);
        typeof(GameSession).GetField("encryption", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(session, new PacketCrypt(123ul));
        var account = (RecordingDispatchProxy<IAccount>)(object)store.Source.Player.Account;
        account.SetProperty(nameof(IAccount.Id), 12u); account.SetProperty(nameof(IAccount.Session), session);
        account.SetProperty(nameof(IAccount.WorldSessionEpoch), 1ul);
        account.SetProperty(nameof(IAccount.CurrencyManager), new AccountCurrencyManager(store.Source.Player.Account, model, store.Source.Tables));
        account.SetProperty(nameof(IAccount.InventoryManager), new AccountInventoryManager(store.Source.Player.Account, model, null,
            gameTableManager: store.Source.Tables, inventoryDatabase: auth));
        var database = new HeldDatabase(store, outcome);
        database.Initialise(new DatabaseConnectionString { Provider = DatabaseProvider.Sqlite, ConnectionString = connection });
        var items = RecordingDispatchProxy<IItemManager>.Create(out var itemManager);
        var itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out var itemDefinition);
        itemDefinition.SetProperty(nameof(IItemInfo.Id), 7001u);
        itemDefinition.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { Id = 7001, MaxStackCount = 20 });
        itemDefinition.SetMethodReturn(nameof(IItemInfo.IsStackable), true);
        itemManager.SetMethodReturn(nameof(IItemManager.GetItemInfo), itemInfo);
        itemManager.SetProperty(nameof(IItemManager.NextItemId), 906ul);
        using var actor = new Actor(store, actorSession: session, actorEvents: session.Events,
            characterDatabase: database, authDatabase: auth,
            inventoryFactory: (owner, _) => BankDeferredCurrencyRecoveryTests.Inventory(owner, items, store.Source.Tables, null));
        actor.Set(nameof(Player.SupplySatchelManager), new SupplySatchelManager(actor.Player, new() { Id = 42 }, store.Source.Tables));
        var wallet = new CurrencyManager(store.Source.Tables); wallet.Initialise(actor.Player, new() { Id = 42 });
        actor.Set(nameof(Player.CurrencyManager), wallet);
        account.SetMethodHandler(nameof(IAccount.Save), args =>
        {
            actor.Player.Account.CurrencyManager.Save((AuthContext)args[0]);
            actor.Player.Account.InventoryManager.Save((AuthContext)args[0]); return null;
        });
        session.Player = actor.Player;
        if (outcome != "unready") await actor.Load();
        if (variant == "tutorial")
        {
            var map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);
            mapProxy.SetProperty(nameof(IBaseMap.Entry), new WorldEntry { Id = 3460 }); actor.Set(nameof(Player.Map), map);
            mapProxy.SetMethodReturn(nameof(IBaseMap.Search), Array.Empty<IGridEntity>());
            tables.SetProperty(nameof(IGameTableManager.WorldLocation2), PlayerBindPointTests.Table<WorldLocation2Entry>());
            typeof(Player).GetField("gameTableManager", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(actor.Player, store.Source.Tables);
            var visible = typeof(GridEntity).GetField("visibleEntities", BindingFlags.NonPublic | BindingFlags.Instance)!;
            visible.SetValue(actor.Player, Activator.CreateInstance(visible.FieldType));
        }
        var queue = (ConcurrentQueue<ServerGamePacket>)typeof(GameSession).GetField("outgoingPackets", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(session)!;
        queue.Clear();
        Assert.Equal(variant == "auto" ? null : QuestState.Achieved, actor.Quests.GetQuestState(questId));
        if (variant == "unpaired")
        {
            Assert.Throws<InvalidOperationException>(() => actor.Quests.SaveOrdinaryQuestMutation(actor.Player, session,
                () => { actor.Quests.QuestComplete(questId, selection, true); return true; }, null));
            Assert.Equal(QuestState.Achieved, actor.Quests.GetQuestState(questId));
            using var rejected = auth.CreateContext(); Assert.Empty(rejected.AccountCurrency); Assert.Empty(rejected.AccountCraftingReceipt);
            Assert.False(database.Entered.Task.IsCompleted); return;
        }
        if (variant == "tutorial") actor.Player.TryRecoverStarterTutorialQuestProgression();
        else if (variant == "direct")
        {
            actor.Quests.QuestComplete(questId, selection, true);
            if (!actor.Player.IsNormalSaveInProgress) actor.Player.Save();
        }
        else new ClientQuestCompleteHandler().HandleMessage(session, Request(questId, selection));
        if (outcome == "unready")
        {
            try
            {
                Assert.False(actor.Player.IsNormalSaveInProgress); Assert.False(database.Entered.Task.IsCompleted);
                Assert.Equal(QuestState.Achieved, actor.Quests.GetQuestState(questId));
                using var unready = auth.CreateContext(); Assert.Empty(unready.AccountCraftingReceipt); Assert.Empty(unready.AccountCurrency);
                return;
            }
            finally { database.Release.TrySetResult(); await actor.Drain(allowErrors: true); }
        }
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try
        {
            while (!database.Entered.Task.IsCompleted) { session.Events.Update(.01); await Task.Delay(5, timeout.Token); }
            using var beforeCharacter = store.Context();
            if (variant == "auto") Assert.Empty(beforeCharacter.CharacterQuest);
            else Assert.Equal(QuestState.Achieved, (QuestState)(await beforeCharacter.CharacterQuest.SingleAsync()).State);
            Assert.Empty(beforeCharacter.Item);
            using var beforeAuth = auth.CreateContext();
            Assert.Empty(await beforeAuth.AccountCurrency.AsNoTracking().ToArrayAsync());
            Assert.Empty(Completions());
            if (outcome == "replacement") session.Player = RecordingDispatchProxy<IPlayer>.Create(out _);
        }
        finally { database.Release.TrySetResult(); await actor.Drain(allowErrors: rejectCharacter || outcome == "character-lost-ack"); }
        using var character = store.Context(); using var paid = auth.CreateContext();
        if (variant == "auto" && rejectCharacter) Assert.Empty(character.CharacterQuest);
        else Assert.Equal(rejectCharacter ? QuestState.Achieved : QuestState.Completed, (QuestState)(await character.CharacterQuest.SingleAsync()).State);
        Assert.Equal(variant == "mixed" && !rejectCharacter ? 1 : 0, await character.Item.CountAsync(row => row.ItemId == 7001));
        if (outcome == "commit")
            Assert.False(session.IsDisconnecting, $"Committed quest disconnected: source={await character.CharacterCraftingAccountOutbox.CountAsync()}, delivered={await character.CharacterCraftingAccountOutbox.CountAsync(row => row.Delivered)}, receipts={await paid.AccountCraftingReceipt.CountAsync()}, errors={string.Join(" | ", actor.Errors.Select(error => error.ToString()))}");
        Assert.Equal(outcome == "commit" ? 1 : 0, Completions().Count());
        if (outcome != "commit") Assert.Empty(await paid.AccountCurrency.AsNoTracking().ToArrayAsync());
        if (outcome is "sql-rollback" or "character-lost-ack") Assert.True(database.Fault.ObservedFlushedCompletion);
        if (rejectCharacter)
        {
            Assert.Empty(character.CharacterCraftingAccountOutbox);
            if (outcome != "rollback-retry") return;
            using (var admitted = auth.CreateContext())
            { (await admitted.AccountWorldSession.SingleAsync()).Epoch = 2; await admitted.SaveChangesAsync(); }
            var fresh = new AccountModel { Id = 12, WorldSessionEpoch = 2 };
            await CraftingAccountRecovery.RecoverAtAdmissionAsync(fresh, auth, database, 1, store.Source.Tables);
            Assert.True((await paid.AccountCraftingReceipt.AsNoTracking().SingleAsync()).Cancelled);
            database.Reject = false;
            var retrySession = new WorldSession(messages, null, null, null);
            typeof(GameSession).GetField("encryption", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(retrySession, new PacketCrypt(123ul));
            account.SetProperty(nameof(IAccount.Session), retrySession); account.SetProperty(nameof(IAccount.WorldSessionEpoch), 2ul);
            account.SetProperty(nameof(IAccount.CurrencyManager), new AccountCurrencyManager(store.Source.Player.Account, fresh, store.Source.Tables));
            account.SetProperty(nameof(IAccount.InventoryManager), new AccountInventoryManager(store.Source.Player.Account, fresh, null,
                gameTableManager: store.Source.Tables, inventoryDatabase: auth));
            using var retryActor = new Actor(store, actorSession: retrySession, actorEvents: retrySession.Events,
                characterDatabase: database, authDatabase: auth,
                inventoryFactory: (owner, _) => BankDeferredCurrencyRecoveryTests.Inventory(owner, items, store.Source.Tables, null));
            var retryWallet = new CurrencyManager(store.Source.Tables); retryWallet.Initialise(retryActor.Player, new() { Id = 42 });
            retryActor.Set(nameof(Player.CurrencyManager), retryWallet);
            retryActor.Set(nameof(Player.SupplySatchelManager), new SupplySatchelManager(retryActor.Player, new() { Id = 42 }, store.Source.Tables));
            account.SetMethodHandler(nameof(IAccount.Save), args =>
            {
                retryActor.Player.Account.CurrencyManager.Save((AuthContext)args[0]);
                retryActor.Player.Account.InventoryManager.Save((AuthContext)args[0]); return null;
            });
            retrySession.Player = retryActor.Player; await retryActor.Load();
            queue = (ConcurrentQueue<ServerGamePacket>)typeof(GameSession).GetField("outgoingPackets", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(retrySession)!;
            queue.Clear();
            Assert.Equal(QuestState.Achieved, retryActor.Quests.GetQuestState(questId));
            new ClientQuestCompleteHandler().HandleMessage(retrySession, Request(questId, selection));
            await retryActor.Drain();
            Assert.Single(Completions()); Assert.False(retrySession.IsDisconnecting);
            Assert.Equal(QuestState.Completed, (QuestState)(await character.CharacterQuest.AsNoTracking().SingleAsync()).State);
            Assert.Equal(10ul, (await paid.AccountCurrency.AsNoTracking().SingleAsync()).Amount);
            Assert.Equal(2, await paid.AccountCraftingReceipt.CountAsync());
            Assert.Equal(1, await character.CharacterCraftingAccountOutbox.CountAsync(row => row.Cancelled));
            Assert.Equal(1, await character.CharacterCraftingAccountOutbox.CountAsync(row => row.Delivered));
            return;
        }
        var retained = await character.CharacterCraftingAccountOutbox.AsNoTracking().SingleAsync();
        var intent = CraftingAccountIntent.Restore(retained);
        if (outcome is "auth-refusal" or "replacement" or "character-lost-ack")
        {
            Assert.False(retained.Delivered);
            if (outcome == "auth-refusal") { Assert.True(auth.Refused); Assert.True(session.IsDisconnecting); }
            auth.RejectAwards = false;
            await new CraftingAccountOutbox(database.CreateContext, auth.CreateContext, 1, store.Source.Tables).DrainAsync(8);
            Assert.True(character.CharacterCraftingAccountOutbox.AsNoTracking().Single().Delivered);
            Assert.Empty(Completions());
        }
        foreach (var (currency, amount) in amounts)
            Assert.Equal(amount, (await paid.AccountCurrency.AsNoTracking().SingleAsync(row => row.CurrencyId == currency)).Amount);
        if (outcome == "commit")
        {
            var currency = (AccountCurrencyType)amounts.Keys.First();
            actor.Player.Account.CurrencyManager.CurrencySubtractAmount(currency, 1);
            await auth.Save(context => actor.Player.Account.CurrencyManager.Save(context));
            new ClientQuestCompleteHandler().HandleMessage(session, Request(questId, selection));
            await actor.Drain();
            Assert.Equal(2, Completions().Count()); // duplicate request resends the authoritative completed state
        }
        await new CraftingAccountDelivery(database.CreateContext, auth.CreateContext, 1).DeliverAsync(intent);
        foreach (var (currency, amount) in amounts)
            Assert.Equal(outcome == "commit" && currency == amounts.Keys.First() ? amount - 1 : amount,
                (await paid.AccountCurrency.AsNoTracking().SingleAsync(row => row.CurrencyId == currency)).Amount);
        Assert.Equal(amounts.Count, await paid.AccountCurrencyOperation.CountAsync(row => !row.Subtract && row.Amount != 0));

        IEnumerable<ServerGamePacket> Completions() => queue.Where(packet =>
        {
            byte[] body = packet.Opcode == GameMessageOpcode.ServerRealmEncrypted
                ? new PacketCrypt(123ul).Decrypt(packet.Data[4..], packet.Data.Length - 4) : [];
            if (body.Length < 2 || (GameMessageOpcode)BitConverter.ToUInt16(body) != GameMessageOpcode.ServerQuestStateChange) return false;
            using var reader = new GamePacketReader(new MemoryStream(body[2..]));
            return reader.ReadUShort(15) == questId && reader.ReadEnum<QuestState>(32) == QuestState.Completed;
        });
    }

    private sealed class AwardDatabase : AuthDatabase
    {
        internal bool RejectAwards, Refused;
        public override AuthContext CreateContext()
        {
            var context = base.CreateContext();
            context.SavingChanges += (_, _) =>
            {
                if (!RejectAwards || !context.ChangeTracker.Entries<AccountCurrencyOperationModel>()
                    .Any(row => row.State == EntityState.Added && row.Entity.OperationKey.StartsWith("craft-award:", StringComparison.Ordinal))) return;
                Refused = true; throw new IOException("Owned quest account award refusal.");
            };
            return context;
        }
    }

    private sealed class HeldDatabase(Store store, string outcome) : CharacterDatabase
    {
        internal bool Reject = outcome is "rollback" or "rollback-retry";
        internal readonly CharacterCommitFault Fault = new(outcome == "character-lost-ack");
        internal readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async Task SaveAtomic(ulong character, Action<CharacterContext> save)
        {
            Entered.TrySetResult(); await Release.Task;
            if (Reject) throw new IOException("Owned ordinary-quest Character refusal.");
            await CharacterAtomicSave.Execute(() => outcome is "sql-rollback" or "character-lost-ack"
                ? store.Context(Fault) : store.Context(), character, save);
        }
    }

    private sealed class CharacterCommitFault(bool after) : DbTransactionInterceptor
    {
        internal bool ObservedFlushedCompletion;
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData data,
            InterceptionResult result, CancellationToken cancellationToken = default)
        {
            var context = (CharacterContext)data.Context!;
            ObservedFlushedCompletion = context.CharacterQuest.AsNoTracking().Any(row => row.State == (byte)QuestState.Completed)
                && context.CharacterCraftingAccountOutbox.AsNoTracking().Any(row => !row.Cancelled);
            Assert.True(ObservedFlushedCompletion);
            if (!after) throw new IOException("Owned quest Character commit refusal after SQL flush.");
            return new(result);
        }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
            => after ? Task.FromException(new IOException("Owned quest Character acknowledgement loss.")) : Task.CompletedTask;
    }

    private static ClientQuestComplete Request(ushort questId, ushort selection = 0)
    {
        using var stream = new MemoryStream(); using var writer = new GamePacketWriter(stream);
        writer.Write(questId, 15); writer.Write(selection, 15); writer.Write(true); writer.FlushBits();
        using var reader = new GamePacketReader(new MemoryStream(stream.ToArray())); var value = new ClientQuestComplete(); value.Read(reader); return value;
    }
}
