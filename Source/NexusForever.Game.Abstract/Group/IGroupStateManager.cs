using NexusForever.Game.Abstract.Entity;
using NexusForever.Shared;

namespace NexusForever.Game.Abstract.Group
{
    public interface IGroupStateManager : IUpdate
    {
        void UpdateGroup(GroupLootState group);
        void RemoveGroup(ulong groupId);
        void RemoveMember(ulong groupId, Identity identity);
        void RemoveCharacter(Identity identity);

        bool TryGetGroup(ulong groupId, out GroupLootState group);
        bool TryGetGroupForCharacter(Identity identity, out GroupLootState group);

        GroupLootMember NextRoundRobinWinner(GroupLootState group, IReadOnlyList<GroupLootMember> eligibleMembers);

        GroupLootMember ResolveHarvestLootRecipient(GroupLootState group, IPlayer harvester, IReadOnlyList<GroupLootMember> eligibleMembers);
    }
}
