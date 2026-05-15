using System;

namespace NexusForever.MapGenerator
{
    internal static class ParallelismHelper
    {
        private const int AutoParallelismCap = 8;

        public static int GetEffectiveMaxDegreeOfParallelism(int configuredValue, int workItemCount = int.MaxValue)
        {
            int candidate = configuredValue > 0
                ? configuredValue
                : Math.Min(Environment.ProcessorCount, AutoParallelismCap);

            if (workItemCount != int.MaxValue)
                candidate = Math.Min(candidate, Math.Max(1, workItemCount));

            return Math.Max(1, candidate);
        }
    }
}