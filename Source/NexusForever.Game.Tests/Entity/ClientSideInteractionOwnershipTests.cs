using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.RealmBank;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Entity;

namespace NexusForever.Game.Tests.Entity;

public class ClientSideInteractionOwnershipTests
{
    [Theory]
    [InlineData(false, "player")]
    [InlineData(true, "player")]
    [InlineData(false, "session")]
    [InlineData(true, "session")]
    [InlineData(false, "queue")]
    [InlineData(true, "queue")]
    [InlineData(false, "map")]
    [InlineData(true, "map")]
    [InlineData(false, "entity")]
    [InlineData(true, "entity")]
    public void PendingCompletion_CannotCrossOriginalOwnership(bool entityEvent, string boundary)
    {
        var f = new Fixture();
        f.Begin();
        if (boundary == "player") f.Session.Player = f.NewPlayer();
        if (boundary == "session") f.PlayerProxy.SetProperty(nameof(IPlayer.Session), RecordingDispatchProxy<IWorldSession>.Create(out _));
        if (boundary == "queue") f.SessionProxy.SetProperty(nameof(IWorldSession.Events), new EventQueue());
        if (boundary == "map") f.PlayerProxy.SetProperty(nameof(IPlayer.Map), f.NewMap());
        if (boundary == "entity") f.Visible = f.NewEntity(out _);

        f.Complete(entityEvent);

        Assert.Empty(f.EntityProxy.GetInvocations(nameof(IWorldEntity.OnActivateSuccess)));
        Assert.Empty(f.EntityProxy.GetInvocations(nameof(IWorldEntity.OnActivateCast)));
        Assert.Empty(f.QuestProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Empty(f.PlayerProxy.GetInvocations(nameof(IPlayer.TryCompleteClientSideInteractionSpell)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OriginalCompletion_CreditsOnceAcrossBothCompletionRoutes(bool entityEvent)
    {
        var f = new Fixture();
        f.Begin();
        f.Complete(entityEvent);
        f.Complete(!entityEvent);
        Assert.Single(f.EntityProxy.GetInvocations(nameof(IWorldEntity.OnActivateCast)));
        Assert.Single(f.EntityProxy.GetInvocations(nameof(IWorldEntity.OnActivateSuccess)));
        Assert.NotEmpty(f.QuestProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
    }

    [Fact]
    public void RetiredCastFinish_CannotReplaceANewerPendingInteraction()
    {
        var f = new Fixture();
        f.EntityProxy.SetProperty(nameof(IWorldEntity.CreatureEntry), new Creature2Entry { Id = 27196u, Spell4IdActivate00 = 32386u, ActivateSpellCastTime = 1000u });
        var old = f.Begin();
        Assert.NotNull(old.FinishCallback);
        var original = f.EntityProxy;
        f.Visible = f.NewEntity(out var replacement, 88u);
        f.Begin();

        old.FinishCallback(null, false);
        f.Complete(false);

        Assert.Empty(original.GetInvocations(nameof(IWorldEntity.OnActivateSuccess)));
        Assert.Single(replacement.GetInvocations(nameof(IWorldEntity.OnActivateSuccess)));
    }

    [Fact]
    public void CastFinish_CannotCreatePendingStateForAReplacementPlayer()
    {
        var f = new Fixture();
        f.EntityProxy.SetProperty(nameof(IWorldEntity.CreatureEntry), new Creature2Entry { Id = 27196u, Spell4IdActivate00 = 32386u, ActivateSpellCastTime = 1000u });
        var cast = f.Begin();
        f.Session.Player = f.NewPlayer();
        cast.FinishCallback(null, false);
        f.Complete(false);
        Assert.Empty(f.EntityProxy.GetInvocations(nameof(IWorldEntity.OnActivateSuccess)));
        Assert.Empty(f.QuestProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
    }

    [Theory]
    [InlineData(false, "cast")]
    [InlineData(true, "cast")]
    [InlineData(false, "success")]
    [InlineData(true, "success")]
    [InlineData(false, "spell-finish")]
    public void ReentrantActorReplacement_StopsRemainingCompletionStages(bool entityEvent, string stage)
    {
        var f = new Fixture();
        f.Begin();
        object Replace(object[] _) { f.Session.Player = f.NewPlayer(); return stage == "spell-finish" ? true : null; }
        if (stage == "spell-finish") f.PlayerProxy.SetMethodHandler(nameof(IPlayer.TryCompleteClientSideInteractionSpell), Replace);
        else f.EntityProxy.SetMethodHandler(stage == "cast" ? nameof(IWorldEntity.OnActivateCast) : nameof(IWorldEntity.OnActivateSuccess), Replace);
        f.Complete(entityEvent);
        if (stage != "success") Assert.Empty(f.EntityProxy.GetInvocations(nameof(IWorldEntity.OnActivateSuccess)));
        Assert.Empty(f.QuestProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
    }

    [Fact]
    public void CancelledCastFinish_CannotArmItsOwnOrANewerTicket()
    {
        var f = new Fixture();
        f.EntityProxy.SetProperty(nameof(IWorldEntity.CreatureEntry), new Creature2Entry { Id = 27196u, Spell4IdActivate00 = 32386u, ActivateSpellCastTime = 1000u });
        var first = f.Begin();
        first.FinishCallback(null, true);
        f.Complete(false);
        Assert.Empty(f.EntityProxy.GetInvocations(nameof(IWorldEntity.OnActivateSuccess)));
        f.Visible = f.NewEntity(out var replacement, 88u);
        f.Begin();
        first.FinishCallback(null, true);
        f.Complete(false);
        Assert.Single(replacement.GetInvocations(nameof(IWorldEntity.OnActivateSuccess)));
    }

    [Fact]
    public void MismatchedReply_DoesNotConsumeTheCurrentTicket()
    {
        var f = new Fixture();
        f.Begin();
        new ClientSpellClientSideInteractionHandler(null).HandleMessage(f.Session,
            Read<ClientSpellClientSideInteraction>(w => { w.Write(1u); w.Write((byte)1, 3u); w.Write(9999u); }));
        Assert.Empty(f.EntityProxy.GetInvocations(nameof(IWorldEntity.OnActivateSuccess)));
        f.Complete(false);
        Assert.Single(f.EntityProxy.GetInvocations(nameof(IWorldEntity.OnActivateSuccess)));
    }

    [Fact]
    public void AnUnfinishedCast_CannotBeCompletedByAnEarlyReply()
    {
        var f = new Fixture();
        f.EntityProxy.SetProperty(nameof(IWorldEntity.CreatureEntry), new Creature2Entry { Id = 27196u, Spell4IdActivate00 = 32386u, ActivateSpellCastTime = 1000u });
        var cast = f.Begin();
        f.Complete(false);
        Assert.Empty(f.EntityProxy.GetInvocations(nameof(IWorldEntity.OnActivateSuccess)));
        cast.FinishCallback(null, false);
        f.Complete(false);
        Assert.Single(f.EntityProxy.GetInvocations(nameof(IWorldEntity.OnActivateSuccess)));
    }

    [Theory]
    [InlineData("epoch")]
    [InlineData("account-session")]
    [InlineData("session-account")]
    public void PendingCompletion_PreservesAccountOwnership(string boundary)
    {
        var f = new Fixture();
        var account = RecordingDispatchProxy<IAccount>.Create(out var ap);
        ap.SetProperty(nameof(IAccount.Session), f.Session);
        ap.SetProperty(nameof(IAccount.WorldSessionEpoch), 1ul);
        f.PlayerProxy.SetProperty(nameof(IPlayer.Account), account);
        f.SessionProxy.SetProperty(nameof(IWorldSession.Account), account);
        f.Begin();
        if (boundary == "epoch") ap.SetProperty(nameof(IAccount.WorldSessionEpoch), 2ul);
        if (boundary == "account-session") ap.SetProperty(nameof(IAccount.Session), RecordingDispatchProxy<IWorldSession>.Create(out _));
        if (boundary == "session-account") f.SessionProxy.SetProperty(nameof(IWorldSession.Account), RecordingDispatchProxy<IAccount>.Create(out _));
        f.Complete(false);
        Assert.Empty(f.EntityProxy.GetInvocations(nameof(IWorldEntity.OnActivateSuccess)));
        Assert.Empty(f.QuestProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
    }

    [Fact]
    public void CancelledWaitingSpell_CannotCreditALaterCompletionReply()
    {
        var f = new Fixture();
        var cast = f.Begin();
        Assert.True(cast.WaitForClientSideInteractionResponse);
        Assert.NotNull(cast.FinishCallback);
        cast.FinishCallback(null, true);
        f.Complete(false);
        Assert.Empty(f.EntityProxy.GetInvocations(nameof(IWorldEntity.OnActivateSuccess)));
        Assert.Empty(f.QuestProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
    }

    [Fact]
    public void PendingEntityCompletion_AfterPlayerRemovalIsIgnored()
    {
        var f = new Fixture();
        f.Begin();
        f.Session.Player = null;
        f.Complete(true);
        Assert.Empty(f.EntityProxy.GetInvocations(nameof(IWorldEntity.OnActivateSuccess)));
    }

    [Fact]
    public void ClientCancel_CleansWaitingSpellAndCallsOnlyOriginalFailure()
    {
        var f = new Fixture();
        var cast = f.Begin();
        f.PlayerProxy.SetMethodHandler(nameof(IPlayer.TryCompleteClientSideInteractionSpell), args =>
        { cast.FinishCallback(null, (bool)args[1]); return true; });
        new ClientSpellClientSideInteractionHandler(null).HandleMessage(f.Session,
            Read<ClientSpellClientSideInteraction>(w => { w.Write(1u); w.Write((byte)0, 3u); w.Write(18614u); }));
        Assert.Single(f.EntityProxy.GetInvocations(nameof(IWorldEntity.OnActivateFail)));
        f.Complete(false);
        Assert.Empty(f.EntityProxy.GetInvocations(nameof(IWorldEntity.OnActivateSuccess)));
        Assert.Empty(f.QuestProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
    }

    private sealed class Fixture
    {
        public IWorldSession Session { get; }
        public RecordingDispatchProxy<IWorldSession> SessionProxy { get; }
        public RecordingDispatchProxy<IPlayer> PlayerProxy { get; private set; }
        public RecordingDispatchProxy<IQuestManager> QuestProxy { get; private set; }
        public RecordingDispatchProxy<IWorldEntity> EntityProxy { get; }
        public IWorldEntity Visible;
        private readonly IGameTableManager tables;

        public Fixture()
        {
            Session = RecordingDispatchProxy<IWorldSession>.Create(out var sp);
            SessionProxy = sp;
            sp.SetProperty(nameof(IWorldSession.Events), new EventQueue());
            Session.Player = NewPlayer();
            Visible = NewEntity(out var ep);
            EntityProxy = ep;
            tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tp);
            tp.SetProperty(nameof(IGameTableManager.Spell4), PlayerBindPointTests.Table(new Spell4Entry { Id = 32386u, Spell4BaseIdBaseSpell = 18613u }));
            tp.SetProperty(nameof(IGameTableManager.Spell4Base), PlayerBindPointTests.Table(new Spell4BaseEntry { Id = 18613u, ClientSideInteractionId = 266u }));
            tp.SetProperty(nameof(IGameTableManager.ClientSideInteraction), PlayerBindPointTests.Table(new ClientSideInteractionEntry { Id = 266u, Duration = 60000u }));
        }

        public IBaseMap NewMap()
        {
            var map = RecordingDispatchProxy<IBaseMap>.Create(out var proxy);
            proxy.SetProperty(nameof(IBaseMap.Entry), new WorldEntry { Id = 426u });
            return map;
        }

        public IPlayer NewPlayer()
        {
            var player = RecordingDispatchProxy<IPlayer>.Create(out var pp);
            PlayerProxy ??= pp;
            pp.SetProperty(nameof(IPlayer.Guid), 17u);
            pp.SetProperty(nameof(IPlayer.Map), NewMap());
            pp.SetProperty(nameof(IPlayer.Session), Session);
            var quest = RecordingDispatchProxy<IQuestManager>.Create(out var qp);
            QuestProxy ??= qp;
            qp.SetMethodReturn(nameof(IQuestManager.GetActiveQuests), Array.Empty<IQuest>());
            qp.SetMethodReturn(nameof(IQuestManager.GetQuestState), NexusForever.Game.Static.Quest.QuestState.Accepted);
            pp.SetProperty(nameof(IPlayer.QuestManager), quest);
            pp.SetProperty(nameof(IPlayer.AchievementManager), RecordingDispatchProxy<ICharacterAchievementManager>.Create(out _));
            pp.SetMethodHandler(nameof(IPlayer.GetVisible), args => (uint)args[0] == Visible?.Guid ? Visible : null);
            pp.SetMethodReturn(nameof(IPlayer.TryCastSpell), CastResult.Ok);
            pp.SetMethodReturn(nameof(IPlayer.TryCompleteClientSideInteractionSpell), true);
            return player;
        }

        public IWorldEntity NewEntity(out RecordingDispatchProxy<IWorldEntity> ep, uint guid = 77u)
        {
            var entity = RecordingDispatchProxy<IWorldEntity>.Create(out ep);
            ep.SetProperty(nameof(IWorldEntity.Guid), guid);
            ep.SetProperty(nameof(IWorldEntity.CreatureId), 27196u);
            ep.SetProperty(nameof(IWorldEntity.Map), Session.Player.Map);
            ep.SetProperty(nameof(IWorldEntity.CreatureEntry), new Creature2Entry { Id = 27196u, Spell4IdActivate00 = 32386u });
            return entity;
        }

        public SpellParameters Begin()
        {
            var handler = new ClientActivateUnitCastHandler(null, null, tables);
            handler.HandleMessage(Session, Read<ClientActivateUnitCast>(w => { w.Write(10u); w.Write(Visible.Guid); }));
            return Assert.IsType<SpellParameters>(PlayerProxy.GetInvocations(nameof(IPlayer.TryCastSpell)).Last().Arguments[1]);
        }

        public void Complete(bool entityEvent)
        {
            if (entityEvent)
                new ClientEntityInteractionHandler(NullLogger<ClientEntityInteractionHandler>.Instance, null, new RealmBankManager())
                    .HandleMessage(Session, Read<ClientEntityInteract>(w => { w.Write(77u); w.Write((byte)101, 7u); }));
            else
                new ClientSpellClientSideInteractionHandler(null)
                    .HandleMessage(Session, Read<ClientSpellClientSideInteraction>(w => { w.Write(1u); w.Write((byte)1, 3u); w.Write(18614u); }));
        }
    }

    private static T Read<T>(Action<GamePacketWriter> write) where T : IReadable, new()
    {
        using var stream = new MemoryStream();
        using var writer = new GamePacketWriter(stream);
        write(writer); writer.FlushBits();
        using var reader = new GamePacketReader(new MemoryStream(stream.ToArray()));
        var packet = new T(); packet.Read(reader); return packet;
    }
}
