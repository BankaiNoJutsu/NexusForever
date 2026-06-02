using System;
using NexusForever.Game.Configuration.Model;
using NexusForever.Shared.Configuration;

namespace NexusForever.Game.Prerequisite
{
    internal static class InGameTimePrerequisiteHelper
    {
        /// <summary>
        /// Seconds since midnight in the in-game day clock, matching <see cref="Entity.Player.SendInGameTime"/>.
        /// </summary>
        public static uint GetTimeOfDaySeconds()
        {
            uint lengthOfInGameDayInSeconds = SharedConfiguration.Instance.Get<RealmConfig>()?.LengthOfInGameDay ?? 0u;
            if (lengthOfInGameDayInSeconds == 0u)
                lengthOfInGameDayInSeconds = (uint)TimeSpan.FromHours(3.5d).TotalSeconds;

            double timeOfDay = DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds / lengthOfInGameDayInSeconds % 1;
            return (uint)(timeOfDay * TimeSpan.FromDays(1).TotalSeconds);
        }
    }
}
