using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Main.Creature
{
    [ScriptFilterCreatureId(16718u, 24251u)]
    public class MarauderMineEntityScript : IWorldEntityScript, IOwnedScript<ICreatureEntity>
    {
        private const uint DetonateSpellId = 26443u;

        private readonly IFactory<ISpellParameters> spellParametersFactory;

        private ICreatureEntity owner;

        public MarauderMineEntityScript(IFactory<ISpellParameters> spellParametersFactory)
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

            ISpellParameters spellParameters = spellParametersFactory.Resolve();
            spellParameters.PrimaryTargetId        = activator.Guid;
            spellParameters.UserInitiatedSpellCast = false;
            spellParameters.ClientRequestSource    = nameof(MarauderMineEntityScript);

            owner.CastSpell(DetonateSpellId, spellParameters);
        }
    }
}
