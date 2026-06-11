using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Static;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Account.Reward;

public class AssetManagerRewardPropertyTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CacheRewardPropertiesByTier_WithMissingModifierTableUsesEmptyCache(bool includeEmptyTable)
    {
        GameTableManager gameTableManager = CreateGameTableManager(
            includeEmptyTable ? CreateGameTable<RewardPropertyPremiumModifierEntry>() : null);
        var assetManager = new AssetManager(null, gameTableManager);

        CacheRewardPropertiesByTier(assetManager);

        Assert.Empty(assetManager.GetRewardPropertiesForTier(AccountTier.Basic));
        Assert.Empty(assetManager.GetRewardPropertiesForTier(AccountTier.Signature));
    }

    [Fact]
    public void CacheRewardPropertiesByTier_WithTableBackedModifiersFiltersHybridAndFallThroughRows()
    {
        RewardPropertyPremiumModifierEntry basicHybrid = CreateModifier(
            id: 1u,
            tier: AccountTier.Basic,
            flags: RewardPropertyPremiumModiferFlags.FallThrough);
        RewardPropertyPremiumModifierEntry signatureHybrid = CreateModifier(
            id: 2u,
            tier: AccountTier.Signature);
        RewardPropertyPremiumModifierEntry vipIgnored = CreateModifier(
            id: 3u,
            tier: AccountTier.Basic,
            premiumSystem: PremiumSystem.VIP,
            flags: RewardPropertyPremiumModiferFlags.FallThrough);

        GameTableManager gameTableManager = CreateGameTableManager(CreateGameTable(
            basicHybrid,
            signatureHybrid,
            vipIgnored));
        var assetManager = new AssetManager(null, gameTableManager);

        CacheRewardPropertiesByTier(assetManager);

        RewardPropertyPremiumModifierEntry basicEntry = Assert.Single(assetManager.GetRewardPropertiesForTier(AccountTier.Basic));
        Assert.Same(basicHybrid, basicEntry);

        Assert.Collection(
            assetManager.GetRewardPropertiesForTier(AccountTier.Signature),
            entry => Assert.Same(signatureHybrid, entry),
            entry => Assert.Same(basicHybrid, entry));
    }

    private static GameTableManager CreateGameTableManager(GameTable<RewardPropertyPremiumModifierEntry> modifierTable)
    {
        var gameTableManager = (GameTableManager)RuntimeHelpers.GetUninitializedObject(typeof(GameTableManager));
        if (modifierTable != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.RewardPropertyPremiumModifier), modifierTable);

        return gameTableManager;
    }

    private static RewardPropertyPremiumModifierEntry CreateModifier(
        uint id,
        AccountTier tier,
        PremiumSystem premiumSystem = PremiumSystem.Hybrid,
        RewardPropertyPremiumModiferFlags flags = RewardPropertyPremiumModiferFlags.None)
    {
        return new RewardPropertyPremiumModifierEntry
        {
            Id                = id,
            PremiumSystemEnum = (uint)premiumSystem,
            Tier              = (uint)tier,
            RewardPropertyId  = id,
            Flags             = (uint)flags
        };
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        return table;
    }

    private static void CacheRewardPropertiesByTier(AssetManager assetManager)
    {
        typeof(AssetManager)
            .GetMethod("CacheRewardPropertiesByTier", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(assetManager, null);
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        backingField.SetValue(instance, value);
    }
}
