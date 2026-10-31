using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Reward;
using NexusForever.Game.Static.Spell;
using NexusForever.Network.World.Message.Model.Reward;

namespace NexusForever.Game.Account.Reward
{
    public partial class RewardPropertyManager
    {
        public void UpdateContractSpellModifier(IPlayer actor, uint effectId, uint spellId, uint sourceEffectId,
            uint trackType, float value, bool removed)
        {
            lock (gate)
            {
                if (challenge == null || trackType > 1)
                {
                    UpdateRewardProperty(RewardPropertyType.RewardTrackType, removed ? -value : value, trackType); return;
                }
                bool supported = SupportedContractEffect(sourceEffectId, trackType, value);
                if (challenge.UpdateContractSpell(actor, effectId, spellId, sourceEffectId, trackType, value, removed, supported))
                    account.Session.EnqueueMessageEncrypted(new ServerPremiumRewards { Properties = challenge.Build().ToList() });
            }
        }

        private bool SupportedContractEffect(uint sourceEffectId, uint type, float value)
        {
            var row = gameTableManager.Spell4Effects?.GetEntry(sourceEffectId);
            return value == 0.5f && row != null && row.EffectType == SpellEffectType.RewardPropertyModifier
                && row.DataBits00 == 4 && row.DataBits01 == type && row.DataBits02 == 0x3F000000
                && row.DataBits03 == 0 && row.DataBits04 == 0 && row.DataBits05 == 0
                && row.DataBits06 == 0 && row.DataBits07 == 0 && row.DataBits08 == 0 && row.DataBits09 == 0
                && row.ParameterType is { Length: 4 } && row.ParameterType.All(parameter => (uint)parameter == 0)
                && row.ParameterValue is { Length: 4 } && row.ParameterValue.All(parameter => parameter == 0);
        }

        public bool CaptureContractModifier(IPlayer actor, uint trackId, uint trackType, out ContractRewardModifierSnapshot snapshot)
        {
            lock (gate)
            {
                snapshot = null;
                var track = gameTableManager.RewardTrack?.GetEntry(trackId);
                var byId = gameTableManager.RewardProperty?.GetEntry((uint)RewardPropertyType.RewardTrackId);
                if (actor == null || actor.Session == null || !ReferenceEquals(actor.Account, account)
                    || !ReferenceEquals(actor.Session, account.Session) || trackType > 1
                    || track == null || track.RewardTrackTypeEnum != trackType || track.RewardTrackIdParent != 0 || track.Flags != 0
                    || gameTableManager.Spell4Effects == null || byId?.RewardModifierValueTypeEnum != 0
                    || challenge?.Entry.Id != (uint)RewardPropertyType.RewardTrackType || challenge.Entry.RewardModifierValueTypeEnum != 0
                    || !rewardProperties.TryGetValue(RewardPropertyType.RewardTrackType, out var current) || !ReferenceEquals(current, challenge)) return false;
                // No actual property3 Contract sources were found. Preserve its
                // legacy value API while declining an unmapped nonneutral bonus.
                var specific = rewardProperties.GetValueOrDefault(RewardPropertyType.RewardTrackId)?.GetValue(trackId);
                if (specific.HasValue && (!float.IsFinite(specific.Value) || specific.Value != 0)) return false;
                return challenge.CaptureContract(actor, trackId, trackType, out snapshot);
            }
        }
    }
}
