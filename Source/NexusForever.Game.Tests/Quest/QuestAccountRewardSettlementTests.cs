using System.Collections.Immutable;
using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
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
    [InlineData("commit")] [InlineData("rollback")] [InlineData("auth-refusal")]
    [InlineData("replacement")] [InlineData("unready")]
    public Task PublicTurnInCannotCreditAuthBeforeItsCharacterQuestCommits(string outcome) => Run(outcome);

    internal static async Task Run(string outcome, IGameTableManager actualTables = null, Quest2RewardEntry[] actualAwards = null)
    {
        bool rejectCharacter = outcome == "rollback";
        var awards = actualAwards ?? [new Quest2RewardEntry { Id = 70001, Quest2Id = 9001,
            Quest2RewardTypeId = (uint)QuestRewardType.AccountCurrency, ObjectId = 14, ObjectAmount = 10 }];
        ushort questId = checked((ushort)awards[0].Quest2Id);
        var amounts = awards.GroupBy(row => row.ObjectId).ToDictionary(group => group.Key,
            group => group.Aggregate(0ul, (sum, row) => checked(sum + row.ObjectAmount)));
        using var store = new Store(null); store.Seed();
        var info = (RecordingDispatchProxy<IQuestInfo>)(object)store.Info;
        info.SetProperty(nameof(IQuestInfo.Entry), new Quest2Entry { Id = questId, PushedItemIds = [], PushedItemCounts = [] });
        info.SetProperty(nameof(IQuestInfo.Objectives), ImmutableList<IQuestObjectiveInfo>.Empty);
        info.SetProperty(nameof(IQuestInfo.Rewards), awards.ToImmutableDictionary(row => row.Id));
        info.SetMethodReturn(nameof(IQuestInfo.IsCommunicatorReceived), true);
        ((RecordingDispatchProxy<IGlobalQuestManager>)(object)store.Global).SetMethodReturn(nameof(IGlobalQuestManager.GetQuestReceivers), Array.Empty<uint>());
        ((RecordingDispatchProxy<IGlobalQuestManager>)(object)store.Global).SetMethodReturn(nameof(IGlobalQuestManager.GetQuestGivers), Array.Empty<uint>());
        var tables = (RecordingDispatchProxy<IGameTableManager>)(object)store.Source.Tables;
        tables.SetProperty(nameof(IGameTableManager.AccountCurrencyType), actualTables?.AccountCurrencyType
            ?? PlayerBindPointTests.Table(new AccountCurrencyTypeEntry { Id = 14 }));
        using var source = store.Context();
        var connection = source.Database.GetDbConnection().ConnectionString;
        (await source.Character.SingleAsync()).AccountId = 12;
        source.CharacterQuestObjective.RemoveRange(await source.CharacterQuestObjective.ToArrayAsync());
        source.CharacterQuest.RemoveRange(await source.CharacterQuest.ToArrayAsync());
        await source.SaveChangesAsync();
        source.CharacterQuest.Add(new() { Id = 42, QuestId = questId, State = (byte)QuestState.Achieved });
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
        var database = new HeldDatabase(store, rejectCharacter);
        database.Initialise(new DatabaseConnectionString { Provider = DatabaseProvider.Sqlite, ConnectionString = connection });
        var items = RecordingDispatchProxy<IItemManager>.Create(out _);
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
        var queue = (ConcurrentQueue<ServerGamePacket>)typeof(GameSession).GetField("outgoingPackets", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(session)!;
        queue.Clear();
        Assert.Equal(QuestState.Achieved, actor.Quests.GetQuestState(questId));
        new ClientQuestCompleteHandler().HandleMessage(session, Request(questId));
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
            Assert.Equal(QuestState.Achieved, (QuestState)(await beforeCharacter.CharacterQuest.SingleAsync()).State);
            using var beforeAuth = auth.CreateContext();
            Assert.Empty(await beforeAuth.AccountCurrency.AsNoTracking().ToArrayAsync());
            Assert.Empty(Completions());
            if (outcome == "replacement") session.Player = RecordingDispatchProxy<IPlayer>.Create(out _);
        }
        finally { database.Release.TrySetResult(); await actor.Drain(allowErrors: rejectCharacter); }
        using var character = store.Context(); using var paid = auth.CreateContext();
        Assert.Equal(rejectCharacter ? QuestState.Achieved : QuestState.Completed, (QuestState)(await character.CharacterQuest.SingleAsync()).State);
        if (outcome == "commit")
            Assert.False(session.IsDisconnecting, $"Committed quest disconnected: source={await character.CharacterCraftingAccountOutbox.CountAsync()}, delivered={await character.CharacterCraftingAccountOutbox.CountAsync(row => row.Delivered)}, receipts={await paid.AccountCraftingReceipt.CountAsync()}, errors={string.Join(" | ", actor.Errors.Select(error => error.ToString()))}");
        Assert.Equal(outcome == "commit" ? 1 : 0, Completions().Count());
        if (outcome != "commit") Assert.Empty(await paid.AccountCurrency.AsNoTracking().ToArrayAsync());
        if (rejectCharacter) { Assert.Empty(character.CharacterCraftingAccountOutbox); return; }
        var retained = await character.CharacterCraftingAccountOutbox.AsNoTracking().SingleAsync();
        var intent = CraftingAccountIntent.Restore(retained);
        if (outcome is "auth-refusal" or "replacement")
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
            new ClientQuestCompleteHandler().HandleMessage(session, Request(questId));
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

    private sealed class HeldDatabase(Store store, bool reject) : CharacterDatabase
    {
        internal readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async Task SaveAtomic(ulong character, Action<CharacterContext> save)
        {
            Entered.TrySetResult(); await Release.Task;
            if (reject) throw new IOException("Owned ordinary-quest Character refusal.");
            await CharacterAtomicSave.Execute(() => store.Context(), character, save);
        }
    }

    private static ClientQuestComplete Request(ushort questId)
    {
        using var stream = new MemoryStream(); using var writer = new GamePacketWriter(stream);
        writer.Write(questId, 15); writer.Write((ushort)0, 15); writer.Write(true); writer.FlushBits();
        using var reader = new GamePacketReader(new MemoryStream(stream.ToArray())); var value = new ClientQuestComplete(); value.Read(reader); return value;
    }
}
