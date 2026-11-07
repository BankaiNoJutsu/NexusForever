using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite
{
    internal static class MovementPrerequisiteHelper
    {
        public static IWorldEntity GetPlatformEntity(IPlayer player)
        {
            if (player.PlatformGuid == null)
                return null;

            return player.GetVisible<IWorldEntity>(player.PlatformGuid.Value);
        }

        public static uint GetPlatformEntityType(IPlayer player)
        {
            IWorldEntity platform = GetPlatformEntity(player);
            return platform == null ? 0u : (uint)platform.Type;
        }

        public static uint GetMountedScalar(IPlayer player) =>
            GetPlatformEntity(player) != null ? 1u : 0u;

        /// <summary>
        /// Native vehicle and mount tests (types 83 and 84, <c>FUN_14049cbb0</c> and <c>FUN_14049ccc0</c>): the unit's vehicle entity must be of <paramref name="platformType"/>; without one Equal is false
        /// and NotEqual true. With one it matches when <paramref name="creatureId"/> is 0 or equals the entity's creature id and, unless <paramref name="seatType"/> is -1 (0xffffffff), when the unit's seat
        /// type equals it. Equal returns the match, NotEqual its negation and any other comparison is false.
        /// </summary>
        public static bool MeetsPlatformVehicle(IPlayer player, EntityType platformType, PrerequisiteComparison comparison, uint seatType, uint creatureId)
        {
            IWorldEntity platform = GetPlatformEntity(player);
            if (platform == null || platform.Type != platformType)
                return comparison == PrerequisiteComparison.NotEqual;

            bool match = creatureId == 0u || platform.CreatureId == creatureId;
            if (seatType != uint.MaxValue)
            {
                // The client initialises a unit's seat type to 3 (invalid) and only a seated rider has another value.
                VehicleSeatType seat = platform is IVehicleEntity vehicle
                    ? vehicle.GetPassenger(player.Guid)?.SeatType ?? VehicleSeatType.Invalid
                    : VehicleSeatType.Invalid;
                if ((uint)seat != seatType)
                    match = false;
            }

            return comparison switch
            {
                PrerequisiteComparison.Equal    => match,
                PrerequisiteComparison.NotEqual => !match,
                _                               => false
            };
        }
    }
}
