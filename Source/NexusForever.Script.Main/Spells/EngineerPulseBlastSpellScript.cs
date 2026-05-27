using System.Collections.Generic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Main.Spells
{
    [ScriptFilterOwnerId(37302u, 56145u, 56146u, 56147u, 56148u, 56149u, 56150u, 56151u, 56152u)]
    public class EngineerPulseBlastSpellScript : ISpellScript, IOwnedScript<ISpell>
    {
        private const uint HiddenVolatilitySpellId = 42148u;

        private readonly IFactory<ISpellParameters> spellParametersFactory;

        private ISpell owner;

        public EngineerPulseBlastSpellScript(IFactory<ISpellParameters> spellParametersFactory)
        {
            this.spellParametersFactory = spellParametersFactory;
        }

        public void OnLoad(ISpell owner)
        {
            this.owner = owner;
        }

        public void OnExecute(ISpell spell, IReadOnlyCollection<ISpellTargetInfo> targets)
        {
            ISpell activeSpell = spell ?? owner;
            if (activeSpell?.Caster == null || !HasHostileTarget(activeSpell, targets))
                return;

            ISpellParameters spellParameters = spellParametersFactory.Resolve();
            spellParameters.ParentSpellInfo        = activeSpell.Parameters.SpellInfo;
            spellParameters.RootSpellInfo          = activeSpell.Parameters.RootSpellInfo;
            spellParameters.PrimaryTargetId        = activeSpell.Caster.Guid;
            spellParameters.UserInitiatedSpellCast = false;
            spellParameters.IgnoreGlobalCooldown   = true;
            spellParameters.ClientContextToken     = activeSpell.Parameters.ClientContextToken;
            spellParameters.ClientRequestSource    = nameof(EngineerPulseBlastSpellScript);

            activeSpell.Caster.CastSpell(HiddenVolatilitySpellId, spellParameters);
        }

        private static bool HasHostileTarget(ISpell spell, IReadOnlyCollection<ISpellTargetInfo> targets)
        {
            if (targets == null)
                return false;

            foreach (ISpellTargetInfo targetInfo in targets)
            {
                if (targetInfo.Entity.Guid == spell.Caster.Guid)
                    continue;

                if (targetInfo.Entity is not IUnitEntity targetUnit)
                    continue;

                if (spell.Caster.CanAttack(targetUnit))
                    return true;
            }

            return false;
        }
    }
}
