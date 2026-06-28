using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Dungeon.SanctuaryOfTheSwordmaiden.Script
{
    /// <summary>
    /// Build 16042 maps public-event objective 479 to TargetGroup 3208,
    /// whose Creature2 members are the Shallaos rows 28597, 28599, and 28600.
    /// </summary>
    [ScriptFilterCreatureId(28597u, 28599u, 28600u)]
    public class DeadringerShallaosEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public DeadringerShallaosEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatDeadringerShallaos)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 495 to TargetGroup 3372,
    /// which nests the Zealous Torine target groups 3373 and 3374 plus the
    /// Shallaos rows in TargetGroup 7366.
    /// </summary>
    [ScriptFilterCreatureId(28580u, 28612u, 28646u, 28585u, 28613u, 28599u, 28600u)]
    public class ZealousTorineEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public ZealousTorineEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.EliminateZealousTorine)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 505 to TargetGroup 3385,
    /// which nests the Corrupted Swordmaiden/Priestess/Deathbringer groups
    /// 3386 and 3387 plus the Rayna, Selene, and corrupted Torine miniboss
    /// target groups 3210, 3211, and 3237-3239.
    /// </summary>
    [ScriptFilterCreatureId(
        29266u, 29303u, 29222u, 72983u, 72984u, 72985u,
        29267u, 29304u, 29223u, 72995u, 72997u, 72998u,
        28733u, 28732u, 28736u, 28735u,
        28985u, 28986u, 28993u, 28992u, 28995u, 28996u)]
    public class CorruptedTorineSistersEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public CorruptedTorineSistersEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.FreeTheSpiritsOfTheCorruptedTorineSisters)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 486 to TargetGroup 3210,
    /// whose Creature2 members are Rayna Darkspeaker 28733 and 28732.
    /// </summary>
    [ScriptFilterCreatureId(28733u, 28732u)]
    public class RaynaDarkspeakerEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public RaynaDarkspeakerEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatRaynaDarkspeaker)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 483 to TargetGroup 3209,
    /// whose Creature2 members are Moldwood Overlord Skash 28728 and 28727.
    /// </summary>
    [ScriptFilterCreatureId(28728u, 28727u)]
    public class MoldwoodOverlordSkashEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public MoldwoodOverlordSkashEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatMoldwoodOverlordSkash)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 1504 to TargetGroup 3209,
    /// whose Creature2 members are Moldwood Overlord Skash 28728 and 28727.
    /// </summary>
    [ScriptFilterCreatureId(28728u, 28727u)]
    public class MoldwoodOverlordSkashPrisonerChallengeEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public MoldwoodOverlordSkashPrisonerChallengeEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatSkashOrHeWillCorruptThePrisoner)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 627 to Script credit with
    /// object 3391; TargetGroup 3391 contains the e627 Moldwood Mauler rows.
    /// </summary>
    [ScriptFilterCreatureId(29311u, 29310u, 72988u, 72999u)]
    public class DistractedMoldwoodMaulerEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public DistractedMoldwoodMaulerEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.KillDistractedMoldwoodMaulers)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 502 to KillClusterTargetGroup
    /// 3380, while DataMapping links Moldwood Skurge Slasher/Tactician and
    /// Blighted Moldwood Crawler Creature2 rows to event 166.
    /// </summary>
    [ScriptFilterCreatureId(28930u, 28892u, 41204u, 15655u, 15654u, 15666u)]
    public class MoldwoodSkurgeAndCrawlerEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public MoldwoodSkurgeAndCrawlerEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DestroyMoldwoodSkurgeAndCrawlers)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 630 to KillClusterTargetGroup
    /// 5702, and DataMapping links Corrupted Terrorantula Creature2 rows to
    /// event 166.
    /// </summary>
    [ScriptFilterCreatureId(28829u, 15664u)]
    public class CorruptedTerrorantulaEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public CorruptedTerrorantulaEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.KillTheCorruptedTerrorantulas)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 503 to TargetGroup 5701.
    /// DataMapping links the event-166 Corrupted Deathsting Swarmer and Hive
    /// Defender rows to current Creature2 rows 29249/29252 and bridge source
    /// rows 15653/15691.
    /// </summary>
    [ScriptFilterCreatureId(29249u, 29252u, 15653u, 15691u)]
    public class CorruptedDeathstingSwarmEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public CorruptedDeathstingSwarmEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DestroyDeathstingSwarms)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 628 to TargetGroup 3392.
    /// DataMapping links the event-166 Corrupted Veteran Swordmaiden rows to
    /// current Creature2 row 29267 and bridge source row 15658.
    /// </summary>
    [ScriptFilterCreatureId(29267u, 15658u)]
    public class CorruptedVeteranSwordmaidenEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public CorruptedVeteranSwordmaidenEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.KillCorruptedVeteranSwordmaidens)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 641 to KillClusterTargetGroup
    /// 5704, and DataMapping links Moldwood Corruptor Creature2 rows to event
    /// 166.
    /// </summary>
    [ScriptFilterCreatureId(30209u, 15683u)]
    public class MoldwoodCorruptorEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public MoldwoodCorruptorEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DestroyTheMoldwoodCorruptors)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 1366 to TargetGroup 5316,
    /// whose Creature2 members are Hammerfist Moldjaw 41219 and 41220.
    /// </summary>
    [ScriptFilterCreatureId(41219u, 41220u)]
    public class HammerfistMoldjawEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public HammerfistMoldjawEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatHammerfistMoldjaw)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 493 to TargetGroup 3237,
    /// whose Creature2 members are Corrupted Edgesmith Torian 28985 and 28986.
    /// </summary>
    [ScriptFilterCreatureId(28985u, 28986u)]
    public class CorruptedEdgesmithTorianEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public CorruptedEdgesmithTorianEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatCorruptedEdgesmithTorian)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 494 to TargetGroup 3238,
    /// whose Creature2 members are Corrupted Lifecaller Khalee 28993 and 28992.
    /// </summary>
    [ScriptFilterCreatureId(28993u, 28992u)]
    public class CorruptedLifecallerKhaleeEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public CorruptedLifecallerKhaleeEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.KillCorruptedLifecallerKhalee)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 500 to TargetGroup 3239,
    /// whose Creature2 members are Corrupted Deathbringer Dareia 28995 and 28996.
    /// </summary>
    [ScriptFilterCreatureId(28995u, 28996u)]
    public class CorruptedDeathbringerDareiaEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public CorruptedDeathbringerDareiaEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.KillCorruptedDeathbringerDareia)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 480 to TargetGroup 3207,
    /// whose Creature2 members are the Ondu Lifeweaver rows 28719, 28720, and 28721.
    /// </summary>
    [ScriptFilterCreatureId(28719u, 28720u, 28721u)]
    public class OnduLifeweaverEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public OnduLifeweaverEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatOnduLifeweaver)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 629 to Lifeweaver Guardian
    /// Creature2 rows 28774 and 28775.
    /// </summary>
    [ScriptFilterCreatureId(28774u, 28775u)]
    public class LifeweaverGuardianEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public LifeweaverGuardianEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.KillTheLifeweaverGuardian)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 492 to TargetGroup 3211,
    /// whose Creature2 members are Spiritmother Selene the Corrupted 28736 and 28735.
    /// </summary>
    [ScriptFilterCreatureId(28736u, 28735u)]
    public class SpiritmotherSeleneTheCorruptedEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public SpiritmotherSeleneTheCorruptedEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatSpiritmotherSeleneTheCorrupted)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 639 to Elder Moldwood Ravager
    /// Creature2 rows 29198 and 29199.
    /// </summary>
    [ScriptFilterCreatureId(29198u, 29199u)]
    public class ElderMoldwoodRavagerEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public ElderMoldwoodRavagerEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DestroyTheElderMoldwoodRavager)
        {
        }
    }
}
