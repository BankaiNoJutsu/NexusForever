using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// The zone rule shared by prerequisite types 194 (MountUsage), 195 (HoverboardUsage), 197 (GliderUsage) and 198 (SpaceshipUsage).
    /// </summary>
    /// <remarks>
    /// Native: <c>14049ced0</c> (194 and 195), <c>14049cfb0</c> (197) and <c>14049d090</c> (198) take the local player's current zone id (unit <c>+0x17e8</c>, the field the Lua current-zone-map
    /// functions read), look it up in the zone tree at local-player global <c>+0x7160</c> (<c>14043ed60</c>) and walk the node and its parents, reading the row flags at <c>+0x1c</c>
    /// (<c>WorldZone.flags</c>). A deny bit anywhere on the chain makes the flag false: <c>0x10</c> for mounts and hoverboards, <c>0x40</c> for gliders, <c>0x80</c> for spaceships. The matching
    /// allow bits are <c>0x100</c>, <c>0x200</c> and <c>0x400</c>. A unit that is not the local player answers NotEqual only.
    /// Retail: 281 of 3,436 WorldZone rows carry <c>0x10</c> (interior rooms such as "Queen's Den" and "Crimson Legion Control Room"), 6 carry <c>0x100</c>. The four 194/195 slots sit in the cast
    /// prerequisite of 224 mount summon spells, and the enum names come from the failure texts ("Ground mounts cannot be used in this area").
    /// Not modelled: when the object at unit <c>+0x16f0</c> exists and its virtual <c>+0x78</c> returns 0, the native rule also requires an allow bit on the chain. That object's state is not
    /// identified, so the allow bits are not read here.
    /// See Decomp/Analysis/PREREQUISITE_CHECK_REVIEW_20261110.md. No retail parity claim.
    /// </remarks>
    internal static class ZoneVehicleUsage
    {
        public const uint DenyMount     = 0x10u;
        public const uint DenyGlider    = 0x40u;
        public const uint DenySpaceship = 0x80u;

        /// <summary>
        /// Returns false when the zone or any of its parents carries the deny bit.
        /// </summary>
        public static bool IsAllowed(WorldZoneEntry zone, uint denyBit, IGameTableManager gameTableManager)
        {
            // a parent chain longer than this is a data loop
            for (int depth = 0; zone != null && depth < 32; depth++)
            {
                if ((zone.Flags & denyBit) != 0u)
                    return false;

                if (zone.ParentZoneId == 0u)
                    break;

                zone = gameTableManager?.WorldZone?.GetEntry(zone.ParentZoneId);
            }

            return true;
        }
    }
}
