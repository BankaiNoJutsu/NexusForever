using System.Collections.Immutable;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Combat;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Entity;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Combat.CrowdControl;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.Script;
using NexusForever.Script.Template.Collection;

namespace NexusForever.Game.Tests.Entity;

public class UnitEntityDamageResultTests
{
    [Fact]
    public void TakeDamage_SetsKilledTargetAndHealthOverkill()
    {
        TestUnitEntity victim = new();
        victim.SetHealthForTest(maxHealth: 100u, health: 100u);
        IUnitEntity attacker = CreateAttacker();

        IDamageDescription damage = CreateDamage(adjustedDamage: 125u);

        victim.TakeDamage(attacker, damage);

        Assert.False(victim.IsAlive);
        Assert.True(damage.KilledTarget);
        Assert.Equal(25u, damage.OverkillAmount);
        Assert.Equal(0u, victim.Health);
    }

    [Fact]
    public void TakeDamage_WhenDamageKills_FinalisesMovementCommands()
    {
        TestUnitEntity victim = new(invokeBaseOnDeath: true);
        victim.SetHealthForTest(maxHealth: 100u, health: 100u);
        IUnitEntity attacker = CreateAttacker();

        victim.TakeDamage(attacker, CreateDamage(adjustedDamage: 125u));

        Assert.False(victim.IsAlive);
        Assert.Single(victim.MovementProxy.GetInvocations(nameof(IMovementManager.Finalise)));
    }

    [Fact]
    public void TakeDamage_WhenDamageKills_CancelsBlockingSpellEvenAfterCastPhase()
    {
        TestUnitEntity victim = new(invokeBaseOnDeath: true);
        victim.SetHealthForTest(maxHealth: 100u, health: 100u);
        IUnitEntity attacker = CreateAttacker();
        ISpell spell = RecordingDispatchProxy<ISpell>.Create(out RecordingDispatchProxy<ISpell> spellProxy);
        spellProxy.SetProperty(nameof(ISpell.IsCasting), false);
        spellProxy.SetProperty(nameof(ISpell.BlocksCasting), true);
        AddPendingSpell(victim, spell);

        victim.TakeDamage(attacker, CreateDamage(adjustedDamage: 125u));

        RecordingDispatchProxy<ISpell>.Invocation cancel = Assert.Single(spellProxy.GetInvocations(nameof(ISpell.CancelCast)));
        Assert.Equal(CastResult.CasterCannotBeDead, cancel.Arguments[0]);
    }

    [Fact]
    public void TakeDamage_ClearsOverkillWhenDamageDoesNotKill()
    {
        TestUnitEntity victim = new();
        victim.SetHealthForTest(maxHealth: 100u, health: 100u);
        IUnitEntity attacker = CreateAttacker();

        IDamageDescription damage = CreateDamage(adjustedDamage: 40u);
        damage.OverkillAmount = 99u;

        victim.TakeDamage(attacker, damage);

        Assert.True(victim.IsAlive);
        Assert.False(damage.KilledTarget);
        Assert.Equal(0u, damage.OverkillAmount);
        Assert.Equal(60u, victim.Health);
    }

    [Fact]
    public void TakeDamage_AppliesAdjustedDamageToHealthAndShieldAbsorbToShield()
    {
        TestUnitEntity victim = new();
        victim.SetHealthForTest(maxHealth: 100u, health: 100u);
        victim.SetShieldForTest(maxShield: 100u, shield: 100u, shieldRegenPct: 0f, shieldRebootTimeMs: 0f, shieldTickTimeMs: 0f);
        IUnitEntity attacker = CreateAttacker();

        victim.TakeDamage(attacker, CreateDamage(adjustedDamage: 41u, shieldAbsorbAmount: 40u));

        Assert.Equal(59u, victim.Health);
        Assert.Equal(60u, victim.Shield);
    }

    [Fact]
    public void TakeDamage_ShieldAbsorbDelaysShieldRegenUntilRebootExpires()
    {
        TestUnitEntity victim = new();
        victim.SetHealthForTest(maxHealth: 100u, health: 100u);
        victim.SetShieldForTest(maxShield: 100u, shield: 100u, shieldRegenPct: 0.25f, shieldRebootTimeMs: 2500f, shieldTickTimeMs: 500f);
        IUnitEntity attacker = CreateAttacker();

        IDamageDescription damage = CreateDamage(adjustedDamage: 0u, shieldAbsorbAmount: 10u);

        victim.TakeDamage(attacker, damage);
        Assert.Equal(90u, victim.Shield);

        for (int i = 0; i < 10; i++)
            victim.Update(0.25d);

        Assert.Equal(90u, victim.Shield);

        victim.Update(0.25d);
        victim.Update(0.25d);

        Assert.Equal(100u, victim.Shield);
    }

    [Fact]
    public void TakeDamage_HealthDamageAlsoDelaysShieldRegen()
    {
        TestUnitEntity victim = new();
        victim.SetHealthForTest(maxHealth: 100u, health: 100u);
        victim.SetShieldForTest(maxShield: 100u, shield: 0u, shieldRegenPct: 0.25f, shieldRebootTimeMs: 2500f, shieldTickTimeMs: 500f);
        IUnitEntity attacker = CreateAttacker();

        victim.TakeDamage(attacker, CreateDamage(adjustedDamage: 10u));

        for (int i = 0; i < 10; i++)
            victim.Update(0.25d);

        Assert.Equal(0u, victim.Shield);

        victim.Update(0.25d);
        victim.Update(0.25d);

        Assert.Equal(12u, victim.Shield);
    }

    [Fact]
    public void TryModifyVital_WithActiveOverdrive_DoesNotChangeKinetic()
    {
        TestUnitEntity unit = new();
        unit.SetResource1ForTest(maxResource: 1000f, resource: 500f);
        unit.AddSpellModifierProperty(
            new SpellPropertyModifier(Property.DamageDealtMultiplierPhysical, 3u, 1.2f, 0f, 0f),
            effectId: 127652u,
            spell4Id: 42908u,
            spell4EffectId: 127652u,
            castingId: 99u);

        Assert.True(unit.TryModifyVital(Vital.Resource1, -125f, out float appliedAmount));
        Assert.Equal(0f, appliedAmount);
        Assert.True(unit.TryGetVitalValue(Vital.Resource1, out float value));
        Assert.Equal(500f, value);

        Assert.True(unit.TryModifyVital(Vital.Resource1, 125f, out appliedAmount));
        Assert.Equal(0f, appliedAmount);
        Assert.True(unit.TryGetVitalValue(Vital.Resource1, out value));
        Assert.Equal(500f, value);
    }

    [Fact]
    public void Update_WithOnslaughtProxyScale_RestoresScaleAfterOverdriveDuration()
    {
        IMovementManager movementManager = CreateScaleMovementManager(out Func<float> getScale);
        TestUnitEntity unit = new(movementManager);
        unit.SetIdentityForTest(guid: 1001u, position: Vector3.Zero);
        unit.SetHealthForTest(maxHealth: 1000u, health: 1000u);

        IGameTableManager gameTableManager = CreateOverdriveSpellGameTableManager();
        var globalSpellManager = new GlobalSpellManager(gameTableManager);
        globalSpellManager.Initialise();
        unit.InitialiseSpellRuntimeForTest(globalSpellManager, gameTableManager, CreateScriptManager());

        Assert.Equal(CastResult.Ok, unit.TryCastSpell(46867u, new SpellParameters()));
        Assert.Equal(1f, getScale(), precision: 3);

        unit.Update(0.002d);

        Assert.Equal(1.3f, getScale(), precision: 3);

        unit.Update(7.99d);

        Assert.Equal(1.3f, getScale(), precision: 3);

        unit.Update(0.02d);

        Assert.Equal(1f, getScale(), precision: 3);
    }

    [Fact]
    public void Update_WithOverlappingOnslaughtProxyScale_RestoresOriginalScaleAfterAllDurations()
    {
        IMovementManager movementManager = CreateScaleMovementManager(out Func<float> getScale);
        TestUnitEntity unit = new(movementManager);
        unit.SetIdentityForTest(guid: 1001u, position: Vector3.Zero);
        unit.SetHealthForTest(maxHealth: 1000u, health: 1000u);

        IGameTableManager gameTableManager = CreateOverdriveSpellGameTableManager();
        var globalSpellManager = new GlobalSpellManager(gameTableManager);
        globalSpellManager.Initialise();
        unit.InitialiseSpellRuntimeForTest(globalSpellManager, gameTableManager, CreateScriptManager());

        Assert.Equal(CastResult.Ok, unit.TryCastSpell(46867u, new SpellParameters()));
        unit.Update(0.002d);
        Assert.Equal(1.3f, getScale(), precision: 3);

        unit.Update(1d);

        Assert.Equal(CastResult.Ok, unit.TryCastSpell(46867u, new SpellParameters()));
        unit.Update(0.002d);
        Assert.Equal(1.3f, getScale(), precision: 3);

        unit.Update(7.01d);
        Assert.Equal(1.3f, getScale(), precision: 3);

        unit.Update(1.01d);

        Assert.Equal(1f, getScale(), precision: 3);
    }

    [Fact]
    public void Update_DoesNotRegenerateHealthWhileInCombat()
    {
        TestUnitEntity unit = new();
        unit.SetHealthForTest(maxHealth: 1000u, health: 500u);
        unit.SetInCombatForTest(true);

        unit.Update(0.25d);

        Assert.Equal(500u, unit.Health);

        unit.SetInCombatForTest(false);

        unit.Update(0.25d);

        Assert.Equal(505u, unit.Health);
    }

    [Fact]
    public void Update_RegeneratesDashEnergyToMax()
    {
        TestUnitEntity unit = new();
        unit.SetHealthForTest(maxHealth: 1000u, health: 1000u);
        unit.SetDashEnergyForTest(maxDashEnergy: 200f, dashEnergy: 100f, regenMultiplier: 0.045f);

        unit.Update(0.25d);

        Assert.True(unit.TryGetVitalValue(Vital.Resource7, out float value));
        Assert.Equal(102.25f, value, 3);

        for (int i = 0; i < 100; i++)
            unit.Update(0.25d);

        Assert.True(unit.TryGetVitalValue(Vital.Resource7, out value));
        Assert.Equal(200f, value, 3);
    }

    [Theory]
    [InlineData(100u, 125u, true, 25u)]
    [InlineData(100u, 100u, true, 0u)]
    [InlineData(100u, 40u, false, 0u)]
    public void CalculateHealthOverkill_UsesKilledTargetAndPreDamageHealth(uint healthBefore, uint adjustedDamage, bool killedTarget, uint expected)
    {
        Assert.Equal(expected, UnitEntity.CalculateHealthOverkill(healthBefore, adjustedDamage, killedTarget));
    }

    [Fact]
    public void TakeDamage_PlayerAttacker_IncrementsPublicEventDamageHitAndKillStats()
    {
        TestUnitEntity victim = new();
        victim.SetHealthForTest(maxHealth: 100u, health: 100u);
        IPlayer attacker = CreatePublicEventCombatPlayer(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);

        victim.TakeDamage(attacker, CreateDamage(adjustedDamage: 125u));

        IReadOnlyList<RecordingDispatchProxy<IPublicEventManager>.Invocation> increments =
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.IncrementStat));
        Assert.Contains(increments, increment =>
            ReferenceEquals(attacker, increment.Arguments[0]) &&
            (PublicEventStat)increment.Arguments[1] == PublicEventStat.Damage &&
            (uint)increment.Arguments[2] == 125u);
        Assert.Contains(increments, increment =>
            ReferenceEquals(attacker, increment.Arguments[0]) &&
            (PublicEventStat)increment.Arguments[1] == PublicEventStat.Hits &&
            (uint)increment.Arguments[2] == 1u);
        Assert.Contains(increments, increment =>
            ReferenceEquals(attacker, increment.Arguments[0]) &&
            (PublicEventStat)increment.Arguments[1] == PublicEventStat.Kills &&
            (uint)increment.Arguments[2] == 1u);
    }

    [Fact]
    public void ModifyHealth_PlayerHealer_IncrementsPublicEventHealingStat()
    {
        TestUnitEntity target = new();
        target.SetHealthForTest(maxHealth: 100u, health: 25u);
        IPlayer healer = CreatePublicEventCombatPlayer(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);

        target.ModifyHealth(50u, DamageType.Heal, healer);

        RecordingDispatchProxy<IPublicEventManager>.Invocation increment = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.IncrementStat)));
        Assert.Same(healer, increment.Arguments[0]);
        Assert.Equal(PublicEventStat.Healed, increment.Arguments[1]);
        Assert.Equal(50u, increment.Arguments[2]);
    }

    [Theory]
    [InlineData(75621u)]
    [InlineData(75622u)]
    public void RewardKiller_ColdbloodKrovakTargetGroup_UpdatesPublicEventKillTargetGroup(uint creatureId)
    {
        TestUnitEntity unit = new();
        unit.InitialiseRewardRuntimeForTest(
            creatureId,
            CreateMapWithPublicEventManager(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy),
            CreateAssetManagerForTargetGroup(creatureId, 14470u, out RecordingDispatchProxy<IAssetManager> assetManagerProxy),
            CreateGameTableManager());
        IPlayer player = CreateRewardPlayer();

        unit.RewardKillerForTest(player);

        RecordingDispatchProxy<IAssetManager>.Invocation lookup = Assert.Single(
            assetManagerProxy.GetInvocations(nameof(IAssetManager.GetTargetGroupsForCreatureId)));
        Assert.Equal(creatureId, lookup.Arguments[0]);

        IReadOnlyList<RecordingDispatchProxy<IPublicEventManager>.Invocation> updates =
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective));
        Assert.Contains(updates, update =>
            update.Arguments.Length == 4 &&
            ReferenceEquals(player, update.Arguments[0]) &&
            (PublicEventObjectiveType)update.Arguments[1] == PublicEventObjectiveType.KillTargetGroup &&
            (uint)update.Arguments[2] == 14470u &&
            (int)update.Arguments[3] == 1);
        Assert.Contains(updates, update =>
            update.Arguments.Length == 4 &&
            ReferenceEquals(player, update.Arguments[0]) &&
            (PublicEventObjectiveType)update.Arguments[1] == PublicEventObjectiveType.KillClusterTargetGroup &&
            (uint)update.Arguments[2] == 14470u &&
            (int)update.Arguments[3] == 1);
    }

    [Theory]
    [InlineData(61775u)]
    [InlineData(62218u)]
    [InlineData(62242u)]
    public void RewardKiller_UltimateProtogamesCubigHolderTargetGroup_UpdatesExactKillCounter(uint creatureId)
    {
        TestUnitEntity unit = new();
        unit.InitialiseRewardRuntimeForTest(
            creatureId,
            CreateMapWithPublicEventManager(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy),
            CreateAssetManagerForTargetGroup(creatureId, 12871u, out RecordingDispatchProxy<IAssetManager> assetManagerProxy),
            CreateGameTableManager());
        IPlayer player = CreateRewardPlayer();

        unit.RewardKillerForTest(player);

        RecordingDispatchProxy<IAssetManager>.Invocation lookup = Assert.Single(
            assetManagerProxy.GetInvocations(nameof(IAssetManager.GetTargetGroupsForCreatureId)));
        Assert.Equal(creatureId, lookup.Arguments[0]);

        IReadOnlyList<RecordingDispatchProxy<IPublicEventManager>.Invocation> updates =
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective));
        Assert.Contains(updates, update =>
            update.Arguments.Length == 4 &&
            ReferenceEquals(player, update.Arguments[0]) &&
            (PublicEventObjectiveType)update.Arguments[1] == PublicEventObjectiveType.KillTargetGroup &&
            (uint)update.Arguments[2] == 12871u &&
            (int)update.Arguments[3] == 1);
    }

    [Theory]
    [InlineData(62451u)]
    [InlineData(62472u)]
    [InlineData(63319u)]
    public void RewardKiller_UltimateProtogamesElementalHolderNestedTargetGroup_UpdatesExactKillCounter(uint creatureId)
    {
        TestUnitEntity unit = new();
        unit.InitialiseRewardRuntimeForTest(
            creatureId,
            CreateMapWithPublicEventManager(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy),
            CreateAssetManagerForTargetGroup(creatureId, 12876u, out RecordingDispatchProxy<IAssetManager> assetManagerProxy),
            CreateGameTableManager());
        IPlayer player = CreateRewardPlayer();

        unit.RewardKillerForTest(player);

        RecordingDispatchProxy<IAssetManager>.Invocation lookup = Assert.Single(
            assetManagerProxy.GetInvocations(nameof(IAssetManager.GetTargetGroupsForCreatureId)));
        Assert.Equal(creatureId, lookup.Arguments[0]);

        IReadOnlyList<RecordingDispatchProxy<IPublicEventManager>.Invocation> updates =
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective));
        Assert.Contains(updates, update =>
            update.Arguments.Length == 4 &&
            ReferenceEquals(player, update.Arguments[0]) &&
            (PublicEventObjectiveType)update.Arguments[1] == PublicEventObjectiveType.KillTargetGroup &&
            (uint)update.Arguments[2] == 12876u &&
            (int)update.Arguments[3] == 1);
    }

    [Theory]
    [InlineData(17160u)]
    [InlineData(33405u)]
    public void RewardKiller_StormtalonThundercallPellTargetGroup_UpdatesPublicEventKillTargetGroup(uint creatureId)
    {
        TestUnitEntity unit = new();
        unit.InitialiseRewardRuntimeForTest(
            creatureId,
            CreateMapWithPublicEventManager(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy),
            CreateAssetManagerForTargetGroup(creatureId, 11121u, out RecordingDispatchProxy<IAssetManager> assetManagerProxy),
            CreateGameTableManager());
        IPlayer player = CreateRewardPlayer();

        unit.RewardKillerForTest(player);

        RecordingDispatchProxy<IAssetManager>.Invocation lookup = Assert.Single(
            assetManagerProxy.GetInvocations(nameof(IAssetManager.GetTargetGroupsForCreatureId)));
        Assert.Equal(creatureId, lookup.Arguments[0]);

        IReadOnlyList<RecordingDispatchProxy<IPublicEventManager>.Invocation> updates =
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective));
        Assert.Contains(updates, update =>
            update.Arguments.Length == 4 &&
            ReferenceEquals(player, update.Arguments[0]) &&
            (PublicEventObjectiveType)update.Arguments[1] == PublicEventObjectiveType.KillTargetGroup &&
            (uint)update.Arguments[2] == 11121u &&
            (int)update.Arguments[3] == 1);
        Assert.Contains(updates, update =>
            update.Arguments.Length == 4 &&
            ReferenceEquals(player, update.Arguments[0]) &&
            (PublicEventObjectiveType)update.Arguments[1] == PublicEventObjectiveType.KillClusterTargetGroup &&
            (uint)update.Arguments[2] == 11121u &&
            (int)update.Arguments[3] == 1);
    }

    [Theory]
    [InlineData(37751u)]
    [InlineData(24578u)]
    [InlineData(24579u)]
    [InlineData(24580u)]
    [InlineData(24581u)]
    [InlineData(24618u)]
    [InlineData(37752u)]
    [InlineData(24913u)]
    [InlineData(24914u)]
    [InlineData(24916u)]
    [InlineData(24919u)]
    [InlineData(24920u)]
    [InlineData(24512u)]
    [InlineData(24510u)]
    [InlineData(24514u)]
    [InlineData(25561u)]
    [InlineData(24910u)]
    [InlineData(24908u)]
    [InlineData(24911u)]
    [InlineData(25562u)]
    [InlineData(24486u)]
    [InlineData(24894u)]
    [InlineData(24490u)]
    [InlineData(24896u)]
    public void RewardKiller_SkullcanoRedmoonMarauderTargetGroup_UpdatesPublicEventKillTargetGroup(uint creatureId)
    {
        TestUnitEntity unit = new();
        unit.InitialiseRewardRuntimeForTest(
            creatureId,
            CreateMapWithPublicEventManager(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy),
            CreateAssetManagerForTargetGroup(creatureId, 3947u, out RecordingDispatchProxy<IAssetManager> assetManagerProxy),
            CreateGameTableManager());
        IPlayer player = CreateRewardPlayer();

        unit.RewardKillerForTest(player);

        RecordingDispatchProxy<IAssetManager>.Invocation lookup = Assert.Single(
            assetManagerProxy.GetInvocations(nameof(IAssetManager.GetTargetGroupsForCreatureId)));
        Assert.Equal(creatureId, lookup.Arguments[0]);

        IReadOnlyList<RecordingDispatchProxy<IPublicEventManager>.Invocation> updates =
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective));
        Assert.Contains(updates, update =>
            update.Arguments.Length == 4 &&
            ReferenceEquals(player, update.Arguments[0]) &&
            (PublicEventObjectiveType)update.Arguments[1] == PublicEventObjectiveType.KillTargetGroup &&
            (uint)update.Arguments[2] == 3947u &&
            (int)update.Arguments[3] == 1);
        Assert.Contains(updates, update =>
            update.Arguments.Length == 4 &&
            ReferenceEquals(player, update.Arguments[0]) &&
            (PublicEventObjectiveType)update.Arguments[1] == PublicEventObjectiveType.KillClusterTargetGroup &&
            (uint)update.Arguments[2] == 3947u &&
            (int)update.Arguments[3] == 1);
    }

    [Theory]
    [InlineData(71014u, 14056u)]
    [InlineData(62542u, 12671u)]
    public void RewardKiller_PublicEventTargetGroup_UpdatesPublicEventExterminate(uint creatureId, uint targetGroupId)
    {
        TestUnitEntity unit = new();
        unit.InitialiseRewardRuntimeForTest(
            creatureId,
            CreateMapWithPublicEventManager(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy),
            CreateAssetManagerForTargetGroup(creatureId, targetGroupId, out RecordingDispatchProxy<IAssetManager> assetManagerProxy),
            CreateGameTableManager());
        IPlayer player = CreateRewardPlayer();

        unit.RewardKillerForTest(player);

        RecordingDispatchProxy<IAssetManager>.Invocation lookup = Assert.Single(
            assetManagerProxy.GetInvocations(nameof(IAssetManager.GetTargetGroupsForCreatureId)));
        Assert.Equal(creatureId, lookup.Arguments[0]);

        IReadOnlyList<RecordingDispatchProxy<IPublicEventManager>.Invocation> updates =
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective));
        Assert.Contains(updates, update =>
            update.Arguments.Length == 4 &&
            ReferenceEquals(player, update.Arguments[0]) &&
            (PublicEventObjectiveType)update.Arguments[1] == PublicEventObjectiveType.Exterminate &&
            (uint)update.Arguments[2] == targetGroupId &&
            (int)update.Arguments[3] == 1);
    }

    [Theory]
    [InlineData(PublicEventObjectiveType.KillEventUnit)]
    [InlineData(PublicEventObjectiveType.KillEventObjectiveUnit)]
    [InlineData(PublicEventObjectiveType.KillClusterEventUnit)]
    [InlineData(PublicEventObjectiveType.KillClusterEventObjectiveUnit)]
    public void RewardKiller_PublicEventOwnedTargetGroupUnit_UpdatesEventUnitObjectives(PublicEventObjectiveType objectiveType)
    {
        const uint creatureId = 67940u;
        const uint publicEventId = 696u;
        const uint targetGroupId = 12262u;

        TestUnitEntity unit = new();
        unit.InitialiseRewardRuntimeForTest(
            creatureId,
            CreateMapWithPublicEvent(publicEventId, out RecordingDispatchProxy<IPublicEvent> publicEventProxy),
            CreateAssetManagerForTargetGroup(creatureId, targetGroupId, out _),
            CreateGameTableManager());
        unit.SetPublicEventForTest(publicEventId);
        IPlayer player = CreateRewardPlayer();

        unit.RewardKillerForTest(player);

        IReadOnlyList<RecordingDispatchProxy<IPublicEvent>.Invocation> updates =
            publicEventProxy.GetInvocations(nameof(IPublicEvent.UpdateObjective));
        Assert.Contains(updates, update =>
            update.Arguments.Length == 4 &&
            ReferenceEquals(player, update.Arguments[0]) &&
            (PublicEventObjectiveType)update.Arguments[1] == objectiveType &&
            (uint)update.Arguments[2] == 0u &&
            (int)update.Arguments[3] == 1);
        Assert.Contains(updates, update =>
            update.Arguments.Length == 4 &&
            ReferenceEquals(player, update.Arguments[0]) &&
            (PublicEventObjectiveType)update.Arguments[1] == objectiveType &&
            (uint)update.Arguments[2] == targetGroupId &&
            (int)update.Arguments[3] == 1);
    }

    [Theory]
    [InlineData(CCState.Interrupt)]
    [InlineData(CCState.Knockdown)]
    public void AddCCState_WhenStateInterruptsActiveCasting_CancelsActiveCastingSpells(CCState state)
    {
        TestUnitEntity unit = new();
        ISpell spell = RecordingDispatchProxy<ISpell>.Create(out RecordingDispatchProxy<ISpell> spellProxy);
        spellProxy.SetProperty(nameof(ISpell.IsCasting), true);
        spellProxy.SetProperty(nameof(ISpell.BlocksCasting), true);
        AddPendingSpell(unit, spell);

        unit.AddCCState(state, effectId: 10u, spell4Id: 20u, castingId: 30u);

        RecordingDispatchProxy<ISpell>.Invocation cancel = Assert.Single(spellProxy.GetInvocations(nameof(ISpell.CancelCast)));
        Assert.Equal(CastResult.SpellInterrupted, cancel.Arguments[0]);
    }

    [Fact]
    public void AddCCState_Knockdown_CancelsAwaitingImpactSpells()
    {
        TestUnitEntity unit = new();
        ISpell spell = RecordingDispatchProxy<ISpell>.Create(out RecordingDispatchProxy<ISpell> spellProxy);
        spellProxy.SetProperty(nameof(ISpell.IsCasting), false);
        spellProxy.SetProperty(nameof(ISpell.BlocksCasting), true);
        AddPendingSpell(unit, spell);

        unit.AddCCState(CCState.Knockdown, effectId: 10u, spell4Id: 20u, castingId: 30u);

        RecordingDispatchProxy<ISpell>.Invocation cancel = Assert.Single(spellProxy.GetInvocations(nameof(ISpell.CancelCast)));
        Assert.Equal(CastResult.SpellInterrupted, cancel.Arguments[0]);
    }

    [Fact]
    public void CancelSpellsOnMove_WhenMovementInterruptedSpellBlocksButIsExecuting_CancelsSpell()
    {
        TestUnitEntity unit = new();
        ISpell spell = RecordingDispatchProxy<ISpell>.Create(out RecordingDispatchProxy<ISpell> spellProxy);
        spellProxy.SetProperty(nameof(ISpell.IsCasting), false);
        spellProxy.SetProperty(nameof(ISpell.BlocksCasting), true);
        spellProxy.SetMethodReturn(nameof(ISpell.IsMovingInterrupted), true);
        AddPendingSpell(unit, spell);

        unit.CancelSpellsOnMove();

        RecordingDispatchProxy<ISpell>.Invocation cancel = Assert.Single(spellProxy.GetInvocations(nameof(ISpell.CancelCast)));
        Assert.Equal(CastResult.CasterMovement, cancel.Arguments[0]);
    }

    [Fact]
    public void CheckActiveCastSlot_WhenActiveCastPending_ReturnsAlreadyCasting()
    {
        TestUnitEntity unit = new();
        ISpell spell = RecordingDispatchProxy<ISpell>.Create(out RecordingDispatchProxy<ISpell> spellProxy);
        spellProxy.SetProperty(nameof(ISpell.BlocksCasting), true);
        AddPendingSpell(unit, spell);

        CastResult result = unit.CheckActiveCastSlot(new SpellParameters());

        Assert.Equal(CastResult.SpellAlreadyCasting, result);
    }

    [Fact]
    public void CheckActiveCastSlot_WhenNestedSpellCastsDuringActiveCast_AllowsCast()
    {
        TestUnitEntity unit = new();
        ISpell spell = RecordingDispatchProxy<ISpell>.Create(out RecordingDispatchProxy<ISpell> spellProxy);
        spellProxy.SetProperty(nameof(ISpell.BlocksCasting), true);
        AddPendingSpell(unit, spell);

        ISpellInfo parentSpellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out _);
        CastResult result = unit.CheckActiveCastSlot(new SpellParameters
        {
            ParentSpellInfo = parentSpellInfo
        });

        Assert.Equal(CastResult.Ok, result);
    }

    [Fact]
    public void CheckActiveCastSlot_WhenPendingSpellDoesNotBlockCasting_AllowsCast()
    {
        TestUnitEntity unit = new();
        ISpell spell = RecordingDispatchProxy<ISpell>.Create(out RecordingDispatchProxy<ISpell> spellProxy);
        spellProxy.SetProperty(nameof(ISpell.BlocksCasting), false);
        AddPendingSpell(unit, spell);

        CastResult result = unit.CheckActiveCastSlot(new SpellParameters());

        Assert.Equal(CastResult.Ok, result);
    }

    private static IDamageDescription CreateDamage(uint adjustedDamage, uint shieldAbsorbAmount = 0u)
    {
        return new SpellTargetInfo.SpellTargetEffectInfo.DamageDescription
        {
            DamageType         = DamageType.Physical,
            RawDamage          = adjustedDamage + shieldAbsorbAmount,
            RawScaledDamage    = adjustedDamage + shieldAbsorbAmount,
            AdjustedDamage     = adjustedDamage,
            ShieldAbsorbAmount = shieldAbsorbAmount,
            ThreatMultiplier   = 0f,
            CombatResult       = CombatResult.Hit
        };
    }

    private static IUnitEntity CreateAttacker()
    {
        IUnitEntity attacker = RecordingDispatchProxy<IUnitEntity>.Create(out var attackerProxy);
        IThreatManager threatManager = RecordingDispatchProxy<IThreatManager>.Create(out _);
        attackerProxy.SetProperty(nameof(IGridEntity.Guid), 42u);
        attackerProxy.SetProperty(nameof(IUnitEntity.IsAlive), true);
        attackerProxy.SetProperty(nameof(IUnitEntity.ThreatManager), threatManager);
        return attacker;
    }

    private static IBaseMap CreateMapWithPublicEventManager(
        out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy)
    {
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out publicEventManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);

        return map;
    }

    private static IBaseMap CreateMapWithPublicEvent(
        uint publicEventId,
        out RecordingDispatchProxy<IPublicEvent> publicEventProxy)
    {
        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out publicEventProxy);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(
            out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);
        publicEventManagerProxy.SetMethodHandler(nameof(IPublicEventManager.GetEvent), args =>
            (uint)args[0] == publicEventId ? publicEvent : null);

        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);

        return map;
    }

    private static IAssetManager CreateAssetManagerForTargetGroup(
        uint expectedCreatureId,
        uint targetGroupId,
        out RecordingDispatchProxy<IAssetManager> assetManagerProxy)
    {
        IAssetManager assetManager = RecordingDispatchProxy<IAssetManager>.Create(out assetManagerProxy);
        assetManagerProxy.SetMethodHandler(nameof(IAssetManager.GetTargetGroupsForCreatureId), args =>
            (uint)args[0] == expectedCreatureId ? ImmutableList.Create(targetGroupId) : null);

        return assetManager;
    }

    private static IGameTableManager CreateGameTableManager()
    {
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(
            out RecordingDispatchProxy<IGameTableManager> gameTableManagerProxy);
        gameTableManagerProxy.SetProperty(
            nameof(IGameTableManager.Creature2Difficulty),
            CreateGameTable<Creature2DifficultyEntry>());

        return gameTableManager;
    }

    private static IGameTableManager CreateOverdriveSpellGameTableManager()
    {
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(
            out RecordingDispatchProxy<IGameTableManager> proxy);
        proxy.SetProperty(
            nameof(IGameTableManager.Creature2Difficulty),
            CreateGameTable<Creature2DifficultyEntry>());
        proxy.SetProperty(nameof(IGameTableManager.Spell4Base), CreateGameTable(
            new Spell4BaseEntry { Id = 46867u },
            new Spell4BaseEntry { Id = 42908u }));
        proxy.SetProperty(nameof(IGameTableManager.Spell4), CreateGameTable(
            new Spell4Entry
            {
                Id                    = 46867u,
                Spell4BaseIdBaseSpell = 46867u,
                TierIndex             = 1u
            },
            new Spell4Entry
            {
                Id                    = 42908u,
                Spell4BaseIdBaseSpell = 42908u,
                TierIndex             = 1u
            }));
        proxy.SetProperty(nameof(IGameTableManager.Spell4Effects), CreateGameTable(
            new Spell4EffectsEntry
            {
                Id         = 127637u,
                SpellId    = 46867u,
                TargetFlags = (uint)SpellEffectTargetFlags.Caster,
                EffectType  = SpellEffectType.Proxy,
                DelayTime   = 1u,
                DataBits00  = 42908u
            },
            new Spell4EffectsEntry
            {
                Id           = 109482u,
                SpellId      = 42908u,
                TargetFlags   = (uint)SpellEffectTargetFlags.Caster,
                EffectType    = SpellEffectType.Scale,
                DurationTime  = 8000u,
                DataBits00    = BitConverter.SingleToUInt32Bits(1.3f),
                DataBits01    = 500u,
                DataBits02    = 1000u
            }));
        proxy.SetProperty(nameof(IGameTableManager.Spell4Telegraph), CreateGameTable<Spell4TelegraphEntry>());
        proxy.SetProperty(nameof(IGameTableManager.TelegraphDamage), CreateGameTable<TelegraphDamageEntry>());
        proxy.SetProperty(nameof(IGameTableManager.Spell4Thresholds), CreateGameTable<Spell4ThresholdsEntry>());
        return gameTableManager;
    }

    private static IScriptManager CreateScriptManager()
    {
        IScriptCollection scriptCollection = RecordingDispatchProxy<IScriptCollection>.Create(out _);
        IScriptManager scriptManager = RecordingDispatchProxy<IScriptManager>.Create(out RecordingDispatchProxy<IScriptManager> scriptManagerProxy);
        scriptManagerProxy.SetMethodReturn(nameof(IScriptManager.InitialiseOwnedScripts), scriptCollection);
        return scriptManager;
    }

    private static IMovementManager CreateScaleMovementManager(out Func<float> getScale)
    {
        float currentScale = 1f;
        IMovementManager movementManager = RecordingDispatchProxy<IMovementManager>.Create(out RecordingDispatchProxy<IMovementManager> proxy);
        proxy.SetMethodHandler(nameof(IMovementManager.GetScale), _ => currentScale);
        proxy.SetMethodHandler(nameof(IMovementManager.SetScale), args =>
        {
            currentScale = (float)args[0];
            return null;
        });
        proxy.SetMethodHandler(nameof(IMovementManager.SetScaleKeys), args =>
        {
            currentScale = ((List<float>)args[1])[^1];
            return null;
        });

        getScale = () => currentScale;
        return movementManager;
    }

    private static IPlayer CreateRewardPlayer()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), RecordingDispatchProxy<IQuestManager>.Create(out _));
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), RecordingDispatchProxy<ICharacterAchievementManager>.Create(out _));
        playerProxy.SetProperty(nameof(IPlayer.PathManager), RecordingDispatchProxy<IPathManager>.Create(out _));
        playerProxy.SetProperty(nameof(IPlayer.XpManager), RecordingDispatchProxy<IXpManager>.Create(out _));

        return player;
    }

    private static IPlayer CreatePublicEventCombatPlayer(
        out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IGridEntity.Guid), 42u);
        playerProxy.SetProperty(nameof(IUnitEntity.IsAlive), true);
        playerProxy.SetProperty(nameof(IPlayer.Map), CreateMapWithPublicEventManager(out publicEventManagerProxy));
        playerProxy.SetProperty(nameof(IUnitEntity.ThreatManager), RecordingDispatchProxy<IThreatManager>.Create(out _));
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), RecordingDispatchProxy<ICharacterAchievementManager>.Create(out _));

        return player;
    }

    private static void AddPendingSpell(UnitEntity unit, ISpell spell)
    {
        var pendingSpells = (List<ISpell>)typeof(UnitEntity)
            .GetField("pendingSpells", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(unit)!;

        pendingSpells.Add(spell);
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
        return (uint)typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public)[0].GetValue(entry);
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        for (Type type = instance.GetType(); type != null; type = type.BaseType)
        {
            PropertyInfo property = type.GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            MethodInfo setter = property?.GetSetMethod(true);
            if (setter == null)
                continue;

            setter.Invoke(instance, [value]);
            return;
        }

        throw new MissingMemberException(instance.GetType().FullName, propertyName);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(instance, value);
    }

    private sealed class TestUnitEntity : UnitEntity
    {
        public override EntityType Type => EntityType.Simple;

        public override uint Health { get; protected set; }

        public RecordingDispatchProxy<IMovementManager> MovementProxy { get; }

        private readonly bool invokeBaseOnDeath;

        public TestUnitEntity(bool invokeBaseOnDeath = false)
            : this(invokeBaseOnDeath, CreateMovementManager(out RecordingDispatchProxy<IMovementManager> movementProxy), movementProxy)
        {
        }

        public TestUnitEntity(IMovementManager movementManager)
            : this(false, movementManager, null)
        {
        }

        private TestUnitEntity(
            bool invokeBaseOnDeath,
            IMovementManager movementManager,
            RecordingDispatchProxy<IMovementManager> movementProxy)
            : base(movementManager)
        {
            this.invokeBaseOnDeath = invokeBaseOnDeath;
            MovementProxy = movementProxy;
        }

        private static IMovementManager CreateMovementManager(out RecordingDispatchProxy<IMovementManager> movementProxy)
        {
            return RecordingDispatchProxy<IMovementManager>.Create(out movementProxy);
        }

        public void SetIdentityForTest(uint guid, Vector3 position)
        {
            Guid      = guid;
            Position  = position;
            HitRadius = 1f;
        }

        public void SetHealthForTest(uint maxHealth, uint health)
        {
            MaxHealth = maxHealth;
            Health    = health;
        }

        public void SetInCombatForTest(bool value)
        {
            typeof(UnitEntity)
                .GetField("inCombat", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(this, value);
        }

        public void InitialiseRewardRuntimeForTest(
            uint creatureId,
            IBaseMap map,
            IAssetManager assetManager,
            IGameTableManager gameTableManager)
        {
            InitialiseRuntimeDependencies(
                summonFactoryResolver: null,
                creatureInfoManagerResolver: null,
                factionManagerResolver: null,
                gameTableManagerResolver: () => gameTableManager);
            InitialiseRuntimeDependencies(
                globalLootManagerResolver: null,
                tradeManagerResolver: null,
                assetManagerResolver: () => assetManager);
            SetAutoProperty(this, nameof(Map), map);
            SetAutoProperty(this, nameof(CreatureEntry), new Creature2Entry
            {
                Id = creatureId
            });
        }

        public void InitialiseSpellRuntimeForTest(
            IGlobalSpellManager globalSpellManager,
            IGameTableManager gameTableManager,
            IScriptManager scriptManager)
        {
            InitialiseRuntimeDependencies(
                summonFactoryResolver: null,
                creatureInfoManagerResolver: null,
                factionManagerResolver: null,
                gameTableManagerResolver: () => gameTableManager);
            InitialiseRuntimeDependencies(
                globalLootManagerResolver: null,
                tradeManagerResolver: null,
                globalSpellManagerResolver: () => globalSpellManager);
            InitialiseScriptManager(() => scriptManager);
        }

        public void RewardKillerForTest(IPlayer player)
        {
            RewardKiller(player);
        }

        public void SetPublicEventForTest(uint publicEventId)
        {
            SetAutoProperty(this, nameof(PublicEventId), publicEventId);
        }

        public void SetShieldForTest(uint maxShield, uint shield, float shieldRegenPct, float shieldRebootTimeMs, float shieldTickTimeMs)
        {
            MaxShieldCapacity = maxShield;
            Shield = shield;
            SetBaseProperty(Property.ShieldRegenPct, shieldRegenPct);
            SetBaseProperty(Property.ShieldRebootTime, shieldRebootTimeMs);
            SetBaseProperty(Property.ShieldTickTime, shieldTickTimeMs);
        }

        public void SetResource1ForTest(float maxResource, float resource)
        {
            SetBaseProperty(Property.ResourceMax1, maxResource);
            SetStat(Stat.Resource1, resource);
        }

        public void SetDashEnergyForTest(float maxDashEnergy, float dashEnergy, float regenMultiplier)
        {
            SetBaseProperty(Property.ResourceMax7, maxDashEnergy);
            SetBaseProperty(Property.ResourceRegenMultiplier7, regenMultiplier);
            SetStat(Stat.Dash, dashEnergy);
        }

        protected override IEntityModel BuildEntityModel()
        {
            return new WorldUnitEntityModel();
        }

        protected override float CalculateDefaultProperty(Property property)
        {
            return 0f;
        }

        protected override void OnDeath()
        {
            if (invokeBaseOnDeath)
            {
                base.OnDeath();
                return;
            }

            // Keep the result-state test isolated from reward, respawn, and map side effects.
        }
    }
}
