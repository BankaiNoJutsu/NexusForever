using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Costume;
using NexusForever.Game.Abstract.Account.Currency;
using NexusForever.Game.Abstract.Account.Entitlement;
using NexusForever.Game.Abstract.Account.Inventory;
using NexusForever.Game.Abstract.Account.Option;
using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.Game.Abstract.Account.Unlock;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entitlement;
using NexusForever.Game.Abstract.RBAC;
using NexusForever.Game.Account.Reward;
using NexusForever.Game.Spell;
using NexusForever.Game.Spell.Effect;
using NexusForever.Game.Static;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Account.Reward;

public class RewardPropertyManagerTests
{
    [Fact]
    public void Constructor_WithMissingRewardPropertyTable_SkipsPremiumModifierRows()
    {
        (GameTableManager gameTableManager, AssetManager assetManager) = BuildServices(
            [
                CreateModifier(RewardPropertyType.XP, modifierValueFloat: 1.5f)
            ]);
        IAccount account = CreateAccount(out RecordingDispatchProxy<IGameSession> sessionProxy);
        var manager = new RewardPropertyManager(account, assetManager, gameTableManager);

        Assert.Null(manager.GetRewardProperty(RewardPropertyType.XP));

        manager.SendInitialPackets();

        ServerRewardPropertySet packet = Assert.Single(GetEncryptedMessages<ServerRewardPropertySet>(sessionProxy));
        Assert.Empty(packet.Properties);
    }

    [Fact]
    public void Constructor_WithMissingEntitlementTable_SkipsEntitlementBackedModifierRows()
    {
        (GameTableManager gameTableManager, AssetManager assetManager) = BuildServices(
            [
                CreateModifier(RewardPropertyType.XP, entitlementId: EntitlementType.Signature)
            ],
            rewardPropertyEntries:
            [
                CreateRewardProperty(RewardPropertyType.XP, RewardPropertyModifierValueType.Discrete)
            ]);
        IAccount account = CreateAccount(out _, entitlementAmount: 7u);
        var manager = new RewardPropertyManager(account, assetManager, gameTableManager);

        Assert.Null(manager.GetRewardProperty(RewardPropertyType.XP));
    }

    [Fact]
    public void Constructor_WithTableBackedEntitlementModifier_UsesEntitlementValue()
    {
        (GameTableManager gameTableManager, AssetManager assetManager) = BuildServices(
            [
                CreateModifier(RewardPropertyType.XP, entitlementId: EntitlementType.Signature)
            ],
            rewardPropertyEntries:
            [
                CreateRewardProperty(RewardPropertyType.XP, RewardPropertyModifierValueType.Discrete)
            ],
            entitlementEntries:
            [
                new EntitlementEntry
                {
                    Id       = (uint)EntitlementType.Signature,
                    MaxCount = 50u,
                    Flags    = (uint)EntitlementFlags.None
                }
            ]);
        IAccount account = CreateAccount(out _, entitlementAmount: 7u);
        var manager = new RewardPropertyManager(account, assetManager, gameTableManager);

        IRewardProperty property = manager.GetRewardProperty(RewardPropertyType.XP);

        Assert.NotNull(property);
        Assert.Equal(7f, property.GetValue(0u));
    }

    [Fact]
    public void UpdateRewardProperty_WithMissingRewardPropertyTable_DoesNotSendPacket()
    {
        (GameTableManager gameTableManager, AssetManager assetManager) = BuildServices([]);
        IAccount account = CreateAccount(out RecordingDispatchProxy<IGameSession> sessionProxy);
        var manager = new RewardPropertyManager(account, assetManager, gameTableManager);

        manager.UpdateRewardProperty(RewardPropertyType.XP, 1f);

        Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        Assert.Null(manager.GetRewardProperty(RewardPropertyType.XP));
    }

    [Fact]
    public void TryResolveRewardPropertyModifier_WithMissingRewardPropertyTable_ReturnsUnknownRewardProperty()
    {
        (GameTableManager gameTableManager, _) = BuildServices([]);

        bool resolved = SpellHandler.TryResolveRewardPropertyModifier(
            gameTableManager,
            new SpellEffectRewardPropertyModifierSemantics(
                (uint)RewardPropertyType.XP,
                0u,
                1f,
                0f,
                0u,
                0u),
            out RewardPropertyEntry entry,
            out float value,
            out string skippedReason);

        Assert.False(resolved);
        Assert.Null(entry);
        Assert.Equal(0f, value);
        Assert.Equal("unknown-reward-property", skippedReason);
    }

    private static (GameTableManager GameTableManager, AssetManager AssetManager) BuildServices(
        IReadOnlyList<RewardPropertyPremiumModifierEntry> modifiers,
        RewardPropertyEntry[] rewardPropertyEntries = null,
        EntitlementEntry[] entitlementEntries = null)
    {
        var gameTableManager = (GameTableManager)RuntimeHelpers.GetUninitializedObject(typeof(GameTableManager));
        if (rewardPropertyEntries != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.RewardProperty), CreateGameTable(rewardPropertyEntries));
        if (entitlementEntries != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.Entitlement), CreateGameTable(entitlementEntries));

        var assetManager = new AssetManager();
        SetPrivateField(assetManager, "rewardPropertiesByTier", ImmutableDictionary<AccountTier, ImmutableList<RewardPropertyPremiumModifierEntry>>
            .Empty
            .Add(AccountTier.Signature, modifiers.ToImmutableList()));

        return (gameTableManager, assetManager);
    }

    private static IAccount CreateAccount(
        out RecordingDispatchProxy<IGameSession> sessionProxy,
        uint? entitlementAmount = null)
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
        IAccountEntitlementManager entitlementManager = RecordingDispatchProxy<IAccountEntitlementManager>.Create(
            out RecordingDispatchProxy<IAccountEntitlementManager> entitlementManagerProxy);

        if (entitlementAmount.HasValue)
        {
            IAccountEntitlement entitlement = RecordingDispatchProxy<IAccountEntitlement>.Create(
                out RecordingDispatchProxy<IAccountEntitlement> entitlementProxy);
            entitlementProxy.SetProperty(nameof(IEntitlement.Amount), entitlementAmount.Value);
            entitlementManagerProxy.SetMethodReturn(nameof(IAccountEntitlementManager.GetEntitlement), entitlement);
        }

        accountProxy.SetProperty(nameof(IAccount.AccountTier), AccountTier.Signature);
        accountProxy.SetProperty(nameof(IAccount.Session), session);
        accountProxy.SetProperty(nameof(IAccount.EntitlementManager), entitlementManager);
        return account;
    }

    private static RewardPropertyPremiumModifierEntry CreateModifier(
        RewardPropertyType rewardPropertyType,
        float modifierValueFloat = 0f,
        EntitlementType entitlementId = 0)
    {
        return new RewardPropertyPremiumModifierEntry
        {
            Id                         = (uint)rewardPropertyType,
            PremiumSystemEnum          = (uint)PremiumSystem.Hybrid,
            Tier                       = (uint)AccountTier.Signature,
            RewardPropertyId           = (uint)rewardPropertyType,
            ModifierValueFloat         = modifierValueFloat,
            EntitlementIdModifierCount = (uint)entitlementId
        };
    }

    private static RewardPropertyEntry CreateRewardProperty(
        RewardPropertyType rewardPropertyType,
        RewardPropertyModifierValueType valueType)
    {
        return new RewardPropertyEntry
        {
            Id                          = (uint)rewardPropertyType,
            RewardModifierValueTypeEnum = (uint)valueType
        };
    }

    private static IReadOnlyList<T> GetEncryptedMessages<T>(RecordingDispatchProxy<IGameSession> sessionProxy)
        where T : class, IWritable
    {
        return sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(i => i.Arguments[0])
            .OfType<T>()
            .ToList();
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        SetPrivateField(table, "header", new GameTableHeader
        {
            MaxId = entries.Length == 0 ? 0u : entries.Max(GetEntryId) + 1u
        });
        SetPrivateField(table, "lookup", BuildLookup(entries));
        return table;
    }

    private static int[] BuildLookup<T>(IReadOnlyList<T> entries)
    {
        if (entries.Count == 0)
            return [];

        int[] lookup = Enumerable.Repeat(-1, (int)(entries.Max(GetEntryId) + 1u)).ToArray();
        for (int i = 0; i < entries.Count; i++)
            lookup[GetEntryId(entries[i])] = i;

        return lookup;
    }

    private static uint GetEntryId<T>(T entry)
    {
        return (uint)typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public)[0].GetValue(entry)!;
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(instance, value);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(instance, value);
    }
}
