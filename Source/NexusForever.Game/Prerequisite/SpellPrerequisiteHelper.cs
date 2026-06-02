using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Prerequisite
{
    internal static class SpellPrerequisiteHelper
    {
        public static bool HasActiveCoolDownNode(IPlayer player, uint spellCoolDownId, IGameTableManager gameTableManager)
        {
            if (player.SpellManager is not SpellManager spellManager)
                return false;

            return spellManager.HasActiveCoolDownNode(spellCoolDownId, gameTableManager);
        }

        public static bool KnowsSpellReferencingCoolDownNode(IPlayer player, uint spellCoolDownId, IGameTableManager gameTableManager)
        {
            if (player.SpellManager is not SpellManager spellManager)
                return false;

            return spellManager.KnowsSpellReferencingCoolDownNode(spellCoolDownId, gameTableManager);
        }

        public static bool HasActiveSpellEffectType(IUnitEntity unit, SpellEffectType effectType)
        {
            return unit is UnitEntity unitEntity && unitEntity.HasActiveSpellEffectType(effectType);
        }

        public static bool HasActiveSpellEffectGroup(IUnitEntity unit, uint effectGroupId, IGameTableManager gameTableManager)
        {
            return unit is UnitEntity unitEntity && unitEntity.HasActiveSpellEffectGroup(effectGroupId, gameTableManager);
        }

        public static bool HasActiveSpellTargetMechanic(IUnitEntity unit, uint mechanicFlags)
        {
            return unit is UnitEntity unitEntity && unitEntity.HasActiveSpellTargetMechanic(mechanicFlags);
        }

        public static bool HasActiveSpell4(IUnitEntity unit, uint spell4Id)
        {
            if (unit is UnitEntity unitEntity)
                return unitEntity.HasActiveSpell4(spell4Id);

            return unit.HasTrackedSpellState(spell4Id);
        }

        public static bool IsUnderSpell(IUnitEntity unit, uint spell4Id) =>
            HasActiveSpell4(unit, spell4Id);

        public static byte GetSpellTierRank(IPlayer player, uint spell4Id, IGameTableManager gameTableManager)
        {
            Spell4Entry entry = gameTableManager.Spell4.GetEntry(spell4Id);
            if (entry == null)
                return 0;

            if (player.SpellManager.GetSpell(entry.Spell4BaseIdBaseSpell) == null)
                return 0;

            return player.SpellManager.GetSpellTier(entry.Spell4BaseIdBaseSpell);
        }

        public static bool MeetsSpellTier(
            IPlayer player,
            PrerequisiteComparison comparison,
            uint requiredTier,
            uint spell4Id,
            IGameTableManager gameTableManager)
        {
            uint tier = GetSpellTierRank(player, spell4Id, gameTableManager);
            return PrerequisiteCompare.Compare(comparison, tier, requiredTier);
        }
    }
}
