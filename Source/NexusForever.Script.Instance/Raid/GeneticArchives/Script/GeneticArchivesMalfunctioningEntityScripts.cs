using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Raid.GeneticArchives.Script
{
    /// <summary>
    /// Build 16042 maps objective 2389 to KillTargetGroup 8515,
    /// whose TargetGroup member is Creature2 56106, Malfunctioning Piston.
    /// </summary>
    [ScriptFilterCreatureId(56106u)]
    public class MalfunctioningPistonEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public MalfunctioningPistonEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatTheMalfunctioningPiston)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 2388 to KillTargetGroup 8516,
    /// whose TargetGroup member is Creature2 56174, Malfunctioning Battery.
    /// </summary>
    [ScriptFilterCreatureId(56174u)]
    public class MalfunctioningBatteryEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public MalfunctioningBatteryEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatTheMalfunctioningBattery)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 2390 to KillTargetGroup 8517,
    /// whose TargetGroup member is Creature2 54935, Malfunctioning Dynamo.
    /// </summary>
    [ScriptFilterCreatureId(54935u)]
    public class MalfunctioningDynamoEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public MalfunctioningDynamoEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatTheMalfunctioningDynamo)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 2391 to KillTargetGroup 8520,
    /// whose TargetGroup member is Creature2 55066, Malfunctioning Gear.
    /// </summary>
    [ScriptFilterCreatureId(55066u)]
    public class MalfunctioningGearEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public MalfunctioningGearEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatTheMalfunctioningGear)
        {
        }
    }
}
