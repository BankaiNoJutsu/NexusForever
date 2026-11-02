using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Reputation;
using NexusForever.GameTable;

namespace NexusForever.Game.Quest
{
    public sealed class PlayerCommunicatorView(IPlayer player, IQuestManager quests, IGameTableManager tables,
        IPrerequisiteManager prerequisites = null) : ICommunicatorPlayerView
    {
        public uint WorldId => player.Map?.Entry.Id ?? 0;
        public Class Class => player.Class;
        public Race Race => player.Race;
        public Faction Faction => player.Faction1;
        public uint Level => player.Level;
        public bool IsInZone(uint requested)
        {
            var visited = new HashSet<uint>();
            var zone = player.Zone;
            while (zone != null && visited.Add(zone.Id))
            {
                if (zone.Id == requested || zone.ParentZoneId == requested && requested != 0) return true;
                if (zone.ParentZoneId == 0) break;
                zone = tables?.WorldZone?.GetEntry(zone.ParentZoneId);
            }
            return false;
        }
        public float GetReputation(Faction faction) => player.ReputationManager?.GetReputation(faction)?.Amount ?? 0;
        public CommunicatorQuestSnapshot? GetQuest(ushort questId) => quests?.GetCommunicatorSnapshot(questId);
        public bool MeetsPrerequisite(uint id) => (prerequisites ?? throw new InvalidOperationException("Communicator requires its prerequisite service.")).Meets(player, id);
        public CommunicatorOfferBlock CheckOffer(ushort questId) => quests?.GetCallOfferBlock(questId) ?? CommunicatorOfferBlock.CannotStart;
    }
}
