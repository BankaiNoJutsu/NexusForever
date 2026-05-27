using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Main.AI;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Expedition.EvilFromTheEther.Script
{
    /// <summary>
    /// WIP-guessed from LaughingWS Instances-and-more: dormant/awaken visual spells
    /// are branch-derived pending retail creature-behavior proof.
    /// </summary>
    [ScriptFilterScriptName("RavenousRefugeeEntityScript")]
    public class RavenousRefugeeEntityScript : CombatAI, IUnitScript
    {
        #region Dependency Injection

        private readonly IFactory<ISpellParameters> spellParametersFactory;

        public RavenousRefugeeEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager)
        {
            this.spellParametersFactory = spellParametersFactory;
        }

        #endregion

        /// <summary>
        /// Invoked when <see cref="IGridEntity"/> is added to <see cref="IBaseMap"/>.
        /// </summary>
        public override void OnAddToMap(IBaseMap map)
        {
            base.OnAddToMap(map);

            ISpellParameters spellParameters = spellParametersFactory.Resolve();
            spellParameters.PrimaryTargetId = entity.Guid;
            entity.CastSpell(87237, spellParameters);
        }

        public void Awaken()
        {
            ISpellParameters spellParameters = spellParametersFactory.Resolve();
            spellParameters.PrimaryTargetId = entity.Guid;
            entity.CastSpell(87245, spellParameters);
        }
    }
}
