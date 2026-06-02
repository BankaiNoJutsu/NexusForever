using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Static.Spell;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 221: live case <c>0xdd</c> and handler table slot 12 body at <c>14049d6d0</c>
    /// search all action-bar shortcuts for <c>objectId0</c> Spell4 id.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.ActionSetSpell)]
    public class PrerequisiteCheckActionSetSpell : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint found = HasSpellOnActionBar(player, objectId) ? 1u : 0u;
            return PrerequisiteCompare.Compare(comparison, found, value);
        }

        private static bool HasSpellOnActionBar(IPlayer player, uint spell4Id)
        {
            if (spell4Id == 0u)
                return false;

            for (byte i = 0; i < ActionSet.MaxActionSets; i++)
            {
                IActionSet actionSet = player.SpellManager.GetActionSet(i);
                foreach (IActionSetShortcut shortcut in actionSet.Actions)
                {
                    if (shortcut.ShortcutType == ShortcutType.SpellbookItem && shortcut.ObjectId == spell4Id)
                        return true;
                }
            }

            return false;
        }
    }
}
