using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.Internal;
using NexusForever.Network.Internal.Message.Friendship;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Friendship;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Friendship;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using Store = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Friendship;

public partial class FriendshipSettingOwnershipTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CharacterLockChangesOnlyTheCharacterFlagAndSurvivesReload(bool blocked)
    {
        using var f = new Fixture();
        f.Actor.Player.Flags = CharacterFlag.HolomarkHideLeft | (blocked ? CharacterFlag.None : CharacterFlag.FriendBlock);
        var publisher = RecordingDispatchProxy<IInternalMessagePublisher>.Create(out var messages);
        messages.SetMethodReturn(nameof(IInternalMessagePublisher.PublishAsync), Task.CompletedTask);
        new ClientFriendshipBlockHandler().HandleMessage(f.Session, Read<ClientFriendshipBlock>(w => w.Write(blocked)));
        var expected = CharacterFlag.HolomarkHideLeft | (blocked ? CharacterFlag.FriendBlock : CharacterFlag.None);
        Assert.Equal(expected, f.Actor.Player.Flags);
        Assert.Empty(messages.Invocations);
        Assert.Equal(expected, Assert.Single(f.Packets<ServerCharacterFlagsUpdated>()).Flags);
        await f.Persist();
        using var read = f.Store.Context(); Assert.Equal((uint)expected, read.Character.Single().Flags);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IgnoreStrangersRoutesToAccountSettingWithoutCharacterFlagEcho(bool blocked)
    {
        using var f = new Fixture();
        var publisher = RecordingDispatchProxy<IInternalMessagePublisher>.Create(out var messages);
        messages.SetMethodReturn(nameof(IInternalMessagePublisher.PublishAsync), Task.CompletedTask);
        using var services = new ServiceCollection().AddSingleton<ILogger<ClientFriendshipIgnoreStrangersStateHandler>>(NullLogger<ClientFriendshipIgnoreStrangersStateHandler>.Instance)
            .AddSingleton(publisher).AddTransient<ClientFriendshipIgnoreStrangersStateHandler>().BuildServiceProvider();
        services.GetRequiredService<ClientFriendshipIgnoreStrangersStateHandler>().HandleMessage(f.Session,
            Read<ClientFriendshipIgnoreStrangersState>(w => w.Write(blocked)));
        var message = Assert.IsType<FriendshipAccountBlockUpdateMessage>(Assert.Single(messages.GetInvocations(nameof(IInternalMessagePublisher.PublishAsync))).Arguments[0]);
        Assert.Equal(f.Actor.Player.Account.Id, message.AccountId);
        Assert.Equal(blocked, message.BlockFriendRequests);
        Assert.Empty(f.Packets<ServerFriendshipIgnoreStrangersState>());
        Assert.Equal(CharacterFlag.None, f.Actor.Player.Flags);
    }

    [Theory]
    [InlineData("rollback")]
    [InlineData("reject")]
    [InlineData("unflushed")]
    [InlineData("unflushed-commit")]
    [InlineData("savepoint")]
    public async Task CharacterFlagIntentSurvivesUnconfirmedSave(string outcome)
    {
        using var f = new Fixture();
        f.Actor.Player.Flags = CharacterFlag.FriendBlock | CharacterFlag.IgnoreDuelRequests;
        using (var save = f.Store.Context())
        {
            using var transaction = save.Database.BeginTransaction();
            if (outcome == "savepoint") transaction.CreateSavepoint("before_flags");
            f.Actor.Player.Save(save);
            if (outcome == "reject") save.RejectSave(new IOException("Owned Character flag rejection."));
            else if (outcome is not ("unflushed" or "unflushed-commit")) save.SaveChanges();
            if (outcome == "savepoint") { transaction.RollbackToSavepoint("before_flags"); transaction.Commit(); }
            else if (outcome == "unflushed-commit") transaction.Commit();
            else transaction.Rollback();
        }
        await f.Persist();
        using var read = f.Store.Context(); Assert.Equal((uint)(CharacterFlag.FriendBlock | CharacterFlag.IgnoreDuelRequests), read.Character.Single().Flags);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task OlderCharacterFlagCommitCannotAcknowledgeLaterIntent(bool returnsToSameValue)
    {
        using var f = new Fixture(); f.Actor.Player.Flags = CharacterFlag.FriendBlock;
        using (var save = f.Store.Context())
        {
            using var transaction = save.Database.BeginTransaction();
            f.Actor.Player.Save(save); save.SaveChanges();
            f.Actor.Player.Flags = CharacterFlag.IgnoreDuelRequests;
            if (returnsToSameValue) f.Actor.Player.Flags = CharacterFlag.FriendBlock;
            transaction.Commit();
        }
        using (var next = f.Store.Context())
        {
            f.Actor.Player.Save(next);
            Assert.True(next.Entry(next.Character.Local.Single()).Property(row => row.Flags).IsModified);
        }
        await f.Persist();
        using var read = f.Store.Context(); Assert.Equal((uint)f.Actor.Player.Flags, read.Character.Single().Flags);
    }

    [Fact]
    public async Task SuppressedCharacterFlagWriteRemainsDirtyAndSuccessfulRetryAcknowledgesIt()
    {
        using var f = new Fixture(); f.Actor.Player.Flags = CharacterFlag.FriendBlock;
        using (var save = f.Store.Context())
        {
            f.Actor.Player.Save(save);
            save.Entry(save.Character.Local.Single()).Property(row => row.Flags).IsModified = false;
            save.SaveChanges();
        }
        using (var before = f.Store.Context()) Assert.Equal(0u, before.Character.Single().Flags);
        await f.Persist();
        using var next = f.Store.Context(); f.Actor.Player.Save(next);
        Assert.Equal((uint)CharacterFlag.FriendBlock, next.Character.AsNoTracking().Single().Flags);
        Assert.False(next.Entry(next.Character.Local.Single()).Property(row => row.Flags).IsModified);
    }

    private sealed class Fixture : IDisposable
    {
        internal readonly Store Store = new(null);
        internal readonly Actor Actor;
        internal readonly IWorldSession Session;
        private readonly RecordingDispatchProxy<IWorldSession> sessionProxy;
        internal Fixture()
        {
            var events = new EventQueue();
            Session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
            sessionProxy.SetProperty(nameof(IWorldSession.Events), events);
            Actor = new(Store, actorSession: Session, actorEvents: events);
            sessionProxy.SetProperty(nameof(IWorldSession.Player), Actor.Player);
            sessionProxy.SetProperty(nameof(IWorldSession.Account), Actor.Player.Account);
            ((RecordingDispatchProxy<IAccount>)(object)Actor.Player.Account).SetProperty(nameof(IAccount.Session), Session);
            ((RecordingDispatchProxy<IAccount>)(object)Actor.Player.Account).SetProperty(nameof(IAccount.Id), 7u);
            ((RecordingDispatchProxy<IAccount>)(object)Actor.Player.Account).SetProperty(nameof(IAccount.WorldSessionEpoch), 1ul);
            typeof(Player).GetField("presenceSessionEpoch", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(Actor.Player, 1ul);
            typeof(Player).GetField("presenceAdmission", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(Actor.Player, 1ul);
        }
        internal IEnumerable<T> Packets<T>() => sessionProxy.GetInvocations("EnqueueMessageEncrypted").Select(i => i.Arguments[0]).OfType<T>();
        internal Task Persist() => CharacterAtomicSave.Execute(() => Store.Context(), 42, Actor.Player.Save);
        public void Dispose() { Actor.Dispose(); Store.Dispose(); }
    }

    private static T Read<T>(Action<GamePacketWriter> write) where T : IReadable, new()
    {
        using var stream = new MemoryStream(); using var writer = new GamePacketWriter(stream);
        write(writer); writer.FlushBits(); using var reader = new GamePacketReader(new MemoryStream(stream.ToArray()));
        var value = new T(); value.Read(reader); return value;
    }
}
