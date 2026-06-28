using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Expedition.OutpostM13.Script
{
    /// <summary>
    /// Build 16042 maps objective 253 to the cargo-hold Novaburn Corsair and
    /// Plunderer rows in event 108. Exact spawn density remains smoke-blocked.
    /// </summary>
    [ScriptFilterCreatureId(23453u, 23454u, 69074u, 69075u)]
    public class CargoHoldNovaburnMarauderEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public CargoHoldNovaburnMarauderEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.KillNovaburnMarauders)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 254 to Creature2 23455, "Ransacker" Rorgh.
    /// </summary>
    [ScriptFilterCreatureId(23455u)]
    public class RansackerRorghEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public RansackerRorghEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.KillRansackerRorgh)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 257 to Hive Queen Creature2 23513 in TargetGroup 5318.
    /// Exact boss mechanics, spawn timing, and variant routing remain blocked.
    /// </summary>
    [ScriptFilterCreatureId(23513u)]
    public class HiveQueenEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public HiveQueenEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.KillHiveQueen)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 256 to Script objective credit for the
    /// TargetGroup 10835 mine cleanup rows: Hive Infector, Hive Pod, and
    /// Infected Miner normal/veteran Creature2 ids.
    /// </summary>
    [ScriptFilterCreatureId(23536u, 23514u, 23500u, 69080u, 69079u, 69077u)]
    public class HivePodMinerInfectorEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public HivePodMinerInfectorEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatHivePods)
        {
        }
    }
}
