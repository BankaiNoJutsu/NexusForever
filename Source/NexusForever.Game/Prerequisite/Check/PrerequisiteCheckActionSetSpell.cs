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
            // Native (evaluator case 0xdd, vtable +0x190, 14049d6d0) receives the unit, the comparison and ObjectId: for the local player it scans the 12 slots of the set selected by the index at
            // DAT_140c65898 + 0x6dec for ObjectId; Equal returns found, NotEqual its negation, anything else false (re-read 2026-11-10, Decomp/Analysis/PREREQUISITE_CHECK_REVIEW_20261110.md).
            // Value is never read; comparing the flag with Value gave the wrong answer on all 59 retail slots. Reading that index as the active action set is inferred.
            return PrerequisiteCompare.CompareFlag(comparison, HasSpellOnActionBar(player, objectId));
        }

        private static bool HasSpellOnActionBar(IPlayer player, uint spell4Id)
        {
            if (spell4Id == 0u)
                return false;

            IActionSet actionSet = player.SpellManager.GetActionSet(player.SpellManager.ActiveActionSet);
            foreach (IActionSetShortcut shortcut in actionSet.Actions)
            {
                if (shortcut.ShortcutType == ShortcutType.SpellbookItem && shortcut.ObjectId == spell4Id)
                    return true;
            }

            return false;
        }
    }
}
