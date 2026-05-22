using NexusForever.Game;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Game.Matching;
using NexusForever.Game.Static.Matching;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Matching;

public class RetailMatchingRealmRulesTests
{
    [Fact]
    public void AreRealmCompatible_WhenRealmOnly_MatchesSameRealmMembers()
    {
        Identity exileA = new() { RealmId = 1, Id = 10 };
        Identity exileB = new() { RealmId = 1, Id = 20 };
        Identity dominion = new() { RealmId = 2, Id = 30 };

        IPlayer playerA = CreatePlayer(exileA);
        IPlayer playerB = CreatePlayer(exileB);
        IPlayer playerC = CreatePlayer(dominion);

        IPlayerManager playerManager = new StubPlayerManager(playerA, playerB, playerC);

        IMatchingQueueProposal realmOnly = CreateProposal(MatchingQueueFlags.RealmOnly, exileA);
        IMatchingQueueProposal mixed = CreateProposal(MatchingQueueFlags.None, dominion);

        Assert.True(RetailMatchingRealmRules.AreRealmCompatible(realmOnly, realmOnly, playerManager));
        Assert.False(RetailMatchingRealmRules.AreRealmCompatible(realmOnly, mixed, playerManager));
    }

    static IPlayer CreatePlayer(Identity identity)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> proxy);
        proxy.SetProperty(nameof(IPlayer.Identity), identity);
        return player;
    }

    static IMatchingQueueProposal CreateProposal(MatchingQueueFlags flags, Identity member)
    {
        IMatchingQueueProposal proposal = RecordingDispatchProxy<IMatchingQueueProposal>.Create(out RecordingDispatchProxy<IMatchingQueueProposal> proxy);
        proxy.SetProperty(nameof(IMatchingQueueProposal.MatchingQueueFlags), flags);
        proxy.SetMethodReturn(nameof(IMatchingQueueProposal.GetMembers), new[]
        {
            CreateMember(member)
        });
        return proposal;
    }

    static IMatchingQueueProposalMember CreateMember(Identity identity)
    {
        IMatchingQueueProposalMember member = RecordingDispatchProxy<IMatchingQueueProposalMember>.Create(out RecordingDispatchProxy<IMatchingQueueProposalMember> proxy);
        proxy.SetProperty(nameof(IMatchingQueueProposalMember.Identity), identity);
        return member;
    }

    sealed class StubPlayerManager(params IPlayer[] players) : IPlayerManager
    {
        readonly Dictionary<Identity, IPlayer> byIdentity = players.ToDictionary(p => p.Identity);

        public IPlayer GetPlayer(Identity identity) => byIdentity.GetValueOrDefault(identity);
        public IPlayer GetPlayer(ulong characterId) => byIdentity.Values.FirstOrDefault(p => p.Identity.Id == characterId);
        public IPlayer GetPlayer(string name) => throw new NotImplementedException();
        public IPlayer GetPlayerByAccountId(uint accountId) => throw new NotImplementedException();
        public void AddPlayer(IPlayer player) => throw new NotImplementedException();
        public void RemovePlayer(IPlayer player) => throw new NotImplementedException();
        public IEnumerator<IPlayer> GetEnumerator() => byIdentity.Values.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
