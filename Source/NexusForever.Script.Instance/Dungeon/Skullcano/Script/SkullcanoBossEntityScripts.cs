using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Dungeon.Skullcano.Script
{
    /// <summary>
    /// Build 16042 maps public-event objective 323 to TargetGroup 2601,
    /// whose Creature2 members are Bosun Octog 24486 and 24894.
    /// </summary>
    [ScriptFilterCreatureId(24486u, 24894u)]
    public class BosunOctogEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public BosunOctogEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatBosunOctog)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 440 to TargetGroup 2869,
    /// whose Creature2 members are Quartermaster Gruh'ar 24490 and 24896.
    /// </summary>
    [ScriptFilterCreatureId(24490u, 24896u)]
    public class QuartermasterGruharEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public QuartermasterGruharEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.KillGruharAndTakeStash)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 324 to script credit, while
    /// TargetGroup 2602 identifies the normal and veteran Mordechai Redmoon rows.
    /// </summary>
    [ScriptFilterCreatureId(24489u, 24895u)]
    public class MordechaiRedmoonEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public MordechaiRedmoonEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatMordechaiRedmoon)
        {
        }
    }
}
