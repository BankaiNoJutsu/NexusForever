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
        private static readonly ConditionalWeakTable<GameTable<Spell4EffectsEntry>, Dictionary<uint, bool>> stanceSupportedCache = new();
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
        public static bool IsSupported(uint spell4Id, IGameTableManager tables) => IsSupported(spell4Id, tables, false);

        /// <summary>
        /// Return the passive spell of the stance the player is in, or 0.
        /// </summary>
        /// <remarks>
        /// <c>Class.spell4IdInnateAbilityPassive00..02</c> runs parallel to <c>spell4IdInnateAbilityActive00..02</c>, the list the stance index selects from, and the spells are
        /// named "... - Stance - Passive" (Warrior Onslaught 44926 and Juggernaut 44932, Engineer Eradicate 45000 and Provoke 41504, three Stalker Nano Skins). They share one
        /// exclusive stack group per class. Read as "the passive of the current stance is in effect while the player is in it"; inferred from the table layout and the names.
        /// A stance passive also carries one-off rows (a resource reset, a SpellForceRemove), so those are allowed here. A class is served only when every one of its stance
        /// passives can be revoked, so that changing stance never leaves one behind. No retail parity claim.
        /// </remarks>
        public static uint GetStancePassive(ClassEntry classEntry, byte innateIndex, IGameTableManager tables)
        {
            uint[] passives = classEntry?.Spell4IdInnateAbilityPassive;
            if (passives == null || innateIndex >= passives.Length || passives[innateIndex] == 0u)
                return 0u;

            return passives.Where(id => id != 0u).All(id => IsSupported(id, tables, true)) ? passives[innateIndex] : 0u;
        }

        private static bool IsSupported(uint spell4Id, IGameTableManager tables, bool allowOneOffRows)
        {
            Spell4Entry spell = tables?.Spell4?.GetEntry(spell4Id);
            GameTable<Spell4EffectsEntry> effects = tables?.Spell4Effects;
            if (spell == null || effects == null
                || spell.CastTime != 0u || spell.ChannelMaxTime != 0u || spell.ChannelInitialDelay != 0u || spell.ChannelPulseTime != 0u
                || (spell.SpellDuration != 0u && spell.SpellDuration != uint.MaxValue))
                return false;

            Dictionary<uint, bool> cache = (allowOneOffRows ? stanceSupportedCache : supportedCache).GetValue(effects, _ => []);
            lock (cache)
            {
                if (!cache.TryGetValue(spell4Id, out bool supported))
                {
                    Spell4EffectsEntry[] rows = effects.Entries.Where(e => e.SpellId == spell4Id).ToArray();
                    supported = rows.Length != 0 && rows.All(row => IsSupportedRow(row) || allowOneOffRows && IsOneOffRow(row));
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

        /// <summary>
        /// A row that acts once when the spell is cast and leaves nothing to take away: a resource change or a removal of another spell.
        /// </summary>
        private static bool IsOneOffRow(Spell4EffectsEntry row)
        {
            return row.DurationTime == 0u && row.TickTime == 0u
                && row.EffectType is SpellEffectType.VitalModifier or SpellEffectType.SpellForceRemove;
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
