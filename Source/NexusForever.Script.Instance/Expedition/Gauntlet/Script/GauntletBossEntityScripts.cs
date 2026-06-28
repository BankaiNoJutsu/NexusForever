using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Expedition.Gauntlet.Script
{
    /// <summary>
    /// Build 16042 maps public-event objective 1854 to Script objective credit for
    /// TargetGroup 6871 first-arena rows, with reward-pane TargetGroup 12668
    /// carrying the veteran row set for the same Arena-Frenzied creatures.
    /// </summary>
    [ScriptFilterCreatureId(
        48461u, 48765u, 48793u, 48833u, 48835u, 48842u,
        48843u, 48844u, 48863u, 48865u, 48866u, 48867u,
        69237u, 69239u, 69240u, 69241u, 69242u, 69243u,
        69244u, 69246u, 69247u, 69248u, 69250u, 69251u)]
    public class FirstArenaFrenziedCreatureEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public FirstArenaFrenziedCreatureEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.SurviveTheFirstArena)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 1865 to TargetGroup 6856,
    /// whose Creature2 members are The Championator 48529 and 69255.
    /// </summary>
    [ScriptFilterCreatureId(48529u, 69255u)]
    public class ChampionatorEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public ChampionatorEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.KillTheChampionator)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 1866 to TargetGroup 6854,
    /// whose Creature2 members are Professor Doctor Voodoo 48511/69258
    /// and The Morticianatrix 48499/69257.
    /// </summary>
    [ScriptFilterCreatureId(48511u, 48499u, 69258u, 69257u)]
    public class VoodooKinEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public VoodooKinEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.KillTheVoodooKin)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 1835 to KillEventObjectiveUnit
    /// with no TargetGroup object and count 3; DataMapping links the normal and
    /// veteran Exile/Dominion Faction Friction team rows to event 446.
    /// </summary>
    [ScriptFilterCreatureId(48666u, 48667u, 48669u, 48670u, 69282u, 69283u, 69284u, 69285u)]
    public class OpposingFactionTeamEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public OpposingFactionTeamEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.KillTheOpposingFactionsTeam)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 1864 to TargetGroup 6855,
    /// whose Creature2 members are Rockstar Yeti 48491 and 69254.
    /// </summary>
    [ScriptFilterCreatureId(48491u, 69254u)]
    public class RockstarYetiEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public RockstarYetiEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.KillTheRockstarYeti)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 1869 to TargetGroup 6863,
    /// whose Creature2 members are Fist 48589/69310, "Bonebreaker" Zoragg 48588/69309,
    /// and Goonbot 48591/69311. DataMapping also maps veteran Gauntlet Goonbot to 52112.
    /// </summary>
    [ScriptFilterCreatureId(48588u, 48589u, 48591u, 69309u, 69310u, 69311u, 52112u)]
    public class GoonSquadEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public GoonSquadEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatTheGoonSquad)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 1870 to TargetGroup 6864,
    /// whose Creature2 members are The Handler 48516/69302 plus Chompers
    /// 48518/69303 and Spud 48519/69304.
    /// </summary>
    [ScriptFilterCreatureId(48516u, 48518u, 48519u, 69302u, 69303u, 69304u)]
    public class HandlerAndPetsEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public HandlerAndPetsEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatTheHandlerAndHisPets)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 1871 to TargetGroup 6865,
    /// whose Creature2 members are Slice 48557/69306 and Dice 48558/69307.
    /// </summary>
    [ScriptFilterCreatureId(48557u, 48558u, 69306u, 69307u)]
    public class SliceAndDiceEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public SliceAndDiceEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatSliceAndDice)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 1872 to TargetGroup 6868,
    /// whose Creature2 members are Pyro 48493/69300 and Maniac 48510/69301.
    /// </summary>
    [ScriptFilterCreatureId(48493u, 48510u, 69300u, 69301u)]
    public class PyroManiacEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public PyroManiacEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatPyroManiac)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 1873 to TargetGroup 6866,
    /// whose Creature2 members are Showtime 48579 and 69308.
    /// </summary>
    [ScriptFilterCreatureId(48579u, 69308u)]
    public class ShowtimeEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public ShowtimeEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatShowtime)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 1874 to TargetGroup 6867,
    /// whose Creature2 members are Shock King 48554 and 69305.
    /// </summary>
    [ScriptFilterCreatureId(48554u, 69305u)]
    public class ShockKingEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public ShockKingEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatTheShockKing)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 4671 to the Creature2 row
    /// "Brick" Braggor 69688 in The Gauntlet.
    /// </summary>
    [ScriptFilterCreatureId(69688u)]
    public class BrickBraggorEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public BrickBraggorEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatBrickBraggor)
        {
        }
    }
}
