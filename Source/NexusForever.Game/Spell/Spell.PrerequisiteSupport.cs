using System.Reflection;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Spell
{
    public partial class Spell
    {
        internal bool HasPersistentEffectOfType(SpellEffectType effectType)
        {
            foreach (SpellTargetInfo targetInfo in targets)
            {
                foreach (ISpellTargetEffectInfo effectInfo in targetInfo.Effects)
                {
                    if (effectInfo is not SpellTargetInfo.SpellTargetEffectInfo concrete)
                        continue;

                    if (concrete.LifetimeEnded)
                        continue;

                    if (concrete.Entry.EffectType != effectType)
                        continue;

                    if (lifetimeEvents.ContainsKey(effectInfo))
                        return true;
                }
            }

            return false;
        }

        internal bool HasPersistentEffectInEffectGroup(uint effectGroupId, IGameTableManager gameTableManager)
        {
            if (effectGroupId == 0u)
                return false;

            foreach (SpellTargetInfo targetInfo in targets)
            {
                foreach (ISpellTargetEffectInfo effectInfo in targetInfo.Effects)
                {
                    if (effectInfo is not SpellTargetInfo.SpellTargetEffectInfo concrete)
                        continue;

                    if (concrete.LifetimeEnded)
                        continue;

                    if (!lifetimeEvents.ContainsKey(effectInfo))
                        continue;

                    Spell4EffectGroupListEntry list = gameTableManager.Spell4EffectGroupList.GetEntry(concrete.Entry.Spell4EffectGroupListId);
                    if (Spell4EffectGroupListContainsGroupId(list, effectGroupId))
                        return true;
                }
            }

            return false;
        }

        internal bool HasPersistentTargetMechanicFlags(uint requiredFlags)
        {
            if (requiredFlags == 0u)
                return false;

            uint flags = Parameters.SpellInfo.BaseInfo.TargetMechanics?.Flags ?? 0u;
            return (flags & requiredFlags) != 0u;
        }

        internal static bool Spell4EffectGroupListContainsGroupId(Spell4EffectGroupListEntry entry, uint groupId)
        {
            if (entry == null || groupId == 0u)
                return false;

            foreach (FieldInfo field in typeof(Spell4EffectGroupListEntry).GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!field.Name.StartsWith("Spell4EffectGroupId", System.StringComparison.Ordinal))
                    continue;

                if ((uint)field.GetValue(entry) == groupId)
                    return true;
            }

            return false;
        }
    }
}
