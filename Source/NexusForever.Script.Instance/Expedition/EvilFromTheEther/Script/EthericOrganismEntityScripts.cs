using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Expedition.EvilFromTheEther.Script
{
    /// <summary>
    /// Build 16042 maps objective 4975 to TargetGroup 14105, whose Creature2
    /// members are Etheric Organism rows 71621 and 71633 for normal/veteran.
    /// </summary>
    [ScriptFilterCreatureId(71621u, 71633u)]
    public class FeastingEthericOrganismEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public FeastingEthericOrganismEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatEthericOrganisms)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 4977 to TargetGroup 14106, whose Creature2
    /// members are Etheric Organism rows 71628 and 71634 for normal/veteran.
    /// </summary>
    [ScriptFilterCreatureId(71628u, 71634u)]
    public class TeleporterEthericOrganismEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public TeleporterEthericOrganismEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatEthericOrganisms2)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 4926 to Exterminate TargetGroup 14056,
    /// whose only member is Creature2 71014 (W3404 - PE781 - Etheric Energy
    /// Rod - PO4926 - DWS/KLW). Exact spawn/visual state remains blocked
    /// pending expedition smoke.
    /// </summary>
    [ScriptFilterCreatureId(71014u)]
    public class EthericEnergyRodEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public EthericEnergyRodEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.KillEtherChargedRavenous)
        {
        }
    }
}
