using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;

namespace NexusForever.Game.Map
{
    public class GridActionRelocate : IGridActionRelocate
    {
        public IGridEntity Entity { get; init; }
        private Vector3 vector;
        public Vector3 Vector { get => vector; init => vector = value; }
        internal ulong? PassengerAdmission { get; init; }
        internal Func<bool> OwnsPassengerSeat { get; init; }

        // BaseMap mutates/reads pending targets under its relocation lock.
        internal void UpdateVector(Vector3 position) => vector = position;
    }
}
