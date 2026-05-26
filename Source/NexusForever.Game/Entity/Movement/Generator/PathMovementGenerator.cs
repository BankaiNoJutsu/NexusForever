using System.Numerics;
using NexusForever.Game.Abstract.Entity.Movement.Generator;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Entity.Movement;

namespace NexusForever.Game.Entity.Movement.Generator
{
    public class PathMovementGenerator : IPathMovementGenerator
    {
        private const float StepSize = 2f;

        public Vector3 Begin { get; set; }
        public Vector3 Final { get; set; }
        public IBaseMap Map { get; set; }

        public List<Vector3> CalculatePath()
        {
            var points = new List<Vector3> { Begin };
            if (!MovementMath.IsFinite(Begin) || !MovementMath.IsFinite(Final))
                return points;

            float distance = Vector2.Distance(new Vector2(Begin.X, Begin.Z), new Vector2(Final.X, Final.Z));
            if (!MovementMath.IsFinite(distance) || distance <= float.Epsilon)
            {
                points.Add(Final);
                return points;
            }

            float angle = MathF.Atan2(Final.Z - Begin.Z, Final.X - Begin.X);
            int steps = (int)MathF.Floor(distance / StepSize);
            for (int i = 0; i < steps; i++)
            {
                float travelled = (i + 1) * StepSize;
                if (travelled >= distance)
                    break;

                Vector3 next = points[^1].GetPoint2D(angle, StepSize);
                float offset = travelled / distance;
                next.Y = Map?.GetTerrainHeight(next.X, next.Z) ?? (Begin.Y + ((Final.Y - Begin.Y) * offset));
                points.Add(next);
            }

            points.Add(Final);
            return points;
        }
    }
}
