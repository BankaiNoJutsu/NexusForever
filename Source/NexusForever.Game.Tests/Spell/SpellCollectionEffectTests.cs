using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Unlock;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Spell;

[Collection(LegacyServiceProviderCollection.Name)]
public class SpellCollectionEffectTests
{
    [Fact]
    public void LearnDyeColor_WithMissingGenericUnlockEntryTable_UsesInvalidUnlockPath()
    {
        MissingGameDataDiagnostics.ResetForTests();
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = new ServiceCollection()
            .AddSingleton(CreateGameTableManagerWithoutStaticTables())
            .BuildServiceProvider();

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
            LegacyServiceProvider.Provider = previousProvider;
            MissingGameDataDiagnostics.ResetForTests();
        }
    }

    [Fact]
    public void UnlockMount_WithMissingSpell4Table_DoesNotAddSpellOrEmitUnlock()
    {
        MissingGameDataDiagnostics.ResetForTests();
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = new ServiceCollection()
            .AddSingleton(CreateGameTableManagerWithoutStaticTables())
            .BuildServiceProvider();

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
            LegacyServiceProvider.Provider = previousProvider;
            MissingGameDataDiagnostics.ResetForTests();
        }
    }

    [Fact]
    public void UnlockMount_WithKnownSpell4_AddsBaseSpellAndEmitsUnlock()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = new ServiceCollection()
            .AddSingleton(CreateGameTableManager(new Spell4Entry
            {
                Id                     = 44u,
                Spell4BaseIdBaseSpell = 440u
            }))
            .BuildServiceProvider();

        try
        {
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
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void UnlockPetFlair_WithMissingPetFlairTable_DoesNotUnlockFlair()
    {
        MissingGameDataDiagnostics.ResetForTests();
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = new ServiceCollection()
            .AddSingleton(CreateGameTableManagerWithoutStaticTables())
            .BuildServiceProvider();

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
            LegacyServiceProvider.Provider = previousProvider;
            MissingGameDataDiagnostics.ResetForTests();
        }
    }

    [Fact]
    public void UnlockPetFlair_WithKnownPetFlair_UnlocksFlair()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = new ServiceCollection()
            .AddSingleton(CreateGameTableManagerWithPetFlair(new PetFlairEntry
            {
                Id = 12u
            }))
            .BuildServiceProvider();

        try
        {
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
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void TitleGrant_WithMissingCharacterTitleTable_DoesNotAddTitle()
    {
        MissingGameDataDiagnostics.ResetForTests();
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = new ServiceCollection()
            .AddSingleton(CreateGameTableManagerWithoutStaticTables())
            .BuildServiceProvider();

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
            LegacyServiceProvider.Provider = previousProvider;
            MissingGameDataDiagnostics.ResetForTests();
        }
    }

    [Fact]
    public void TitleGrant_WithKnownCharacterTitle_AddsTitle()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = new ServiceCollection()
            .AddSingleton(CreateGameTableManagerWithCharacterTitle(new CharacterTitleEntry
            {
                Id = 21u
            }))
            .BuildServiceProvider();

        try
        {
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
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void TitleRevoke_WithMissingCharacterTitleTable_DoesNotRevokeTitle()
    {
        MissingGameDataDiagnostics.ResetForTests();
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = new ServiceCollection()
            .AddSingleton(CreateGameTableManagerWithoutStaticTables())
            .BuildServiceProvider();

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
            LegacyServiceProvider.Provider = previousProvider;
            MissingGameDataDiagnostics.ResetForTests();
        }
    }

    [Fact]
    public void TitleRevoke_WithKnownOwnedCharacterTitle_RevokesTitle()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = new ServiceCollection()
            .AddSingleton(CreateGameTableManagerWithCharacterTitle(new CharacterTitleEntry
            {
                Id = 21u
            }))
            .BuildServiceProvider();

        try
        {
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
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
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
