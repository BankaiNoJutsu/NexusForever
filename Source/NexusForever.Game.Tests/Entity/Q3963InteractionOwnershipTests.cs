using System.Collections.Immutable;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Entity;
using NexusForever.Game.Quest;
using NexusForever.Game.RealmBank;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Entity;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using Store = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Entity;

public partial class Q3963InteractionOwnershipTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompletionSurvivesRejectedPlayerSaveAndFreshReload(bool entityEvent)
    {
        using var f = new Fixture();
        f.Pending();
        f.Complete(entityEvent);
        Assert.Equal(1u, Assert.Single(f.Quest).Progress);
        Assert.Equal(QuestState.Achieved, f.Quest.State);
        await Assert.ThrowsAsync<IOException>(() => f.Persist(fail: true));
        using (var read = f.Storage.Context()) Assert.Equal(0u, read.CharacterQuestObjective.Single().Progress);
        f.Complete(!entityEvent);
        Assert.Equal(1u, Assert.Single(f.Quest).Progress);
        await f.Persist();
        using var reload = f.Reload();
        Assert.Equal(QuestState.Achieved, reload.State);
        Assert.Equal(1u, Assert.Single(reload).Progress);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SameMapReAdmissionInvalidatesPendingCredit(bool entityEvent)
    {
        using var f = new Fixture();
        f.Pending();
        ((IPlayerMapAdmission)f.Actor.Player).BeginMapAdmission();
        f.Complete(entityEvent);
        Assert.Equal(0u, Assert.Single(f.Quest).Progress);
        Assert.Equal(QuestState.Accepted, f.Quest.State);
    }

    private sealed class Fixture : IDisposable
    {
        internal readonly Store Storage = new(null);
        internal readonly Actor Actor;
        internal readonly NexusForever.Game.Quest.Quest Quest;
        private readonly IWorldSession session;
        private readonly IWorldEntity entity;
        private readonly IAssetManager assets;
        private readonly IQuestInfo info;

        public Fixture()
        {
            var table = (RecordingDispatchProxy<IGameTableManager>)(object)Storage.Source.Tables;
            table.SetProperty(nameof(IGameTableManager.QuestObjective), PlayerBindPointTests.Table(new QuestObjectiveEntry
            { Id = 5201u, Type = 5u, Data = 0u, Count = 1u, Flags = 16u, TargetGroupIdRewardPane = 7573u,
                WorldLocationsIdIndicator00 = 45401u, WorldLocationsIdIndicator01 = 45402u }));
            var entry = new Quest2Entry { Id = 3963u, Objectives = [5201u, 0u, 0u, 0u, 0u, 0u], PushedItemIds = new uint[6], PushedItemCounts = new uint[6] };
            info = new QuestInfo(entry, Storage.Global, Storage.Source.Tables);
            ((RecordingDispatchProxy<IGlobalQuestManager>)(object)Storage.Global).SetMethodReturn(nameof(IGlobalQuestManager.GetQuestInfo), info);
            using (var seed = Storage.Context())
            {
                seed.CharacterQuest.Add(new() { Id = 42, QuestId = 3963, State = (byte)QuestState.Accepted,
                    QuestObjective = new List<CharacterQuestObjectiveModel> { new() { Id = 42, QuestId = 3963, Index = 0 } } });
                seed.SaveChanges();
            }
            session = RecordingDispatchProxy<IWorldSession>.Create(out var sp);
            var events = new EventQueue(); sp.SetProperty(nameof(IWorldSession.Events), events);
            using var read = Storage.Context();
            var model = read.Character.AsNoTracking().Include(row => row.Quest).ThenInclude(row => row.QuestObjective).Single();
            Actor = new(Storage, actorModel: model, actorSession: session, actorEvents: events);
            session.Player = Actor.Player;
            sp.SetProperty(nameof(IWorldSession.Account), Actor.Player.Account);
            ((RecordingDispatchProxy<IAccount>)(object)Actor.Player.Account).SetProperty(nameof(IAccount.Session), session);
            var map = RecordingDispatchProxy<IBaseMap>.Create(out var mp); mp.SetProperty(nameof(IBaseMap.Entry), new WorldEntry { Id = 426u });
            Actor.Set(nameof(IPlayer.Map), map);
            entity = RecordingDispatchProxy<IWorldEntity>.Create(out var ep);
            ep.SetProperty(nameof(IWorldEntity.Guid), 77u); ep.SetProperty(nameof(IWorldEntity.CreatureId), 27196u); ep.SetProperty(nameof(IWorldEntity.Map), map);
            typeof(GridEntity).GetField("visibleEntities", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(Actor.Player, new Dictionary<uint, IGridEntity> { [77u] = entity });
            assets = RecordingDispatchProxy<IAssetManager>.Create(out var ap);
            ap.SetMethodHandler(nameof(IAssetManager.GetQuestObjectiveTargetIds), args => (uint)args[0] == 5201u ? ImmutableList.Create(27196u) : ImmutableList<uint>.Empty);
            Quest = new(Actor.Player, info, Assert.Single(model.Quest), assets, Storage.Global, Storage.Scripts, Storage.Source.Tables);
            var active = (Dictionary<ushort, IQuest>)typeof(QuestManager).GetField("activeQuests", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Actor.Quests)!;
            active[3963] = Quest;
        }

        internal void Pending() => PendingClientSideInteractionActivationStore.Set(session, entity, 266u, 18613u, true);
        internal void Complete(bool entityEvent)
        {
            if (entityEvent)
                new ClientEntityInteractionHandler(NullLogger<ClientEntityInteractionHandler>.Instance, assets, new RealmBankManager())
                    .HandleMessage(session, Read<ClientEntityInteract>(w => { w.Write(77u); w.Write((byte)101, 7u); }));
            else
                new ClientSpellClientSideInteractionHandler(assets).HandleMessage(session,
                    Read<ClientSpellClientSideInteraction>(w => { w.Write(1u); w.Write((byte)1, 3u); w.Write(18614u); }));
        }
        internal Task Persist(bool fail = false) => CharacterAtomicSave.Execute(() =>
        {
            var context = Storage.Context();
            if (fail) context.SavingChanges += (_, _) => throw new IOException("Owned Q3963 Character flush refusal.");
            return context;
        }, 42, Actor.Player.Save);
        internal NexusForever.Game.Quest.Quest Reload()
        {
            using var read = Storage.Context();
            return new(Actor.Player, info, read.CharacterQuest.AsNoTracking().Include(row => row.QuestObjective).Single(), assets, Storage.Global, Storage.Scripts, Storage.Source.Tables);
        }
        public void Dispose() { Actor.Dispose(); Storage.Dispose(); }
    }

    private static T Read<T>(Action<GamePacketWriter> write) where T : IReadable, new()
    {
        using var stream = new MemoryStream(); using var writer = new GamePacketWriter(stream);
        write(writer); writer.FlushBits(); using var reader = new GamePacketReader(new MemoryStream(stream.ToArray()));
        var packet = new T(); packet.Read(reader); return packet;
    }
}
