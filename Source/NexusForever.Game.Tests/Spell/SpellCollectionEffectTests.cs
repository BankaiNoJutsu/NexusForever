using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Unlock;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement.Force;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Spell;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Spell;

[Collection(MissingGameDataDiagnosticsCollection.Name)]
public class SpellCollectionEffectTests
{
    [Fact]
    public void LearnDyeColor_WithMissingGenericUnlockEntryTable_UsesInvalidUnlockPath()
    {
        MissingGameDataDiagnostics.ResetForTests();
        using IDisposable dependencyScope = UseGameTableManager(CreateGameTableManagerWithoutStaticTables());

        try
        {
            IPlayer player = CreatePlayer(out RecordingDispatchProxy<IGenericUnlockManager> unlockProxy);
            ISpell spell = CreateSpell(player);
            ISpellTargetEffectInfo info = CreateLearnDyeColorInfo(11u);

            global::NexusForever.Game.Spell.SpellHandler.HandleEffectLearnDyeColor(spell, player, info);

            RecordingDispatchProxy<IGenericUnlockManager>.Invocation unlock =
                Assert.Single(unlockProxy.GetInvocations(nameof(IGenericUnlockManager.Unlock)));
            Assert.Equal((ushort)11, unlock.Arguments[0]);
            Assert.Empty(unlockProxy.GetInvocations(nameof(IGenericUnlockManager.IsUnlocked)));
            AssertMissingTableDiagnostic("GenericUnlockEntry.tbl", "SpellHandler.HandleEffectLearnDyeColor");
        }
        finally
        {
            MissingGameDataDiagnostics.ResetForTests();
        }
    }

    // Retail: "Learn Dye - Red_Light1" is Spell4 46411 with LearnDyeColor DataBits00 = 2. GenericUnlockEntry 2 is the dye "Red_Dark2_Dull" (UnlockObject 11); the entry whose
    // UnlockObject is 2 is "Red_Light1". DataBits00 is therefore the dye colour, and the unlock must go to the entry that owns that colour.
    [Fact]
    public void LearnDyeColor_ResolvesTheUnlockEntryByDyeColourNotByEntryId()
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.GenericUnlockEntry), NexusForever.Game.Tests.Transport.PlayerBindPointTests.Table(
            new GenericUnlockEntryEntry { Id = 2u, GenericUnlockTypeEnum = NexusForever.Game.Static.Entity.GenericUnlockType.Dye, UnlockObject = 11u },
            new GenericUnlockEntryEntry { Id = 77u, GenericUnlockTypeEnum = NexusForever.Game.Static.Entity.GenericUnlockType.Dye, UnlockObject = 2u }));
        using IDisposable dependencyScope = UseGameTableManager(tables);

        IPlayer player = CreatePlayer(out RecordingDispatchProxy<IGenericUnlockManager> unlockProxy);
        ISpell spell = CreateSpell(player);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectLearnDyeColor(spell, player, CreateLearnDyeColorInfo(2u));

        RecordingDispatchProxy<IGenericUnlockManager>.Invocation unlock =
            Assert.Single(unlockProxy.GetInvocations(nameof(IGenericUnlockManager.Unlock)));
        Assert.Equal((ushort)77, unlock.Arguments[0]);
    }

    [Fact]
    public void LearnDyeColor_WithNoEntryForTheColour_DoesNotUnlockTheRowWithTheSameId()
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.GenericUnlockEntry), NexusForever.Game.Tests.Transport.PlayerBindPointTests.Table(
            new GenericUnlockEntryEntry { Id = 2u, GenericUnlockTypeEnum = NexusForever.Game.Static.Entity.GenericUnlockType.Dye, UnlockObject = 11u }));
        using IDisposable dependencyScope = UseGameTableManager(tables);

        IPlayer player = CreatePlayer(out RecordingDispatchProxy<IGenericUnlockManager> unlockProxy);
        ISpell spell = CreateSpell(player);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectLearnDyeColor(spell, player, CreateLearnDyeColorInfo(2u));

        Assert.Empty(unlockProxy.GetInvocations(nameof(IGenericUnlockManager.Unlock)));
    }

    // Retail: Spell4 83778 "Galeras - 4 - Q5324 - Chain Lock - Activate" has QuestAdvanceObjective with DataBits00 = 5324 (the quest), DataBits01 = 0 (objective index), DataBits02 = 1.
    [Fact]
    public void QuestAdvanceObjective_AdvancesTheIndexedObjectiveOfTheNamedQuest()
    {
        IQuestObjectiveInfo firstInfo = RecordingDispatchProxy<IQuestObjectiveInfo>.Create(out RecordingDispatchProxy<IQuestObjectiveInfo> firstInfoProxy);
        firstInfoProxy.SetProperty(nameof(IQuestObjectiveInfo.Id), 9001u);
        IQuestObjectiveInfo secondInfo = RecordingDispatchProxy<IQuestObjectiveInfo>.Create(out RecordingDispatchProxy<IQuestObjectiveInfo> secondInfoProxy);
        secondInfoProxy.SetProperty(nameof(IQuestObjectiveInfo.Id), 9002u);

        IQuestObjective first = RecordingDispatchProxy<IQuestObjective>.Create(out RecordingDispatchProxy<IQuestObjective> firstProxy);
        firstProxy.SetProperty(nameof(IQuestObjective.Index), (byte)0);
        firstProxy.SetProperty(nameof(IQuestObjective.ObjectiveInfo), firstInfo);
        IQuestObjective second = RecordingDispatchProxy<IQuestObjective>.Create(out RecordingDispatchProxy<IQuestObjective> secondProxy);
        secondProxy.SetProperty(nameof(IQuestObjective.Index), (byte)1);
        secondProxy.SetProperty(nameof(IQuestObjective.ObjectiveInfo), secondInfo);

        IQuest quest = RecordingDispatchProxy<IQuest>.Create(out RecordingDispatchProxy<IQuest> questProxy);
        questProxy.SetProperty(nameof(IQuest.Id), (ushort)5324);
        questProxy.SetMethodHandler("GetEnumerator", _ => new List<IQuestObjective> { first, second }.GetEnumerator());
        IQuest otherQuest = RecordingDispatchProxy<IQuest>.Create(out RecordingDispatchProxy<IQuest> otherQuestProxy);
        otherQuestProxy.SetProperty(nameof(IQuest.Id), (ushort)77);

        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out RecordingDispatchProxy<IQuestManager> questManagerProxy);
        questManagerProxy.SetMethodHandler(nameof(IQuestManager.GetActiveQuests), _ => new List<IQuest> { otherQuest, quest });
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 42u);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);

        ISpell spell = CreateSpell(player);
        ISpellTargetEffectInfo info = RecordingDispatchProxy<ISpellTargetEffectInfo>.Create(out RecordingDispatchProxy<ISpellTargetEffectInfo> infoProxy);
        infoProxy.SetProperty(nameof(ISpellTargetEffectInfo.Entry), new Spell4EffectsEntry
        {
            Id         = 701u,
            SpellId    = 83778u,
            EffectType = SpellEffectType.QuestAdvanceObjective,
            DataBits00 = 5324u,
            DataBits01 = 1u,
            DataBits02 = 2u
        });

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectQuestAdvanceObjective(spell, player, info);

        // the quest's own objective at index 1 advances by the amount; the id is never used as an objective id on other quests
        RecordingDispatchProxy<IQuest>.Invocation update = Assert.Single(questProxy.GetInvocations(nameof(IQuest.ObjectiveUpdate)));
        Assert.Equal(9002u, update.Arguments[0]);
        Assert.Equal(2u, update.Arguments[1]);
        Assert.Empty(otherQuestProxy.GetInvocations(nameof(IQuest.ObjectiveUpdate)));
        Assert.Empty(questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
    }

    [Fact]
    public void UnlockMount_WithMissingSpell4Table_DoesNotAddSpellOrEmitUnlock()
    {
        MissingGameDataDiagnostics.ResetForTests();
        using IDisposable dependencyScope = UseGameTableManager(CreateGameTableManagerWithoutStaticTables());

        try
        {
            IPlayer player = CreateCollectionPlayer(
                out RecordingDispatchProxy<ISpellManager> spellManagerProxy,
                out RecordingDispatchProxy<IGameSession> sessionProxy);
            ISpell spell = CreateSpell(player);
            ISpellTargetEffectInfo info = CreateCollectionInfo(SpellEffectType.UnlockMount, 44u);

            global::NexusForever.Game.Spell.SpellHandler.HandleEffectUnlockMount(spell, player, info);

            Assert.Empty(spellManagerProxy.GetInvocations(nameof(ISpellManager.GetSpell)));
            Assert.Empty(spellManagerProxy.GetInvocations(nameof(ISpellManager.AddSpell)));
            Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
            AssertSkippedGrantDiagnostic("Spell4.tbl", 44u, "Spell mount unlock");
        }
        finally
        {
            MissingGameDataDiagnostics.ResetForTests();
        }
    }

    [Fact]
    public void UnlockMount_WithKnownSpell4_AddsBaseSpellAndEmitsUnlock()
    {
        using IDisposable dependencyScope = UseGameTableManager(CreateGameTableManager(new Spell4Entry
            {
                Id                     = 44u,
                Spell4BaseIdBaseSpell = 440u
            }));

        IPlayer player = CreateCollectionPlayer(
            out RecordingDispatchProxy<ISpellManager> spellManagerProxy,
            out RecordingDispatchProxy<IGameSession> sessionProxy);
        ISpell spell = CreateSpell(player);
        ISpellTargetEffectInfo info = CreateCollectionInfo(SpellEffectType.UnlockMount, 44u);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectUnlockMount(spell, player, info);

        RecordingDispatchProxy<ISpellManager>.Invocation getSpell =
            Assert.Single(spellManagerProxy.GetInvocations(nameof(ISpellManager.GetSpell)));
        Assert.Equal(440u, getSpell.Arguments[0]);

        RecordingDispatchProxy<ISpellManager>.Invocation addSpell =
            Assert.Single(spellManagerProxy.GetInvocations(nameof(ISpellManager.AddSpell)));
        Assert.Equal(440u, addSpell.Arguments[0]);

        RecordingDispatchProxy<IGameSession>.Invocation sessionCall =
            Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        ServerUnlockMount unlockMount = Assert.IsType<ServerUnlockMount>(sessionCall.Arguments[0]);
        Assert.Equal(44u, unlockMount.Spell4Id);
    }

    [Fact]
    public void UnlockPetFlair_WithMissingPetFlairTable_DoesNotUnlockFlair()
    {
        MissingGameDataDiagnostics.ResetForTests();
        using IDisposable dependencyScope = UseGameTableManager(CreateGameTableManagerWithoutStaticTables());

        try
        {
            IPlayer player = CreatePetFlairPlayer(out RecordingDispatchProxy<IPetCustomisationManager> petProxy);
            ISpell spell = CreateSpell(player);
            ISpellTargetEffectInfo info = CreateCollectionInfo(SpellEffectType.UnlockPetFlair, 12u);

            global::NexusForever.Game.Spell.SpellHandler.HandleEffectUnlockPetFlair(spell, player, info);

            Assert.Empty(petProxy.GetInvocations(nameof(IPetCustomisationManager.HasFlair)));
            Assert.Empty(petProxy.GetInvocations(nameof(IPetCustomisationManager.UnlockFlair)));
            AssertSkippedGrantDiagnostic("PetFlair.tbl", 12u, "Spell pet flair unlock");
        }
        finally
        {
            MissingGameDataDiagnostics.ResetForTests();
        }
    }

    [Fact]
    public void UnlockPetFlair_WithKnownPetFlair_UnlocksFlair()
    {
        using IDisposable dependencyScope = UseGameTableManager(CreateGameTableManagerWithPetFlair(new PetFlairEntry
            {
                Id = 12u
            }));

        IPlayer player = CreatePetFlairPlayer(out RecordingDispatchProxy<IPetCustomisationManager> petProxy);
        ISpell spell = CreateSpell(player);
        ISpellTargetEffectInfo info = CreateCollectionInfo(SpellEffectType.UnlockPetFlair, 12u);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectUnlockPetFlair(spell, player, info);

        RecordingDispatchProxy<IPetCustomisationManager>.Invocation hasFlair =
            Assert.Single(petProxy.GetInvocations(nameof(IPetCustomisationManager.HasFlair)));
        Assert.Equal((ushort)12, hasFlair.Arguments[0]);

        RecordingDispatchProxy<IPetCustomisationManager>.Invocation unlockFlair =
            Assert.Single(petProxy.GetInvocations(nameof(IPetCustomisationManager.UnlockFlair)));
        Assert.Equal((ushort)12, unlockFlair.Arguments[0]);
    }

    [Fact]
    public void TitleGrant_WithMissingCharacterTitleTable_DoesNotAddTitle()
    {
        MissingGameDataDiagnostics.ResetForTests();
        using IDisposable dependencyScope = UseGameTableManager(CreateGameTableManagerWithoutStaticTables());

        try
        {
            IPlayer player = CreateTitlePlayer(out RecordingDispatchProxy<ITitleManager> titleProxy);
            ISpell spell = CreateSpell(player);
            ISpellTargetEffectInfo info = CreateCollectionInfo(SpellEffectType.TitleGrant, 21u);

            global::NexusForever.Game.Spell.SpellHandler.HandleEffectTitleGrant(spell, player, info);

            Assert.Empty(titleProxy.GetInvocations(nameof(ITitleManager.HasTitle)));
            Assert.Empty(titleProxy.GetInvocations(nameof(ITitleManager.AddTitle)));
            AssertSkippedGrantDiagnostic("CharacterTitle.tbl", 21u, "Spell title grant");
        }
        finally
        {
            MissingGameDataDiagnostics.ResetForTests();
        }
    }

    [Fact]
    public void TitleGrant_WithKnownCharacterTitle_AddsTitle()
    {
        using IDisposable dependencyScope = UseGameTableManager(CreateGameTableManagerWithCharacterTitle(new CharacterTitleEntry
            {
                Id = 21u
            }));

        IPlayer player = CreateTitlePlayer(out RecordingDispatchProxy<ITitleManager> titleProxy);
        ISpell spell = CreateSpell(player);
        ISpellTargetEffectInfo info = CreateCollectionInfo(SpellEffectType.TitleGrant, 21u);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectTitleGrant(spell, player, info);

        RecordingDispatchProxy<ITitleManager>.Invocation hasTitle =
            Assert.Single(titleProxy.GetInvocations(nameof(ITitleManager.HasTitle)));
        Assert.Equal((ushort)21, hasTitle.Arguments[0]);

        RecordingDispatchProxy<ITitleManager>.Invocation addTitle =
            Assert.Single(titleProxy.GetInvocations(nameof(ITitleManager.AddTitle)));
        Assert.Equal((ushort)21, addTitle.Arguments[0]);
    }

    [Fact]
    public void TitleRevoke_WithMissingCharacterTitleTable_DoesNotRevokeTitle()
    {
        MissingGameDataDiagnostics.ResetForTests();
        using IDisposable dependencyScope = UseGameTableManager(CreateGameTableManagerWithoutStaticTables());

        try
        {
            IPlayer player = CreateTitlePlayer(out RecordingDispatchProxy<ITitleManager> titleProxy);
            ISpell spell = CreateSpell(player);
            ISpellTargetEffectInfo info = CreateCollectionInfo(SpellEffectType.TitleRevoke, 21u);

            global::NexusForever.Game.Spell.SpellHandler.HandleEffectTitleRevoke(spell, player, info);

            Assert.Empty(titleProxy.GetInvocations(nameof(ITitleManager.HasTitle)));
            Assert.Empty(titleProxy.GetInvocations(nameof(ITitleManager.RevokeTitle)));
            AssertMissingTableDiagnostic("CharacterTitle.tbl", "SpellHandler.HandleEffectTitleRevoke");
        }
        finally
        {
            MissingGameDataDiagnostics.ResetForTests();
        }
    }

    [Fact]
    public void TitleRevoke_WithKnownOwnedCharacterTitle_RevokesTitle()
    {
        using IDisposable dependencyScope = UseGameTableManager(CreateGameTableManagerWithCharacterTitle(new CharacterTitleEntry
            {
                Id = 21u
            }));

        IPlayer player = CreateTitlePlayer(out RecordingDispatchProxy<ITitleManager> titleProxy);
        titleProxy.SetMethodReturn(nameof(ITitleManager.HasTitle), true);
        ISpell spell = CreateSpell(player);
        ISpellTargetEffectInfo info = CreateCollectionInfo(SpellEffectType.TitleRevoke, 21u);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectTitleRevoke(spell, player, info);

        RecordingDispatchProxy<ITitleManager>.Invocation hasTitle =
            Assert.Single(titleProxy.GetInvocations(nameof(ITitleManager.HasTitle)));
        Assert.Equal((ushort)21, hasTitle.Arguments[0]);

        RecordingDispatchProxy<ITitleManager>.Invocation revokeTitle =
            Assert.Single(titleProxy.GetInvocations(nameof(ITitleManager.RevokeTitle)));
        Assert.Equal((ushort)21, revokeTitle.Arguments[0]);
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    public void ZeroLocationTeleportRecallsOnlyBoundPlayersWithoutPendingTeleport(bool canTeleport, bool bound, bool expected)
    {
        var player = RecordingDispatchProxy<IPlayer>.Create(out var pp);
        pp.SetMethodReturn(nameof(IPlayer.CanTeleport), canTeleport);
        pp.SetMethodHandler(nameof(IPlayer.TryGetBindPoint), args =>
        {
            args[0] = (ushort)51;
            args[1] = new System.Numerics.Vector3(4, 5, 6);
            return bound;
        });
        var info = CreateCollectionInfo(SpellEffectType.Teleport, 0);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectTeleport(CreateSpell(player), player, info);

        var calls = pp.GetInvocations(nameof(IPlayer.TeleportTo));
        Assert.Equal(expected ? 1 : 0, calls.Count);
        if (expected)
        {
            Assert.Equal((ushort)51, calls[0].Arguments[0]);
            Assert.Equal(4f, calls[0].Arguments[1]);
            Assert.Equal(5f, calls[0].Arguments[2]);
            Assert.Equal(6f, calls[0].Arguments[3]);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SummonMountReservesOnlyAcceptedSummonsAndRetainsExplicitAuraCooldownBypass(bool canEnter)
    {
        var player = RecordingDispatchProxy<IPlayer>.Create(out var pp);
        var map = RecordingDispatchProxy<NexusForever.Game.Abstract.Map.IBaseMap>.Create(out var mp);
        pp.SetProperty(nameof(IPlayer.Guid), 42u);
        pp.SetProperty(nameof(IPlayer.Map), map);
        pp.SetMethodReturn(nameof(IPlayer.CanMount), true);
        mp.SetMethodReturn(nameof(NexusForever.Game.Abstract.Map.IBaseMap.CanEnter), canEnter);
        var mount = RecordingDispatchProxy<IMountEntity>.Create(out _);
        var factory = RecordingDispatchProxy<IEntityFactory>.Create(out var fp);
        fp.SetMethodReturn(nameof(IEntityFactory.CreateEntity), mount);
        var tables = CreateGameTableManagerWithoutStaticTables();
        SetAutoProperty(tables, nameof(GameTableManager.Creature2), CreateGameTable(new Creature2Entry { Id = 11 }));
        using var scope = UseGameTableManager(tables, factory);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectSummonMount(CreateSpell(player), player,
            CreateCollectionInfo(SpellEffectType.SummonMount, 11));

        Assert.Equal(canEnter ? 1 : 0, pp.GetInvocations(nameof(IPlayer.SetPendingMount)).Count);
        Assert.Equal(canEnter ? 1 : 0, mp.GetInvocations(nameof(NexusForever.Game.Abstract.Map.IBaseMap.EnqueueAdd)).Count);
        var casts = pp.GetInvocations(nameof(IPlayer.CastSpell));
        Assert.Equal(canEnter ? 2 : 0, casts.Count);
        foreach (var cast in casts)
        {
            var parameters = Assert.IsAssignableFrom<ISpellParameters>(cast.Arguments[1]);
            Assert.True(parameters.IgnoreGlobalCooldown);
            Assert.False(parameters.UserInitiatedSpellCast);
            Assert.Equal(42u, parameters.PrimaryTargetId);
        }
    }

    [Theory]
    [InlineData(0u, false, (byte)1)]
    [InlineData(2u, false, (byte)2)]
    [InlineData(255u, false, (byte)255)]
    [InlineData(2u, true, (byte)2)]
    public void AddSpellPreservesLearnedTierAndRequestsPassiveApplicationWithoutDuplicateGrant(uint tier, bool known, byte expectedTier)
    {
        var tables = CreateGameTableManager(new Spell4Entry { Id = 700, Spell4BaseIdBaseSpell = 70, TierIndex = tier });
        using var scope = UseGameTableManager(tables);
        var player = CreateCollectionPlayer(out var spells, out _);
        if (known) spells.SetMethodReturn(nameof(ISpellManager.GetSpell), RecordingDispatchProxy<ICharacterSpell>.Create(out _));

        Game.Spell.SpellHandler.HandleEffectAddSpell(CreateSpell(player), player, CreateCollectionInfo(SpellEffectType.AddSpell, 700));

        var grants = spells.GetInvocations(nameof(ISpellManager.AddSpell));
        Assert.Equal(known ? 0 : 1, grants.Count);
        if (!known)
        {
            Assert.Equal(70u, grants[0].Arguments[0]);
            Assert.Equal(expectedTier, grants[0].Arguments[1]);
        }
        Assert.Equal(700u, Assert.Single(spells.GetInvocations(nameof(ISpellManager.TryApplyLearnedPassive))).Arguments[0]);
    }

    [Theory]
    [InlineData(0u, 1u)]
    [InlineData(70u, 256u)]
    public void AddSpellRejectsInvalidBaseOrUnrepresentableTier(uint baseId, uint tier)
    {
        using var scope = UseGameTableManager(CreateGameTableManager(new Spell4Entry { Id = 700, Spell4BaseIdBaseSpell = baseId, TierIndex = tier }));
        var player = CreateCollectionPlayer(out var spells, out _);
        Game.Spell.SpellHandler.HandleEffectAddSpell(CreateSpell(player), player, CreateCollectionInfo(SpellEffectType.AddSpell, 700));
        Assert.Empty(spells.GetInvocations(nameof(ISpellManager.AddSpell)));
        Assert.Empty(spells.GetInvocations(nameof(ISpellManager.TryApplyLearnedPassive)));
    }

    [Theory]
    [InlineData("owned")]
    [InlineData("pending")]
    [InlineData("missing-entrance")]
    public void HousingRecallRetainsParentInstanceEntranceAndPendingTransferGuards(string scenario)
    {
        var residences = RecordingDispatchProxy<IGlobalResidenceManager>.Create(out var rp);
        var residence = RecordingDispatchProxy<IResidence>.Create(out var residenceProxy);
        var parent = RecordingDispatchProxy<IResidence>.Create(out _);
        residenceProxy.SetProperty(nameof(IResidence.Parent), parent);
        rp.SetMethodReturn(nameof(IGlobalResidenceManager.GetResidenceByOwner), residence);
        var entrance = RecordingDispatchProxy<IResidenceEntrance>.Create(out var ep);
        var world = new WorldEntry { Id = 51 };
        ep.SetProperty(nameof(IResidenceEntrance.Entry), world);
        ep.SetProperty(nameof(IResidenceEntrance.Position), new System.Numerics.Vector3(4, 5, 6));
        ep.SetProperty(nameof(IResidenceEntrance.Rotation), System.Numerics.Quaternion.Identity);
        if (scenario == "missing-entrance")
            rp.SetMethodHandler(nameof(IGlobalResidenceManager.GetResidenceEntrance), _ => throw new Game.Housing.HousingException("missing"));
        else
            rp.SetMethodReturn(nameof(IGlobalResidenceManager.GetResidenceEntrance), entrance);
        var locks = RecordingDispatchProxy<IMapLockManager>.Create(out var lp);
        var mapLock = RecordingDispatchProxy<Game.Map.Lock.IResidenceMapLock>.Create(out _);
        lp.SetMethodReturn(nameof(IMapLockManager.GetResidenceLock), mapLock);
        using var scope = UseGameTableManager(CreateGameTableManagerWithoutStaticTables(), residences: residences, locks: locks);
        var player = CreateCollectionPlayer(out _, out _);
        var pp = (RecordingDispatchProxy<IPlayer>)(object)player;
        pp.SetMethodReturn(nameof(IPlayer.CanTeleport), scenario != "pending");

        Game.Spell.SpellHandler.HandleEffectHousingTeleport(CreateSpell(player), player, CreateCollectionInfo(SpellEffectType.HousingTeleport, 0));

        var calls = pp.GetInvocations(nameof(IPlayer.TeleportTo));
        Assert.Equal(scenario == "owned" ? 1 : 0, calls.Count);
        if (scenario == "owned")
        {
            Assert.Same(world, calls[0].Arguments[0]);
            Assert.Equal(new System.Numerics.Vector3(4, 5, 6), calls[0].Arguments[1]);
            Assert.Same(mapLock, calls[0].Arguments[2]);
            Assert.Same(parent, Assert.Single(lp.GetInvocations(nameof(IMapLockManager.GetResidenceLock))).Arguments[0]);
        }
        Assert.Empty(rp.GetInvocations(nameof(IGlobalResidenceManager.CreateResidence)));
    }

    private static IDisposable UseGameTableManager(IGameTableManager gameTableManager, IEntityFactory factory = null,
        IGlobalResidenceManager residences = null, IMapLockManager locks = null)
    {
        ISpellEffectDependencyResolver previousResolver =
            global::NexusForever.Game.Spell.SpellHandler.InitialiseDependencyResolver(new TestSpellEffectDependencyResolver(gameTableManager, factory, residences, locks));
        return new DependencyResolverScope(previousResolver);
    }

    private sealed class DependencyResolverScope(ISpellEffectDependencyResolver previousResolver) : IDisposable
    {
        public void Dispose()
        {
            global::NexusForever.Game.Spell.SpellHandler.InitialiseDependencyResolver(previousResolver);
        }
    }

    private sealed class TestSpellEffectDependencyResolver(IGameTableManager gameTableManager, IEntityFactory factory = null,
        IGlobalResidenceManager residences = null, IMapLockManager locks = null) : ISpellEffectDependencyResolver
    {
        public IDamageCalculator CreateDamageCalculator()
        {
            return null;
        }

        public IEntityFactory GetEntityFactory()
        {
            return factory;
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
            return residences;
        }

        public IGlobalLootManager GetGlobalLootManager()
        {
            return null;
        }

        public IMapLockManager GetMapLockManager()
        {
            return locks;
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

    private static IPlayer CreatePlayer(out RecordingDispatchProxy<IGenericUnlockManager> unlockProxy)
    {
        IGenericUnlockManager unlockManager = RecordingDispatchProxy<IGenericUnlockManager>.Create(out unlockProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);

        accountProxy.SetProperty(nameof(IAccount.GenericUnlockManager), unlockManager);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 42u);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);
        return player;
    }

    private static IPlayer CreateCollectionPlayer(
        out RecordingDispatchProxy<ISpellManager> spellManagerProxy,
        out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        ISpellManager spellManager = RecordingDispatchProxy<ISpellManager>.Create(out spellManagerProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);

        playerProxy.SetProperty(nameof(IPlayer.Guid), 42u);
        playerProxy.SetProperty(nameof(IPlayer.SpellManager), spellManager);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        return player;
    }

    private static IPlayer CreatePetFlairPlayer(out RecordingDispatchProxy<IPetCustomisationManager> petProxy)
    {
        IPetCustomisationManager petCustomisationManager = RecordingDispatchProxy<IPetCustomisationManager>.Create(out petProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);

        playerProxy.SetProperty(nameof(IPlayer.Guid), 42u);
        playerProxy.SetProperty(nameof(IPlayer.PetCustomisationManager), petCustomisationManager);
        return player;
    }

    private static IPlayer CreateTitlePlayer(out RecordingDispatchProxy<ITitleManager> titleProxy)
    {
        ITitleManager titleManager = RecordingDispatchProxy<ITitleManager>.Create(out titleProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);

        playerProxy.SetProperty(nameof(IPlayer.Guid), 42u);
        playerProxy.SetProperty(nameof(IPlayer.TitleManager), titleManager);
        return player;
    }

    private static ISpell CreateSpell(IPlayer caster)
    {
        ISpellBaseInfo baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out RecordingDispatchProxy<ISpellBaseInfo> baseInfoProxy);
        baseInfoProxy.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry
        {
            Id = 70u
        });

        ISpellInfo spellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out RecordingDispatchProxy<ISpellInfo> spellInfoProxy);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry
        {
            Id = 700u
        });
        spellInfoProxy.SetProperty(nameof(ISpellInfo.BaseInfo), baseInfo);

        ISpellParameters parameters = RecordingDispatchProxy<ISpellParameters>.Create(out RecordingDispatchProxy<ISpellParameters> parametersProxy);
        parametersProxy.SetProperty(nameof(ISpellParameters.SpellInfo), spellInfo);

        ISpell spell = RecordingDispatchProxy<ISpell>.Create(out RecordingDispatchProxy<ISpell> spellProxy);
        spellProxy.SetProperty(nameof(ISpell.Caster), caster);
        spellProxy.SetProperty(nameof(ISpell.CastingId), 7u);
        spellProxy.SetProperty(nameof(ISpell.Parameters), parameters);
        return spell;
    }

    private static ISpellTargetEffectInfo CreateLearnDyeColorInfo(uint genericUnlockEntryId)
    {
        return CreateCollectionInfo(SpellEffectType.LearnDyeColor, genericUnlockEntryId);
    }

    private static ISpellTargetEffectInfo CreateCollectionInfo(SpellEffectType effectType, uint objectId)
    {
        ISpellTargetEffectInfo info = RecordingDispatchProxy<ISpellTargetEffectInfo>.Create(out RecordingDispatchProxy<ISpellTargetEffectInfo> infoProxy);
        infoProxy.SetProperty(nameof(ISpellTargetEffectInfo.Entry), new Spell4EffectsEntry
        {
            Id         = 701u,
            SpellId    = 700u,
            EffectType = effectType,
            DataBits00 = objectId
        });
        return info;
    }

    private static void AssertMissingTableDiagnostic(string tableName, string context)
    {
        MissingGameDataDiagnostic diagnostic = Assert.Single(MissingGameDataDiagnostics.GetSnapshot(), d =>
            d.Kind == MissingGameDataDiagnosticKind.MissingTable
            && d.Severity == MissingGameDataSeverity.PlayerImpacting
            && d.TableName == tableName
            && d.Context == context);

        Assert.Equal(1, diagnostic.Count);
    }

    private static void AssertSkippedGrantDiagnostic(string tableName, uint staticId, string detail)
    {
        MissingGameDataDiagnostic diagnostic = Assert.Single(MissingGameDataDiagnostics.GetSnapshot(), d =>
            d.Kind == MissingGameDataDiagnosticKind.SkippedGrant
            && d.Severity == MissingGameDataSeverity.PlayerImpacting
            && d.TableName == tableName
            && d.StaticId == staticId.ToString()
            && d.Detail.Contains(detail));

        Assert.Equal(1, diagnostic.Count);
    }

    private static GameTableManager CreateGameTableManagerWithoutStaticTables()
    {
        return (GameTableManager)RuntimeHelpers.GetUninitializedObject(typeof(GameTableManager));
    }

    private static GameTableManager CreateGameTableManager(params Spell4Entry[] spell4Entries)
    {
        var manager = (GameTableManager)RuntimeHelpers.GetUninitializedObject(typeof(GameTableManager));
        SetAutoProperty(manager, nameof(GameTableManager.Spell4), CreateGameTable(spell4Entries));
        return manager;
    }

    private static GameTableManager CreateGameTableManagerWithPetFlair(params PetFlairEntry[] petFlairEntries)
    {
        var manager = (GameTableManager)RuntimeHelpers.GetUninitializedObject(typeof(GameTableManager));
        SetAutoProperty(manager, nameof(GameTableManager.PetFlair), CreateGameTable(petFlairEntries));
        return manager;
    }

    private static GameTableManager CreateGameTableManagerWithCharacterTitle(params CharacterTitleEntry[] characterTitleEntries)
    {
        var manager = (GameTableManager)RuntimeHelpers.GetUninitializedObject(typeof(GameTableManager));
        SetAutoProperty(manager, nameof(GameTableManager.CharacterTitle), CreateGameTable(characterTitleEntries));
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

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        backingField.SetValue(instance, value);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(instance, value);
    }

    private static int[] BuildLookup<T>(IReadOnlyList<T> entries)
    {
        if (entries.Count == 0)
            return [];

        var lookup = new int[(int)(entries.Max(GetEntryId) + 1u)];
        Array.Fill(lookup, -1);
        for (int i = 0; i < entries.Count; i++)
            lookup[(int)GetEntryId(entries[i])] = i;

        return lookup;
    }

    private static uint GetEntryId<T>(T entry)
    {
        FieldInfo idField = typeof(T).GetField("Id", BindingFlags.Instance | BindingFlags.Public);
        return (uint)idField.GetValue(entry);
    }
}
