using System.Collections.Generic;

namespace NexusForever.Game.Prerequisite
{
    internal static class PathScientistPrerequisiteHelper
    {
        /// <summary>
        /// Script-backed scientist scan missions that grant <see cref="PathScientistCreatureInfo"/> scan credit.
        /// </summary>
        public static IReadOnlyDictionary<ushort, uint> MissionToCreatureInfoId { get; } = new Dictionary<ushort, uint>
        {
            { 42, 34 },   // NorthernWilds Turning the Tide
            { 160, 130 }, // Vitalium Crystal
            { 648, 128 }, // Skeech Physiology
        };
    }
}
