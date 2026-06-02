using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 103: live <c>Prerequisite_CheckSpellCooldownNodeOnTarget</c> (<c>14046a190</c>),
    /// handler table <c>14049fbe0</c>. NF requires caster cooldown-node hit and a prerequisite target.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.SpellCooldownNodeOnTarget)]
    public class PrerequisiteCheckSpellCooldownNodeOnTarget : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckSpellCooldownNodeOnTarget(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (parameters.Target == null)
                return false;

            uint onCooldown = SpellPrerequisiteHelper.HasActiveCoolDownNode(player, value, gameTableManager) ? 1u : 0u;
            return PrerequisiteCompare.Compare(comparison, onCooldown, 1u);
        }
    }
}
