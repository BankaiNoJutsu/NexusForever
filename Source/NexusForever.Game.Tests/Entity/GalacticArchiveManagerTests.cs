using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.GalacticArchive;
using NexusForever.Shared;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.GalacticArchive;

namespace NexusForever.Game.Tests.Entity;

[Collection(MissingGameDataDiagnosticsCollection.Name)]
public class GalacticArchiveManagerTests
{
    private const uint ArticleUnlockedFlag = 0x80000000u;
    private const ulong CharacterId = 42ul;
    private const uint ParentArticleId = 1001u;
    private const uint ChildArticleId = 1002u;

    [Fact]
    public void UnlockLinkedArticle_WithUnlockedParent_UnlocksChildWithoutRewards()
    {
        GameTableManager gameTableManager = CreateGameTableManager(
            [CreateArticle(ParentArticleId), CreateArticle(ChildArticleId)],
            [CreateLink(ParentArticleId, ChildArticleId)]);
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<IGameSession> sessionProxy);
        var model = CreateModelWithUnlockedArticle(ParentArticleId);
        var manager = new GalacticArchiveManager(player, model, gameTableManager);

        bool unlocked = manager.UnlockLinkedArticle(ChildArticleId);

        Assert.True(unlocked);
        ServerGalacticArchiveUpdate update = Assert.Single(GetMessages<ServerGalacticArchiveUpdate>(sessionProxy));
        Assert.Equal(ChildArticleId, update.ArchiveArticleId);
        Assert.Equal(ArticleUnlockedFlag, update.UnlockedFlags);
    }

    [Fact]
    public void UnlockLinkedArticle_WithoutUnlockedParent_RejectsChild()
    {
        GameTableManager gameTableManager = CreateGameTableManager(
            [CreateArticle(ParentArticleId), CreateArticle(ChildArticleId)],
            [CreateLink(ParentArticleId, ChildArticleId)]);
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<IGameSession> sessionProxy);
        var manager = new GalacticArchiveManager(player, new CharacterModel { Id = CharacterId }, gameTableManager);

        bool unlocked = manager.UnlockLinkedArticle(ChildArticleId);

        Assert.False(unlocked);
        Assert.Empty(GetMessages<ServerGalacticArchiveUpdate>(sessionProxy));
    }

    [Fact]
    public void ClientGalacticArchiveUnlockHandler_DelegatesToLinkedUnlock()
    {
        IGalacticArchiveManager archiveManager = RecordingDispatchProxy<IGalacticArchiveManager>.Create(out var archiveProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.GalacticArchiveManager), archiveManager);

        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out var sessionProxy);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);

        var handler = new ClientGalacticArchiveUnlockHandler(NullLogger<ClientGalacticArchiveUnlockHandler>.Instance);
        handler.HandleMessage(session, CreateClientUnlock(ChildArticleId));

        RecordingDispatchProxy<IGalacticArchiveManager>.Invocation linkedCall = Assert.Single(
            archiveProxy.GetInvocations(nameof(IGalacticArchiveManager.UnlockLinkedArticle)));
        Assert.Equal(ChildArticleId, linkedCall.Arguments[0]);
        Assert.Empty(archiveProxy.GetInvocations(nameof(IGalacticArchiveManager.UnlockArticle)));
    }

    [Fact]
    public void Constructor_WhenArchiveArticleTableMissing_SkipsPersistedArchiveState()
    {
        GameTableManager gameTableManager = CreateGameTableManager();
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<IGameSession> sessionProxy);
        var manager = new GalacticArchiveManager(player, CreateModelWithUnlockedArticle(ParentArticleId), gameTableManager);

        manager.SendInitialPackets();

        Assert.Single(GetMessages<ServerGalacticArchiveRefresh>(sessionProxy));
        Assert.Empty(GetMessages<ServerGalacticArchiveUpdate>(sessionProxy));
    }

    [Fact]
    public void UnlockArticle_WhenArchiveArticleTableMissing_ReturnsFalse()
    {
        GameTableManager gameTableManager = CreateGameTableManager();
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<IGameSession> sessionProxy);
        var manager = new GalacticArchiveManager(player, new CharacterModel { Id = CharacterId }, gameTableManager);

        bool unlocked = manager.UnlockArticle(ParentArticleId);

        Assert.False(unlocked);
        Assert.Empty(GetMessages<ServerGalacticArchiveUpdate>(sessionProxy));
    }

    [Fact]
    public void SendInitialPackets_WhenRuleTableMissing_SendsRefreshWithoutRuleUnlocks()
    {
        GameTableManager gameTableManager = CreateGameTableManager(
            [CreateArticle(ParentArticleId, entryId: 500u)],
            []);
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<IGameSession> sessionProxy);
        var manager = new GalacticArchiveManager(player, new CharacterModel { Id = CharacterId }, gameTableManager);

        manager.SendInitialPackets();

        Assert.Single(GetMessages<ServerGalacticArchiveRefresh>(sessionProxy));
        Assert.Empty(GetMessages<ServerGalacticArchiveUpdate>(sessionProxy));
    }

    [Fact]
    public void UnlockArticle_WhenArchiveEntryTableMissing_SkipsEntryRewards()
    {
        MissingGameDataDiagnostics.ResetForTests();
        try
        {
            GameTableManager gameTableManager = CreateGameTableManager(
                [CreateArticle(ParentArticleId, entryId: 500u)],
                []);
            IPlayer player = CreatePlayer(out RecordingDispatchProxy<IGameSession> sessionProxy);
            var manager = new GalacticArchiveManager(player, new CharacterModel { Id = CharacterId }, gameTableManager);

            bool unlocked = manager.UnlockArticle(ParentArticleId);

            Assert.True(unlocked);
            ServerGalacticArchiveUpdate update = Assert.Single(GetMessages<ServerGalacticArchiveUpdate>(sessionProxy));
            Assert.Equal(ParentArticleId, update.ArchiveArticleId);
            Assert.Equal(ArticleUnlockedFlag | 1u, update.UnlockedFlags);

            MissingGameDataDiagnostic diagnostic = Assert.Single(MissingGameDataDiagnostics.GetSnapshot(), d =>
                d.Kind == MissingGameDataDiagnosticKind.MissingTable
                && d.Severity == MissingGameDataSeverity.PlayerImpacting
                && d.TableName == "ArchiveEntry.tbl"
                && d.Context == "GalacticArchiveManager.GrantUnlockRewards");
            Assert.Equal(1, diagnostic.Count);
        }
        finally
        {
            MissingGameDataDiagnostics.ResetForTests();
        }
    }

    private static CharacterModel CreateModelWithUnlockedArticle(uint archiveArticleId)
    {
        var model = new CharacterModel { Id = CharacterId };
        model.GalacticArchive.Add(new CharacterGalacticArchiveModel
        {
            Id               = CharacterId,
            ArchiveArticleId = archiveArticleId,
            UnlockedFlags    = ArticleUnlockedFlag
        });
        return model;
    }

    private static IPlayer CreatePlayer(out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), CharacterId);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        return player;
    }

    private static ArchiveArticleEntry CreateArticle(uint id, uint entryId = 0u)
    {
        return new ArchiveArticleEntry
        {
            Id = id,
            ArchiveEntryId00 = entryId
        };
    }

    private static ArchiveLinkEntry CreateLink(uint parentArticleId, uint childArticleId)
    {
        return new ArchiveLinkEntry
        {
            Id                     = childArticleId,
            ArchiveArticleIdParent = parentArticleId,
            ArchiveArticleIdChild  = childArticleId
        };
    }

    private static ClientGalacticArchiveUnlock CreateClientUnlock(uint archiveArticleId)
    {
        var message = (ClientGalacticArchiveUnlock)RuntimeHelpers.GetUninitializedObject(typeof(ClientGalacticArchiveUnlock));
        typeof(ClientGalacticArchiveUnlock)
            .GetProperty(nameof(ClientGalacticArchiveUnlock.ArchiveArticleId))!
            .SetValue(message, archiveArticleId);
        return message;
    }

    private static GameTableManager CreateGameTableManager(
        ArchiveArticleEntry[] articleEntries = null,
        ArchiveLinkEntry[] linkEntries = null,
        ArchiveEntryEntry[] entryEntries = null,
        ArchiveEntryUnlockRuleEntry[] ruleEntries = null)
    {
        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));

        if (articleEntries != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.ArchiveArticle), CreateGameTable(articleEntries));
        if (linkEntries != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.ArchiveLink), CreateGameTable(linkEntries));
        if (entryEntries != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.ArchiveEntry), CreateGameTable(entryEntries));
        if (ruleEntries != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.ArchiveEntryUnlockRule), CreateGameTable(ruleEntries));

        return gameTableManager;
    }

    private static IReadOnlyList<T> GetMessages<T>(RecordingDispatchProxy<IGameSession> sessionProxy)
        where T : class, IWritable
    {
        return sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0])
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

    private static void SetAutoProperty(object target, string propertyName, object value)
    {
        FieldInfo backingField = target.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(target, value);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(target, value);
    }
}
