using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.ItemSpecial)]
    public class PrerequisiteCheckItemSpecial : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckItemSpecial(IGameTableManager gameTableManager = null)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (parameters.Item == null)
                return false;

            // Client Prerequisite_CheckItemSpecial 1404a0dd0: Equal passes when item-eval +0x114 or +0x118 is non-zero.
            bool hasSpecial = HasSpecialSlotPresence(parameters.Item);

            return comparison switch
            {
                PrerequisiteComparison.Equal    => hasSpecial,
                PrerequisiteComparison.NotEqual => !hasSpecial,
                _                               => false
            };
        }

        private bool HasSpecialSlotPresence(IItem item)
        {
            if (ItemRuneSocketMaskBuilder.BuildAllowedSocketMask(item, gameTableManager) != 0)
                return true;

            if (item.MicrochipIds.Count > 0)
                return true;

            if (item.Info == null || item.Info.Entry.ItemSpecialId00 == 0u)
                return false;

            if (gameTableManager == null)
                return true;

            ItemSpecialEntry special = gameTableManager.ItemSpecial.GetEntry(item.Info.Entry.ItemSpecialId00);
            if (special == null)
                return true;

            // ItemEval_ApplyItemSpecialToEval 14040c310 also copies ItemSpecial +0xc into item-eval +0x118.
            return special.PrerequisiteIdGeneric00 != 0u
                || special.Spell4IdOnEquip != 0u
                || special.Spell4IdOnActivate != 0u;
        }
    }
}
