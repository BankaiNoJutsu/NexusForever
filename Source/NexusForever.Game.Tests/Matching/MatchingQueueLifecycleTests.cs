using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Game.Static.Matching;
using NexusForever.Network.World.Message.Model;
using MatchType = NexusForever.Game.Static.Matching.MatchType;

namespace NexusForever.Game.Tests.Matching;

public sealed partial class MatchingCancellationTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void RealQueueJoinFeedbackCanCancelBeforeTheBackendReturns(bool all)
    {
        var f = new Fixture(realBackend: true);
        f.OnMessage(f.LeaderSession, message => { if (message is ServerMatchingQueueJoin) f.Leave(all); });
        f.Join(false); f.Manager.Update(.01);
        Assert.Empty(f.Manager.GetMatchingCharacter(f.Leader.Identity).GetMatchingCharacterQueues());
        var group = Assert.Single(f.RealGroups);
        Assert.True(group.IsFinalised); Assert.Empty(group.GetTeams().SelectMany(team => team.GetMembers()));
        Assert.Empty(group.GetMatchingMaps());
    }

    [Fact]
    public void RealPartyCancellationDuringFirstJoinDoesNotPublishALaterJoinToItsPeer()
    {
        var f = new Fixture(realBackend: true);
        f.OnMessage(f.LeaderSession, message => { if (message is ServerMatchingQueueJoin) f.Leave(false); });
        f.JoinParty(); f.Respond(f.LeaderSession); f.Respond(f.MemberSession); f.Manager.Update(.01);
        Assert.Empty(f.Packets<ServerMatchingQueueJoin>(f.MemberSession));
        Assert.Empty(f.Manager.GetMatchingCharacter(f.Leader.Identity).GetMatchingCharacterQueues());
        Assert.Empty(f.Manager.GetMatchingCharacter(f.Member.Identity).GetMatchingCharacterQueues());
    }

    [Fact]
    public void CancelledDebugImmediateQueueCannotBecomeAMatch()
    {
        var f = new Fixture(realBackend: true, instant: true);
        f.OnMessage(f.LeaderSession, message => { if (message is ServerMatchingQueueJoin) f.Leave(false); });
        f.Join(false); Assert.Null(Record.Exception(() => f.Manager.Update(.01)));
        Assert.Empty(f.Matched);
        Assert.Empty(f.Manager.GetMatchingCharacter(f.Leader.Identity).GetMatchingCharacterQueues());
    }

    [Fact]
    public void PartyLeaveDetachesEveryMemberAndGroupBeforeItsFirstNotice()
    {
        var f = new Fixture(realBackend: true);
        f.JoinParty(); f.Respond(f.LeaderSession); f.Respond(f.MemberSession); f.Manager.Update(.01);
        bool observed = false;
        f.OnMessage(f.LeaderSession, message =>
        {
            if (message is not ServerMatchingQueueResultAnnounce { Result: MatchingQueueResult.Left }) return;
            observed = true;
            Assert.Empty(f.Manager.GetMatchingCharacter(f.Member.Identity).GetMatchingCharacterQueues());
            var group = Assert.Single(f.RealGroups);
            Assert.True(group.IsFinalised); Assert.Empty(group.GetMatchingMaps());
        });
        f.Leave(false); Assert.True(observed);
    }

    [Fact]
    public void RealQueueNormalJoinLeaveAndAdmittedReconnectRemainSupported()
    {
        var f = new Fixture(realBackend: true);
        f.Join(false); f.Manager.Update(.01);
        var queue = Assert.Single(f.Manager.GetMatchingCharacter(f.Leader.Identity).GetMatchingCharacterQueues());
        Assert.Single(queue.MatchingQueueGroup.GetTeams().SelectMany(team => team.GetMembers()));
        Assert.Single(queue.MatchingQueueGroup.GetMatchingMaps()); Assert.False(queue.MatchingQueueGroup.IsFinalised);
        var replacement = f.ReplaceLeader(); f.Manager.OnLogout(f.Leader); f.Manager.OnLogin(replacement.Player);
        Assert.Single(f.Packets<ServerMatchingQueueStatus>(replacement));
        f.Manager.LeaveQueue(replacement.Player, MatchType.Dungeon);
        Assert.True(queue.MatchingQueueGroup.IsFinalised);
        Assert.Empty(f.Manager.GetMatchingCharacter(f.Leader.Identity).GetMatchingCharacterQueues());
    }

    [Fact]
    public void ExistingQueueAndMapSelectorCanMatchTwoPublicSoloRequests()
    {
        var f = new Fixture(realBackend: true, teamSize: 2);
        f.Join(false); f.Manager.Update(.01); f.Join(false, f.MemberSession); f.Manager.Update(.01);
        var group = Assert.Single(f.Matched);
        Assert.Equal(2, group.GetTeams().SelectMany(team => team.GetMembers()).Count());
        Assert.Single(f.RealGroups);
    }

    [Fact]
    public void FirstPartyLeaveNoticeCanAdmitANewPartyWithoutOldCleanupOrTailPackets()
    {
        var f = new Fixture(realBackend: true);
        f.JoinParty(); f.Respond(f.LeaderSession); f.Respond(f.MemberSession); f.Manager.Update(.01);
        var old = Assert.Single(f.RealGroups); bool replaced = false;
        f.OnMessage(f.LeaderSession, message =>
        {
            if (message is not ServerMatchingQueueResultAnnounce { Result: MatchingQueueResult.Left } || replaced) return;
            replaced = true; f.JoinParty(); f.Respond(f.LeaderSession); f.Respond(f.MemberSession); f.Manager.Update(.01);
        });
        Assert.Null(Record.Exception(() => f.Leave(false))); Assert.True(replaced);
        Assert.True(old.IsFinalised);
        foreach (var actor in new[] { f.Leader, f.Member })
        {
            var queue = Assert.Single(f.Manager.GetMatchingCharacter(actor.Identity).GetMatchingCharacterQueues());
            Assert.NotSame(old, queue.MatchingQueueGroup);
            Assert.Equal(2, queue.MatchingQueueGroup.GetTeams().SelectMany(team => team.GetMembers()).Count());
        }
        foreach (var session in new[] { f.LeaderSession, f.MemberSession })
            Assert.IsType<ServerMatchingQueueJoin>(f.Messages(session)
                .Last(message => message is ServerMatchingQueueJoin or ServerMatchingLeftQueue or ServerMatchingQueueResultAnnounce));
    }

    [Fact]
    public void CancellingTheLastJoinPreservesTheEarlierProposalAndPreventsAMatch()
    {
        var f = new Fixture(realBackend: true, teamSize: 2);
        f.Join(false); f.Manager.Update(.01);
        var original = Assert.Single(f.Manager.GetMatchingCharacter(f.Leader.Identity).GetMatchingCharacterQueues());
        f.OnMessage(f.MemberSession, message =>
        {
            if (message is ServerMatchingQueueJoin) f.Manager.LeaveQueue(f.Member, MatchType.Dungeon);
        });
        f.Join(false, f.MemberSession); f.Manager.Update(.01);
        Assert.Empty(f.Matched); Assert.Empty(f.Manager.GetMatchingCharacter(f.Member.Identity).GetMatchingCharacterQueues());
        Assert.Same(original, Assert.Single(f.Manager.GetMatchingCharacter(f.Leader.Identity).GetMatchingCharacterQueues()));
        Assert.Single(original.MatchingQueueGroup.GetTeams().SelectMany(team => team.GetMembers()));
        Assert.Single(original.MatchingQueueGroup.GetMatchingMaps()); Assert.False(original.MatchingQueueGroup.IsFinalised);
    }

    [Fact]
    public void RealDebugImmediateQueueStillCreatesOneNonemptyMatch()
    {
        var f = new Fixture(realBackend: true, instant: true);
        f.Join(false); f.Manager.Update(.01);
        var group = Assert.Single(f.Matched);
        Assert.Single(group.GetTeams().SelectMany(team => team.GetMembers()));
        Assert.Single(group.GetMatchingMaps()); Assert.False(group.IsFinalised);
    }

    [Fact]
    public void PartyJoinFeedbackSeesAllMembersAndTheGroupCacheAlreadyInstalled()
    {
        var f = new Fixture(realBackend: true); bool observed = false;
        f.OnMessage(f.LeaderSession, message =>
        {
            if (message is not ServerMatchingQueueJoin) return;
            observed = true;
            var queue = Assert.Single(f.Manager.GetMatchingCharacter(f.Member.Identity).GetMatchingCharacterQueues());
            Assert.Single(queue.MatchingQueueGroup.GetMatchingMaps());
            Assert.Equal(2, queue.MatchingQueueGroup.GetTeams().SelectMany(team => team.GetMembers()).Count());
        });
        f.JoinParty(); f.Respond(f.LeaderSession); f.Respond(f.MemberSession); f.Manager.Update(.01);
        Assert.True(observed);
    }

    [Fact]
    public void PartyJoinPacketsAreIndependentOfTheFirstRecipientsRetainedPacket()
    {
        var f = new Fixture(realBackend: true);
        f.OnMessage(f.LeaderSession, message =>
        {
            if (message is ServerMatchingQueueJoin joined) joined.MapData.MatchingGameMapIds.Clear();
        });
        f.JoinParty(); f.Respond(f.LeaderSession); f.Respond(f.MemberSession); f.Manager.Update(.01);
        Assert.Equal(new[] { 71u }, Assert.Single(f.Packets<ServerMatchingQueueJoin>(f.MemberSession)).MapData.MatchingGameMapIds);
    }

    [Fact]
    public void ReentrantLeaveAndRepeatedOldGroupRemovalPublishOnlyOnce()
    {
        var f = new Fixture(realBackend: true);
        f.JoinParty(); f.Respond(f.LeaderSession); f.Respond(f.MemberSession); f.Manager.Update(.01);
        var old = Assert.Single(f.Manager.GetMatchingCharacter(f.Leader.Identity).GetMatchingCharacterQueues());
        bool repeated = false;
        f.OnMessage(f.LeaderSession, message =>
        {
            if (message is not ServerMatchingQueueResultAnnounce { Result: MatchingQueueResult.Left } || repeated) return;
            repeated = true; f.Leave(true); old.MatchingQueueGroup.RemoveMatchingQueueProposal(old.MatchingQueueProposal);
        });
        f.Leave(false); Assert.True(repeated);
        foreach (var session in new[] { f.LeaderSession, f.MemberSession })
            Assert.Single(f.Packets<ServerMatchingLeftQueue>(session));
    }

    [Fact]
    public void LeaveNoticeCannotPublishAnOldTailAfterANewDifferentTypeQueueJoin()
    {
        var f = new Fixture(realBackend: true);
        f.Join(false); f.Manager.Update(.01); bool replaced = false;
        f.OnMessage(f.LeaderSession, message =>
        {
            if (message is not ServerMatchingQueueResultAnnounce { Result: MatchingQueueResult.Left } || replaced) return;
            replaced = true; f.Join(false, type: MatchType.Adventure); f.Manager.Update(.01);
        });
        f.Leave(false); Assert.True(replaced);
        Assert.Equal(MatchType.Adventure, Assert.Single(f.Manager.GetMatchingCharacter(f.Leader.Identity)
            .GetMatchingCharacterQueues()).MatchingQueueProposal.MatchType);
        Assert.IsType<ServerMatchingQueueJoin>(f.Messages(f.LeaderSession)
            .Last(message => message is ServerMatchingQueueJoin or ServerMatchingLeftQueue or ServerMatchingQueueResultAnnounce));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void RoleCancellationFeedbackCannotMakeLeaveRemoveTheReplacementParty(bool all)
    {
        var f = new Fixture(realBackend: true); f.JoinParty(); bool replaced = false;
        f.OnMessage(f.LeaderSession, message =>
        {
            if (message is not ServerMatchingQueueResultAnnounce { Result: MatchingQueueResult.Role } || replaced) return;
            replaced = true; f.JoinParty(); f.Respond(f.LeaderSession); f.Respond(f.MemberSession); f.Manager.Update(.01);
        });
        f.Leave(all); Assert.True(replaced);
        foreach (var actor in new[] { f.Leader, f.Member })
            Assert.Single(f.Manager.GetMatchingCharacter(actor.Identity).GetMatchingCharacterQueues());
        foreach (var session in new[] { f.LeaderSession, f.MemberSession })
            Assert.IsType<ServerMatchingQueueJoin>(f.Messages(session)
                .Last(message => message is ServerMatchingQueueJoin or ServerMatchingLeftQueue or ServerMatchingQueueResultAnnounce));
    }

    [Fact]
    public void LeaveAllDetachesBothOldQueueTypesBeforeFeedbackAndPreservesTheNewJoin()
    {
        var f = new Fixture(realBackend: true);
        f.Join(false); f.Manager.Update(.01); f.Join(false, type: MatchType.Adventure); f.Manager.Update(.01);
        Assert.Equal(2, f.Manager.GetMatchingCharacter(f.Leader.Identity).GetMatchingCharacterQueues().Count());
        bool replaced = false, allOldStateDetached = false;
        f.OnMessage(f.LeaderSession, message =>
        {
            if (message is not ServerMatchingQueueResultAnnounce { Result: MatchingQueueResult.Left } || replaced) return;
            replaced = true;
            allOldStateDetached = !f.Manager.GetMatchingCharacter(f.Leader.Identity).GetMatchingCharacterQueues().Any()
                && f.RealGroups.All(group => group.IsFinalised && !group.GetMatchingMaps().Any());
            f.Join(false); f.Manager.Update(.01);
        });
        f.Leave(true); Assert.True(replaced);
        Assert.Equal(MatchType.Dungeon, Assert.Single(f.Manager.GetMatchingCharacter(f.Leader.Identity)
            .GetMatchingCharacterQueues()).MatchingQueueProposal.MatchType);
        object last = f.Messages(f.LeaderSession)
            .Last(message => message is ServerMatchingQueueJoin or ServerMatchingLeftQueue or ServerMatchingQueueResultAnnounce);
        Assert.True(allOldStateDetached && last is ServerMatchingQueueJoin,
            $"All original queue state detached before feedback: {allOldStateDetached}; last notice: {last.GetType().Name}");
    }

    [Fact]
    public void OrdinaryLeaveAllStillPublishesBothOldQueueRemovalsAfterDetachment()
    {
        var f = new Fixture(realBackend: true);
        f.Join(false); f.Manager.Update(.01); f.Join(false, type: MatchType.Adventure); f.Manager.Update(.01);
        f.OnMessage(f.LeaderSession, message =>
        {
            if (message is ServerMatchingQueueResultAnnounce { Result: MatchingQueueResult.Left })
                Assert.Empty(f.Manager.GetMatchingCharacter(f.Leader.Identity).GetMatchingCharacterQueues());
        });
        f.Leave(true);
        Assert.Equal(2, f.Packets<ServerMatchingLeftQueue>(f.LeaderSession).Count());
        Assert.Equal(2, f.Packets<ServerMatchingQueueResultAnnounce>(f.LeaderSession).Count(p => p.Result == MatchingQueueResult.Left));
        Assert.All(f.RealGroups, group => Assert.True(group.IsFinalised));
    }

    [Fact]
    public void RoleCancellationNoticeSeesTheOriginalAdmittedQueueAlreadyDetached()
    {
        var f = new Fixture(realBackend: true);
        f.Join(false); f.Manager.Update(.01); f.JoinParty(); bool observed = false;
        f.OnMessage(f.LeaderSession, message =>
        {
            if (message is not ServerMatchingQueueResultAnnounce { Result: MatchingQueueResult.Role }) return;
            observed = true;
            Assert.Empty(f.Manager.GetMatchingCharacter(f.Leader.Identity).GetMatchingCharacterQueues());
            Assert.All(f.RealGroups, group => Assert.True(group.IsFinalised));
        });
        f.Leave(false); Assert.True(observed);
    }
}
