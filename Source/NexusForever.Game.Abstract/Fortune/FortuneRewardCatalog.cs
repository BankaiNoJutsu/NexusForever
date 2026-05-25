namespace NexusForever.Game.Abstract.Fortune
{
    public readonly record struct FortuneRewardCatalog(
        IReadOnlyList<uint> Item2IdRewards,
        IReadOnlyList<float> RewardItemProbabilities);
}
