using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Spell
{
    /// <summary>
    /// Which spells a set of equipped items grants, and which of those the server can both cast and take away again.
    /// </summary>
    /// <remarks>
    /// Read from the tables (Decomp/Analysis/EQUIPMENT_SPELL_GRANTS_20261110.md); nothing here is read from native code or a capture.
    /// <list type="bullet">
    /// <item>An item with <c>Item2.itemSpecialId00</c> grants <c>ItemSpecial.spell4IdOnEquip</c> while it is equipped (1,185 of 6,035 ItemSpecial rows carry one).</item>
    /// <item>A rune is an item of its own (family 33) socketed into gear, with its own ItemSpecial: the "Rune Special - ..." spells.</item>
    /// <item>A rune also carries <c>ItemRuneInstance.itemSetId</c> and <c>itemSetPower</c> (1 to 3). The powers of the socketed runes of one set add up, and each
    /// <c>ItemSetBonus</c> of the set whose <c>requiredPower</c> is reached grants its <c>spell4Id</c> (135 of 749 bonuses carry one). That power adds up across all equipped
    /// gear is assumed.</item>
    /// </list>
    /// No retail parity claim.
    /// </remarks>
    public static class EquipmentSpellRules
    {
        private static readonly ConditionalWeakTable<GameTable<Spell4EffectsEntry>, Dictionary<uint, bool>> supportedCache = new();
        private static readonly ConditionalWeakTable<GameTable<Spell4EffectsEntry>, Dictionary<uint, bool>> persistentCache = new();

        /// <summary>
        /// Return the Spell4 ids granted by the supplied equipped items, before any prerequisite is applied.
        /// </summary>
        public static IEnumerable<(uint Spell4Id, uint PrerequisiteId, IItem Item)> GetGrantedSpells(IEnumerable<IItem> equippedItems, IGameTableManager tables)
        {
            if (equippedItems == null || tables == null)
                yield break;

            var setPower = new Dictionary<uint, uint>();
            foreach (IItem item in equippedItems)
            {
                Item2Entry entry = item?.Info?.Entry;
                if (entry == null)
                    continue;

                foreach ((uint spell4Id, uint prerequisiteId) in GetSpecial(entry.ItemSpecialId00, tables))
                    yield return (spell4Id, prerequisiteId, item);

                foreach (ItemRuneSlot slot in item.RuneSlots ?? [])
                {
                    Item2Entry rune = slot.RuneItem2Id == 0u ? null : tables.Item?.GetEntry(slot.RuneItem2Id);
                    if (rune == null)
                        continue;

                    foreach ((uint spell4Id, uint prerequisiteId) in GetSpecial(rune.ItemSpecialId00, tables))
                        yield return (spell4Id, prerequisiteId, item);

                    ItemRuneInstanceEntry instance = rune.ItemRuneInstanceId == 0u ? null : tables.ItemRuneInstance?.GetEntry(rune.ItemRuneInstanceId);
                    if (instance != null && instance.ItemSetId != 0u && instance.ItemSetPower != 0u)
                        setPower[instance.ItemSetId] = setPower.GetValueOrDefault(instance.ItemSetId) + instance.ItemSetPower;
                }
            }

            foreach ((uint itemSetId, uint power) in setPower)
            {
                ItemSetEntry set = tables.ItemSet?.GetEntry(itemSetId);
                if (set == null)
                    continue;

                foreach (uint bonusId in GetBonusIds(set))
                {
                    ItemSetBonusEntry bonus = bonusId == 0u ? null : tables.ItemSetBonus?.GetEntry(bonusId);
                    if (bonus != null && bonus.Spell4Id != 0u && bonus.RequiredPower <= power)
                        yield return (bonus.Spell4Id, 0u, null);
                }
            }
        }

        private static IEnumerable<(uint Spell4Id, uint PrerequisiteId)> GetSpecial(uint itemSpecialId, IGameTableManager tables)
        {
            ItemSpecialEntry special = itemSpecialId == 0u ? null : tables.ItemSpecial?.GetEntry(itemSpecialId);
            if (special != null && special.Spell4IdOnEquip != 0u)
                yield return (special.Spell4IdOnEquip, special.PrerequisiteIdGeneric00);
        }

        private static uint[] GetBonusIds(ItemSetEntry set) =>
        [
            set.ItemSetBonusId00, set.ItemSetBonusId01, set.ItemSetBonusId02, set.ItemSetBonusId03, set.ItemSetBonusId04, set.ItemSetBonusId05,
            set.ItemSetBonusId06, set.ItemSetBonusId07, set.ItemSetBonusId08, set.ItemSetBonusId09, set.ItemSetBonusId10, set.ItemSetBonusId11
        ];

        /// <summary>
        /// Whether the server can grant the spell on equip and take everything it did away again on unequip.
        /// </summary>
        /// <remarks>
        /// Only spells without a cast time, channel or finite duration whose rows are property modifiers, procs, proxies or visuals, none with a duration of its own. Those are
        /// either tracked on the unit by spell id (and removed by <see cref="IUnitEntity.RemoveTrackedSpellStates"/>) or rows that live with the running spell (ended by
        /// <see cref="IUnitEntity.EndSpellLifetimes"/>). Anything else is left alone rather than granted without a way to revoke it.
        /// </remarks>
        public static bool IsSupported(uint spell4Id, IGameTableManager tables)
        {
            Spell4Entry spell = tables?.Spell4?.GetEntry(spell4Id);
            GameTable<Spell4EffectsEntry> effects = tables?.Spell4Effects;
            if (spell == null || effects == null
                || spell.CastTime != 0u || spell.ChannelMaxTime != 0u || spell.ChannelInitialDelay != 0u || spell.ChannelPulseTime != 0u
                || (spell.SpellDuration != 0u && spell.SpellDuration != uint.MaxValue))
                return false;

            Dictionary<uint, bool> cache = supportedCache.GetValue(effects, _ => []);
            lock (cache)
            {
                if (!cache.TryGetValue(spell4Id, out bool supported))
                {
                    Spell4EffectsEntry[] rows = effects.Entries.Where(e => e.SpellId == spell4Id).ToArray();
                    supported = rows.Length != 0 && rows.All(IsSupportedRow);
                    cache[spell4Id] = supported;
                }

                return supported;
            }
        }

        /// <summary>
        /// Whether a granted spell leaves something on the unit that shows it is still in place (a tracked state or a running spell), so that a missing one can be granted again.
        /// </summary>
        public static bool LeavesPresence(uint spell4Id, IGameTableManager tables)
        {
            GameTable<Spell4EffectsEntry> effects = tables?.Spell4Effects;
            if (effects == null)
                return false;

            Dictionary<uint, bool> cache = persistentCache.GetValue(effects, _ => []);
            lock (cache)
            {
                if (!cache.TryGetValue(spell4Id, out bool persistent))
                {
                    persistent = effects.Entries.Any(e => e.SpellId == spell4Id
                        && (e.EffectType is SpellEffectType.UnitPropertyModifier or SpellEffectType.Proc || e.TickTime != 0u));
                    cache[spell4Id] = persistent;
                }

                return persistent;
            }
        }

        private static bool IsSupportedRow(Spell4EffectsEntry row)
        {
            if (row.DurationTime != 0u)
                return false;

            return row.EffectType switch
            {
                SpellEffectType.UnitPropertyModifier => row.TickTime == 0u && row.DelayTime == 0u,
                SpellEffectType.Proc                 => row.TickTime == 0u && row.DelayTime == 0u,
                SpellEffectType.Fluff                => row.TickTime == 0u,
                // a ticking proxy must be marked as living with the spell (effect flag 0x2); a plain proxy fires once when the item is equipped
                SpellEffectType.Proxy                => row.TickTime == 0u || (row.Flags & 2u) != 0u,
                _                                    => false
            };
        }
    }
}
