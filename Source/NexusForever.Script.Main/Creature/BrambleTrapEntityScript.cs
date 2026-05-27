using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Spell;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Main.Creature
{
    [ScriptFilterCreatureId(27768u)]
    public class BrambleTrapEntityScript : IWorldEntityScript, IOwnedScript<ICreatureEntity>
    {
        private const uint PenaltySpellId = 46051u;

        private readonly IFactory<ISpellParameters> spellParametersFactory;

        private ICreatureEntity owner;

        public BrambleTrapEntityScript(IFactory<ISpellParameters> spellParametersFactory)
        {
            this.spellParametersFactory = spellParametersFactory;
        }

        public void OnLoad(ICreatureEntity owner)
        {
            this.owner = owner;
        }

        public void OnActivateSuccess(IPlayer activator)
        {
            if (activator == null || owner == null)
                return;

            activator.RemoveTrackedSpellStates(spell4Id => spell4Id == PenaltySpellId, uint.MaxValue);
            owner.ModifyHealth(owner.MaxHealth, DamageType.Physical, null);
        }

        public void OnActivateFail(IPlayer activator)
        {
            if (activator == null)
                return;

            ISpellParameters spellParameters = spellParametersFactory.Resolve();
            spellParameters.UserInitiatedSpellCast = false;
            spellParameters.ClientRequestSource    = nameof(BrambleTrapEntityScript);

            activator.CastSpell(PenaltySpellId, spellParameters);
        }
    }
}
