using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Duplicate-body alias for ItemTradeSkill, with account-item claim rows using objectId as a dye colour ramp unlock id.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Unknown245)]
    public class PrerequisiteCheckUnknown245 : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckUnknown245(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (parameters?.AccountItemContext == true)
            {
                uint expected = objectId != 0u ? objectId : value;
                uint current = player.Account?.GenericUnlockManager?.IsDyeUnlocked(expected) == true ? expected : 0u;
                return PrerequisiteCompare.Compare(comparison, current, expected);
            }

            return TradeskillPrerequisiteHelper.MeetsItemTradeSkill(player, gameTableManager, comparison, value, objectId, parameters);
        }
    }
}
