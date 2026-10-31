using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.ItemProficiency)]
    public class PrerequisiteCheckItemProficiency : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckItemProficiency(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (player == null || gameTableManager?.ItemProficiency == null)
                return false;

            // Native type 8 resolves value as ItemProficiency.Id, then tests its
            // BitMask. The row ID itself is not a proficiency bit mask.
            ItemProficiencyEntry entry = gameTableManager.ItemProficiency.GetEntry(value);
            bool matches = entry == null || entry.BitMask == 0
                || ((uint)player.GetItemProficiencies() & entry.BitMask) != 0;
            return comparison switch
            {
                PrerequisiteComparison.Equal => matches,
                PrerequisiteComparison.NotEqual => !matches,
                _ => false
            };
        }
    }
}
