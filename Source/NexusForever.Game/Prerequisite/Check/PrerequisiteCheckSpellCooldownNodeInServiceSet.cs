using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 104: live <c>Prerequisite_CheckSpellCooldownNodeInServiceSet</c> (<c>14046a210</c>),
    /// handler table <c>14049fc70</c>. NF proxies with spell-book membership for the cooldown-node id.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.SpellCooldownNodeInServiceSet)]
    public class PrerequisiteCheckSpellCooldownNodeInServiceSet : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckSpellCooldownNodeInServiceSet(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint known = SpellPrerequisiteHelper.KnowsSpellReferencingCoolDownNode(player, value, gameTableManager) ? 1u : 0u;
            return PrerequisiteCompare.Compare(comparison, known, 1u);
        }
    }
}
