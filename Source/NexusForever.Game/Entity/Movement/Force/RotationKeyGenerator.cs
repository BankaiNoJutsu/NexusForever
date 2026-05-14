using System.Numerics;
using NexusForever.Game.Abstract.Entity.Movement.Force;

namespace NexusForever.Game.Entity.Movement.Force
{
    public class RotationKeyGenerator : IRotationKeyGenerator
    {
        private static readonly TimeSpan StepSize = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Generate key data for rotation with the supplied parameters.
        /// </summary>
        public void Calculate(Vector3 startRotation, TimeSpan duration, float spin, out List<Vector3> rotations, out List<uint> times)
        {
            rotations = [];
            times     = [];

            for (uint i = 0; i <= (uint)Math.Ceiling(duration / StepSize); i++)
            {
                TimeSpan currentTime = StepSize * i;
                if (currentTime > duration)
                    currentTime = duration;

                times.Add((uint)currentTime.TotalMilliseconds);

                float currentTimeSeconds = (float)(currentTime - duration).TotalSeconds;
                float angle = (currentTimeSeconds * spin + startRotation.X + MathF.PI) * (1f / (2f * MathF.PI));
                angle -= MathF.Floor(angle);

                float normalisedAngle = angle * (2f * MathF.PI) - MathF.PI;
                rotations.Add(new Vector3(normalisedAngle, startRotation.Y, startRotation.Z));
            }
        }
    }
}
