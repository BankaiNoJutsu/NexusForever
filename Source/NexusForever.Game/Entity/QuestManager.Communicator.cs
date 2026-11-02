using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Quest;
using NexusForever.Game.Static;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Network.Session;

namespace NexusForever.Game.Entity
{
    public partial class QuestManager
    {
        private readonly CommunicatorTracker communicatorTracker;
        private readonly IGameSession communicatorSession;
        private readonly Abstract.Account.IAccount communicatorAccount;
        private readonly uint communicatorAccountId;
        private readonly ulong communicatorCharacter;
        private readonly ushort communicatorRealm;
        public ICommunicatorTracker Communicator => communicatorTracker;

        private bool CommunicatorActor() => communicatorSession != null && ReferenceEquals(player.QuestManager, this)
            && ReferenceEquals(player.Session, communicatorSession) && ReferenceEquals(player.Account, communicatorAccount)
            && ReferenceEquals(communicatorAccount?.Session, communicatorSession) && communicatorAccount?.Id == communicatorAccountId
            && player.CharacterId == communicatorCharacter && player.Identity?.RealmId == communicatorRealm
            && communicatorSession is not NetworkSession { IsDisconnecting: true }
            && player.LogoutManager?.State is not (LogoutState.Logout or LogoutState.Finished);

        private bool CommunicatorBusy() => normalSave != null || questRewardCaptureDepth != 0 || questRewards.Count != 0
            || HasContractCompletionWork || contractClaimOperation != null || player.ChallengeManager?.HasPendingRewardOperation == true
            || player.Inventory?.IsSaveBlocked == true || player is Player real && real.IsNormalSaveInProgress;

        private void UpdateCommunicator(double lastTick)
        {
            lock (questRewardGate)
            {
                if (questRewardReload || questRewardInvalidated || !CommunicatorActor()) { communicatorTracker.Clear(); return; }
                if (player.IsLoading || player.Map == null || CommunicatorBusy()) { communicatorTracker.Elapse(lastTick); return; }
                communicatorTracker.Update(lastTick);
            }
        }
        private void DeliverCommunicator(ICommunicatorMessage message, string reason)
        {
            if (!CommunicatorActor() || CommunicatorBusy() || questRewardReload || questRewardInvalidated) return;
            if (message.ServerPushed) message.Send(communicatorSession, true);
            if (!CommunicatorActor() || CommunicatorBusy() || questRewardInvalidated || questRewardReload)
            { questRewardReload = true; return; }
            if (message.Delivers) QuestMention(message.QuestId);
            log.Trace("Communicator {0} delivered to Character {1}: {2}.", message.Id, communicatorCharacter, reason);
        }
        public CommunicatorQuestSnapshot? GetCommunicatorSnapshot(ushort id)
        {
            var quest = GetQuest(id);
            if (quest == null || quest.PendingDelete) return null;
            bool active = quest.State is QuestState.Accepted or QuestState.Achieved
                && activeQuests.TryGetValue(id, out var current) && ReferenceEquals(current, quest);
            return new(quest.State, active, active ? quest.GetObjectiveCompletionMask() : (byte)0);
        }
        public CommunicatorOfferBlock GetCallOfferBlock(ushort id)
        {
            if (IsDisabled(DisableType.Quest, id)) return CommunicatorOfferBlock.Disabled;
            var quest = GetQuest(id);
            if (quest is { PendingDelete: false }) return CommunicatorOfferBlock.RecordExists;
            var info = GetGlobalQuestManager().GetQuestInfo(id);
            return CommunicatorConditions.Offer(null, info != null && MeetsPrerequisites(info), info?.Entry.Flags ?? 0);
        }
        private bool IsOfferedByGiverOrCall(IQuestInfo info, IQuest quest)
        {
            ushort id = (ushort)info.Entry.Id;
            if (GetGlobalQuestManager().GetQuestGivers(id).Any(c => player.GetVisibleCreature<WorldEntity>(c).Any())) return true;
            if (quest is { State: QuestState.Mentioned, PendingDelete: false } && info.IsQuestMentioned) return true;
            return CommunicatorConditions.OfferedByCall(GetGlobalQuestManager().GetQuestCommunicatorMessages(id),
                communicatorTracker.View, CommunicatorGates.Zone | CommunicatorGates.Offer);
        }
        public void QuestCommunicatorAction(ushort id, bool engaged)
        {
            lock (questRewardGate)
            {
                if (!engaged || !CommunicatorActor() || player.IsLoading || CommunicatorBusy() || questRewardReload || questRewardInvalidated) return;
                var info = GetGlobalQuestManager().GetQuestInfo(id);
                if (info == null || GetQuest(id) is { PendingDelete: false } || !info.IsQuestMentioned
                    || GetCallOfferBlock(id) != CommunicatorOfferBlock.None
                    || !CommunicatorConditions.OfferedByCall(GetGlobalQuestManager().GetQuestCommunicatorMessages(id),
                        communicatorTracker.View, CommunicatorGates.Zone | CommunicatorGates.Offer)) return;
                QuestMention(info);
            }
        }
    }
}
