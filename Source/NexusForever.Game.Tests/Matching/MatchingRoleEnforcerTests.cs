using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Game.Matching.Queue;
using NexusForever.Game.Static.Matching;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Matching;

public class MatchingRoleEnforcerTests
{
    [Fact]
    public void Check_FitsFlexibleRolesIntoOneTankOneHealerThreeDps()
    {
        var enforcer = new MatchingRoleEnforcer(new ResultFactory());
        IMatchingQueueProposalMember[] members =
        [
            CreateMember(1ul, Role.Tank | Role.DPS),
            CreateMember(2ul, Role.Healer),
            CreateMember(3ul, Role.DPS),
            CreateMember(4ul, Role.DPS),
            CreateMember(5ul, Role.DPS)
        ];

        IMatchingRoleEnforcerResult result = enforcer.Check(members);

        Assert.True(result.Success);
        Assert.Collection(result.Members,
            member => Assert.Equal(Role.Tank, member.Role),
            member => Assert.Equal(Role.Healer, member.Role),
            member => Assert.Equal(Role.DPS, member.Role),
            member => Assert.Equal(Role.DPS, member.Role),
            member => Assert.Equal(Role.DPS, member.Role));
    }

    [Fact]
    public void Check_RejectsTooManySingleRoleDps()
    {
        var enforcer = new MatchingRoleEnforcer(new ResultFactory());
        IMatchingQueueProposalMember[] members =
        [
            CreateMember(1ul, Role.DPS),
            CreateMember(2ul, Role.DPS),
            CreateMember(3ul, Role.DPS),
            CreateMember(4ul, Role.DPS)
        ];

        IMatchingRoleEnforcerResult result = enforcer.Check(members);

        Assert.False(result.Success);
        Assert.Collection(result.Members,
            member => Assert.Equal(Role.DPS, member.Role),
            member => Assert.Equal(Role.DPS, member.Role),
            member => Assert.Equal(Role.DPS, member.Role),
            member => Assert.Equal(Role.DPS, member.Role));
    }

    [Fact]
    public void Check_RejectsMemberWithoutRole()
    {
        var enforcer = new MatchingRoleEnforcer(new ResultFactory());
        IMatchingQueueProposalMember[] members =
        [
            CreateMember(1ul, Role.Tank),
            CreateMember(2ul, Role.Healer),
            CreateMember(3ul, Role.None)
        ];

        IMatchingRoleEnforcerResult result = enforcer.Check(members);

        Assert.False(result.Success);
        Assert.Contains(result.Members, member => member.Role == Role.None);
    }

    private static IMatchingQueueProposalMember CreateMember(ulong characterId, Role roles)
    {
        IMatchingQueueProposalMember member = RecordingDispatchProxy<IMatchingQueueProposalMember>.Create(out RecordingDispatchProxy<IMatchingQueueProposalMember> memberProxy);
        memberProxy.SetProperty(nameof(IMatchingQueueProposalMember.Identity), new Identity
        {
            RealmId = 1,
            Id      = characterId
        });
        memberProxy.SetProperty(nameof(IMatchingQueueProposalMember.Roles), roles);
        return member;
    }

    private sealed class ResultFactory : IFactory<IMatchingRoleEnforcerResult>
    {
        public IMatchingRoleEnforcerResult Resolve()
        {
            return new MatchingRoleEnforcerResult();
        }
    }
}
