using NexusForever.Game.Abstract.Cinematic;

namespace NexusForever.Game.Cinematic
{
    public sealed class GlobalCinematicManager : IGlobalCinematicManager
    {
        /// <summary>
        /// Unique Id to be assigned to the next Cinematic element. For actors, this is the unitId as for regular units
        /// and can easily check if they are actors as the unitIds will all being with 0x40000000.
        /// </summary>
        public uint NextCinematicId => AllocateCinematicId();

        private static uint nextCinematicId = 0x40000000;

        public static uint AllocateCinematicId()
        {
            return nextCinematicId++;
        }

        /// <summary>
        /// Initialises the <see cref="GlobalCinematicManager"/>.
        /// </summary>
        public void Initialise()
        {
            // Deliberately left empty
        }
    }
}
