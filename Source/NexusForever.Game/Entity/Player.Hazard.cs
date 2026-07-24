using NexusForever.Game.Static.Hazard;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.Hazard;

namespace NexusForever.Game.Entity
{
    public partial class Player
    {
        private readonly HazardStateCollection hazardStates = new();

        public bool TryEnableHazard(uint effectId, uint hazardId, out string skippedReason)
        {
            HazardEntry entry = gameTableManager?.Hazard?.GetEntry(hazardId);
            if (!hazardStates.TryEnable(effectId, entry, out bool firstEnable, out skippedReason))
                return false;

            SendHazardList();
            if (firstEnable)
            {
                Session?.EnqueueMessageEncrypted(new ServerHazardAction
                {
                    HazardId = hazardId,
                    Action   = HazardAction.Enable
                });
            }

            return true;
        }

        public bool RemoveHazard(uint effectId)
        {
            if (!hazardStates.RemoveEnable(effectId, out uint hazardId, out bool lastEnable))
                return false;

            if (lastEnable)
            {
                Session?.EnqueueMessageEncrypted(new ServerHazardAction
                {
                    HazardId = hazardId,
                    Action   = HazardAction.Remove
                });
            }
            else
            {
                SendHazardList();
            }

            return true;
        }

        public bool TryModifyHazard(uint hazardId, float amount, out string skippedReason)
        {
            if (!hazardStates.TryModifyMeter(hazardId, amount, out skippedReason))
                return false;

            SendHazardList();
            return true;
        }

        public bool TrySuspendHazard(uint effectId, uint hazardId, uint targetMode, out string skippedReason)
        {
            HazardEntry entry = gameTableManager?.Hazard?.GetEntry(hazardId);
            if (!hazardStates.TrySuspend(effectId, entry, targetMode, out skippedReason))
                return false;

            SendHazardModifiers();
            SendHazardList();
            return true;
        }

        public bool RemoveHazardSuspension(uint effectId)
        {
            if (!hazardStates.RemoveSuspension(effectId))
                return false;

            SendHazardModifiers();
            SendHazardList();
            return true;
        }

        private void SendHazardList()
        {
            Session?.EnqueueMessageEncrypted(new ServerHazardList
            {
                Hazards = hazardStates.BuildHazards()
            });
        }

        private void SendHazardModifiers()
        {
            int hazardIdCount = 0;
            if (gameTableManager?.Hazard?.Entries != null)
            {
                uint maxHazardId = gameTableManager.Hazard.Entries
                    .Where(entry => entry != null)
                    .Select(entry => entry.Id)
                    .DefaultIfEmpty(0u)
                    .Max();
                hazardIdCount = checked((int)maxHazardId + 1);
            }

            Session?.EnqueueMessageEncrypted(hazardStates.BuildModifiers(hazardIdCount));
        }
    }
}
