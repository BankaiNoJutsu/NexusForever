using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Reward;
using NexusForever.Game.Abstract.Account.Entitlement;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Abstract.Account.Reward
{
    public interface IRewardPropertyManager
    {
        void Initialise(IPlayer player);

        void SendInitialPackets();

        /// <summary>
        /// Returns a <see cref="IRewardProperty"/> with the supplied <see cref="RewardPropertyType"/>.
        /// </summary>
        IRewardProperty GetRewardProperty(RewardPropertyType type);

        /// <summary>
        /// Update <see cref="RewardPropertyType"/> with supplied value and data.
        /// </summary>
        /// <remarks>
        /// A positive value will increment and a negative value will decrement the value.
        /// </remarks>
        void UpdateRewardProperty(RewardPropertyType type, float value, uint data = 0);

        /// <summary>
        /// Update <see cref="RewardPropertyEntry"/> with supplied value and data.
        /// </summary>
        /// <remarks>
        /// A positive value will increment and a negative value will decrement the value.
        /// </remarks>
        void UpdateRewardProperty(RewardPropertyEntry entry, float value, uint data = 0);

        /// <summary>Track one Challenge modifier lifetime; legacy implementations retain their delta behavior.</summary>
        void UpdateChallengeSpellModifier(IPlayer actor, uint effectId, uint spellId, float value, bool removed)
            => UpdateRewardProperty(RewardPropertyType.RewardTrackType, removed ? -value : value, 3);

        /// <summary>Refresh derived state after the real entitlement owner acknowledges its Auth commit.</summary>
        void RefreshCommittedEntitlement(IAccountEntitlement entitlement, uint amount, ulong revision) { }

        /// <summary>Capture the concrete Challenge projection; legacy adapters do not opt in.</summary>
        bool CaptureChallengeModifier(out ChallengeRewardModifierSnapshot snapshot)
        {
            snapshot = default;
            return false;
        }

        /// <summary>Track an actual Contract effect lifetime; legacy managers retain raw delta behavior.</summary>
        void UpdateContractSpellModifier(IPlayer actor, uint effectId, uint spellId, uint sourceEffectId,
            uint trackType, float value, bool removed)
            => UpdateRewardProperty(RewardPropertyType.RewardTrackType, removed ? -value : value, trackType);

        /// <summary>Capture the current concrete actor's Contract projection; legacy adapters do not opt in.</summary>
        bool CaptureContractModifier(IPlayer actor, uint trackId, uint trackType, out ContractRewardModifierSnapshot snapshot)
        {
            snapshot = null; return false;
        }
    }
}
