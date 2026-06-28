using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Dungeon.StormtalonsLair.Script
{
    /// <summary>
    /// Build 16042 maps public-event objective 556 to TargetGroup 3030,
    /// whose Creature2 members are Arcanist Breeze-Binder 24474 and 34711.
    /// </summary>
    [ScriptFilterCreatureId(24474u, 34711u)]
    public class ArcanistBreezeBinderEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public ArcanistBreezeBinderEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatArcanistBreezeBinderForTheEncryptionKey)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 828 to TargetGroup 3921,
    /// whose Creature2 members are Overseer Drift-Catcher 33361 and 33362.
    /// </summary>
    [ScriptFilterCreatureId(33361u, 33362u)]
    public class OverseerDriftCatcherEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public OverseerDriftCatcherEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.KillOverseerDriftCatcher)
        {
        }
    }
}
