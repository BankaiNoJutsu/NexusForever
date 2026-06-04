using NexusForever.Game.Abstract.Fortune;
using NexusForever.Game.Static.Fortune;

namespace NexusForever.Game.Tests.Fortune;

internal sealed class StubFortuneRewardPool : IFortuneRewardPool
{
    private readonly IReadOnlyList<uint> displayItem2Ids;
    private readonly FortuneCardReward[] cardRewards;
    private readonly IReadOnlySet<uint> displayableAccountItemIds;

    public StubFortuneRewardPool(
        IReadOnlyList<uint> displayItem2Ids = null,
        FortuneCardReward[] cardRewards = null,
        IReadOnlySet<uint> displayableAccountItemIds = null)
    {
        this.displayItem2Ids = displayItem2Ids ?? [101u, 202u, 303u];
        this.cardRewards = cardRewards ??
        [
            new FortuneCardReward(11u, 101u, RewardRarity.Normal),
            new FortuneCardReward(22u, 202u, RewardRarity.Rare),
            new FortuneCardReward(33u, 303u, RewardRarity.Epic)
        ];
        this.displayableAccountItemIds = displayableAccountItemIds
            ?? this.cardRewards.Select(reward => reward.AccountItemId).ToHashSet();
    }

    public IReadOnlyList<uint> GetDisplayItem2Ids() => displayItem2Ids;

    public FortuneRewardCatalog GetRewardCatalog()
    {
        var probabilities = new float[displayItem2Ids.Count];
        if (probabilities.Length > 0)
            probabilities[0] = 1f;

        return new FortuneRewardCatalog(displayItem2Ids, probabilities);
    }

    public FortuneCardReward[] PickCardRewards(Random random) => cardRewards;

    public bool IsCardRewardDisplayable(uint accountItemId)
    {
        return displayableAccountItemIds.Contains(accountItemId);
    }
}
