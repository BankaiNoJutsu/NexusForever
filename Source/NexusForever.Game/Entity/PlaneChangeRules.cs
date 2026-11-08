using NexusForever.Game.Abstract.Entity;

namespace NexusForever.Game.Entity
{
    /// <summary>
    /// Folds the active <see cref="PlaneChange"/> records of a player into the two plane words sent in packet 0x0172.
    /// </summary>
    public static class PlaneChangeRules
    {
        /// <summary>
        /// Both words start at zero (the client's constructor zero-initialises them) and every active change ORs its bits in.
        /// </summary>
        public static void Fold(IEnumerable<PlaneChange> changes, out uint planesCanSee, out uint planesCanBeSeen)
        {
            planesCanSee    = 0u;
            planesCanBeSeen = 0u;

            foreach (PlaneChange change in changes)
            {
                planesCanSee    |= change.AddCanSee;
                planesCanBeSeen |= change.AddCanBeSeen;
            }
        }
    }
}
