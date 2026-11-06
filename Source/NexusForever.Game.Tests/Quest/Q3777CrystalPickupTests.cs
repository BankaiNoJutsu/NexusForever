using System.Security.Cryptography;
using System.Text.Json;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Entity;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Script.Main.Quests.NorthernWilds;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using Store = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;
using GameItem = NexusForever.Game.Entity.Item;

namespace NexusForever.Game.Tests.Quest;

public sealed class Q3777CrystalPickupTests
{
    private readonly IGameTableManager actualTables;
    public Q3777CrystalPickupTests() { }
    private Q3777CrystalPickupTests(IGameTableManager tables) => actualTables = tables;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CrystalPickup_RespectsSequentialConversationAndCreatesOneCanonicalItem(bool conversationDone)
    {
        using var f = new PickupFixture(actualTables, conversationDone);
        var crystal = f.Crystal();
        crystal.Script.OnEnterRange(f.Actor.Player);
        if (!conversationDone)
        {
            Assert.Equal(0u, f.Actor.Player.Inventory.GetItemCount(6998));
            Assert.Empty(crystal.Proxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
            Assert.Equal(0u, f.Objective(5076).Progress);
            f.Quest.ObjectiveUpdate(5075, 1);
            crystal.Script.OnEnterRange(f.Actor.Player);
        }
        Assert.Equal(1u, f.Actor.Player.Inventory.GetItemCount(6998));
        Assert.Equal(1u, f.Objective(5076).Progress);
        Assert.Equal(1u, f.Objective(4859).Progress);
        Assert.Single(crystal.Proxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
        await f.Persist();
        using var read = f.Storage.Context();
        Assert.Equal(1u, Assert.Single(read.Item).StackCount);
        Assert.Equal(1u, read.CharacterQuestObjective.Single(row => row.Index == 2).Progress);
    }

    [Fact]
    public void CrystalPickup_FullInventoryPreservesTheCrystalForALaterPass()
    {
        using var f = new PickupFixture(actualTables, slots: 1);
        f.Actor.Player.Inventory.ItemCreate(InventoryLocation.Inventory, 81917, 1);
        var crystal = f.Crystal();
        crystal.Script.OnEnterRange(f.Actor.Player);
        Assert.Equal(0u, f.Actor.Player.Inventory.GetItemCount(6998));
        Assert.Equal(0u, f.Objective(5076).Progress);
        Assert.Equal(0u, f.Objective(4859).Progress);
        Assert.Empty(crystal.Proxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
        f.Actor.Player.Inventory.ItemDelete(81917, 1);
        crystal.Script.OnEnterRange(f.Actor.Player);
        Assert.Equal(1u, f.Actor.Player.Inventory.GetItemCount(6998));
        Assert.Equal(1u, f.Objective(4859).Progress);
        Assert.Single(crystal.Proxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void CrystalPickup_FrozenInventoryDoesNotConsumeOrAdvanceUntilItsLeaseEnds()
    {
        using var f = new PickupFixture(actualTables);
        var crystal = f.Crystal();
        Assert.True(f.Actor.Player.Inventory.TryAcquireSaveLease(out var lease));
        using (lease)
        {
            crystal.Script.OnEnterRange(f.Actor.Player);
            Assert.Equal(0u, f.Actor.Player.Inventory.GetItemCount(6998));
            Assert.Equal(0u, f.Objective(5076).Progress);
            Assert.Empty(crystal.Proxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
        }
        crystal.Script.OnEnterRange(f.Actor.Player);
        Assert.Equal(1u, f.Actor.Player.Inventory.GetItemCount(6998));
        Assert.Single(crystal.Proxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Theory]
    [InlineData("item-packet")]
    [InlineData("removal")]
    public async Task CrystalPickup_AppliedItemSurvivesFeedbackFailureWithoutAnotherGrant(string failure)
    {
        using var f = new PickupFixture(actualTables);
        var crystal = f.Crystal();
        if (failure == "item-packet")
            ((RecordingDispatchProxy<IGameSession>)(object)f.Actor.Player.Session).SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted),
                args => args[0] is ServerItemAdd ? throw new IOException("Owned pickup item enqueue refusal.") : null);
        int removals = 0;
        if (failure == "removal")
            crystal.Proxy.SetMethodHandler(nameof(IGridEntity.RemoveFromMap), _ =>
            {
                if (++removals == 1) throw new IOException("Owned pickup removal refusal.");
                return null;
            });
        Assert.Null(Record.Exception(() => crystal.Script.OnEnterRange(f.Actor.Player)));
        Assert.Equal(1u, f.Actor.Player.Inventory.GetItemCount(6998));
        Assert.Equal(1u, f.Objective(4859).Progress);
        crystal.Script.OnEnterRange(f.Actor.Player);
        Assert.Equal(1u, f.Actor.Player.Inventory.GetItemCount(6998));
        Assert.Equal(failure == "removal" ? 2 : 1, crystal.Proxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)).Count);
        await f.Persist();
        using var stored = f.Storage.Context();
        Assert.Equal(1u, Assert.Single(stored.Item).StackCount);
        Assert.Equal(1u, stored.CharacterQuestObjective.Single(row => row.Index == 2).Progress);
    }

    [Fact]
    public async Task CrystalPickup_EightDistinctPickupsAchieveAndPersistTheRealItemCount()
    {
        using var f = new PickupFixture(actualTables);
        for (uint i = 0; i < 8; i++)
        {
            var crystal = f.Crystal();
            crystal.Script.OnEnterRange(f.Actor.Player);
            crystal.Script.OnEnterRange(f.Actor.Player);
            Assert.Equal(i + 1, f.Actor.Player.Inventory.GetItemCount(6998));
            Assert.Single(crystal.Proxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
        }
        Assert.Equal(QuestState.Achieved, f.Quest.State);
        await Assert.ThrowsAsync<IOException>(() => f.Persist(true));
        await f.Persist();
        using var stored = f.Storage.Context();
        Assert.Equal(8u, Assert.Single(stored.Item).StackCount);
        Assert.Equal((byte)QuestState.Achieved, Assert.Single(stored.CharacterQuest).State);
        Assert.Equal(8u, stored.CharacterQuestObjective.Single(row => row.Index == 2).Progress);
        using var reloaded = f.Reload();
        Assert.Equal(8u, reloaded.Player.Inventory.GetItemCount(6998));
        Assert.Equal(QuestState.Achieved, reloaded.Quests.GetQuestState(3777));
        var extra = f.Crystal(); extra.Script.OnEnterRange(reloaded.Player);
        Assert.Equal(8u, reloaded.Player.Inventory.GetItemCount(6998));
        Assert.Empty(extra.Proxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void CrystalPickup_WithoutAnActiveQuestDoesNotConsumeOrCreateItems()
    {
        using var f = new PickupFixture(actualTables);
        f.Quest.State = QuestState.Abandoned;
        var crystal = f.Crystal();
        crystal.Script.OnEnterRange(f.Actor.Player);
        Assert.Equal(0u, f.Actor.Player.Inventory.GetItemCount(6998));
        Assert.Empty(crystal.Proxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }


    [Fact]
    public void CrystalPickup_ReentrantItemFeedbackCannotGrantTheSameCrystalTwice()
    {
        using var f = new PickupFixture(actualTables);
        var crystal = f.Crystal();
        ((RecordingDispatchProxy<IGameSession>)(object)f.Actor.Player.Session).SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerItemAdd) crystal.Script.OnEnterRange(f.Actor.Player);
            return null;
        });
        crystal.Script.OnEnterRange(f.Actor.Player);
        Assert.Equal(1u, f.Actor.Player.Inventory.GetItemCount(6998));
        Assert.Equal(1u, f.Objective(4859).Progress);
        Assert.Single(crystal.Proxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void CrystalPickup_OwnerReplacementDuringFeedbackDoesNotConsumeTheNewCrystal()
    {
        using var f = new PickupFixture(actualTables);
        var crystal = f.Crystal();
        var nextOwner = RecordingDispatchProxy<ICreatureEntity>.Create(out var nextProxy);
        nextProxy.SetProperty(nameof(ICreatureEntity.Guid), 9999u);
        nextProxy.SetProperty(nameof(ICreatureEntity.CreatureId), 13120u);
        ((RecordingDispatchProxy<IGameSession>)(object)f.Actor.Player.Session).SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerItemAdd) crystal.Script.OnLoad(nextOwner);
            return null;
        });
        crystal.Script.OnEnterRange(f.Actor.Player);
        Assert.Single(crystal.Proxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
        Assert.Empty(nextProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
        crystal.Script.OnEnterRange(f.Actor.Player);
        Assert.Single(nextProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
        Assert.Equal(2u, f.Actor.Player.Inventory.GetItemCount(6998));
        Assert.Equal(2u, f.Objective(4859).Progress);
    }

    [Fact]
    public void CrystalPickup_AlreadyOwnedFragmentsSyncWithoutGrantingANinthItem()
    {
        using var f = new PickupFixture(actualTables);
        f.Actor.Player.Inventory.ItemCreate(InventoryLocation.Inventory, 6998, 8);
        Assert.Equal(0u, f.Objective(4859).Progress);
        var crystal = f.Crystal(); crystal.Script.OnEnterRange(f.Actor.Player);
        Assert.Equal(8u, f.Actor.Player.Inventory.GetItemCount(6998));
        Assert.Equal(8u, f.Objective(4859).Progress);
        Assert.Equal(QuestState.Achieved, f.Quest.State);
        Assert.Empty(crystal.Proxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void CrystalPickup_CollectionSyncCannotGrantAfterItsOwnerIsReplaced()
    {
        using var f = new PickupFixture(actualTables);
        f.Actor.Player.Inventory.ItemCreate(InventoryLocation.Inventory, 6998, 1);
        f.Quest.ObjectiveUpdate(5076, 1);
        Assert.Equal(4859u, f.Quest.GetCurrentObjectiveId());
        var crystal = f.Crystal();
        var nextOwner = RecordingDispatchProxy<ICreatureEntity>.Create(out var nextProxy);
        nextProxy.SetProperty(nameof(ICreatureEntity.Guid), 9999u);
        nextProxy.SetProperty(nameof(ICreatureEntity.CreatureId), 13120u);
        ((RecordingDispatchProxy<IGameSession>)(object)f.Actor.Player.Session).SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerQuestObjectiveUpdate { QuestId: 3777, QuestObjectiveIndex: 2 })
                crystal.Script.OnLoad(nextOwner);
            return null;
        });
        crystal.Script.OnEnterRange(f.Actor.Player);
        Assert.Equal(1u, f.Actor.Player.Inventory.GetItemCount(6998));
        Assert.Equal(1u, f.Objective(4859).Progress);
        Assert.Empty(crystal.Proxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
        Assert.Empty(nextProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void CrystalPickup_FirstObjectiveFeedbackSeesTheAlreadyAcceptedPhysicalItem()
    {
        using var f = new PickupFixture(actualTables, slots: 1);
        var crystal = f.Crystal();
        bool observed = false;
        ((RecordingDispatchProxy<IGameSession>)(object)f.Actor.Player.Session).SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerQuestObjectiveUpdate { QuestId: 3777, QuestObjectiveIndex: 1 })
            {
                observed = f.Actor.Player.Inventory.GetItemCount(6998) == 1;
                f.Actor.Player.Inventory.ItemCreate(InventoryLocation.Inventory, 81917, 1);
            }
            return null;
        });
        crystal.Script.OnEnterRange(f.Actor.Player);
        Assert.True(observed);
        Assert.Equal(1u, f.Actor.Player.Inventory.GetItemCount(6998));
        Assert.Equal(0u, f.Actor.Player.Inventory.GetItemCount(81917));
        Assert.Equal(1u, f.Objective(5076).Progress);
        Assert.Equal(1u, f.Objective(4859).Progress);
        Assert.Single(crystal.Proxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    // Explicit offline entrypoint: recovered tables stay outside the repository.
    internal static async Task RunActualCatalogAsync(string recoveryRoot, string outputDirectory)
    {
        recoveryRoot = Path.GetFullPath(recoveryRoot);
        outputDirectory = Path.GetFullPath(outputDirectory);
        if (Directory.Exists(outputDirectory))
            throw new IOException("Choose a new output directory to preserve prior evidence.");
        Directory.CreateDirectory(outputDirectory);
        var (manifest, inputs) = ContractActualCatalogQualification.VerifyInputs(recoveryRoot);
        var tables = await ContractActualCatalogQualification.LoadTablesAsync(recoveryRoot);
        await tables.Initialise(new GameTableLoader()
            .AddGameTable<Quest2Entry>().AddGameTable<QuestObjectiveEntry>()
            .AddGameTable<Quest2RewardEntry>().AddGameTable<TargetGroupEntry>().AddGameTable<Creature2Entry>());
        Assert.Equal(new uint[] { 5075, 5076, 4859, 0, 0, 0 }, tables.Quest2.GetEntry(3777).Objectives);
        foreach (var expected in new[] { (5075u, 17u, 0u, 219u, 1u), (5076u, 5u, 3u, 6952u, 1u), (4859u, 4u, 3u, 6998u, 8u) })
        {
            var row = tables.QuestObjective.GetEntry(expected.Item1);
            Assert.Equal((expected.Item2, expected.Item3, expected.Item4, expected.Item5),
                (row.Type, row.Flags, row.Data, row.Count));
        }
        var info = new ItemInfo(tables.Item.GetEntry(6998), tables);
        Assert.Equal(100u, info.Entry.MaxStackCount);
        Assert.Equal(170u, info.Entry.Item2TypeId);
        Assert.True(info.IsStackable());

        var tests = new Q3777CrystalPickupTests(tables);
        var cases = new List<(string Name, Func<Task> Run)>
        {
            ("SequentialConversationIncomplete", () => tests.CrystalPickup_RespectsSequentialConversationAndCreatesOneCanonicalItem(false)),
            ("SequentialConversationComplete", () => tests.CrystalPickup_RespectsSequentialConversationAndCreatesOneCanonicalItem(true)),
            ("FullInventoryRetry", () => { tests.CrystalPickup_FullInventoryPreservesTheCrystalForALaterPass(); return Task.CompletedTask; }),
            ("FrozenInventoryRetry", () => { tests.CrystalPickup_FrozenInventoryDoesNotConsumeOrAdvanceUntilItsLeaseEnds(); return Task.CompletedTask; }),
            ("RejectedItemFeedback", () => tests.CrystalPickup_AppliedItemSurvivesFeedbackFailureWithoutAnotherGrant("item-packet")),
            ("RejectedRemovalRetry", () => tests.CrystalPickup_AppliedItemSurvivesFeedbackFailureWithoutAnotherGrant("removal")),
            ("EightPickupsFailedSaveRetryReload", tests.CrystalPickup_EightDistinctPickupsAchieveAndPersistTheRealItemCount),
            ("InactiveQuest", () => { tests.CrystalPickup_WithoutAnActiveQuestDoesNotConsumeOrCreateItems(); return Task.CompletedTask; }),
            ("ReentrantItemFeedback", () => { tests.CrystalPickup_ReentrantItemFeedbackCannotGrantTheSameCrystalTwice(); return Task.CompletedTask; }),
            ("ReplacementCrystal", () => { tests.CrystalPickup_OwnerReplacementDuringFeedbackDoesNotConsumeTheNewCrystal(); return Task.CompletedTask; }),
            ("PreownedFragments", () => { tests.CrystalPickup_AlreadyOwnedFragmentsSyncWithoutGrantingANinthItem(); return Task.CompletedTask; }),
            ("CollectionSyncOwnerReplacement", () => { tests.CrystalPickup_CollectionSyncCannotGrantAfterItsOwnerIsReplaced(); return Task.CompletedTask; }),
            ("FirstObjectiveFeedbackCapacity", () => { tests.CrystalPickup_FirstObjectiveFeedbackSeesTheAlreadyAcceptedPhysicalItem(); return Task.CompletedTask; })
        };
        var results = new List<string>();
        string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
        var json = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(Path.Combine(outputDirectory, "inputs.json"), JsonSerializer.Serialize(new
        {
            ManifestSha256 = Hash(manifest), Files = inputs,
            ReaderSha256 = Hash(File.ReadAllBytes(typeof(GameTable<>).Assembly.Location)),
            RuntimeSha256 = Hash(File.ReadAllBytes(typeof(ItemInfo).Assembly.Location)),
            ScriptSha256 = Hash(File.ReadAllBytes(typeof(Q3777LoftiteCrystalEntityScript).Assembly.Location)),
            ProbeSha256 = Hash(File.ReadAllBytes(typeof(Q3777CrystalPickupTests).Assembly.Location))
        }, json));
        foreach (var test in cases)
        {
            try { await test.Run(); results.Add(test.Name); Console.WriteLine($"PASS {test.Name}"); }
            catch (Exception error)
            {
                File.WriteAllText(Path.Combine(outputDirectory, "failure.json"), JsonSerializer.Serialize(new
                    { Case = test.Name, Passed = results, Error = error.ToString() }, json));
                throw;
            }
        }
        File.WriteAllText(Path.Combine(outputDirectory, "result.json"), JsonSerializer.Serialize(new
        {
            CompletedUtc = DateTime.UtcNow, Passed = results.Count, Results = results,
            Limit = "Real script, Player inventory/quest and SQLite save/reload with recovered metadata; seeded accepted quest and prepared map/session. No public acceptance/turnin, native flight, provider restart, sockets or retail qualification."
        }, json));
    }

    internal sealed class PickupFixture : IDisposable
    {
        internal readonly Store Storage = new(null);
        internal readonly Actor Actor;
        internal readonly NexusForever.Game.Quest.Quest Quest;
        private uint nextCrystal = 5000;
        private readonly IItemManager items;
        private readonly uint slots;

        internal PickupFixture(IGameTableManager actual = null, bool conversationDone = true, uint slots = 2)
        {
            this.slots = slots;
            var tablesProxy = (RecordingDispatchProxy<IGameTableManager>)(object)Storage.Source.Tables;
            Quest2Entry entry;
            if (actual != null)
            {
                foreach (var property in typeof(IGameTableManager).GetProperties())
                {
                    object value = property.GetValue(actual);
                    if (value != null) tablesProxy.SetProperty(property.Name, value);
                }
                entry = actual.Quest2.GetEntry(3777);
            }
            else
            {
                entry = new Quest2Entry { Id = 3777, Objectives = [5075, 5076, 4859, 0, 0, 0] };
                tablesProxy.SetProperty(nameof(IGameTableManager.QuestObjective), PlayerBindPointTests.Table(
                    new QuestObjectiveEntry { Id = 5075, Type = 17, Data = 219, Count = 1 },
                    new QuestObjectiveEntry { Id = 5076, Type = 5, Flags = 3, Data = 6952, Count = 1 },
                    new QuestObjectiveEntry { Id = 4859, Type = 4, Flags = 3, Data = 6998, Count = 8 }));
            }
            var info = new QuestInfo(entry, Storage.Global, Storage.Source.Tables);
            ((RecordingDispatchProxy<IGlobalQuestManager>)(object)Storage.Global).SetMethodReturn(nameof(IGlobalQuestManager.GetQuestInfo), info);
            using (var seed = Storage.Context())
            {
                seed.CharacterQuest.Add(new() { Id = 42, QuestId = 3777, State = (byte)QuestState.Accepted,
                    QuestObjective = new List<CharacterQuestObjectiveModel> {
                        new() { Id = 42, QuestId = 3777, Index = 0, Progress = conversationDone ? 1u : 0u },
                        new() { Id = 42, QuestId = 3777, Index = 1 },
                        new() { Id = 42, QuestId = 3777, Index = 2 } } });
                seed.SaveChanges();
            }
            items = RecordingDispatchProxy<IItemManager>.Create(out var itemProxy);
            ulong nextItem = 1000;
            itemProxy.SetMethodReturnFactory("get_" + nameof(IItemManager.NextItemId), () => nextItem++);
            var definitions = new Dictionary<uint, IItemInfo>();
            itemProxy.SetMethodHandler(nameof(IItemManager.GetItemInfo), args =>
            {
                uint id = (uint)args[0];
                if (definitions.TryGetValue(id, out var existing)) return existing;
                if (actual != null) return definitions[id] = new ItemInfo(actual.Item.GetEntry(id), actual);
                var value = RecordingDispatchProxy<IItemInfo>.Create(out var p);
                p.SetProperty(nameof(IItemInfo.Id), id);
                p.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { Id = id, MaxStackCount = id == 6998 ? 100u : 1u });
                p.SetMethodReturn(nameof(IItemInfo.IsStackable), id == 6998);
                return definitions[id] = value;
            });
            CharacterModel loaded;
            using (var read = Storage.Context())
                loaded = read.Character.AsNoTracking().Include(row => row.Quest).ThenInclude(row => row.QuestObjective).Single();
            Actor = LoadActor(loaded);
            Quest = Assert.IsType<NexusForever.Game.Quest.Quest>(Assert.Single(Actor.Quests.GetActiveQuests()));
        }


        private Actor LoadActor(CharacterModel loaded)
        {
            return new Actor(Storage, actorModel: loaded, inventoryFactory: (owner, model) =>
            {
                var inventory = BankDeferredCurrencyRecoveryTests.Inventory(owner, items, Storage.Source.Tables, null);
                var bag = new Bag(InventoryLocation.Inventory, slots, items);
                foreach (var row in model.Item) bag.AddItem(new GameItem(row, items, Storage.Source.Tables), row.BagIndex);
                typeof(Inventory).GetField("bags", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(inventory,
                    new Dictionary<InventoryLocation, IBag> { [InventoryLocation.Inventory] = bag });
                return inventory;
            });
        }

        internal Actor Reload()
        {
            using var read = Storage.Context();
            return LoadActor(read.Character.AsNoTracking().Include(row => row.Quest).ThenInclude(row => row.QuestObjective).Include(row => row.Item).Single());
        }

        internal IQuestObjective Objective(uint id) => Quest.Single(o => o.ObjectiveInfo.Id == id);
        internal (Q3777LoftiteCrystalEntityScript Script, ICreatureEntity Owner, RecordingDispatchProxy<ICreatureEntity> Proxy) Crystal()
        {
            var owner = RecordingDispatchProxy<ICreatureEntity>.Create(out var proxy);
            proxy.SetProperty(nameof(ICreatureEntity.Guid), nextCrystal++);
            proxy.SetProperty(nameof(ICreatureEntity.CreatureId), 13120u);
            var script = new Q3777LoftiteCrystalEntityScript(); script.OnLoad(owner);
            return (script, owner, proxy);
        }
        internal Task Persist(bool fail = false) => CharacterAtomicSave.Execute(() =>
        {
            var context = Storage.Context();
            if (fail) context.SavingChanges += (_, _) => throw new IOException("Owned pickup Character flush refusal.");
            return context;
        }, 42, Actor.Player.Save);
        public void Dispose() { Actor.Dispose(); Storage.Dispose(); }
    }
}
