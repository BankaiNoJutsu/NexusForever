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
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Loot;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Abilities;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Tests.Spell;

public class SpellEffectCombatRegressionTests
{
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

        ISpell spell = CreateSpell(35123u, player, primaryTargetId: 900u);
        ISpellTargetEffectInfo info = CreatePetCastSpellInfo(
            effectId: 121046u,
            spellId: 35123u,
            requiredSummonSpell4Id: 42814u,
            petSpell4Id: 49502u);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectPetCastSpell(spell, player, info);

        RecordingDispatchProxy<IUnitEntity>.Invocation cast =
            Assert.Single(petProxy.GetInvocations(nameof(IUnitEntity.CastSpell)));
        Assert.Equal(49502u, (uint)cast.Arguments[0]);
        Assert.Equal(900u, ((ISpellParameters)cast.Arguments[1]).PrimaryTargetId);
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
    public void HandleEffectSummonPet_WithArtillerybotTier2_RegistersTieredBarrageAction()
    {
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<ISpellManager> spellManagerProxy, out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Position), Vector3.Zero);
        playerProxy.SetProperty(nameof(IPlayer.Rotation), Vector3.Zero);
        playerProxy.SetProperty(nameof(IPlayer.Level), 35u);
        playerProxy.SetProperty(nameof(IPlayer.Faction1), Faction.Exile);
        playerProxy.SetProperty(nameof(IPlayer.Faction2), Faction.Exile);

        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);

        IActionSet actionSet = CreateArtillerybotActionSet();
        spellManagerProxy.SetProperty(nameof(ISpellManager.ActiveActionSet), (byte)0);
        spellManagerProxy.SetMethodReturn(nameof(ISpellManager.GetActionSet), actionSet);

        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetMethodHandler(nameof(IBaseMap.CanEnter), _ => true);
        playerProxy.SetProperty(nameof(IPlayer.Map), map);

        IEntitySummonFactory summonFactory = RecordingDispatchProxy<IEntitySummonFactory>.Create(out _);
        playerProxy.SetProperty(nameof(IPlayer.SummonFactory), summonFactory);

        IWorldEntity pet = RecordingDispatchProxy<IWorldEntity>.Create(out _);
        IEntityFactory entityFactory = RecordingDispatchProxy<IEntityFactory>.Create(out RecordingDispatchProxy<IEntityFactory> entityFactoryProxy);
        entityFactoryProxy.SetMethodHandler(nameof(IEntityFactory.CreateWorldEntity), _ => pet);

        IGameTableManager gameTableManager = CreateGameTableManager(
            creatureEntries:
            [
                new Creature2Entry
                {
                    Id               = 42683u,
                    CreationTypeEnum = (uint)EntityType.NonPlayer,
                    MinLevel         = 1u,
                    MaxLevel         = 60u,
                    Description      = "Engineer Pet - Artillery Bot - Exile"
                }
            ],
            spellEntries:
            [
                new Spell4Entry
                {
                    Id                    = 56249u,
                    Spell4BaseIdBaseSpell = 27002u,
                    TierIndex             = 2u,
                    Spell4IdPetSwitch     = 56267u
                },
                new Spell4Entry
                {
                    Id                    = 56275u,
                    Spell4BaseIdBaseSpell = 20884u,
                    TierIndex             = 2u
                },
                new Spell4Entry
                {
                    Id                    = 56267u,
                    Spell4BaseIdBaseSpell = 34051u,
                    TierIndex             = 2u
                }
            ]);
        using IDisposable resolverScope = UseDependencyResolver(gameTableManager, entityFactory: entityFactory);

        ISpell spell = CreateSpell(new Spell4Entry
        {
            Id                    = 56249u,
            Spell4BaseIdBaseSpell = 27002u,
            TierIndex             = 2u,
            Spell4IdPetSwitch     = 56267u
        }, player);
        ISpellTargetEffectInfo info = CreateSummonPetInfo(
            effectId: 134760u,
            spellId: 56249u,
            creatureId: 42683u,
            petType: 3u,
            out _);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectSummonPet(spell, player, info);

        RecordingDispatchProxy<ISpellManager>.Invocation petAction =
            Assert.Single(spellManagerProxy.GetInvocations(nameof(ISpellManager.SetActivePetActionSpell)));
        Assert.Equal(56267u, petAction.Arguments[0]);
        Assert.Equal(56275u, petAction.Arguments[1]);
        Assert.Equal(56249u, petAction.Arguments[2]);

        IReadOnlyList<RecordingDispatchProxy<IGameSession>.Invocation> messages =
            sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted));
        ServerSpellUpdate spellUpdate = Assert.IsType<ServerSpellUpdate>(messages[0].Arguments[0]);
        Assert.Equal(34051u, spellUpdate.Spell4BaseId);
        Assert.Equal(2, spellUpdate.TierIndex);
        Assert.True(spellUpdate.Activated);

        ServerActionSet actionSetPacket = Assert.IsType<ServerActionSet>(messages[1].Arguments[0]);
        Assert.Equal(48, actionSetPacket.Actions.Count);
        Assert.Equal(ShortcutType.SpellbookItem, actionSetPacket.Actions[4].ShortcutType);
        Assert.Equal(34051u, actionSetPacket.Actions[4].ObjectId);
    }

    [Fact]
    public void UnregisterEngineerArtillerybotBarrageAction_WithLastBotGone_ClearsTemporaryBarrageAction()
    {
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<ISpellManager> spellManagerProxy, out RecordingDispatchProxy<IPlayer> playerProxy);

        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);

        IEntitySummonFactory summonFactory = RecordingDispatchProxy<IEntitySummonFactory>.Create(out RecordingDispatchProxy<IEntitySummonFactory> summonFactoryProxy);
        summonFactoryProxy.SetMethodReturn(nameof(IEntitySummonFactory.GetSummonCreatureCount), 0u);
        playerProxy.SetProperty(nameof(IPlayer.SummonFactory), summonFactory);

        var restorePacket = new ServerActionSet();
        IActionSet actionSet = RecordingDispatchProxy<IActionSet>.Create(out RecordingDispatchProxy<IActionSet> actionSetProxy);
        actionSetProxy.SetMethodReturn(nameof(IActionSet.BuildServerActionSet), restorePacket);
        spellManagerProxy.SetProperty(nameof(ISpellManager.ActiveActionSet), (byte)0);
        spellManagerProxy.SetMethodReturn(nameof(ISpellManager.GetActionSet), actionSet);

        IWorldEntity bot = RecordingDispatchProxy<IWorldEntity>.Create(out RecordingDispatchProxy<IWorldEntity> botProxy);
        botProxy.SetProperty(nameof(IWorldEntity.CreatureId), 42683u);

        global::NexusForever.Game.Spell.SpellHandler.UnregisterEngineerArtillerybotBarrageAction(player, bot);

        Assert.Single(spellManagerProxy.GetInvocations(nameof(ISpellManager.ClearActivePetActionSpells)));

        IReadOnlyList<RecordingDispatchProxy<IGameSession>.Invocation> messages =
            sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted));
        ServerSpellUpdate spellUpdate = Assert.IsType<ServerSpellUpdate>(messages[0].Arguments[0]);
        Assert.Equal(34051u, spellUpdate.Spell4BaseId);
        Assert.Equal(0, spellUpdate.TierIndex);
        Assert.False(spellUpdate.Activated);
        Assert.Same(restorePacket, messages[1].Arguments[0]);
    }

    [Fact]
    public void HandleEffectSummonPet_WithArtillerybotCapReached_DoesNotCreateAdditionalBot()
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
            (uint)args[0] == 42683u ? 2u : 0u);
        playerProxy.SetProperty(nameof(IPlayer.SummonFactory), summonFactory);

        IEntityFactory entityFactory = RecordingDispatchProxy<IEntityFactory>.Create(out RecordingDispatchProxy<IEntityFactory> entityFactoryProxy);

        IGameTableManager gameTableManager = CreateGameTableManagerWithCreature2(
            new Creature2Entry
            {
                Id               = 42683u,
                CreationTypeEnum = (uint)EntityType.NonPlayer,
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

    private static IActionSet CreateArtillerybotActionSet()
    {
        IActionSetShortcut artillerybotShortcut = RecordingDispatchProxy<IActionSetShortcut>.Create(out RecordingDispatchProxy<IActionSetShortcut> shortcutProxy);
        shortcutProxy.SetProperty(nameof(IActionSetShortcut.Location), (UILocation)4);
        shortcutProxy.SetProperty(nameof(IActionSetShortcut.ShortcutType), ShortcutType.SpellbookItem);
        shortcutProxy.SetProperty(nameof(IActionSetShortcut.ObjectId), 27002u);
        shortcutProxy.SetProperty(nameof(IActionSetShortcut.Tier), (byte)2);

        IActionSet actionSet = RecordingDispatchProxy<IActionSet>.Create(out RecordingDispatchProxy<IActionSet> actionSetProxy);
        actionSetProxy.SetProperty(nameof(IActionSet.Index), (byte)0);
        actionSetProxy.SetMethodHandler(nameof(IActionSet.GetShortcut), args =>
        {
            if (args.Length == 1 && args[0] is UILocation location)
                return location == (UILocation)4 ? artillerybotShortcut : null;

            return null;
        });

        return actionSet;
    }

    private static ISpell CreateSpell(uint spell4Id, IUnitEntity caster, uint primaryTargetId = 0u)
    {
        return CreateSpell(new Spell4Entry { Id = spell4Id }, caster, primaryTargetId);
    }

    private static ISpell CreateSpell(Spell4Entry spell4Entry, IUnitEntity caster, uint primaryTargetId = 0u)
    {
        ISpellInfo spellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out RecordingDispatchProxy<ISpellInfo> spellInfoProxy);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Entry), spell4Entry);

        ISpellParameters parameters = RecordingDispatchProxy<ISpellParameters>.Create(out RecordingDispatchProxy<ISpellParameters> parametersProxy);
        parametersProxy.SetProperty(nameof(ISpellParameters.SpellInfo), spellInfo);
        parametersProxy.SetProperty(nameof(ISpellParameters.PrimaryTargetId), primaryTargetId);

        ISpell spell = RecordingDispatchProxy<ISpell>.Create(out RecordingDispatchProxy<ISpell> spellProxy);
        spellProxy.SetProperty(nameof(ISpell.Caster), caster);
        spellProxy.SetProperty(nameof(ISpell.CastingId), 7u);
        spellProxy.SetProperty(nameof(ISpell.Parameters), parameters);
        return spell;
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

    private static IGameTableManager CreateGameTableManager(params Spell4Entry[] spellEntries)
    {
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> proxy);
        proxy.SetProperty(nameof(IGameTableManager.Spell4), CreateGameTable(spellEntries));
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
