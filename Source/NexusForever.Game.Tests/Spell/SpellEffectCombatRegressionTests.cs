using System.Reflection;
using System.Numerics;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement.Force;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Entity;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Loot;
using NexusForever.Game.Static.Pet;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Abilities;
using NexusForever.Network.World.Message.Model.Pet;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Tests.Spell;

[Collection(MissingGameDataDiagnosticsCollection.Name)]
public class SpellEffectCombatRegressionTests
{
    [Fact]
    public void HandleEffectVendorPriceModifier_WithSmallDiscountRow_AddsExactPacketChannelMultipliers()
    {
        IPlayer player = CreatePlayer(out _, out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetMethodHandler(nameof(IPlayer.TryAddVendorPriceModifier), args =>
        {
            args[8] = null;
            return true;
        });

        ISpell spell = CreateSpell(new Spell4Entry
        {
            Id                        = 28618u,
            Spell4BaseIdBaseSpell     = 15098u,
            Spell4StackGroupId        = 349u
        }, player);
        ISpellTargetEffectInfo info = CreateEffectInfo(
            49392u,
            28618u,
            SpellEffectType.VendorPriceModifier,
            BitConverter.SingleToUInt32Bits(0.95f),
            BitConverter.SingleToUInt32Bits(1.05f));

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectVendorPriceModifier(spell, player, info);

        RecordingDispatchProxy<IPlayer>.Invocation add =
            Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.TryAddVendorPriceModifier)));
        Assert.Equal(49392u, (uint)add.Arguments[0]);
        Assert.Equal(0.95f, (float)add.Arguments[1]);
        Assert.Equal(1.05f, (float)add.Arguments[2]);
        Assert.Equal(28618u, (uint)add.Arguments[3]);
        Assert.Equal(49392u, (uint)add.Arguments[4]);
        Assert.Equal(349u, (uint)add.Arguments[6]);
    }

    [Fact]
    public void HandleEffectUnitPropertyConversion_WithMountainRow_AddsLiveDependency()
    {
        IUnitEntity target = RecordingDispatchProxy<IUnitEntity>.Create(out RecordingDispatchProxy<IUnitEntity> targetProxy);
        targetProxy.SetMethodHandler(nameof(IUnitEntity.TryAddSpellPropertyConversion), args =>
        {
            args[5] = null;
            return true;
        });

        ISpell spell = CreateSpell(new Spell4Entry
        {
            Id                    = 82056u,
            Spell4BaseIdBaseSpell = 58340u
        }, target);
        ISpellTargetEffectInfo info = CreateEffectInfo(
            215867u,
            82056u,
            SpellEffectType.UnitPropertyConversion,
            (uint)Property.BaseHealth,
            (uint)Property.Armor,
            BitConverter.SingleToUInt32Bits(0.01f));

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectUnitPropertyConversion(spell, target, info);

        RecordingDispatchProxy<IUnitEntity>.Invocation add =
            Assert.Single(targetProxy.GetInvocations(nameof(IUnitEntity.TryAddSpellPropertyConversion)));
        var conversion = Assert.IsType<SpellPropertyConversion>(add.Arguments[0]);
        Assert.Equal(Property.BaseHealth, conversion.SourceProperty);
        Assert.Equal(Property.Armor, conversion.Property);
        Assert.Equal(0.01f, conversion.Multiplier);
        Assert.Equal(215867u, (uint)add.Arguments[1]);
    }

    [Fact]
    public void HandleEffectGrantLevelScaledPrestige_WithSupportedMode_RemainsDiagnosticOnly()
    {
        IPlayer player = CreatePlayer(out _, out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Level), 10u);

        ICurrencyManager currencyManager = RecordingDispatchProxy<ICurrencyManager>.Create(out RecordingDispatchProxy<ICurrencyManager> currencyManagerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CurrencyManager), currencyManager);

        ISpell spell = CreateSpell(42932u, player);
        ISpellTargetEffectInfo info = CreateLevelScaledRewardInfo(
            effectId: 100001u,
            spellId: 42932u,
            effectType: SpellEffectType.GrantLevelScaledPrestige,
            percentOfLevel: 10f,
            maxLevel: 50u,
            mode: 1u);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectGrantLevelScaledPrestige(spell, player, info);

        Assert.Empty(currencyManagerProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
    }

    [Fact]
    public void HandleEffectGrantLevelScaledXp_UsesSharedLevelSpanCalculation()
    {
        IPlayer player = CreatePlayer(out _, out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Level), 10u);

        IXpManager xpManager = RecordingDispatchProxy<IXpManager>.Create(out RecordingDispatchProxy<IXpManager> xpManagerProxy);
        playerProxy.SetProperty(nameof(IPlayer.XpManager), xpManager);

        IGameTableManager gameTableManager = CreateGameTableManager(
            CreateGameTable(
                new XpPerLevelEntry { Id = 10u, MinXpForLevel = 1_000u },
                new XpPerLevelEntry { Id = 11u, MinXpForLevel = 2_501u }));
        using IDisposable resolverScope = UseDependencyResolver(gameTableManager);

        ISpell spell = CreateSpell(42932u, player);
        ISpellTargetEffectInfo info = CreateLevelScaledRewardInfo(
            effectId: 100002u,
            spellId: 42932u,
            effectType: SpellEffectType.GrantLevelScaledXP,
            percentOfLevel: 10f,
            maxLevel: 50u,
            mode: 1u);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectGrantLevelScaledXp(spell, player, info);

        RecordingDispatchProxy<IXpManager>.Invocation grant =
            Assert.Single(xpManagerProxy.GetInvocations(nameof(IXpManager.GrantXp)));
        Assert.Equal(151u, (uint)grant.Arguments[0]);
        Assert.Equal(ExpReason.Spell, (ExpReason)grant.Arguments[1]);
    }

    [Fact]
    public void HandleEffectGrantLevelScaledPrestige_WithUnsupportedMode_DoesNotGrantCurrency()
    {
        IPlayer player = CreatePlayer(out _, out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Level), 10u);

        ICurrencyManager currencyManager = RecordingDispatchProxy<ICurrencyManager>.Create(out RecordingDispatchProxy<ICurrencyManager> currencyManagerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CurrencyManager), currencyManager);

        ISpell spell = CreateSpell(42932u, player);
        ISpellTargetEffectInfo info = CreateLevelScaledRewardInfo(
            effectId: 100003u,
            spellId: 42932u,
            effectType: SpellEffectType.GrantLevelScaledPrestige,
            percentOfLevel: 10f,
            maxLevel: 50u,
            mode: 2u);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectGrantLevelScaledPrestige(spell, player, info);

        Assert.Empty(currencyManagerProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
    }

    [Fact]
    public void HandleEffectTradeSkillProfession_WithFishingRow_LearnsFishingWithoutDroppingProfession()
    {
        IPlayer player = CreatePlayer(out _, out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetMethodHandler(nameof(IPlayer.LearnTradeskill), _ => true);

        ISpell spell = CreateSpell(32135u, player);
        ISpellTargetEffectInfo info = CreateProgressionEffectInfo(
            effectId: 100010u,
            spellId: 32135u,
            effectType: SpellEffectType.TradeSkillProfession,
            dataBits00: (uint)TradeskillType.Fishing);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectTradeSkillProfession(spell, player, info);

        RecordingDispatchProxy<IPlayer>.Invocation learn =
            Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.LearnTradeskill)));
        Assert.Equal(TradeskillType.Fishing, (TradeskillType)learn.Arguments[0]);
        Assert.Equal((TradeskillType)0, (TradeskillType)learn.Arguments[1]);
    }

    [Fact]
    public void HandleEffectTradeSkillProfession_WithUnmappedPayload_DoesNotLearnTradeskill()
    {
        IPlayer player = CreatePlayer(out _, out RecordingDispatchProxy<IPlayer> playerProxy);
        ISpell spell = CreateSpell(32135u, player);
        ISpellTargetEffectInfo info = CreateProgressionEffectInfo(
            effectId: 100011u,
            spellId: 32135u,
            effectType: SpellEffectType.TradeSkillProfession,
            dataBits00: (uint)TradeskillType.Fishing,
            dataBits01: 1u);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectTradeSkillProfession(spell, player, info);

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.LearnTradeskill)));
    }

    [Fact]
    public void HandleEffectPathMissionIncrement_WithSoldierSwatRow_DelegatesExactMissionAndAmount()
    {
        IPlayer player = CreatePlayer(out _, out RecordingDispatchProxy<IPlayer> playerProxy);
        IPathManager pathManager = RecordingDispatchProxy<IPathManager>.Create(out RecordingDispatchProxy<IPathManager> pathManagerProxy);
        pathManagerProxy.SetMethodHandler(nameof(IPathManager.ProgressSoldierSwatMission), _ => true);
        playerProxy.SetProperty(nameof(IPlayer.PathManager), pathManager);

        ISpell spell = CreateSpell(42373u, player);
        ISpellTargetEffectInfo info = CreateProgressionEffectInfo(
            effectId: 100012u,
            spellId: 42373u,
            effectType: SpellEffectType.PathMissionIncrement,
            dataBits00: 2424u,
            dataBits01: 1u);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectPathMissionIncrement(spell, player, info);

        RecordingDispatchProxy<IPathManager>.Invocation increment =
            Assert.Single(pathManagerProxy.GetInvocations(nameof(IPathManager.ProgressSoldierSwatMission)));
        Assert.Equal((ushort)2424, (ushort)increment.Arguments[0]);
        Assert.Equal(1u, (uint)increment.Arguments[1]);
    }

    [Fact]
    public void HandleEffectGiveAbilityPointsToPlayer_WithUnlockRow_GrantsOneTierPoint()
    {
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<ISpellManager> spellManagerProxy, out _);
        ISpell spell = CreateSpell(67478u, player);
        ISpellTargetEffectInfo info = CreateProgressionEffectInfo(
            effectId: 169485u,
            spellId: 67478u,
            effectType: SpellEffectType.GiveAbilityPointsToPlayer,
            dataBits00: 1u);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectGiveAbilityPointsToPlayer(spell, player, info);

        RecordingDispatchProxy<ISpellManager>.Invocation grant =
            Assert.Single(spellManagerProxy.GetInvocations(nameof(ISpellManager.AddAbilityTierPoints)));
        Assert.Equal((byte)1, (byte)grant.Arguments[0]);
    }

    [Fact]
    public void HandleEffectGiveAbilityPointsToPlayer_WithUnobservedPayload_DoesNotGrantTierPoint()
    {
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<ISpellManager> spellManagerProxy, out _);
        ISpell spell = CreateSpell(67478u, player);
        ISpellTargetEffectInfo info = CreateProgressionEffectInfo(
            effectId: 169485u,
            spellId: 67478u,
            effectType: SpellEffectType.GiveAbilityPointsToPlayer,
            dataBits00: 2u);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectGiveAbilityPointsToPlayer(spell, player, info);

        Assert.Empty(spellManagerProxy.GetInvocations(nameof(ISpellManager.AddAbilityTierPoints)));
    }

    [Fact]
    public void HandleEffectGiveAbilityPointsToPlayer_WithUnobservedTail_DoesNotGrantTierPoint()
    {
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<ISpellManager> spellManagerProxy, out _);
        ISpell spell = CreateSpell(67478u, player);
        ISpellTargetEffectInfo info = CreateProgressionEffectInfo(
            effectId: 169485u,
            spellId: 67478u,
            effectType: SpellEffectType.GiveAbilityPointsToPlayer,
            dataBits00: 1u,
            dataBits09: 1u);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectGiveAbilityPointsToPlayer(spell, player, info);

        Assert.Empty(spellManagerProxy.GetInvocations(nameof(ISpellManager.AddAbilityTierPoints)));
    }

    [Fact]
    public void HandleEffectSpellImmunity_WithConcreteSpellMode_AddsImmunity()
    {
        IUnitEntity target = RecordingDispatchProxy<IUnitEntity>.Create(out RecordingDispatchProxy<IUnitEntity> targetProxy);
        targetProxy.SetProperty(nameof(IUnitEntity.Guid), 170u);
        ISpell spell = CreateSpell(48019u, target);
        ISpellTargetEffectInfo info = CreateSpellImmunityInfo(100004u, 48019u, 0u, 48020u);

        using IDisposable resolverScope = UseDependencyResolver(CreateGameTableManager(new Spell4Entry { Id = 48020u }));

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectSpellImmunity(spell, target, info);

        RecordingDispatchProxy<IUnitEntity>.Invocation add =
            Assert.Single(targetProxy.GetInvocations(nameof(IUnitEntity.AddSpellImmunity)));
        Assert.Equal(48020u, (uint)add.Arguments[3]);
        Assert.Equal(0u, (uint)add.Arguments[4]);
    }

    [Theory]
    [InlineData(1u, 1316u)]
    [InlineData(2u, 7u)]
    public void HandleEffectSpellImmunity_WithUnsupportedMode_DoesNotTreatPayloadAsSpellId(uint mode, uint payload)
    {
        IUnitEntity target = RecordingDispatchProxy<IUnitEntity>.Create(out RecordingDispatchProxy<IUnitEntity> targetProxy);
        targetProxy.SetProperty(nameof(IUnitEntity.Guid), 170u);
        ISpell spell = CreateSpell(48019u, target);
        ISpellTargetEffectInfo info = CreateSpellImmunityInfo(100005u + mode, 48019u, mode, payload);

        using IDisposable resolverScope = UseDependencyResolver(CreateGameTableManager(new Spell4Entry { Id = payload }));

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectSpellImmunity(spell, target, info);

        Assert.Empty(targetProxy.GetInvocations(nameof(IUnitEntity.AddSpellImmunity)));
    }

    [Fact]
    public void HandleEffectModifySpellCooldown_WithOnslaughtCategoryPayload_DoesNotResetCurrentSpellCooldown()
    {
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<ISpellManager> spellManagerProxy, out _);
        ISpell spell = CreateSpell(46867u, player);
        ISpellTargetEffectInfo info = CreateModifySpellCooldownInfo(
            effectId: 173869u,
            spellId: 46867u,
            mode: 2u,
            targetSpell4Id: 66u,
            operation: 0u,
            dataFloat03: 0f);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectModifySpellCooldown(spell, player, info);

        Assert.Empty(spellManagerProxy.GetInvocations(nameof(ISpellManager.GetSpellCooldown)));
        Assert.Empty(spellManagerProxy.GetInvocations(nameof(ISpellManager.SetSpellCooldown)));
    }

    [Fact]
    public void HandleEffectProxy_WithRelentlessAddCellChain_CastsChildOnOriginalCaster()
    {
        IPlayer player = CreatePlayer(out _, out RecordingDispatchProxy<IPlayer> playerProxy);
        IUnitEntity target = RecordingDispatchProxy<IUnitEntity>.Create(out RecordingDispatchProxy<IUnitEntity> targetProxy);
        targetProxy.SetProperty(nameof(IUnitEntity.Guid), 170u);

        ISpell spell = CreateSpell(70033u, player);
        ISpellTargetEffectInfo info = CreateProxyInfo(
            effectId: 177057u,
            spellId: 70033u,
            proxySpell4Id: 53865u);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectProxy(spell, target, info);

        RecordingDispatchProxy<IPlayer>.Invocation cast = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.CastSpell)));
        Assert.Equal(53865u, (uint)cast.Arguments[0]);
        Assert.Equal(42u, ((ISpellParameters)cast.Arguments[1]).PrimaryTargetId);
        Assert.Empty(targetProxy.GetInvocations(nameof(IUnitEntity.CastSpell)));
    }

    [Fact]
    public void HandleEffectProxy_WithExplicitPosition_ForwardsPositionToChildSpell()
    {
        IPlayer player = CreatePlayer(out _, out _);
        IUnitEntity target = RecordingDispatchProxy<IUnitEntity>.Create(out RecordingDispatchProxy<IUnitEntity> targetProxy);
        targetProxy.SetProperty(nameof(IUnitEntity.Guid), 170u);

        var position = new Position(new Vector3(10f, 2f, 30f));
        ISpell spell = CreateSpell(45954u, player, position: position);
        ISpellTargetEffectInfo info = CreateProxyInfo(
            effectId: 108266u,
            spellId: 45954u,
            proxySpell4Id: 46252u);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectProxy(spell, target, info);

        RecordingDispatchProxy<IUnitEntity>.Invocation cast = Assert.Single(targetProxy.GetInvocations(nameof(IUnitEntity.CastSpell)));
        Assert.Equal(46252u, (uint)cast.Arguments[0]);
        ISpellParameters parameters = Assert.IsAssignableFrom<ISpellParameters>(cast.Arguments[1]);
        Assert.Equal(170u, parameters.PrimaryTargetId);
        Assert.Same(position, parameters.Position);
    }

    [Theory]
    [InlineData(34589u)]
    [InlineData(63375u)]
    public void HandleEffectProxy_WithArtillerybotBarragePetProxy_CastsOnceFromPetCaster(uint proxySpell4Id)
    {
        IUnitEntity pet = RecordingDispatchProxy<IUnitEntity>.Create(out RecordingDispatchProxy<IUnitEntity> petProxy);
        petProxy.SetProperty(nameof(IUnitEntity.Guid), 661u);

        IUnitEntity firstTelegraphTarget = RecordingDispatchProxy<IUnitEntity>.Create(out RecordingDispatchProxy<IUnitEntity> firstTargetProxy);
        firstTargetProxy.SetProperty(nameof(IUnitEntity.Guid), 457u);
        IUnitEntity secondTelegraphTarget = RecordingDispatchProxy<IUnitEntity>.Create(out RecordingDispatchProxy<IUnitEntity> secondTargetProxy);
        secondTargetProxy.SetProperty(nameof(IUnitEntity.Guid), 657u);

        var position = new Position(new Vector3(10f, 2f, 30f));
        ISpell spell = CreateSpell(
            new Spell4Entry
            {
                Id                    = 49502u,
                Spell4BaseIdBaseSpell = 32710u
            },
            pet,
            primaryTargetId: 657u,
            position: position);
        ISpellTargetEffectInfo info = CreateProxyInfo(
            effectId: 117258u,
            spellId: 49502u,
            proxySpell4Id: proxySpell4Id);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectProxy(spell, firstTelegraphTarget, info);
        global::NexusForever.Game.Spell.SpellHandler.HandleEffectProxy(spell, secondTelegraphTarget, info);

        RecordingDispatchProxy<IUnitEntity>.Invocation cast = Assert.Single(petProxy.GetInvocations(nameof(IUnitEntity.CastSpell)));
        Assert.Equal(proxySpell4Id, (uint)cast.Arguments[0]);
        ISpellParameters parameters = Assert.IsAssignableFrom<ISpellParameters>(cast.Arguments[1]);
        Assert.Equal(661u, parameters.PrimaryTargetId);
        Assert.Same(position, parameters.Position);
        Assert.Empty(firstTargetProxy.GetInvocations(nameof(IUnitEntity.CastSpell)));
        Assert.Empty(secondTargetProxy.GetInvocations(nameof(IUnitEntity.CastSpell)));
    }

    [Fact]
    public void HandleEffectProxy_WithArtillerybotBarrageDamageProxy_CastsDamageFromPetCaster()
    {
        IUnitEntity pet = RecordingDispatchProxy<IUnitEntity>.Create(out RecordingDispatchProxy<IUnitEntity> petProxy);
        petProxy.SetProperty(nameof(IUnitEntity.Guid), 661u);

        IUnitEntity target = RecordingDispatchProxy<IUnitEntity>.Create(out RecordingDispatchProxy<IUnitEntity> targetProxy);
        targetProxy.SetProperty(nameof(IUnitEntity.Guid), 457u);

        ISpellInfo parentSpellInfo = CreateSpellInfo(new Spell4Entry
        {
            Id                    = 49502u,
            Spell4BaseIdBaseSpell = 32710u
        });
        var position = new Position(new Vector3(10f, 2f, 30f));
        ISpell spell = CreateSpell(
            new Spell4Entry
            {
                Id                    = 34589u,
                Spell4BaseIdBaseSpell = 20559u
            },
            pet,
            primaryTargetId: 457u,
            position: position,
            parentSpellInfo: parentSpellInfo);
        ISpellTargetEffectInfo info = CreateProxyInfo(
            effectId: 68577u,
            spellId: 34589u,
            proxySpell4Id: 35548u);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectProxy(spell, target, info);

        RecordingDispatchProxy<IUnitEntity>.Invocation cast = Assert.Single(petProxy.GetInvocations(nameof(IUnitEntity.CastSpell)));
        Assert.Equal(35548u, (uint)cast.Arguments[0]);
        ISpellParameters parameters = Assert.IsAssignableFrom<ISpellParameters>(cast.Arguments[1]);
        Assert.Equal(661u, parameters.PrimaryTargetId);
        Assert.Same(position, parameters.Position);
        Assert.Empty(targetProxy.GetInvocations(nameof(IUnitEntity.CastSpell)));
    }

    [Fact]
    public void HandleEffectProxyRandomExclusive_WithSingleWeightedCandidate_CastsSelectedChild()
    {
        IPlayer player = CreatePlayer(out _, out _);
        IUnitEntity target = RecordingDispatchProxy<IUnitEntity>.Create(out RecordingDispatchProxy<IUnitEntity> targetProxy);
        targetProxy.SetProperty(nameof(IUnitEntity.Guid), 170u);

        ISpell spell = CreateSpell(45954u, player);
        ISpellTargetEffectInfo info = CreateProxyRandomExclusiveInfo(
            effectId: 108266u,
            spellId: 45954u,
            zeroWeightSpell4Id: 46251u,
            weightedSpell4Id: 46252u);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectProxyRandomExclusive(spell, target, info);

        RecordingDispatchProxy<IUnitEntity>.Invocation cast = Assert.Single(targetProxy.GetInvocations(nameof(IUnitEntity.CastSpell)));
        Assert.Equal(46252u, (uint)cast.Arguments[0]);
        Assert.Equal(170u, ((ISpellParameters)cast.Arguments[1]).PrimaryTargetId);
    }

    [Fact]
    public void HandleEffectGiveLootTableToPlayer_WithGeneratedLoot_DeliversGrantedLoot()
    {
        IPlayer player = CreatePlayer(out _, out _);
        ISpell spell = CreateSpell(3649u, player);
        ISpellTargetEffectInfo info = CreateGiveLootTableToPlayerInfo(
            effectId: 6142u,
            spellId: 3649u,
            lootGroupId: 2097u,
            rollCount: 0u);

        IReadOnlyList<GeneratedLootItem> generatedItems =
        [
            new GeneratedLootItem(LootItemType.Cash, (uint)CurrencyType.Credits, 25u)
        ];
        IGlobalLootManager lootManager = RecordingDispatchProxy<IGlobalLootManager>.Create(out RecordingDispatchProxy<IGlobalLootManager> lootProxy);
        lootProxy.SetMethodHandler(nameof(IGlobalLootManager.TryGenerateLoot), args =>
        {
            Assert.Equal(2097u, (uint)args[0]);
            Assert.Same(player, args[1]);
            Assert.Equal(1u, (uint)args[2]);
            args[3] = generatedItems;
            args[4] = string.Empty;
            return true;
        });
        lootProxy.SetMethodHandler(nameof(IGlobalLootManager.CanDeliverGeneratedLoot), args =>
        {
            Assert.Same(player, args[0]);
            Assert.Same(generatedItems, args[1]);
            args[2] = string.Empty;
            return true;
        });

        using IDisposable resolverScope = UseDependencyResolver(globalLootManager: lootManager);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectGiveLootTableToPlayer(spell, player, info);

        RecordingDispatchProxy<IGlobalLootManager>.Invocation give =
            Assert.Single(lootProxy.GetInvocations(nameof(IGlobalLootManager.GiveGeneratedLoot)));
        Assert.Same(player, give.Arguments[0]);
        Assert.Same(generatedItems, give.Arguments[1]);
        Assert.Equal(42u, (uint)give.Arguments[2]);
        Assert.True((bool)give.Arguments[3]);
        Assert.Equal(42u, (uint)give.Arguments[4]);
    }

    [Fact]
    public void HandleEffectGiveLootTableToPlayer_WhenLootGroupMissing_DoesNotDeliver()
    {
        IPlayer player = CreatePlayer(out _, out _);
        ISpell spell = CreateSpell(60332u, player);
        ISpellTargetEffectInfo info = CreateGiveLootTableToPlayerInfo(
            effectId: 145680u,
            spellId: 60332u,
            lootGroupId: 19868u,
            rollCount: 0u);

        IGlobalLootManager lootManager = RecordingDispatchProxy<IGlobalLootManager>.Create(out RecordingDispatchProxy<IGlobalLootManager> lootProxy);
        lootProxy.SetMethodHandler(nameof(IGlobalLootManager.TryGenerateLoot), args =>
        {
            args[3] = Array.Empty<GeneratedLootItem>();
            args[4] = "unknown-loot-group:19868";
            return false;
        });

        using IDisposable resolverScope = UseDependencyResolver(globalLootManager: lootManager);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectGiveLootTableToPlayer(spell, player, info);

        Assert.Empty(lootProxy.GetInvocations(nameof(IGlobalLootManager.CanDeliverGeneratedLoot)));
        Assert.Empty(lootProxy.GetInvocations(nameof(IGlobalLootManager.GiveGeneratedLoot)));
    }

    [Fact]
    public void HandleEffectPetCastSpell_WithActiveSummon_CastsPetSpellFromSummon()
    {
        IPlayer player = CreatePlayer(out _, out RecordingDispatchProxy<IPlayer> playerProxy);
        IUnitEntity pet = RecordingDispatchProxy<IUnitEntity>.Create(out RecordingDispatchProxy<IUnitEntity> petProxy);
        petProxy.SetProperty(nameof(IUnitEntity.Guid), 777u);

        IEntitySummonFactory summonFactory = RecordingDispatchProxy<IEntitySummonFactory>.Create(out RecordingDispatchProxy<IEntitySummonFactory> summonFactoryProxy);
        summonFactoryProxy.SetMethodHandler(nameof(IEntitySummonFactory.TryGetSummonCreature), args =>
        {
            Assert.Equal(42683u, (uint)args[0]);
            args[1] = pet;
            return true;
        });
        playerProxy.SetProperty(nameof(IPlayer.SummonFactory), summonFactory);

        IGlobalSpellManager globalSpellManager = CreateGlobalSpellManager(
            new Spell4EffectsEntry
            {
                SpellId    = 42814u,
                EffectType = SpellEffectType.SummonPet,
                DataBits00 = 42683u
            });
        IGameTableManager gameTableManager = CreateGameTableManager(
            new Spell4Entry { Id = 42814u },
            new Spell4Entry { Id = 49502u });

        using IDisposable resolverScope = UseDependencyResolver(gameTableManager, globalSpellManager: globalSpellManager);

        var position = new Position(new Vector3(10f, 2f, 30f));
        ISpell spell = CreateSpell(35123u, player, primaryTargetId: 900u, position: position);
        ISpellTargetEffectInfo info = CreatePetCastSpellInfo(
            effectId: 121046u,
            spellId: 35123u,
            requiredSummonSpell4Id: 42814u,
            petSpell4Id: 49502u);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectPetCastSpell(spell, player, info);

        RecordingDispatchProxy<IUnitEntity>.Invocation cast =
            Assert.Single(petProxy.GetInvocations(nameof(IUnitEntity.CastSpell)));
        Assert.Equal(49502u, (uint)cast.Arguments[0]);
        ISpellParameters parameters = (ISpellParameters)cast.Arguments[1];
        Assert.Equal(900u, parameters.PrimaryTargetId);
        Assert.Same(position, parameters.Position);
    }

    [Fact]
    public void HandleEffectPetCastSpell_WithoutActiveSummon_DoesNotCastPetSpell()
    {
        IPlayer player = CreatePlayer(out _, out RecordingDispatchProxy<IPlayer> playerProxy);
        IEntitySummonFactory summonFactory = RecordingDispatchProxy<IEntitySummonFactory>.Create(out RecordingDispatchProxy<IEntitySummonFactory> summonFactoryProxy);
        summonFactoryProxy.SetMethodHandler(nameof(IEntitySummonFactory.TryGetSummonCreature), args =>
        {
            args[1] = null;
            return false;
        });
        playerProxy.SetProperty(nameof(IPlayer.SummonFactory), summonFactory);

        IGlobalSpellManager globalSpellManager = CreateGlobalSpellManager(
            new Spell4EffectsEntry
            {
                SpellId    = 42814u,
                EffectType = SpellEffectType.SummonPet,
                DataBits00 = 42683u
            });
        IGameTableManager gameTableManager = CreateGameTableManager(
            new Spell4Entry { Id = 42814u },
            new Spell4Entry { Id = 49502u });

        using IDisposable resolverScope = UseDependencyResolver(gameTableManager, globalSpellManager: globalSpellManager);

        ISpell spell = CreateSpell(35123u, player, primaryTargetId: 900u);
        ISpellTargetEffectInfo info = CreatePetCastSpellInfo(
            effectId: 121046u,
            spellId: 35123u,
            requiredSummonSpell4Id: 42814u,
            petSpell4Id: 49502u);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectPetCastSpell(spell, player, info);

        Assert.Single(summonFactoryProxy.GetInvocations(nameof(IEntitySummonFactory.TryGetSummonCreature)));
    }

    [Fact]
    public void HandleEffectSummonPet_WithArtillerybotRow_CreatesTrackedSummon()
    {
        IPlayer player = CreatePlayer(out _, out RecordingDispatchProxy<IPlayer> playerProxy);
        var position = new Vector3(12f, 3f, -8f);
        var rotation = new Vector3(0.25f, 0f, 0f);
        playerProxy.SetProperty(nameof(IPlayer.Position), position);
        playerProxy.SetProperty(nameof(IPlayer.Rotation), rotation);
        playerProxy.SetProperty(nameof(IPlayer.Level), 35u);
        playerProxy.SetProperty(nameof(IPlayer.Faction1), Faction.Exile);
        playerProxy.SetProperty(nameof(IPlayer.Faction2), Faction.Exile);
        playerProxy.SetMethodHandler(nameof(IPlayer.GetPropertyValue), args =>
            (Property)args[0] == Property.AssaultRating ? 1234.5f : 0f);

        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetMethodHandler(nameof(IBaseMap.CanEnter), _ => true);
        playerProxy.SetProperty(nameof(IPlayer.Map), map);

        IEntitySummonFactory summonFactory = RecordingDispatchProxy<IEntitySummonFactory>.Create(out _);
        playerProxy.SetProperty(nameof(IPlayer.SummonFactory), summonFactory);

        IWorldEntity pet = RecordingDispatchProxy<IWorldEntity>.Create(out RecordingDispatchProxy<IWorldEntity> petProxy);
        petProxy.SetProperty(nameof(IWorldEntity.Guid), 777u);

        IEntityFactory entityFactory = RecordingDispatchProxy<IEntityFactory>.Create(out RecordingDispatchProxy<IEntityFactory> entityFactoryProxy);
        entityFactoryProxy.SetMethodHandler(nameof(IEntityFactory.CreateWorldEntity), args =>
        {
            Assert.Equal(EntityType.NonPlayer, (EntityType)args[0]);
            return pet;
        });

        IGameTableManager gameTableManager = CreateGameTableManagerWithCreature2(
            new Creature2Entry
            {
                Id               = 42683u,
                CreationTypeEnum = (uint)EntityType.NonPlayer,
                MinLevel         = 1u,
                MaxLevel         = 60u,
                Description      = "Engineer Pet - Artillery Bot - Exile"
            });
        using IDisposable resolverScope = UseDependencyResolver(gameTableManager, entityFactory: entityFactory);

        ISpell spell = CreateSpell(42814u, player);
        ISpellTargetEffectInfo info = CreateSummonPetInfo(
            effectId: 97661u,
            spellId: 42814u,
            creatureId: 42683u,
            petType: 3u,
            out RecordingDispatchProxy<ISpellTargetEffectInfo> infoProxy);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectSummonPet(spell, player, info);

        RecordingDispatchProxy<IWorldEntity>.Invocation initialise =
            Assert.Single(petProxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        Assert.Equal(42683u, (uint)initialise.Arguments[0]);
        Assert.Equal(rotation, pet.Rotation);
        Assert.Equal(42u, pet.SummonerGuid);
        Assert.Equal(Faction.Exile, pet.Faction1);
        Assert.Equal(Faction.Exile, pet.Faction2);
        Assert.Equal(35u, pet.Level);
        Assert.Single(petProxy.GetInvocations(nameof(IWorldEntity.RecalculateCreatureProperties)));
        RecordingDispatchProxy<IWorldEntity>.Invocation assaultRating =
            Assert.Single(petProxy.GetInvocations(nameof(IWorldEntity.SetBaseProperty)),
                i => (Property)i.Arguments[0] == Property.AssaultRating);
        Assert.Equal(1234.5f, (float)assaultRating.Arguments[1]);

        RecordingDispatchProxy<IBaseMap>.Invocation enqueue =
            Assert.Single(mapProxy.GetInvocations(nameof(IBaseMap.EnqueueAdd)));
        Assert.Same(pet, enqueue.Arguments[0]);
        Assert.Equal(position, ((IMapPosition)enqueue.Arguments[1]).Position);

        RecordingDispatchProxy<ISpellTargetEffectInfo>.Invocation created =
            Assert.Single(infoProxy.GetInvocations(nameof(ISpellTargetEffectInfo.AddCreatedEntity)));
        Assert.Same(pet, created.Arguments[0]);
        Assert.Single(entityFactoryProxy.GetInvocations(nameof(IEntityFactory.CreateWorldEntity)));
    }

    [Fact]
    public void HandleEffectSummonPet_WithArtillerybotTier2_ShowsPetBarAndProjectsBarrageIntoSummonSlot()
    {
        AssertEngineerCombatBotCommandRegistration(
            creatureId: 42683u,
            summonSpell4Id: 56249u,
            summonBaseSpell4Id: 27002u,
            tierIndex: 2u,
            petSwitchSpell4Id: 56267u,
            petSwitchBaseSpell4Id: 34051u,
            actionSpell4Id: 56275u,
            actionBaseSpell4Id: 20884u);
    }

    [Fact]
    public void HandleEffectSummonPet_WithArtillerybotExactTierShortcut_ProjectsBarrageIntoSummonSlot()
    {
        AssertEngineerCombatBotCommandRegistration(
            creatureId: 42683u,
            summonSpell4Id: 56249u,
            summonBaseSpell4Id: 27002u,
            tierIndex: 2u,
            petSwitchSpell4Id: 56267u,
            petSwitchBaseSpell4Id: 34051u,
            actionSpell4Id: 56275u,
            actionBaseSpell4Id: 20884u,
            actionSetSpell4Id: 56249u);
    }

    [Fact]
    public void HandleEffectSummonPet_WithRepairbotTier2_RegistersShieldBoostCommand()
    {
        AssertEngineerCombatBotCommandRegistration(
            creatureId: 42682u,
            summonSpell4Id: 55822u,
            summonBaseSpell4Id: 26998u,
            tierIndex: 2u,
            petSwitchSpell4Id: 55864u,
            petSwitchBaseSpell4Id: 21307u,
            actionSpell4Id: 55864u,
            actionBaseSpell4Id: 21307u);
    }

    [Fact]
    public void HandleEffectSummonPet_WithDiminisherbotTier1_RegistersStrobeCommand()
    {
        AssertEngineerCombatBotCommandRegistration(
            creatureId: 42684u,
            summonSpell4Id: 42833u,
            summonBaseSpell4Id: 27021u,
            tierIndex: 1u,
            petSwitchSpell4Id: 70593u,
            petSwitchBaseSpell4Id: 47569u,
            actionSpell4Id: 82988u,
            actionBaseSpell4Id: 58362u);
    }

    [Fact]
    public void HandleEffectSummonPet_WithBruiserbotTier1_RegistersBlitzCommand()
    {
        AssertEngineerCombatBotCommandRegistration(
            creatureId: 42685u,
            summonSpell4Id: 42894u,
            summonBaseSpell4Id: 27082u,
            tierIndex: 1u,
            petSwitchSpell4Id: 35501u,
            petSwitchBaseSpell4Id: 21192u,
            actionSpell4Id: 52419u,
            actionBaseSpell4Id: 34410u);
    }

    [Fact]
    public void UnregisterEngineerCombatBotActions_WithLastArtillerybotGone_ClearsOnlyArtillerybotCommands()
    {
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<ISpellManager> spellManagerProxy, out RecordingDispatchProxy<IPlayer> playerProxy);

        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);

        spellManagerProxy.SetProperty(nameof(ISpellManager.ActiveActionSet), (byte)0);
        ActionSet actionSet = CreateActionSetWithSpellShortcut(player, 27002u, 2);
        spellManagerProxy.SetMethodReturn(nameof(ISpellManager.GetActionSet), actionSet);

        IEntitySummonFactory summonFactory = RecordingDispatchProxy<IEntitySummonFactory>.Create(out RecordingDispatchProxy<IEntitySummonFactory> summonFactoryProxy);
        summonFactoryProxy.SetMethodReturn(nameof(IEntitySummonFactory.GetSummonCreatureCount), 0u);
        playerProxy.SetProperty(nameof(IPlayer.SummonFactory), summonFactory);

        IWorldEntity bot = RecordingDispatchProxy<IWorldEntity>.Create(out RecordingDispatchProxy<IWorldEntity> botProxy);
        botProxy.SetProperty(nameof(IWorldEntity.Guid), 777u);
        botProxy.SetProperty(nameof(IWorldEntity.CreatureId), 42683u);

        IGameTableManager gameTableManager = CreateGameTableManager(
            new Spell4Entry
            {
                Id                    = 51365u,
                Spell4BaseIdBaseSpell = 34051u,
                TierIndex             = 1u
            },
            new Spell4Entry
            {
                Id                    = 56267u,
                Spell4BaseIdBaseSpell = 34051u,
                TierIndex             = 2u
            },
            new Spell4Entry
            {
                Id                    = 35123u,
                Spell4BaseIdBaseSpell = 20884u,
                TierIndex             = 1u
            },
            new Spell4Entry
            {
                Id                    = 56275u,
                Spell4BaseIdBaseSpell = 20884u,
                TierIndex             = 2u
            });
        using IDisposable resolverScope = UseDependencyResolver(gameTableManager);

        global::NexusForever.Game.Spell.SpellHandler.UnregisterEngineerCombatBotActions(player, bot);

        IReadOnlyList<RecordingDispatchProxy<ISpellManager>.Invocation> clears =
            spellManagerProxy.GetInvocations(nameof(ISpellManager.ClearActivePetActionSpell));
        Assert.Equal(2, clears.Count);
        Assert.Equal(51365u, clears[0].Arguments[0]);
        Assert.Equal(35123u, clears[0].Arguments[1]);
        Assert.Equal(56267u, clears[1].Arguments[0]);
        Assert.Equal(56275u, clears[1].Arguments[1]);
        Assert.Empty(spellManagerProxy.GetInvocations(nameof(ISpellManager.ClearActivePetActionSpells)));

        IReadOnlyList<RecordingDispatchProxy<IGameSession>.Invocation> messages =
            sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted));
        ServerPetDespawned petDespawned = Assert.IsType<ServerPetDespawned>(messages[0].Arguments[0]);
        Assert.Equal(777u, petDespawned.PetUnitId);

        ServerActionBarSet hiddenPetBar = Assert.IsType<ServerActionBarSet>(messages[1].Arguments[0]);
        Assert.Equal(ShortcutSet.PrimaryPetBar, hiddenPetBar.ShortcutSet);
        Assert.Equal((ushort)0, hiddenPetBar.ActionBarShortcutSetId);
        Assert.Equal(0u, hiddenPetBar.AssociatedUnitId);

        AssertSpellUpdate(messages, 2, 46724u, 0, false);
        AssertSpellUpdate(messages, 3, 46725u, 0, false);
        AssertSpellUpdate(messages, 4, 25888u, 0, false);

        ServerPetDespawned commandSurfaceDespawned = Assert.IsType<ServerPetDespawned>(messages[5].Arguments[0]);
        Assert.Equal(0u, commandSurfaceDespawned.PetUnitId);

        AssertSpellUpdate(messages, 6, 20884u, 0, false);

        Assert.IsType<ServerActionSetClearCache>(messages[7].Arguments[0]);
        ServerActionSet restoredActionSet = Assert.IsType<ServerActionSet>(messages[8].Arguments[0]);
        ServerActionSet.Action restoredAction = restoredActionSet.Actions[(int)UILocation.LAS3];
        Assert.Equal(ShortcutType.SpellbookItem, restoredAction.ShortcutType);
        Assert.Equal(27002u, restoredAction.ObjectId);
        Assert.Equal(InventoryLocation.Ability, restoredAction.Location.Location);
        Assert.Equal((uint)UILocation.LAS3, restoredAction.Location.BagIndex);
        Assert.DoesNotContain(messages, message => message.Arguments[0] is ServerQuestSpellShortcut);
        Assert.Equal(9, messages.Count);
        Assert.Single(spellManagerProxy.GetInvocations(nameof(ISpellManager.GetActionSet)));
    }

    [Fact]
    public void UnregisterEngineerCombatBotActions_WithOtherEngineerBotActive_KeepsCommandSurface()
    {
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<ISpellManager> spellManagerProxy, out RecordingDispatchProxy<IPlayer> playerProxy);

        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);

        spellManagerProxy.SetProperty(nameof(ISpellManager.ActiveActionSet), (byte)0);
        ActionSet actionSet = CreateActionSetWithSpellShortcut(player, 27002u, 2);
        spellManagerProxy.SetMethodReturn(nameof(ISpellManager.GetActionSet), actionSet);

        IEntitySummonFactory summonFactory = RecordingDispatchProxy<IEntitySummonFactory>.Create(out RecordingDispatchProxy<IEntitySummonFactory> summonFactoryProxy);
        summonFactoryProxy.SetMethodHandler(nameof(IEntitySummonFactory.GetSummonCreatureCount), args =>
            (uint)args[0] == 42682u ? 1u : 0u);
        playerProxy.SetProperty(nameof(IPlayer.SummonFactory), summonFactory);

        IWorldEntity bot = RecordingDispatchProxy<IWorldEntity>.Create(out RecordingDispatchProxy<IWorldEntity> botProxy);
        botProxy.SetProperty(nameof(IWorldEntity.Guid), 777u);
        botProxy.SetProperty(nameof(IWorldEntity.CreatureId), 42683u);

        IGameTableManager gameTableManager = CreateGameTableManager(
            new Spell4Entry
            {
                Id                    = 51365u,
                Spell4BaseIdBaseSpell = 34051u,
                TierIndex             = 1u
            },
            new Spell4Entry
            {
                Id                    = 35123u,
                Spell4BaseIdBaseSpell = 20884u,
                TierIndex             = 1u
            });
        using IDisposable resolverScope = UseDependencyResolver(gameTableManager);

        global::NexusForever.Game.Spell.SpellHandler.UnregisterEngineerCombatBotActions(player, bot);

        IReadOnlyList<RecordingDispatchProxy<IGameSession>.Invocation> messages =
            sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted));
        ServerPetDespawned petDespawned = Assert.IsType<ServerPetDespawned>(messages[0].Arguments[0]);
        Assert.Equal(777u, petDespawned.PetUnitId);
        Assert.DoesNotContain(messages, message => message.Arguments[0] is ServerActionBarSet);
        Assert.DoesNotContain(messages, message => message.Arguments[0] is ServerPetDespawned despawned && despawned.PetUnitId == 0u);

        ServerSpellUpdate spellUpdate = Assert.IsType<ServerSpellUpdate>(messages[1].Arguments[0]);
        Assert.Equal(20884u, spellUpdate.Spell4BaseId);
        Assert.False(spellUpdate.Activated);
        Assert.IsType<ServerActionSetClearCache>(messages[2].Arguments[0]);
        Assert.IsType<ServerActionSet>(messages[3].Arguments[0]);
        Assert.Equal(4, messages.Count);
    }

    [Fact]
    public void HandleEffectSummonPet_WithActiveArtillerybot_DoesNotCreateDuplicateBot()
    {
        AssertEngineerCombatBotSummonRejected(42683u, creatureId => creatureId == 42683u ? 1u : 0u);
    }

    [Fact]
    public void HandleEffectSummonPet_WithActiveRepairbot_DoesNotCreateDuplicateBot()
    {
        AssertEngineerCombatBotSummonRejected(42682u, creatureId => creatureId == 42682u ? 1u : 0u);
    }

    [Fact]
    public void HandleEffectSummonPet_WithTwoEngineerCombatBotsActive_DoesNotCreateAdditionalBot()
    {
        AssertEngineerCombatBotSummonRejected(42684u, creatureId => creatureId is 42682u or 42685u ? 1u : 0u);
    }

    private static void AssertEngineerCombatBotCommandRegistration(
        uint creatureId,
        uint summonSpell4Id,
        uint summonBaseSpell4Id,
        uint tierIndex,
        uint petSwitchSpell4Id,
        uint petSwitchBaseSpell4Id,
        uint actionSpell4Id,
        uint actionBaseSpell4Id,
        uint? actionSetSpell4Id = null)
    {
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<ISpellManager> spellManagerProxy, out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Position), Vector3.Zero);
        playerProxy.SetProperty(nameof(IPlayer.Rotation), Vector3.Zero);
        playerProxy.SetProperty(nameof(IPlayer.Level), 35u);
        playerProxy.SetProperty(nameof(IPlayer.Faction1), Faction.Exile);
        playerProxy.SetProperty(nameof(IPlayer.Faction2), Faction.Exile);

        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);

        spellManagerProxy.SetProperty(nameof(ISpellManager.ActiveActionSet), (byte)0);
        uint originalActionSetSpell4Id = actionSetSpell4Id ?? summonBaseSpell4Id;
        ActionSet actionSet = CreateActionSetWithSpellShortcut(player, originalActionSetSpell4Id, (byte)tierIndex);
        spellManagerProxy.SetMethodReturn(nameof(ISpellManager.GetActionSet), actionSet);

        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetMethodHandler(nameof(IBaseMap.CanEnter), _ => true);
        playerProxy.SetProperty(nameof(IPlayer.Map), map);

        IEntitySummonFactory summonFactory = RecordingDispatchProxy<IEntitySummonFactory>.Create(out _);
        playerProxy.SetProperty(nameof(IPlayer.SummonFactory), summonFactory);

        IWorldEntity pet = RecordingDispatchProxy<IWorldEntity>.Create(out RecordingDispatchProxy<IWorldEntity> petProxy);
        petProxy.SetProperty(nameof(IWorldEntity.Guid), 777u);
        petProxy.SetProperty(nameof(IWorldEntity.CreatureId), creatureId);
        IEntityFactory entityFactory = RecordingDispatchProxy<IEntityFactory>.Create(out RecordingDispatchProxy<IEntityFactory> entityFactoryProxy);
        entityFactoryProxy.SetMethodHandler(nameof(IEntityFactory.CreateWorldEntity), _ => pet);

        List<Spell4Entry> spellEntries =
        [
            new Spell4Entry
            {
                Id                    = summonSpell4Id,
                Spell4BaseIdBaseSpell = summonBaseSpell4Id,
                TierIndex             = tierIndex,
                Spell4IdPetSwitch     = petSwitchSpell4Id
            },
            new Spell4Entry
            {
                Id                    = petSwitchSpell4Id,
                Spell4BaseIdBaseSpell = petSwitchBaseSpell4Id,
                TierIndex             = tierIndex
            }
        ];
        if (actionSpell4Id != petSwitchSpell4Id)
        {
            spellEntries.Add(new Spell4Entry
            {
                Id                    = actionSpell4Id,
                Spell4BaseIdBaseSpell = actionBaseSpell4Id,
                TierIndex             = tierIndex
            });
        }

        IGameTableManager gameTableManager = CreateGameTableManager(
            creatureEntries:
            [
                new Creature2Entry
                {
                    Id               = creatureId,
                    CreationTypeEnum = (uint)EntityType.NonPlayer,
                    MinLevel         = 1u,
                    MaxLevel         = 60u,
                    Description      = "Engineer combat bot"
                }
            ],
            spellEntries: spellEntries.ToArray());
        using IDisposable resolverScope = UseDependencyResolver(gameTableManager, entityFactory: entityFactory);

        ISpell spell = CreateSpell(spellEntries[0], player);
        ISpellTargetEffectInfo info = CreateSummonPetInfo(
            effectId: 97661u,
            spellId: summonSpell4Id,
            creatureId: creatureId,
            petType: 3u,
            out _);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectSummonPet(spell, player, info);

        RecordingDispatchProxy<ISpellManager>.Invocation petAction =
            Assert.Single(spellManagerProxy.GetInvocations(nameof(ISpellManager.SetActivePetActionSpell)));
        Assert.Equal(petSwitchSpell4Id, petAction.Arguments[0]);
        Assert.Equal(actionSpell4Id, petAction.Arguments[1]);
        Assert.Equal(summonSpell4Id, petAction.Arguments[2]);

        IReadOnlyList<RecordingDispatchProxy<IGameSession>.Invocation> messages =
            sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted));
        Assert.DoesNotContain(messages, message => message.Arguments[0] is ServerPetSpawned);

        ServerSpellUpdate spellUpdate = Assert.IsType<ServerSpellUpdate>(messages[0].Arguments[0]);
        Assert.Equal(actionBaseSpell4Id, spellUpdate.Spell4BaseId);
        Assert.Equal((byte)tierIndex, spellUpdate.TierIndex);
        Assert.True(spellUpdate.Activated);

        Assert.IsType<ServerActionSetClearCache>(messages[1].Arguments[0]);
        ServerActionSet projectedActionSet = Assert.IsType<ServerActionSet>(messages[2].Arguments[0]);
        Assert.Equal(0, projectedActionSet.SpecIndex);
        Assert.Equal(1, projectedActionSet.Unlocked);
        Assert.Equal(LimitedActionSetResult.Ok, projectedActionSet.Result);
        Assert.Equal(ActionSet.MaxActionCount, projectedActionSet.Actions.Count);
        ServerActionSet.Action projectedAction = projectedActionSet.Actions[(int)UILocation.LAS3];
        Assert.Equal(ShortcutType.Spell, projectedAction.ShortcutType);
        Assert.Equal(actionSpell4Id, projectedAction.ObjectId);
        Assert.Equal(InventoryLocation.Ability, projectedAction.Location.Location);
        Assert.Equal((uint)UILocation.LAS3, projectedAction.Location.BagIndex);

        Assert.Equal(originalActionSetSpell4Id, actionSet.GetShortcut(UILocation.LAS3).ObjectId);
        Assert.DoesNotContain(messages, message => message.Arguments[0] is ServerQuestSpellShortcut);
        Assert.Equal(3, messages.Count);

        global::NexusForever.Game.Spell.SpellHandler.OnEngineerCombatBotSummoned(player, pet);
        messages = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted));
        ServerPetSpawned commandSurfacePetSpawned = Assert.IsType<ServerPetSpawned>(messages[3].Arguments[0]);
        Assert.Equal(0u, commandSurfacePetSpawned.PetUnitId);
        Assert.Equal(summonSpell4Id, commandSurfacePetSpawned.SummoningSpell4Id);
        Assert.Equal(0b11101u, commandSurfacePetSpawned.ValidStances);
        Assert.Equal(PetStance.Assist, commandSurfacePetSpawned.Stance);

        ServerPetSpawned petSpawned = Assert.IsType<ServerPetSpawned>(messages[4].Arguments[0]);
        Assert.Equal(777u, petSpawned.PetUnitId);
        Assert.Equal(summonSpell4Id, petSpawned.SummoningSpell4Id);
        Assert.Equal(0b11101u, petSpawned.ValidStances);
        Assert.Equal(PetStance.Assist, petSpawned.Stance);

        AssertSpellUpdate(messages, 5, 46724u, 1, true);
        AssertSpellUpdate(messages, 6, 46725u, 1, true);
        AssertSpellUpdate(messages, 7, 25888u, 1, true);

        ServerActionBarSet visiblePetBar = Assert.IsType<ServerActionBarSet>(messages[8].Arguments[0]);
        Assert.Equal(ShortcutSet.PrimaryPetBar, visiblePetBar.ShortcutSet);
        Assert.Equal((ushort)299, visiblePetBar.ActionBarShortcutSetId);
        Assert.Equal(0u, visiblePetBar.AssociatedUnitId);
        Assert.Equal(9, messages.Count);
        Assert.Single(spellManagerProxy.GetInvocations(nameof(ISpellManager.GetActionSet)));
    }

    private static void AssertEngineerCombatBotSummonRejected(uint creatureId, Func<uint, uint> activeCreatureCount)
    {
        IPlayer player = CreatePlayer(out _, out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Position), new Vector3(12f, 3f, -8f));
        playerProxy.SetProperty(nameof(IPlayer.Rotation), new Vector3(0.25f, 0f, 0f));
        playerProxy.SetProperty(nameof(IPlayer.Faction1), Faction.Exile);
        playerProxy.SetProperty(nameof(IPlayer.Faction2), Faction.Exile);

        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetMethodHandler(nameof(IBaseMap.CanEnter), _ => true);
        playerProxy.SetProperty(nameof(IPlayer.Map), map);

        IEntitySummonFactory summonFactory = RecordingDispatchProxy<IEntitySummonFactory>.Create(out RecordingDispatchProxy<IEntitySummonFactory> summonFactoryProxy);
        summonFactoryProxy.SetMethodHandler(nameof(IEntitySummonFactory.GetSummonCreatureCount), args =>
            activeCreatureCount((uint)args[0]));
        playerProxy.SetProperty(nameof(IPlayer.SummonFactory), summonFactory);

        IEntityFactory entityFactory = RecordingDispatchProxy<IEntityFactory>.Create(out RecordingDispatchProxy<IEntityFactory> entityFactoryProxy);

        IGameTableManager gameTableManager = CreateGameTableManagerWithCreature2(
            new Creature2Entry
            {
                Id               = creatureId,
                CreationTypeEnum = (uint)EntityType.NonPlayer,
                Description      = "Engineer combat bot"
            });
        using IDisposable resolverScope = UseDependencyResolver(gameTableManager, entityFactory: entityFactory);

        ISpell spell = CreateSpell(42814u, player);
        ISpellTargetEffectInfo info = CreateSummonPetInfo(
            effectId: 97661u,
            spellId: 42814u,
            creatureId: creatureId,
            petType: 3u,
            out RecordingDispatchProxy<ISpellTargetEffectInfo> infoProxy);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectSummonPet(spell, player, info);

        Assert.Empty(entityFactoryProxy.GetInvocations(nameof(IEntityFactory.CreateWorldEntity)));
        Assert.Empty(mapProxy.GetInvocations(nameof(IBaseMap.EnqueueAdd)));
        Assert.Empty(infoProxy.GetInvocations(nameof(ISpellTargetEffectInfo.AddCreatedEntity)));
    }

    private static IPlayer CreatePlayer(
        out RecordingDispatchProxy<ISpellManager> spellManagerProxy,
        out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        ISpellManager spellManager = RecordingDispatchProxy<ISpellManager>.Create(out spellManagerProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 42u);
        playerProxy.SetProperty(nameof(IPlayer.SpellManager), spellManager);
        return player;
    }

    private static ActionSet CreateActionSetWithSpellShortcut(
        IPlayer player,
        uint spell4BaseId,
        byte tier,
        UILocation location = UILocation.LAS3)
    {
        var actionSet = new ActionSet(0, player);
        actionSet.AddShortcut(location, ShortcutType.SpellbookItem, spell4BaseId, tier);
        return actionSet;
    }

    private static ISpell CreateSpell(uint spell4Id, IUnitEntity caster, uint primaryTargetId = 0u, Position position = null)
    {
        return CreateSpell(new Spell4Entry { Id = spell4Id }, caster, primaryTargetId, position);
    }

    private static ISpell CreateSpell(
        Spell4Entry spell4Entry,
        IUnitEntity caster,
        uint primaryTargetId = 0u,
        Position position = null,
        ISpellInfo parentSpellInfo = null)
    {
        ISpellInfo spellInfo = CreateSpellInfo(spell4Entry);

        ISpellParameters parameters = RecordingDispatchProxy<ISpellParameters>.Create(out RecordingDispatchProxy<ISpellParameters> parametersProxy);
        parametersProxy.SetProperty(nameof(ISpellParameters.SpellInfo), spellInfo);
        parametersProxy.SetProperty(nameof(ISpellParameters.ParentSpellInfo), parentSpellInfo);
        parametersProxy.SetProperty(nameof(ISpellParameters.PrimaryTargetId), primaryTargetId);
        parametersProxy.SetProperty(nameof(ISpellParameters.Position), position);

        ISpell spell = RecordingDispatchProxy<ISpell>.Create(out RecordingDispatchProxy<ISpell> spellProxy);
        spellProxy.SetProperty(nameof(ISpell.Caster), caster);
        spellProxy.SetProperty(nameof(ISpell.CastingId), 7u);
        spellProxy.SetProperty(nameof(ISpell.Parameters), parameters);
        return spell;
    }

    private static ISpellInfo CreateSpellInfo(Spell4Entry spell4Entry)
    {
        ISpellBaseInfo baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out RecordingDispatchProxy<ISpellBaseInfo> baseInfoProxy);
        baseInfoProxy.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry
        {
            Id = spell4Entry.Spell4BaseIdBaseSpell
        });

        ISpellInfo spellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out RecordingDispatchProxy<ISpellInfo> spellInfoProxy);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Entry), spell4Entry);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.BaseInfo), baseInfo);
        return spellInfo;
    }

    private static ISpellTargetEffectInfo CreateModifySpellCooldownInfo(
        uint effectId,
        uint spellId,
        uint mode,
        uint targetSpell4Id,
        uint operation,
        float dataFloat03)
    {
        ISpellTargetEffectInfo info = RecordingDispatchProxy<ISpellTargetEffectInfo>.Create(out RecordingDispatchProxy<ISpellTargetEffectInfo> infoProxy);
        infoProxy.SetProperty(nameof(ISpellTargetEffectInfo.EffectId), effectId);
        infoProxy.SetProperty(nameof(ISpellTargetEffectInfo.Entry), new Spell4EffectsEntry
        {
            Id         = effectId,
            SpellId    = spellId,
            TargetFlags = 1u,
            EffectType  = SpellEffectType.ModifySpellCooldown,
            DataBits00  = mode,
            DataBits01  = targetSpell4Id,
            DataBits02  = operation,
            DataBits03  = BitConverter.SingleToUInt32Bits(dataFloat03)
        });
        return info;
    }

    private static ISpellTargetEffectInfo CreateLevelScaledRewardInfo(
        uint effectId,
        uint spellId,
        SpellEffectType effectType,
        float percentOfLevel,
        uint maxLevel,
        uint mode)
    {
        ISpellTargetEffectInfo info = RecordingDispatchProxy<ISpellTargetEffectInfo>.Create(out RecordingDispatchProxy<ISpellTargetEffectInfo> infoProxy);
        infoProxy.SetProperty(nameof(ISpellTargetEffectInfo.EffectId), effectId);
        infoProxy.SetProperty(nameof(ISpellTargetEffectInfo.Entry), new Spell4EffectsEntry
        {
            Id         = effectId,
            SpellId    = spellId,
            TargetFlags = 1u,
            EffectType  = effectType,
            DataBits00  = BitConverter.SingleToUInt32Bits(percentOfLevel),
            DataBits01  = maxLevel,
            DataBits02  = mode
        });
        return info;
    }

    private static ISpellTargetEffectInfo CreateProgressionEffectInfo(
        uint effectId,
        uint spellId,
        SpellEffectType effectType,
        uint dataBits00,
        uint dataBits01 = 0u,
        uint dataBits09 = 0u)
    {
        ISpellTargetEffectInfo info = RecordingDispatchProxy<ISpellTargetEffectInfo>.Create(out RecordingDispatchProxy<ISpellTargetEffectInfo> infoProxy);
        infoProxy.SetProperty(nameof(ISpellTargetEffectInfo.EffectId), effectId);
        infoProxy.SetProperty(nameof(ISpellTargetEffectInfo.Entry), new Spell4EffectsEntry
        {
            Id          = effectId,
            SpellId     = spellId,
            TargetFlags = 1u,
            EffectType  = effectType,
            DataBits00  = dataBits00,
            DataBits01  = dataBits01,
            DataBits09  = dataBits09
        });
        return info;
    }

    private static ISpellTargetEffectInfo CreateEffectInfo(
        uint effectId,
        uint spellId,
        SpellEffectType effectType,
        uint dataBits00,
        uint dataBits01 = 0u,
        uint dataBits02 = 0u)
    {
        ISpellTargetEffectInfo info = RecordingDispatchProxy<ISpellTargetEffectInfo>.Create(out RecordingDispatchProxy<ISpellTargetEffectInfo> infoProxy);
        infoProxy.SetProperty(nameof(ISpellTargetEffectInfo.EffectId), effectId);
        infoProxy.SetProperty(nameof(ISpellTargetEffectInfo.Entry), new Spell4EffectsEntry
        {
            Id          = effectId,
            SpellId     = spellId,
            TargetFlags = 1u,
            EffectType  = effectType,
            DataBits00  = dataBits00,
            DataBits01  = dataBits01,
            DataBits02  = dataBits02
        });
        return info;
    }

    private static ISpellTargetEffectInfo CreateSpellImmunityInfo(
        uint effectId,
        uint spellId,
        uint mode,
        uint payload)
    {
        ISpellTargetEffectInfo info = RecordingDispatchProxy<ISpellTargetEffectInfo>.Create(out RecordingDispatchProxy<ISpellTargetEffectInfo> infoProxy);
        infoProxy.SetProperty(nameof(ISpellTargetEffectInfo.EffectId), effectId);
        infoProxy.SetProperty(nameof(ISpellTargetEffectInfo.Entry), new Spell4EffectsEntry
        {
            Id         = effectId,
            SpellId    = spellId,
            TargetFlags = 1u,
            EffectType  = SpellEffectType.SpellImmunity,
            DataBits00  = mode,
            DataBits01  = payload
        });
        return info;
    }

    private static ISpellTargetEffectInfo CreateGiveLootTableToPlayerInfo(
        uint effectId,
        uint spellId,
        uint lootGroupId,
        uint rollCount)
    {
        ISpellTargetEffectInfo info = RecordingDispatchProxy<ISpellTargetEffectInfo>.Create(out RecordingDispatchProxy<ISpellTargetEffectInfo> infoProxy);
        infoProxy.SetProperty(nameof(ISpellTargetEffectInfo.EffectId), effectId);
        infoProxy.SetProperty(nameof(ISpellTargetEffectInfo.Entry), new Spell4EffectsEntry
        {
            Id         = effectId,
            SpellId    = spellId,
            TargetFlags = 1u,
            EffectType  = SpellEffectType.GiveLootTableToPlayer,
            DataBits00  = lootGroupId,
            DataBits01  = rollCount
        });
        return info;
    }

    private static ISpellTargetEffectInfo CreatePetCastSpellInfo(
        uint effectId,
        uint spellId,
        uint requiredSummonSpell4Id,
        uint petSpell4Id)
    {
        ISpellTargetEffectInfo info = RecordingDispatchProxy<ISpellTargetEffectInfo>.Create(out RecordingDispatchProxy<ISpellTargetEffectInfo> infoProxy);
        infoProxy.SetProperty(nameof(ISpellTargetEffectInfo.EffectId), effectId);
        infoProxy.SetProperty(nameof(ISpellTargetEffectInfo.Entry), new Spell4EffectsEntry
        {
            Id         = effectId,
            SpellId    = spellId,
            TargetFlags = 1u,
            EffectType  = SpellEffectType.PetCastSpell,
            DataBits00  = requiredSummonSpell4Id,
            DataBits01  = petSpell4Id,
            DataBits03  = 1u,
            DataBits04  = 1u,
            DataBits05  = 1u
        });
        return info;
    }

    private static ISpellTargetEffectInfo CreateSummonPetInfo(
        uint effectId,
        uint spellId,
        uint creatureId,
        uint petType,
        out RecordingDispatchProxy<ISpellTargetEffectInfo> infoProxy)
    {
        ISpellTargetEffectInfo info = RecordingDispatchProxy<ISpellTargetEffectInfo>.Create(out infoProxy);
        infoProxy.SetProperty(nameof(ISpellTargetEffectInfo.EffectId), effectId);
        infoProxy.SetProperty(nameof(ISpellTargetEffectInfo.Entry), new Spell4EffectsEntry
        {
            Id         = effectId,
            SpellId    = spellId,
            TargetFlags = 1u,
            EffectType  = SpellEffectType.SummonPet,
            DataBits00  = creatureId,
            DataBits01  = petType,
            DataBits03  = 1500u,
            DataBits04  = 2215u,
            DataBits06  = 15000u,
            DataBits08  = 9u,
            DataBits09  = 4u
        });
        return info;
    }

    private static ISpellTargetEffectInfo CreateProxyInfo(uint effectId, uint spellId, uint proxySpell4Id)
    {
        ISpellTargetEffectInfo info = RecordingDispatchProxy<ISpellTargetEffectInfo>.Create(out RecordingDispatchProxy<ISpellTargetEffectInfo> infoProxy);
        infoProxy.SetProperty(nameof(ISpellTargetEffectInfo.EffectId), effectId);
        infoProxy.SetProperty(nameof(ISpellTargetEffectInfo.Entry), new Spell4EffectsEntry
        {
            Id         = effectId,
            SpellId    = spellId,
            TargetFlags = 4u,
            EffectType  = SpellEffectType.Proxy,
            DataBits00  = proxySpell4Id
        });
        return info;
    }

    private static ISpellTargetEffectInfo CreateProxyRandomExclusiveInfo(
        uint effectId,
        uint spellId,
        uint zeroWeightSpell4Id,
        uint weightedSpell4Id)
    {
        ISpellTargetEffectInfo info = RecordingDispatchProxy<ISpellTargetEffectInfo>.Create(out RecordingDispatchProxy<ISpellTargetEffectInfo> infoProxy);
        infoProxy.SetProperty(nameof(ISpellTargetEffectInfo.EffectId), effectId);
        infoProxy.SetProperty(nameof(ISpellTargetEffectInfo.Entry), new Spell4EffectsEntry
        {
            Id         = effectId,
            SpellId    = spellId,
            TargetFlags = 1u,
            EffectType  = SpellEffectType.ProxyRandomExclusive,
            DataBits00  = zeroWeightSpell4Id,
            DataBits01  = 0u,
            DataBits02  = weightedSpell4Id,
            DataBits03  = 1u
        });
        return info;
    }

    private static IDisposable UseDependencyResolver(
        IGameTableManager gameTableManager = null,
        IGlobalSpellManager globalSpellManager = null,
        IGlobalLootManager globalLootManager = null,
        IEntityFactory entityFactory = null)
    {
        ISpellEffectDependencyResolver previousResolver =
            global::NexusForever.Game.Spell.SpellHandler.InitialiseDependencyResolver(new TestSpellEffectDependencyResolver(
                gameTableManager,
                globalSpellManager,
                globalLootManager,
                entityFactory));
        return new DependencyResolverScope(previousResolver);
    }

    private sealed class DependencyResolverScope(ISpellEffectDependencyResolver previousResolver) : IDisposable
    {
        public void Dispose()
        {
            global::NexusForever.Game.Spell.SpellHandler.InitialiseDependencyResolver(previousResolver);
        }
    }

    private sealed class TestSpellEffectDependencyResolver(
        IGameTableManager gameTableManager,
        IGlobalSpellManager globalSpellManager,
        IGlobalLootManager globalLootManager,
        IEntityFactory entityFactory) : ISpellEffectDependencyResolver
    {
        public IDamageCalculator CreateDamageCalculator()
        {
            return null;
        }

        public IEntityFactory GetEntityFactory()
        {
            return entityFactory;
        }

        public IForcedMovementGenerator GetForcedMovementGenerator()
        {
            return null;
        }

        public IAssetManager GetAssetManager()
        {
            return null;
        }

        public IGlobalAchievementManager GetGlobalAchievementManager()
        {
            return null;
        }

        public IGlobalResidenceManager GetGlobalResidenceManager()
        {
            return null;
        }

        public IGlobalLootManager GetGlobalLootManager()
        {
            return globalLootManager;
        }

        public IMapLockManager GetMapLockManager()
        {
            return null;
        }

        public IGlobalSpellManager GetGlobalSpellManager()
        {
            return globalSpellManager;
        }

        public IGameTableManager GetGameTableManager()
        {
            return gameTableManager;
        }
    }

    private static IGlobalSpellManager CreateGlobalSpellManager(params Spell4EffectsEntry[] effects)
    {
        IGlobalSpellManager globalSpellManager = RecordingDispatchProxy<IGlobalSpellManager>.Create(out RecordingDispatchProxy<IGlobalSpellManager> proxy);
        proxy.SetMethodHandler(nameof(IGlobalSpellManager.GetSpell4EffectEntries), args =>
        {
            uint spell4Id = (uint)args[0];
            return effects
                .Where(e => e.SpellId == spell4Id)
                .ToList();
        });
        return globalSpellManager;
    }

    private static void AssertSpellUpdate(
        IReadOnlyList<RecordingDispatchProxy<IGameSession>.Invocation> messages,
        int index,
        uint spell4BaseId,
        byte tierIndex,
        bool activated)
    {
        ServerSpellUpdate spellUpdate = Assert.IsType<ServerSpellUpdate>(messages[index].Arguments[0]);
        Assert.Equal(spell4BaseId, spellUpdate.Spell4BaseId);
        Assert.Equal(tierIndex, spellUpdate.TierIndex);
        Assert.Equal(activated, spellUpdate.Activated);
    }

    private static IGameTableManager CreateGameTableManager(params Spell4Entry[] spellEntries)
    {
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> proxy);
        proxy.SetProperty(nameof(IGameTableManager.Spell4), CreateGameTable(spellEntries));
        return gameTableManager;
    }

    private static IGameTableManager CreateGameTableManager(GameTable<XpPerLevelEntry> xpPerLevelEntries)
    {
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> proxy);
        proxy.SetProperty(nameof(IGameTableManager.XpPerLevel), xpPerLevelEntries);
        return gameTableManager;
    }

    private static IGameTableManager CreateGameTableManagerWithCreature2(params Creature2Entry[] creatureEntries)
    {
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> proxy);
        proxy.SetProperty(nameof(IGameTableManager.Creature2), CreateGameTable(creatureEntries));
        return gameTableManager;
    }

    private static IGameTableManager CreateGameTableManager(Creature2Entry[] creatureEntries, Spell4Entry[] spellEntries)
    {
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> proxy);
        proxy.SetProperty(nameof(IGameTableManager.Creature2), CreateGameTable(creatureEntries));
        proxy.SetProperty(nameof(IGameTableManager.Spell4), CreateGameTable(spellEntries));
        return gameTableManager;
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
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

        int[] lookup = Enumerable.Repeat(-1, (int)entries.Max(GetEntryId) + 1).ToArray();
        for (int i = 0; i < entries.Count; i++)
            lookup[(int)GetEntryId(entries[i])] = i;

        return lookup;
    }

    private static uint GetEntryId<T>(T entry)
    {
        return (uint)entry.GetType()
            .GetField("Id", BindingFlags.Instance | BindingFlags.Public)!
            .GetValue(entry)!;
    }

    private static void SetAutoProperty<T>(object target, string propertyName, T value)
    {
        target.GetType()
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(target, value);
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        target.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(target, value);
    }
}
