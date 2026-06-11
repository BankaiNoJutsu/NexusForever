using System;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;

namespace NexusForever.Game.Prerequisite
{
    public static class PrerequisiteEvaluation
    {
        public static bool Meets(
            IPlayer player,
            uint prerequisiteId,
            IItem item = null,
            IPrerequisiteManager prerequisiteManager = null)
        {
            if (prerequisiteId == 0u)
                return true;

            if (player == null)
                return false;

            var parameters = new PrerequisiteParameters();
            if (item != null)
                parameters.Item = item;

            return GetPrerequisiteManager(prerequisiteManager).Meets(player, prerequisiteId, parameters);
        }

        public static bool MeetsAccountItem(
            IPlayer player,
            uint prerequisiteId,
            IPrerequisiteManager prerequisiteManager = null)
        {
            if (prerequisiteId == 0u)
                return true;

            if (player == null)
                return false;

            var parameters = new PrerequisiteParameters
            {
                AccountItemContext = true
            };

            return GetPrerequisiteManager(prerequisiteManager).Meets(player, prerequisiteId, parameters);
        }

        private static IPrerequisiteManager GetPrerequisiteManager(IPrerequisiteManager prerequisiteManager)
        {
            return prerequisiteManager ?? throw new InvalidOperationException($"{nameof(PrerequisiteEvaluation)} requires an {nameof(IPrerequisiteManager)}.");
        }
    }
}
