using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Raid.GeneticArchives.Script
{
    /// <summary>
    /// Build 16042 maps objective 2433 to KillTargetGroup 8519,
    /// whose TargetGroup member is Creature2 56377, Fetid Miscreation.
    /// </summary>
    [ScriptFilterCreatureId(56377u)]
    public class FetidMiscreationEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public FetidMiscreationEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatTheFetidMiscreation)
        {
        }
    }
}
