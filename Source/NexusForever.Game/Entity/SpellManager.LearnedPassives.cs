using NexusForever.Game.Spell;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Entity
{
    public partial class SpellManager
    {
        private readonly HashSet<uint> pendingLearnedPassives = [];
        private double learnedPassiveRetryTime;

        public bool TryApplyLearnedPassive(uint spell4Id)
        {
            Spell4Entry entry = GetGameTableManager().Spell4?.GetEntry(spell4Id);
            var known = entry == null ? null : GetSpell(entry.Spell4BaseIdBaseSpell);
            if (known == null || known.Tier == 0 || known.SpellInfo?.Entry?.Id != spell4Id
                || !LearnedPassiveSpellRules.IsLearnedPassive(spell4Id, GetGameTableManager()))
            {
                pendingLearnedPassives.Remove(spell4Id);
                return false;
            }

            if (!player.InWorld || !player.IsAlive)
            {
                pendingLearnedPassives.Add(spell4Id);
                return false;
            }

            CastResult result = player.TryCastSpell(spell4Id, new SpellParameters
            {
                PrimaryTargetId = player.Guid,
                UserInitiatedSpellCast = false,
                IgnoreGlobalCooldown = true,
                ClientRequestSource = nameof(SpellManager) + ".LearnedPassive"
            });
            if (result == CastResult.Ok)
                pendingLearnedPassives.Remove(spell4Id);
            else
                pendingLearnedPassives.Add(spell4Id);
            return result == CastResult.Ok;
        }

        private void ReapplyLearnedPassives()
        {
            foreach (var spell in spells.Values.ToList())
                if (spell.SpellInfo?.Entry != null)
                    TryApplyLearnedPassive(spell.SpellInfo.Entry.Id);
        }

        private void UpdateLearnedPassives(double lastTick)
        {
            if (pendingLearnedPassives.Count == 0 || !player.InWorld || !player.IsAlive)
                return;
            learnedPassiveRetryTime -= lastTick;
            if (learnedPassiveRetryTime > 0d)
                return;
            learnedPassiveRetryTime = 1d;
            foreach (uint id in pendingLearnedPassives.ToArray())
                TryApplyLearnedPassive(id);
        }
    }
}
