using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;

namespace NexusForever.Game.Prerequisite
{
    public static class PrerequisiteEvaluation
    {
        public static bool Meets(IPlayer player, uint prerequisiteId, IItem item = null)
        {
            if (prerequisiteId == 0u)
                return true;

            if (player == null)
                return false;

            var parameters = new PrerequisiteParameters();
            if (item != null)
                parameters.Item = item;

            return PrerequisiteManager.Instance.Meets(player, prerequisiteId, parameters);
        }
    }
}
