using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 93: handler table <c>14049f690</c> (live case <c>0x5d</c> via <c>1403d6f1c</c>);
    /// walks active spell effects at entity <c>+0x15c8</c> and compares <c>Spell4EffectGroupList</c> membership for
    /// <c>value0</c> (effect-group id). NF proxies via persistent lifetime effects and
    /// <see cref="Spell4EffectGroupListEntry"/> rows.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Spell4EffectCategoryOnUnit)]
    public class PrerequisiteCheckSpell4EffectCategoryOnUnit : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckSpell4EffectCategoryOnUnit(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            IUnitEntity unit = parameters.Target as IUnitEntity ?? player;
            uint hasCategory = SpellPrerequisiteHelper.HasActiveSpellEffectGroup(unit, value, gameTableManager) ? 1u : 0u;
            return PrerequisiteCompare.Compare(comparison, hasCategory, 1u);
        }
    }
}
