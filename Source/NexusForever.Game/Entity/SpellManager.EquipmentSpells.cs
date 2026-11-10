using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Entity
{
    public partial class SpellManager
    {
        private readonly HashSet<uint> equipmentSpells = [];
        private string runeSetPropertyKey = string.Empty;
        private double equipmentSpellRetryTime;
        private bool refreshingEquipmentSpells;

        /// <summary>
        /// The spells the player's equipped items, socketed runes, rune sets and current stance grant right now, limited to those the server can also revoke.
        /// </summary>
        private HashSet<uint> GetDesiredEquipmentSpells()
        {
            IGameTableManager tables = GetGameTableManager();
            IEnumerable<IItem> equipped = player.Inventory?.Where(bag => bag.Location == InventoryLocation.Equipped).SelectMany(bag => bag) ?? [];

            var desired = new HashSet<uint>();
            foreach ((uint spell4Id, uint prerequisiteId, IItem item) in EquipmentSpellRules.GetGrantedSpells(equipped, tables))
            {
                if (!EquipmentSpellRules.IsSupported(spell4Id, tables))
                    continue;

                if (prerequisiteId != 0u && !GetPrerequisiteManager().Meets(player, prerequisiteId, new PrerequisiteParameters { Item = item }))
                    continue;

                desired.Add(spell4Id);
            }

            // the passive of the stance the player is in
            uint stancePassive = EquipmentSpellRules.GetStancePassive(tables.Class?.GetEntry((uint)player.Class), player.InnateIndex, tables);
            if (stancePassive != 0u)
                desired.Add(stancePassive);

            return desired;
        }

        /// <summary>
        /// Grant the on-equip spells of newly equipped items, runes and rune sets and revoke those whose source is gone.
        /// </summary>
        /// <remarks>
        /// Polled once a second rather than hooked into every path that moves an item, changes a rune or kills the player. A granted spell is cast on the player; revoking it
        /// removes the states it left (by Spell4 id) and ends it if it is still running. See <see cref="EquipmentSpellRules"/>.
        /// </remarks>
        private void RefreshEquipmentSpells()
        {
            if (!initialised || refreshingEquipmentSpells)
                return;

            refreshingEquipmentSpells = true;
            try
            {
                RefreshRuneSetProperties();
                HashSet<uint> desired = GetDesiredEquipmentSpells();
                foreach (uint id in equipmentSpells.Where(id => !desired.Contains(id)).ToArray())
                {
                    player.EndSpellLifetimes(spell4Id => spell4Id == id);
                    player.RemoveTrackedSpellStates(spell4Id => spell4Id == id, uint.MaxValue);
                    equipmentSpells.Remove(id);
                }

                if (!player.InWorld || !player.IsAlive)
                    return;

                IGameTableManager tables = GetGameTableManager();
                foreach (uint id in desired)
                {
                    if (equipmentSpells.Contains(id))
                    {
                        // Still in place, or nothing to check it by: leave it. Otherwise (the player died, say) grant it again.
                        if (!EquipmentSpellRules.LeavesPresence(id, tables) || player.GetActiveSpellStackCount(id) != 0u || player.HasSpellLifetime(id))
                            continue;
                    }

                    CastResult result = player.TryCastSpell(id, new SpellParameters
                    {
                        PrimaryTargetId        = player.Guid,
                        UserInitiatedSpellCast = false,
                        IgnoreGlobalCooldown   = true,
                        ClientRequestSource    = nameof(SpellManager) + ".EquipmentSpell"
                    });
                    if (result == CastResult.Ok)
                        equipmentSpells.Add(id);
                }
            }
            finally
            {
                refreshingEquipmentSpells = false;
            }
        }

        /// <summary>
        /// Hand the property bonuses of the reached rune sets to the player, only when they changed. Several sets may name the same property: offsets add up and scalars multiply.
        /// </summary>
        private void RefreshRuneSetProperties()
        {
            IEnumerable<IItem> equipped = player.Inventory?.Where(bag => bag.Location == InventoryLocation.Equipped).SelectMany(bag => bag) ?? [];

            var bonuses = new Dictionary<Property, (float Scalar, float Offset)>();
            foreach ((Property property, float scalar, float offset) in EquipmentSpellRules.GetSetPropertyBonuses(equipped, GetGameTableManager()))
            {
                (float Scalar, float Offset) current = bonuses.GetValueOrDefault(property);
                float combinedScalar = scalar == 0f ? current.Scalar : (current.Scalar == 0f ? scalar : current.Scalar * scalar);
                bonuses[property] = (combinedScalar, current.Offset + offset);
            }

            string key = string.Join(";", bonuses.OrderBy(b => b.Key).Select(b => $"{(uint)b.Key}:{b.Value.Scalar:R}:{b.Value.Offset:R}"));
            if (key == runeSetPropertyKey)
                return;

            runeSetPropertyKey = key;
            player.SetRuneSetProperties(bonuses);
        }

        private void UpdateEquipmentSpells(double lastTick)
        {
            if (!initialised)
                return;

            equipmentSpellRetryTime -= lastTick;
            if (equipmentSpellRetryTime > 0d)
                return;

            equipmentSpellRetryTime = 1d;
            RefreshEquipmentSpells();
        }
    }
}
