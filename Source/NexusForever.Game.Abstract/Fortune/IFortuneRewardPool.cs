namespace NexusForever.Game.Abstract.Fortune
{
    public interface IFortuneRewardPool
    {
        /// <summary>
        /// Item2 ids advertised in <c>ServerFortuneRewards</c> when the fortune UI opens.
        /// </summary>
        IReadOnlyList<uint> GetDisplayItem2Ids();

        /// <summary>
        /// Picks three unique card rewards for a new fortune session.
        /// </summary>
        FortuneCardReward[] PickCardRewards(Random random);
    }
}
