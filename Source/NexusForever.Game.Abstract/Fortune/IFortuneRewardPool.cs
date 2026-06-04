namespace NexusForever.Game.Abstract.Fortune
{
    public interface IFortuneRewardPool
    {
        /// <summary>
        /// Item2 ids advertised in <c>ServerFortuneRewards</c> when the fortune UI opens.
        /// </summary>
        IReadOnlyList<uint> GetDisplayItem2Ids();

        /// <summary>
        /// Item2 ids and parallel display probabilities for <c>ServerFortuneRewards</c>.
        /// Probabilities are normalized fractions in [0, 1] as consumed by the retail client UI.
        /// </summary>
        FortuneRewardCatalog GetRewardCatalog();

        /// <summary>
        /// Picks three unique card rewards for a new fortune session.
        /// </summary>
        FortuneCardReward[] PickCardRewards(Random random);

        /// <summary>
        /// Returns whether an account item can be displayed safely as a fortune card.
        /// </summary>
        bool IsCardRewardDisplayable(uint accountItemId);
    }
}
