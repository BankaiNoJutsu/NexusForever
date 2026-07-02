using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement.Force;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Abilities;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Spell;

namespace NexusForever.Game.Tests.Spell;

[Collection(MissingGameDataDiagnosticsCollection.Name)]
public class FloatingActionBarSpellTests
{
    private const uint LockboxSpell4Id = 55640u;
    private const uint LockboxSpellGroupListId = 1176u;
    private const uint FloatingActionBarSpellGroupId = 444u;
    private const uint LockboxActionBarShortcutSetId = 474u;
    private const uint AtomNeutralizerSpell4Id = 55643u;
    private const uint FragGrenadeSpell4Id = 60906u;
    private const uint RemoveActionBarSpell4Id = 55644u;
    private const uint ArtillerybotSummonSpell4Id = 42814u;
    private const uint ArtillerybotSummonSpellGroupListId = 1177u;
    private const uint ArtillerybotSpellGroupId = 445u;
    private const uint ArtillerybotPetSwitchSpell4Id = 51365u;
    private const uint ArtillerybotPlayerBarrageSpell4Id = 35123u;
    private const uint ArtillerybotPlayerBarrageBaseSpell4Id = 20884u;
    private const uint RepairbotSummonSpell4Id = 42810u;
    private const uint RepairbotSummonSpellGroupListId = 1178u;
    private const uint RepairbotSpellGroupId = 446u;
    private const uint RepairbotShieldBoostSpell4Id = 35657u;

    [Fact]
    public void ActionBarSet_RegistersFloatingSpellShortcuts()
    {
        using IDisposable dependencyScope = UseGameTableManager(CreateGameTableManager(
            actionBarShortcutSets: CreateGameTable(new ActionBarShortcutSetEntry
            {
                Id             = LockboxActionBarShortcutSetId,
                ShortcutType00 = (uint)ShortcutType.Spell,
                ShortcutType01 = (uint)ShortcutType.Spell,
                ShortcutType02 = (uint)ShortcutType.SpellbookItem,
                ObjectId00     = AtomNeutralizerSpell4Id,
                ObjectId01     = FragGrenadeSpell4Id,
                ObjectId02     = 123u
            })));

        IPlayer player = CreatePlayer(
            out _,
            out RecordingDispatchProxy<ISpellManager> spellManagerProxy,
            out RecordingDispatchProxy<IGameSession> sessionProxy);
        ISpell spell = CreateSpell(LockboxSpell4Id, player);
        ISpellTargetEffectInfo info = CreateEffectInfo(SpellEffectType.ActionBarSet, dataBits00: LockboxActionBarShortcutSetId);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectActionBarSet(spell, player, info);

        ServerActionBarSet packet = Assert.IsType<ServerActionBarSet>(Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))).Arguments[0]);
        Assert.Equal(LockboxActionBarShortcutSetId, packet.ActionBarShortcutSetId);

        RecordingDispatchProxy<ISpellManager>.Invocation activeSet =
            Assert.Single(spellManagerProxy.GetInvocations(nameof(ISpellManager.SetActiveFloatingActionBarShortcutSet)));
        Assert.Equal(LockboxActionBarShortcutSetId, activeSet.Arguments[0]);
        Assert.Equal(LockboxSpell4Id, activeSet.Arguments[1]);
    }

    [Fact]
    public void SpellManager_TracksAndClearsActiveFloatingActionBarSpells()
    {
        IGameTableManager gameTableManager = CreateGameTableManager(
            spell4Entries: CreateGameTable(new Spell4Entry
            {
                Id                = LockboxSpell4Id,
                Spell4GroupListId = LockboxSpellGroupListId
            }),
            spell4GroupLists: CreateGameTable(new Spell4GroupListEntry
            {
                Id             = LockboxSpellGroupListId,
                SpellGroupId00 = FloatingActionBarSpellGroupId
            }),
            actionBarShortcutSets: CreateGameTable(new ActionBarShortcutSetEntry
            {
                Id             = LockboxActionBarShortcutSetId,
                ShortcutType00 = (uint)ShortcutType.Spell,
                ShortcutType01 = (uint)ShortcutType.Spell,
                ShortcutType02 = (uint)ShortcutType.SpellbookItem,
                ObjectId00     = AtomNeutralizerSpell4Id,
                ObjectId01     = FragGrenadeSpell4Id,
                ObjectId02     = 123u
            }));
        global::NexusForever.Game.Entity.SpellManager manager = CreateSpellManager(gameTableManager);

        manager.SetActiveFloatingActionBarShortcutSet(LockboxActionBarShortcutSetId, LockboxSpell4Id);

        Assert.True(manager.IsActiveFloatingActionBarSpell(AtomNeutralizerSpell4Id));
        Assert.True(manager.IsActiveFloatingActionBarSpell(FragGrenadeSpell4Id));
        Assert.False(manager.IsActiveFloatingActionBarSpell(123u));

        Assert.True(manager.ClearActiveFloatingActionBarShortcutSetForSpellGroup(FloatingActionBarSpellGroupId));
        Assert.False(manager.IsActiveFloatingActionBarSpell(AtomNeutralizerSpell4Id));
    }

    [Fact]
    public void SpellManager_TracksAndClearsActivePetActionSpells()
    {
        IGameTableManager gameTableManager = CreateGameTableManager(
            spell4Entries: CreateGameTable(new Spell4Entry
            {
                Id                = ArtillerybotSummonSpell4Id,
                Spell4GroupListId = ArtillerybotSummonSpellGroupListId
            },
            new Spell4Entry
            {
                Id                    = ArtillerybotPlayerBarrageSpell4Id,
                Spell4BaseIdBaseSpell = ArtillerybotPlayerBarrageBaseSpell4Id
            }),
            spell4GroupLists: CreateGameTable(new Spell4GroupListEntry
            {
                Id             = ArtillerybotSummonSpellGroupListId,
                SpellGroupId00 = ArtillerybotSpellGroupId
            }));
        global::NexusForever.Game.Entity.SpellManager manager = CreateSpellManager(gameTableManager);

        manager.SetActivePetActionSpell(ArtillerybotPetSwitchSpell4Id, ArtillerybotPlayerBarrageSpell4Id, ArtillerybotSummonSpell4Id);

        Assert.True(manager.TryResolveActivePetActionSpell(ArtillerybotPetSwitchSpell4Id, out uint actionSpell4Id));
        Assert.Equal(ArtillerybotPlayerBarrageSpell4Id, actionSpell4Id);
        Assert.True(manager.TryResolveActivePetActionSpell(ArtillerybotPlayerBarrageSpell4Id, out actionSpell4Id));
        Assert.Equal(ArtillerybotPlayerBarrageSpell4Id, actionSpell4Id);
        Assert.True(manager.TryResolveActivePetActionSpell(ArtillerybotPlayerBarrageBaseSpell4Id, out actionSpell4Id));
        Assert.Equal(ArtillerybotPlayerBarrageSpell4Id, actionSpell4Id);
        Assert.True(manager.TryResolveSingleActivePetActionSpell(out actionSpell4Id));
        Assert.Equal(ArtillerybotPlayerBarrageSpell4Id, actionSpell4Id);

        Assert.True(manager.ClearActivePetActionSpellsForSpellGroup(ArtillerybotSpellGroupId));
        Assert.False(manager.TryResolveActivePetActionSpell(ArtillerybotPetSwitchSpell4Id, out _));
        Assert.False(manager.TryResolveActivePetActionSpell(ArtillerybotPlayerBarrageBaseSpell4Id, out _));
        Assert.False(manager.TryResolveSingleActivePetActionSpell(out _));
    }

    [Fact]
    public void SpellManager_ClearsOnlyMatchingActivePetActionSpellGroup()
    {
        IGameTableManager gameTableManager = CreateGameTableManager(
            spell4Entries: CreateGameTable(
                new Spell4Entry
                {
                    Id                = ArtillerybotSummonSpell4Id,
                    Spell4GroupListId = ArtillerybotSummonSpellGroupListId
                },
                new Spell4Entry
                {
                    Id                    = ArtillerybotPlayerBarrageSpell4Id,
                    Spell4BaseIdBaseSpell = ArtillerybotPlayerBarrageBaseSpell4Id
                },
                new Spell4Entry
                {
                    Id                = RepairbotSummonSpell4Id,
                    Spell4GroupListId = RepairbotSummonSpellGroupListId
                }),
            spell4GroupLists: CreateGameTable(
                new Spell4GroupListEntry
                {
                    Id             = ArtillerybotSummonSpellGroupListId,
                    SpellGroupId00 = ArtillerybotSpellGroupId
                },
                new Spell4GroupListEntry
                {
                    Id             = RepairbotSummonSpellGroupListId,
                    SpellGroupId00 = RepairbotSpellGroupId
                }));
        global::NexusForever.Game.Entity.SpellManager manager = CreateSpellManager(gameTableManager);

        manager.SetActivePetActionSpell(ArtillerybotPetSwitchSpell4Id, ArtillerybotPlayerBarrageSpell4Id, ArtillerybotSummonSpell4Id);
        manager.SetActivePetActionSpell(RepairbotShieldBoostSpell4Id, RepairbotShieldBoostSpell4Id, RepairbotSummonSpell4Id);

        Assert.True(manager.ClearActivePetActionSpellsForSpellGroup(ArtillerybotSpellGroupId));

        Assert.False(manager.TryResolveActivePetActionSpell(ArtillerybotPetSwitchSpell4Id, out _));
        Assert.False(manager.TryResolveActivePetActionSpell(ArtillerybotPlayerBarrageSpell4Id, out _));
        Assert.False(manager.TryResolveActivePetActionSpell(ArtillerybotPlayerBarrageBaseSpell4Id, out _));
        Assert.True(manager.TryResolveActivePetActionSpell(RepairbotShieldBoostSpell4Id, out uint repairbotActionSpell4Id));
        Assert.Equal(RepairbotShieldBoostSpell4Id, repairbotActionSpell4Id);
        Assert.True(manager.TryResolveSingleActivePetActionSpell(out uint singleActionSpell4Id));
        Assert.Equal(RepairbotShieldBoostSpell4Id, singleActionSpell4Id);
    }

    [Fact]
    public void SpellManager_DoesNotResolveSingleActivePetActionWhenMultipleCommandsAreActive()
    {
        IGameTableManager gameTableManager = CreateGameTableManager(
            spell4Entries: CreateGameTable(
                new Spell4Entry
                {
                    Id                    = ArtillerybotPlayerBarrageSpell4Id,
                    Spell4BaseIdBaseSpell = ArtillerybotPlayerBarrageBaseSpell4Id
                },
                new Spell4Entry
                {
                    Id = RepairbotShieldBoostSpell4Id
                }));
        global::NexusForever.Game.Entity.SpellManager manager = CreateSpellManager(gameTableManager);

        manager.SetActivePetActionSpell(ArtillerybotPetSwitchSpell4Id, ArtillerybotPlayerBarrageSpell4Id, ArtillerybotSummonSpell4Id);
        manager.SetActivePetActionSpell(RepairbotShieldBoostSpell4Id, RepairbotShieldBoostSpell4Id, RepairbotSummonSpell4Id);

        Assert.False(manager.TryResolveSingleActivePetActionSpell(out _));
    }

    [Fact]
    public void ForceRemove_RemoveTypeOneMatchesSpellGroupListAndClearsFloatingBar()
    {
        using IDisposable dependencyScope = UseGameTableManager(CreateGameTableManager(
            spell4Entries: CreateGameTable(
                new Spell4Entry
                {
                    Id                = LockboxSpell4Id,
                    Spell4GroupListId = LockboxSpellGroupListId
                },
                new Spell4Entry
                {
                    Id                = AtomNeutralizerSpell4Id,
                    Spell4GroupListId = 1253u
                }),
            spell4GroupLists: CreateGameTable(
                new Spell4GroupListEntry
                {
                    Id             = LockboxSpellGroupListId,
                    SpellGroupId00 = FloatingActionBarSpellGroupId
                },
                new Spell4GroupListEntry
                {
                    Id             = 1253u,
                    SpellGroupId00 = 477u
                })));

        IPlayer player = CreatePlayer(
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<ISpellManager> spellManagerProxy,
            out _);
        ISpell spell = CreateSpell(RemoveActionBarSpell4Id, player);
        ISpellTargetEffectInfo info = CreateEffectInfo(
            SpellEffectType.SpellForceRemove,
            dataBits00: 1u,
            dataBits01: FloatingActionBarSpellGroupId,
            dataBits02: 1u,
            dataBits03: 1u,
            dataBits04: 1u,
            dataBits06: 3u);

        playerProxy.SetMethodHandler(nameof(IUnitEntity.RemoveTrackedSpellStates), args =>
        {
            var predicate = (Func<uint, bool>)args[0];
            Assert.True(predicate(LockboxSpell4Id));
            Assert.False(predicate(AtomNeutralizerSpell4Id));
            return Array.Empty<SpellStateRemoval>();
        });

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectSpellForceRemove(spell, player, info);

        RecordingDispatchProxy<ISpellManager>.Invocation clear =
            Assert.Single(spellManagerProxy.GetInvocations(nameof(ISpellManager.ClearActiveFloatingActionBarShortcutSetForSpellGroup)));
        Assert.Equal(FloatingActionBarSpellGroupId, clear.Arguments[0]);
    }

    [Fact]
    public void ClientCastSpellSelected_AllowsActiveFloatingActionBarSpell()
    {
        IWorldSession session = CreateWorldSession(
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<ISpellManager> spellManagerProxy,
            out RecordingDispatchProxy<IWorldSession> sessionProxy);
        spellManagerProxy.SetMethodReturn(nameof(ISpellManager.GetSpellForSpell4Id), null);
        spellManagerProxy.SetMethodReturn(nameof(ISpellManager.IsActiveFloatingActionBarSpell), true);

        var request = CreateSelectedCast(AtomNeutralizerSpell4Id, targetId: 99u, contextToken: 1234u);
        var handler = new ClientCastSpellSelectedHandler(NullLogger<ClientCastSpellSelectedHandler>.Instance);

        handler.HandleMessage(session, request);

        RecordingDispatchProxy<IPlayer>.Invocation cast =
            Assert.Single(playerProxy.GetInvocations(nameof(IUnitEntity.TryCastSpell)));
        Assert.Equal(AtomNeutralizerSpell4Id, cast.Arguments[0]);
        SpellParameters parameters = Assert.IsType<SpellParameters>(cast.Arguments[1]);
        Assert.Equal(99u, parameters.PrimaryTargetId);
        Assert.Equal(1234u, parameters.ClientContextToken);
        Assert.True(parameters.UserInitiatedSpellCast);
        Assert.Equal(nameof(ClientCastSpellSelected), parameters.ClientRequestSource);
        Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
    }

    [Fact]
    public void ClientCastSpellSelected_MapsActivePetActionSpell()
    {
        IWorldSession session = CreateWorldSession(
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<ISpellManager> spellManagerProxy,
            out RecordingDispatchProxy<IWorldSession> sessionProxy);
        spellManagerProxy.SetMethodReturn(nameof(ISpellManager.GetSpellForSpell4Id), null);
        spellManagerProxy.SetMethodReturn(nameof(ISpellManager.IsActiveFloatingActionBarSpell), false);
        spellManagerProxy.SetMethodReturn(nameof(ISpellManager.GetSpellCooldown), 0d);
        spellManagerProxy.SetMethodHandler(nameof(ISpellManager.TryResolveActivePetActionSpell), args =>
        {
            args[1] = ArtillerybotPlayerBarrageSpell4Id;
            return true;
        });

        IGameTableManager gameTableManager = CreateGameTableManager(
            spell4Entries: CreateGameTable(new Spell4Entry
            {
                Id            = ArtillerybotPetSwitchSpell4Id,
                SpellCoolDown = 15000u
            }));
        var request = CreateSelectedCast(ArtillerybotPetSwitchSpell4Id, targetId: 99u, contextToken: 1234u);
        var handler = new ClientCastSpellSelectedHandler(NullLogger<ClientCastSpellSelectedHandler>.Instance, gameTableManager);

        handler.HandleMessage(session, request);

        RecordingDispatchProxy<IPlayer>.Invocation cast =
            Assert.Single(playerProxy.GetInvocations(nameof(IUnitEntity.TryCastSpell)));
        Assert.Equal(ArtillerybotPlayerBarrageSpell4Id, cast.Arguments[0]);
        SpellParameters parameters = Assert.IsType<SpellParameters>(cast.Arguments[1]);
        Assert.Equal(99u, parameters.PrimaryTargetId);
        Assert.Equal(1234u, parameters.ClientContextToken);
        Assert.True(parameters.UserInitiatedSpellCast);
        Assert.Equal(nameof(ClientCastSpellSelected), parameters.ClientRequestSource);

        RecordingDispatchProxy<ISpellManager>.Invocation cooldown =
            Assert.Single(spellManagerProxy.GetInvocations(nameof(ISpellManager.SetSpellCooldown)));
        Assert.Equal(ArtillerybotPetSwitchSpell4Id, cooldown.Arguments[0]);
        Assert.Equal(15d, cooldown.Arguments[1]);
        Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
    }

    [Fact]
    public void ClientCastSpellSelected_RejectsActivePetActionOnSelectorCooldown()
    {
        IWorldSession session = CreateWorldSession(
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<ISpellManager> spellManagerProxy,
            out RecordingDispatchProxy<IWorldSession> sessionProxy);
        spellManagerProxy.SetMethodReturn(nameof(ISpellManager.GetSpellForSpell4Id), null);
        spellManagerProxy.SetMethodReturn(nameof(ISpellManager.IsActiveFloatingActionBarSpell), false);
        spellManagerProxy.SetMethodReturn(nameof(ISpellManager.GetSpellCooldown), 12d);
        spellManagerProxy.SetMethodHandler(nameof(ISpellManager.TryResolveActivePetActionSpell), args =>
        {
            args[1] = ArtillerybotPlayerBarrageSpell4Id;
            return true;
        });

        var request = CreateSelectedCast(ArtillerybotPetSwitchSpell4Id, targetId: 99u, contextToken: 1234u);
        var handler = new ClientCastSpellSelectedHandler(NullLogger<ClientCastSpellSelectedHandler>.Instance);

        handler.HandleMessage(session, request);

        Assert.Empty(playerProxy.GetInvocations(nameof(IUnitEntity.TryCastSpell)));
        ServerSpellCastResult result = Assert.IsType<ServerSpellCastResult>(Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))).Arguments[0]);
        Assert.Equal(ArtillerybotPetSwitchSpell4Id, result.Spell4Id);
        Assert.Equal(1234u, result.ContextToken);
        Assert.Equal(CastResult.SpellCooldown, result.CastResult);
    }

    [Fact]
    public void ClientCastSpellSelected_RejectsUnknownInactiveFloatingActionBarSpell()
    {
        IWorldSession session = CreateWorldSession(
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<ISpellManager> spellManagerProxy,
            out RecordingDispatchProxy<IWorldSession> sessionProxy);
        spellManagerProxy.SetMethodReturn(nameof(ISpellManager.GetSpellForSpell4Id), null);
        spellManagerProxy.SetMethodReturn(nameof(ISpellManager.IsActiveFloatingActionBarSpell), false);
        spellManagerProxy.SetMethodHandler(nameof(ISpellManager.TryResolveActivePetActionSpell), args =>
        {
            args[1] = 0u;
            return false;
        });

        var request = CreateSelectedCast(AtomNeutralizerSpell4Id, targetId: 99u, contextToken: 1234u);
        var handler = new ClientCastSpellSelectedHandler(NullLogger<ClientCastSpellSelectedHandler>.Instance);

        handler.HandleMessage(session, request);

        Assert.Empty(playerProxy.GetInvocations(nameof(IUnitEntity.TryCastSpell)));
        ServerSpellCastResult result = Assert.IsType<ServerSpellCastResult>(Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))).Arguments[0]);
        Assert.Equal(AtomNeutralizerSpell4Id, result.Spell4Id);
        Assert.Equal(1234u, result.ContextToken);
        Assert.Equal(CastResult.SpellUnknown, result.CastResult);
    }

    private static IPlayer CreatePlayer(
        out RecordingDispatchProxy<IPlayer> playerProxy,
        out RecordingDispatchProxy<ISpellManager> spellManagerProxy,
        out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        ISpellManager spellManager = RecordingDispatchProxy<ISpellManager>.Create(out spellManagerProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);

        playerProxy.SetProperty(nameof(IPlayer.Guid), 9u);
        playerProxy.SetProperty(nameof(IPlayer.SpellManager), spellManager);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.IsAlive), true);
        return player;
    }

    private static IWorldSession CreateWorldSession(
        out RecordingDispatchProxy<IPlayer> playerProxy,
        out RecordingDispatchProxy<ISpellManager> spellManagerProxy,
        out RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        ISpellManager spellManager = RecordingDispatchProxy<ISpellManager>.Create(out spellManagerProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);

        playerProxy.SetProperty(nameof(IPlayer.Guid), 9u);
        playerProxy.SetProperty(nameof(IPlayer.SpellManager), spellManager);
        playerProxy.SetProperty(nameof(IPlayer.IsAlive), true);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        sessionProxy.SetMethodHandler(nameof(IWorldSession.TryConsumeNextClientSpellEvidenceCapture), args =>
        {
            args[0] = false;
            return false;
        });
        return session;
    }

    private static ISpell CreateSpell(uint spell4Id, IUnitEntity caster)
    {
        ISpell spell = RecordingDispatchProxy<ISpell>.Create(out RecordingDispatchProxy<ISpell> spellProxy);
        ISpellParameters parameters = RecordingDispatchProxy<ISpellParameters>.Create(out RecordingDispatchProxy<ISpellParameters> parametersProxy);
        ISpellInfo spellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out RecordingDispatchProxy<ISpellInfo> spellInfoProxy);

        spellInfoProxy.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = spell4Id });
        parametersProxy.SetProperty(nameof(ISpellParameters.SpellInfo), spellInfo);
        spellProxy.SetProperty(nameof(ISpell.Parameters), parameters);
        spellProxy.SetProperty(nameof(ISpell.Caster), caster);
        spellProxy.SetProperty(nameof(ISpell.CastingId), 22u);
        return spell;
    }

    private static global::NexusForever.Game.Entity.SpellManager CreateSpellManager(IGameTableManager gameTableManager)
    {
        var manager = (global::NexusForever.Game.Entity.SpellManager)RuntimeHelpers.GetUninitializedObject(
            typeof(global::NexusForever.Game.Entity.SpellManager));
        SetPrivateField(manager, "gameTableManager", gameTableManager);
        SetPrivateField(manager, "activeFloatingActionBarSpell4Ids", new HashSet<uint>());
        SetPrivateField(manager, "activeFloatingActionBarOwnerSpellGroupIds", new HashSet<uint>());
        SetPrivateField(manager, "activePetActionSpell4Ids", new Dictionary<uint, uint>());
        SetPrivateField(manager, "activePetActionOwnerSpellGroupIds", new HashSet<uint>());
        SetPrivateField(manager, "activePetActionOwnerSpellGroupIdsBySpell4Id", new Dictionary<uint, HashSet<uint>>());
        return manager;
    }

    private static ISpellTargetEffectInfo CreateEffectInfo(
        SpellEffectType effectType,
        uint dataBits00 = 0u,
        uint dataBits01 = 0u,
        uint dataBits02 = 0u,
        uint dataBits03 = 0u,
        uint dataBits04 = 0u,
        uint dataBits06 = 0u)
    {
        ISpellTargetEffectInfo info = RecordingDispatchProxy<ISpellTargetEffectInfo>.Create(out RecordingDispatchProxy<ISpellTargetEffectInfo> infoProxy);
        infoProxy.SetProperty(nameof(ISpellTargetEffectInfo.EffectId), 77u);
        infoProxy.SetProperty(nameof(ISpellTargetEffectInfo.Entry), new Spell4EffectsEntry
        {
            Id          = 77u,
            EffectType  = effectType,
            TargetFlags = 1u,
            DataBits00  = dataBits00,
            DataBits01  = dataBits01,
            DataBits02  = dataBits02,
            DataBits03  = dataBits03,
            DataBits04  = dataBits04,
            DataBits06  = dataBits06
        });
        infoProxy.SetProperty(nameof(ISpellTargetEffectInfo.CombatLogs), new List<NexusForever.Network.World.Combat.ICombatLog>());
        return info;
    }

    private static ClientCastSpellSelected CreateSelectedCast(uint selectedEntryId, uint targetId, uint contextToken)
    {
        var request = new ClientCastSpellSelected();
        SetPrivateProperty(request, nameof(ClientCastSpellSelected.SelectedEntryId), selectedEntryId);
        SetPrivateProperty(request, nameof(ClientCastSpellSelected.TargetEntityId), targetId);
        SetPrivateProperty(request, nameof(ClientCastSpellSelected.ContextToken), contextToken);
        return request;
    }

    private static void SetPrivateProperty<T>(T instance, string propertyName, object value)
    {
        SetPrivateField(instance, $"<{propertyName}>k__BackingField", value);
    }

    private static IGameTableManager CreateGameTableManager(
        GameTable<Spell4Entry> spell4Entries = null,
        GameTable<Spell4GroupListEntry> spell4GroupLists = null,
        GameTable<ActionBarShortcutSetEntry> actionBarShortcutSets = null)
    {
        IGameTableManager manager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> proxy);
        proxy.SetProperty(nameof(IGameTableManager.Spell4), spell4Entries ?? CreateGameTable<Spell4Entry>());
        proxy.SetProperty(nameof(IGameTableManager.Spell4GroupList), spell4GroupLists ?? CreateGameTable<Spell4GroupListEntry>());
        proxy.SetProperty(nameof(IGameTableManager.ActionBarShortcutSet), actionBarShortcutSets ?? CreateGameTable<ActionBarShortcutSetEntry>());
        return manager;
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

    private static void SetAutoProperty<T>(T instance, string propertyName, object value)
    {
        SetPrivateField(instance, $"<{propertyName}>k__BackingField", value);
    }

    private static void SetPrivateField<T>(T instance, string fieldName, object value)
    {
        typeof(T).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(instance, value);
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
        return (uint)typeof(T).GetField("Id", BindingFlags.Instance | BindingFlags.Public)!.GetValue(entry)!;
    }

    private static IDisposable UseGameTableManager(IGameTableManager gameTableManager)
    {
        ISpellEffectDependencyResolver previousResolver =
            global::NexusForever.Game.Spell.SpellHandler.InitialiseDependencyResolver(new TestSpellEffectDependencyResolver(gameTableManager));
        return new DependencyResolverScope(previousResolver);
    }

    private sealed class DependencyResolverScope(ISpellEffectDependencyResolver previousResolver) : IDisposable
    {
        public void Dispose()
        {
            global::NexusForever.Game.Spell.SpellHandler.InitialiseDependencyResolver(previousResolver);
        }
    }

    private sealed class TestSpellEffectDependencyResolver(IGameTableManager gameTableManager) : ISpellEffectDependencyResolver
    {
        public IDamageCalculator CreateDamageCalculator()
        {
            return null;
        }

        public IEntityFactory GetEntityFactory()
        {
            return null;
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
            return null;
        }

        public IMapLockManager GetMapLockManager()
        {
            return null;
        }

        public IGlobalSpellManager GetGlobalSpellManager()
        {
            return null;
        }

        public IGameTableManager GetGameTableManager()
        {
            return gameTableManager;
        }
    }
}
