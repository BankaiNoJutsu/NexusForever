using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Duplicate-body alias for ItemTradeSkillKnown, with account-item claim rows using objectId as a Holo-Wardrobe Item2 unlock id.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Unknown275)]
    public class PrerequisiteCheckUnknown275 : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckUnknown275(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (parameters?.AccountItemContext == true)
            {
                uint expected = objectId != 0u ? objectId : value;
                uint current = player.Account?.CostumeManager?.HasItemUnlock(expected) == true ? expected : 0u;
                return PrerequisiteCompare.Compare(comparison, current, expected);
            }

            return TradeskillPrerequisiteHelper.MeetsItemTradeSkillKnown(player, gameTableManager, comparison, value, objectId, parameters);
        }
    }
}
