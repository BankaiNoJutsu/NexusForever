using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable.Model;
using NexusForever.GameTable.Static;

namespace NexusForever.Game.Prerequisite
{
    /// <summary>
    /// Proxies client eval-context list walks used by prerequisite table[96] @ <c>14049f810</c>
    /// (nodes keyed at <c>+0x20</c>, float scalar at <c>+0x28</c>, compared via float ApplyComparison).
    /// </summary>
    internal static class PrerequisiteEvalListHelper
    {
        public static bool TryCompareLinkedEvalScalar(
            IPrerequisiteParameters parameters,
            PrerequisiteComparison comparison,
            uint value,
            uint objectId)
        {
            if (parameters?.Item?.Info?.StatEntry == null)
                return false;

            ItemStatEntry stat = parameters.Item.Info.StatEntry;
            if (TryFindStatScalar(stat, objectId, out uint measured))
                return PrerequisiteCompare.Compare(comparison, measured, value);

            if (parameters.Item.Info.Entry?.Id == objectId
                && TryFindStatScalar(stat, (uint)ItemStatType.Unknown4, out measured))
                return PrerequisiteCompare.Compare(comparison, measured, value);

            return false;
        }

        private static bool TryFindStatScalar(ItemStatEntry stat, uint objectId, out uint measured)
        {
            measured = 0u;
            for (int i = 0; i < stat.ItemStatTypeEnum.Length; i++)
            {
                if ((uint)stat.ItemStatTypeEnum[i] != objectId)
                    continue;

                measured = stat.ItemStatData[i];
                return true;
            }

            return false;
        }
    }
}
