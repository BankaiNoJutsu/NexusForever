using System.Collections.Generic;
using NexusForever.Game.Abstract.Spell;

namespace NexusForever.Script.Template
{
    public interface ISpellScript
    {
        /// <summary>
        /// Invoked after cast validation and before the spell start message is broadcast.
        /// </summary>
        void OnCast(ISpell spell)
        {
        }

        /// <summary>
        /// Invoked after target selection and before effects are applied.
        /// </summary>
        void OnExecute(ISpell spell, IReadOnlyCollection<ISpellTargetInfo> targets)
        {
        }

        /// <summary>
        /// Invoked when the spell leaves the executing state.
        /// </summary>
        void OnFinish(ISpell spell, bool cancelled)
        {
        }
    }
}
