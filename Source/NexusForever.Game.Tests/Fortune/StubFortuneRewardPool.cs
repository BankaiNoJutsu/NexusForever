using NexusForever.Game.Abstract.Fortune;
using NexusForever.Game.Static.Fortune;

namespace NexusForever.Game.Tests.Fortune;

internal sealed class StubFortuneRewardPool : IFortuneRewardPool
{
    private readonly IReadOnlyList<uint> displayItem2Ids;
    private readonly FortuneCardReward[] cardRewards;

    public StubFortuneRewardPool(
        IReadOnlyList<uint> displayItem2Ids = null,
        FortuneCardReward[] cardRewards = null)
    {
        this.displayItem2Ids = displayItem2Ids ?? [101u, 202u, 303u];
        this.cardRewards = cardRewards ??
        [
            new FortuneCardReward(11u, 101u, RewardRarity.Normal),
            new FortuneCardReward(22u, 202u, RewardRarity.Rare),
            new FortuneCardReward(33u, 303u, RewardRarity.Epic)
        ];
    }

    public IReadOnlyList<uint> GetDisplayItem2Ids() => displayItem2Ids;

    public FortuneCardReward[] PickCardRewards(Random random) => cardRewards;
}
