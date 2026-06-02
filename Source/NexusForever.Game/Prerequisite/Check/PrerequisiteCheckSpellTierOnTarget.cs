using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 60: live <c>Prerequisite_GetSpellTierOnTargetUnit</c> (<c>14046a110</c>),
    /// handler table <c>14049e850</c>. Tier check on prerequisite target player.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.SpellTierOnTarget)]
    public class PrerequisiteCheckSpellTierOnTarget : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckSpellTierOnTarget(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (parameters.Target is not IPlayer targetPlayer)
                return false;

            return SpellPrerequisiteHelper.MeetsSpellTier(targetPlayer, comparison, value, objectId, gameTableManager);
        }
    }
}
