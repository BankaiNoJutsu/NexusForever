using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.WorldStory.HallOfTheHundred.Script
{
    /// <summary>
    /// Build 16042 maps PublicEventObjective 4300 to KillTargetGroup 12157,
    /// whose only member is Creature2 67457 (Varegor the Abominable). Spawn
    /// placement and combat mechanics remain blocked pending dungeon smoke.
    /// </summary>
    [ScriptFilterCreatureId(67457u)]
    public class VaregorEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public VaregorEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatVaregor)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps optional PublicEventObjective 4318 to KillEventUnit
    /// object 14128, whose TargetGroup member is Creature2 71414 (Unbound
    /// Flame Elemental - w3009 - WS2 - Optional Boss). Spawn placement and
    /// encounter mechanics remain blocked pending dungeon smoke.
    /// </summary>
    [ScriptFilterCreatureId(71414u)]
    public class UnboundFlameElementalEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public UnboundFlameElementalEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatUnboundFlameElemental)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps optional PublicEventObjective 4972 to KillTargetGroup
    /// object 14275, whose TargetGroup member is Creature2 71577 (Icebound
    /// Overlord - w3009 - WS2 - Ice Boss). Spawn placement and encounter
    /// mechanics remain blocked pending dungeon smoke.
    /// </summary>
    [ScriptFilterCreatureId(71577u)]
    public class IceboundOverlordEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public IceboundOverlordEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatIceboundOverlord)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps optional PublicEventObjective 5001 to KillEventUnit
    /// object 14048, whose TargetGroup member is Creature2 71173 (Darkwitch
    /// Yotul - Osun Witch Boss - Optional). Spawn placement and encounter
    /// mechanics remain blocked pending dungeon smoke.
    /// </summary>
    [ScriptFilterCreatureId(71173u)]
    public class DarkwitchYotulEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public DarkwitchYotulEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatDarkwitchYotul)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps PublicEventObjective 4328 to KillTargetGroup 12162,
    /// whose only member is Creature2 67444 (Harizog Coldblood). Encounter
    /// mechanics and spawn placement remain blocked pending dungeon smoke.
    /// </summary>
    [ScriptFilterCreatureId(67444u)]
    public class HarizogColdbloodEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public HarizogColdbloodEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatHarizog)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps Harizog add objective 4952 to the summoned Havik
    /// Shiverhound Creature2 row 67851. TargetGroup 12211 also groups the
    /// Harizog add set; exact spawn cadence remains blocked pending smoke.
    /// </summary>
    [ScriptFilterCreatureId(67851u)]
    public class HavikShiverhoundEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public HavikShiverhoundEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatHavikShiverhound)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps Harizog add objective 4953 to the summoned Darkwitch
    /// Uhrga Creature2 row 67855. Encounter choreography remains blocked.
    /// </summary>
    [ScriptFilterCreatureId(67855u)]
    public class DarkwitchUhrgaEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public DarkwitchUhrgaEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatDarkwitchUhrga)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps Harizog add objective 4954 to the summoned Havik
    /// Honorguard Creature2 row 67853. Encounter choreography remains blocked.
    /// </summary>
    [ScriptFilterCreatureId(67853u)]
    public class HavikHonorguardEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public HavikHonorguardEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatHavikHonorguard)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps PublicEventObjective 4329 as the final Hall escape
    /// Exterminate objective. QuestDirection 2450 points through the W3009
    /// Part 6 exit route; the concrete blocker rows are Creature2 67429/67430,
    /// also present in the late-Hall TargetGroup 14376 set. Exact wave/spawn
    /// choreography remains blocked pending World Story smoke.
    /// </summary>
    [ScriptFilterCreatureId(67429u, 67430u)]
    public class OsunBlockingTheWayEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public OsunBlockingTheWayEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatOsunBlockingTheWay)
        {
        }
    }

    /// <summary>
    /// Build 16042 side-event objective 4404 is KillEventObjectiveUnit text
    /// for Creature2 67884 (Varegor Watchhound) in the first Warhound Kennel
    /// combat volume. Spawn placement remains blocked pending dungeon smoke.
    /// </summary>
    [ScriptFilterCreatureId(67884u)]
    public class VaregorWatchhoundEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public VaregorWatchhoundEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatVaregorWatchhound)
        {
        }
    }

    /// <summary>
    /// Build 16042 side-event objective 4441 is KillClusterEventObjectiveUnit
    /// object 12262; TargetGroup 12262 maps to Creature2 67940 (Frozen Lever).
    /// The objective text names Creature2 67924/67925/67935, but exact pack
    /// spawn/combat choreography remains blocked pending World Story smoke.
    /// </summary>
    [ScriptFilterCreatureId(67940u)]
    public class FrozenLeverEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public FrozenLeverEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatWarhoundPack)
        {
        }
    }

    /// <summary>
    /// Build 16042 side-event objective 4463 points at Creature2 68013
    /// (Primal Wraith - Boss - D5 - w3009 - Side Mission 4). Exact echo-wave
    /// spawn choreography remains blocked pending dungeon smoke.
    /// </summary>
    [ScriptFilterCreatureId(68013u)]
    public class PrimalWraithEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public PrimalWraithEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatPrimalWraith)
        {
        }
    }

    /// <summary>
    /// Build 16042 side-event objective 4381 names Creature2 67657 in the Cold
    /// and Hungry escape volume. Spawn/chase choreography remains blocked.
    /// </summary>
    [ScriptFilterCreatureId(67657u)]
    public class ColdAndHungryYetiEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public ColdAndHungryYetiEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatYeti)
        {
        }
    }
}
