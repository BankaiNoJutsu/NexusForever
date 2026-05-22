using NexusForever.Game.Static.Fortune;

namespace NexusForever.Game.Abstract.Fortune
{
    public readonly record struct FortuneCardReward(uint AccountItemId, uint Item2Id, RewardRarity Rarity);
}
