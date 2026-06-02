using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 280: live case <c>0x118</c> requires caster and target to pass
    /// <c>Prerequisite_CheckSpellTier</c> (<c>1404a4f60</c>).
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.SpellTierOnCasterAndTarget)]
    public class PrerequisiteCheckSpellTierOnCasterAndTarget : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckSpellTierOnCasterAndTarget(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (!SpellPrerequisiteHelper.MeetsSpellTier(player, comparison, value, objectId, gameTableManager))
                return false;

            if (parameters.Target is not IPlayer targetPlayer)
                return false;

            return SpellPrerequisiteHelper.MeetsSpellTier(targetPlayer, comparison, value, objectId, gameTableManager);
        }
    }
}
