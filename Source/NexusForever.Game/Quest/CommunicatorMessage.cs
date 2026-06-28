using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Reputation;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Story;

namespace NexusForever.Game.Quest
{
    public class CommunicatorMessage : ICommunicatorMessage
    {
        public uint Id => entry.Id;
        public ushort QuestId => (ushort)entry.QuestIdDelivered;
        public bool DeliversQuest => entry.QuestIdDelivered != 0u;

        private readonly CommunicatorMessagesEntry entry;
        private readonly IPrerequisiteManager prerequisiteManager;
        private readonly IGameTableManager gameTableManager;

        /// <summary>
        /// Create a new <see cref="ICommunicatorMessage"/> with supplied <see cref="CommunicatorMessagesEntry"/>.
        /// </summary>
        public CommunicatorMessage(
            CommunicatorMessagesEntry entry,
            IPrerequisiteManager prerequisiteManager = null,
            IGameTableManager gameTableManager = null)
        {
            this.entry = entry;
            this.prerequisiteManager = prerequisiteManager;
            this.gameTableManager = gameTableManager;
        }

        /// <summary>
        /// Checks if <see cref="IPlayer"/> meets the required conditions for this quest to be added to their communicator.
        /// </summary>
        public bool Meets(IPlayer player)
        {
            if (player.Map == null)
                return false;

            if (entry.WorldId != 0u && entry.WorldId != player.Map.Entry.Id)
                return false;

            if (entry.WorldZoneId != 0u && !IsInWorldZone(player.Zone, entry.WorldZoneId))
                return false;

            if (entry.MinLevel != 0u && player.Level < entry.MinLevel)
                return false;

            if (entry.MaxLevel != 0u && player.Level > entry.MaxLevel)
                return false;

            for (int i = 0; i < entry.Quests.Length; i++)
            {
                ushort questId = (ushort)entry.Quests[i];
                if (questId == 0)
                    continue;

                if (!CommunicatorQuestState.Meets(player.QuestManager.GetQuestState(questId), entry.States[i]))
                    return false;
            }

            if (entry.FactionId != 0u && (Faction)entry.FactionId != player.Faction1)
                return false;

            if (entry.ClassId != 0u && (Class)entry.ClassId != player.Class)
                return false;

            if (entry.FactionIdReputation != 0u && !MeetsReputation(player))
                return false;

            if (entry.PrerequisiteId != 0u && !GetPrerequisiteManager().Meets(player, entry.PrerequisiteId))
                return false;

            return true;
        }

        private bool IsInWorldZone(WorldZoneEntry zone, uint worldZoneId)
        {
            if (zone == null || worldZoneId == 0u)
                return false;

            HashSet<uint> visitedZones = [];
            WorldZoneEntry currentZone = zone;
            while (currentZone != null && visitedZones.Add(currentZone.Id))
            {
                if (currentZone.Id == worldZoneId)
                    return true;

                if (currentZone.ParentZoneId == 0u)
                    return false;

                if (currentZone.ParentZoneId == worldZoneId)
                    return true;

                currentZone = gameTableManager?.WorldZone?.GetEntry(currentZone.ParentZoneId);
            }

            return false;
        }

        private IPrerequisiteManager GetPrerequisiteManager()
        {
            return prerequisiteManager ?? throw new InvalidOperationException($"{nameof(CommunicatorMessage)} requires an {nameof(IPrerequisiteManager)}.");
        }

        private bool MeetsReputation(IPlayer player)
        {
            float amount = player.ReputationManager.GetReputation((Faction)entry.FactionIdReputation)?.Amount ?? 0f;
            return amount >= entry.ReputationMin
                && (entry.ReputationMax == 0u || amount <= entry.ReputationMax);
        }

        /// <summary>
        /// Send communicator message to <see cref="IGameSession"/>.
        /// </summary>
        public void Send(IGameSession session)
        {
            if (entry.LocalizedTextIdMessage == 0u)
                return;

            session.EnqueueMessageEncrypted(new ServerCommunicatorMessage
            {
                CommunicatorMessagesId = (ushort)entry.Id
            });
        }
    }
}
