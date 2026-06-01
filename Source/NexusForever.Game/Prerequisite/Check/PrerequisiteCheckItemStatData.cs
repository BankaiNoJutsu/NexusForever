using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable.Model;
using NexusForever.GameTable.Static;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.ItemStatData)]
    public class PrerequisiteCheckItemStatData : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (parameters.Item?.Info == null)
                return false;

            // Client Prerequisite_CheckItemStatData 1404a0d00 compares item-eval +0x140 to value0 (cached from ItemStat load).
            uint itemStatData = GetCachedItemStatData(parameters.Item.Info.StatEntry);
            return PrerequisiteCompare.Compare(comparison, itemStatData, value);
        }

        private static uint GetCachedItemStatData(ItemStatEntry statEntry)
        {
            if (statEntry == null)
                return 0u;

            for (int i = 0; i < statEntry.ItemStatTypeEnum.Length; i++)
            {
                if (statEntry.ItemStatTypeEnum[i] != ItemStatType.Standard)
                    continue;

                return statEntry.ItemStatData[i];
            }

            return 0u;
        }
    }
}
