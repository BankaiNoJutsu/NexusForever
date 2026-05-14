using System.Numerics;
using NexusForever.Game.Abstract.Entity.Movement.Force;

namespace NexusForever.Game.Entity.Movement.Force
{
    public class ProjectileKeyGenerator : IProjectileKeyGenerator
    {
        private static readonly TimeSpan StepSize = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Generate key data for a projectile with the supplied parameters.
        /// </summary>
        public void Calculate(TimeSpan flightTime, float gravity, Vector3 start, Vector3 end, out List<Vector3> positions, out List<Vector3> velocities, out List<uint> times)
        {
            positions  = [];
            velocities = [];
            times      = [];

            var gravityVector = new Vector3(0f, -gravity, 0f);

            Vector3 initialVelocity = (end - start) / (float)flightTime.TotalSeconds;
            initialVelocity -= 0.5f * (float)flightTime.TotalSeconds * gravityVector;

            for (uint i = 0; i <= (uint)Math.Ceiling(flightTime / StepSize); i++)
            {
                TimeSpan currentTime = StepSize * i;
                if (currentTime > flightTime)
                    currentTime = flightTime;

                times.Add((uint)currentTime.TotalMilliseconds);

                float currentTimeSeconds = (float)currentTime.TotalSeconds;

                Vector3 velocityDisplacement = initialVelocity * currentTimeSeconds;
                Vector3 gravityDisplacement  = 0.5f * gravityVector * currentTimeSeconds * currentTimeSeconds;
                positions.Add(start + velocityDisplacement + gravityDisplacement);

                velocities.Add(initialVelocity + gravityVector * currentTimeSeconds);
            }
        }
    }
}
