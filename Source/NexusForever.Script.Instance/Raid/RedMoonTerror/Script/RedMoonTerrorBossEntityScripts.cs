using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Raid.RedMoonTerror.Script
{
    /// <summary>
    /// Build 16042 maps public-event objective 4590 directly to Creature2
    /// 75214, Chief Warden Lockjaw.
    /// </summary>
    [ScriptFilterCreatureId(75214u)]
    public class ChiefWardenLockjawEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public ChiefWardenLockjawEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatChiefWardenLockjaw)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 4558 directly to Creature2
    /// 68655, Swabbie Ski'li.
    /// </summary>
    [ScriptFilterCreatureId(68655u)]
    public class SwabbieSkiLiEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public SwabbieSkiLiEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatSwabbieSkiLi)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 4549 directly to Creature2
    /// 66085, the Robomination.
    /// </summary>
    [ScriptFilterCreatureId(66085u)]
    public class RobominationEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public RobominationEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatTheRobomination)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 4552 directly to Creature2
    /// 72872, Assistant Technician Skooty.
    /// </summary>
    [ScriptFilterCreatureId(72872u)]
    public class AssistantTechnicianSkootyEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public AssistantTechnicianSkootyEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatAssistantTechnicianSkooty)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 4553 directly to Creature2
    /// 72873, Chief Engine Scrubber Thrag.
    /// </summary>
    [ScriptFilterCreatureId(72873u)]
    public class ChiefEngineScrubberThragEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public ChiefEngineScrubberThragEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatChiefEngineScrubberThrag)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 4550 directly to Creature2
    /// 65759 and 65758, the Redmoon Engineers.
    /// </summary>
    [ScriptFilterCreatureId(65759u, 65758u)]
    public class RedMoonEngineerEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public RedMoonEngineerEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatTheEngineers)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 4554 directly to Creature2
    /// 65800, Mordechai Redmoon.
    /// </summary>
    [ScriptFilterCreatureId(65800u)]
    public class RedMoonTerrorMordechaiRedmoonEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public RedMoonTerrorMordechaiRedmoonEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatMordechaiRedmoon)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 4556 directly to Creature2
    /// 72758, Star-Eater the Voracious.
    /// </summary>
    [ScriptFilterCreatureId(72758u)]
    public class StarEaterTheVoraciousEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public StarEaterTheVoraciousEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatStarEaterTheVoracious)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 4555 to KillTargetGroup
    /// 14458, whose Creature2 member is the Anti-Boarding Turret 75645.
    /// </summary>
    [ScriptFilterCreatureId(75645u)]
    public class AntiBoardingTurretEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public AntiBoardingTurretEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DestroyTheAntiBoardingTurret)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 4617 to KillClusterTargetGroup
    /// 14460, whose Creature2 members are the four Marauder Officers.
    /// </summary>
    [ScriptFilterCreatureId(72884u, 72885u, 72886u, 72887u)]
    public class MarauderOfficerEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public MarauderOfficerEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatMarauderOfficers)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 4620 to the Starmap Simulation
    /// Creature2 row 73622 in the objective text.
    /// </summary>
    [ScriptFilterCreatureId(73622u)]
    public class StarmapSimulationEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public StarmapSimulationEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatTheStarmapSimulation)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 4613 directly to Creature2
    /// 72888, Bonedoctor Muburu.
    /// </summary>
    [ScriptFilterCreatureId(72888u)]
    public class BonedoctorMuburuEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public BonedoctorMuburuEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatBonedoctorMuburu)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 4623 directly to Creature2
    /// 72889, Headshrinker W'gasa.
    /// </summary>
    [ScriptFilterCreatureId(72889u)]
    public class HeadshrinkerWgasaEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public HeadshrinkerWgasaEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatHeadshrinkerWgasa)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 5141 directly to Creature2
    /// 72891, Tiny.
    /// </summary>
    [ScriptFilterCreatureId(72891u)]
    public class TinyEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public TinyEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatTiny)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 4625 directly to Creature2
    /// 72890, the Untombed Horror.
    /// </summary>
    [ScriptFilterCreatureId(72890u)]
    public class UntombedHorrorEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public UntombedHorrorEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatUntombedHorror)
        {
        }
    }
}
