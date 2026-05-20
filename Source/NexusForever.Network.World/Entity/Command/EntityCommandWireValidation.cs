namespace NexusForever.Network.World.Entity.Command
{
    internal static class EntityCommandWireValidation
    {
        public static void RequireSameCount<TLeft, TRight>(
            ICollection<TLeft> left,
            string leftName,
            ICollection<TRight> right,
            string rightName,
            string packetName)
        {
            if (left.Count != right.Count)
                throw new InvalidOperationException($"{packetName} requires {leftName} and {rightName} to have the same count.");
        }
    }
}
