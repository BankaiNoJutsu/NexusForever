namespace NexusForever.Game.Abstract.Matching.Queue
{
    public interface IMatchingMapSelector
    {
        /// <summary>
        /// Determines the best <see cref="IMatchingMap"/> and role-selection result for the given <see cref="IMatchingQueueGroup"/>.
        /// </summary>
        IMatchingMapSelectorResult Select(IMatchingQueueGroup matchingQueueGroup);
    }
}
