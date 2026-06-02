using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 102: live <c>Prerequisite_CheckSpellCooldownNodeOnUnit</c> (<c>1404a4fe0</c>),
    /// handler table <c>14049fb50</c>. NF proxies via active spell cooldowns referencing
    /// <see cref="SpellCoolDownEntry"/> id <c>value0</c>.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.SpellCooldownNodeOnUnit)]
    public class PrerequisiteCheckSpellCooldownNodeOnUnit : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckSpellCooldownNodeOnUnit(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            IUnitEntity unit = parameters.Target as IUnitEntity ?? player;
            if (unit is not IPlayer unitPlayer)
                return false;

            uint onCooldown = SpellPrerequisiteHelper.HasActiveCoolDownNode(unitPlayer, value, gameTableManager) ? 1u : 0u;
            return PrerequisiteCompare.Compare(comparison, onCooldown, 1u);
        }
    }
}
