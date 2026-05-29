using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.PlayerPath;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Path;

namespace NexusForever.Game.Tests.Entity;

public class PathSettlerBuildHandlerTests
{
    [Fact]
    public void HandleMessage_WithKnownImprovementGroup_SendsBuildResultAndStatus()
    {
        IGameTableManager gameTableManager = CreateGameTableManager(
            new PathSettlerImprovementGroupEntry
            {
                Id = 11u,
                PathSettlerHubId = 46u,
                DurationPerBundleMs = 45000u,
                PathSettlerImprovementIdTier00 = 9001u,
                PathSettlerImprovementIdTier01 = 9002u
            });
        IWorldSession session = CreateSession(
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<IPathManager> pathManagerProxy);
        ClientPathSettlerImprovementBuildTier buildTier = CreateBuildTier(11u, 1u);

        var handler = new ClientPathSettlerImprovementBuildTierHandler(
            NullLogger<ClientPathSettlerImprovementBuildTierHandler>.Instance,
            gameTableManager);

        handler.HandleMessage(session, buildTier);

        RecordingDispatchProxy<IPlayer>.Invocation visibleMessage =
            Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.EnqueueToVisible)));
        ServerPathSettlerBuildStatus status = Assert.IsType<ServerPathSettlerBuildStatus>(visibleMessage.Arguments[0]);
        Assert.True((bool)visibleMessage.Arguments[1]);
        Assert.Equal(46, status.PathSettlerHubId);
        Assert.Equal(11, status.Status.PathSettlerImprovementGroupId);
        Assert.Equal(1, status.Status.Tier);
        Assert.Equal(45000u, status.Status.RemainingTimeMs);
        Assert.Equal(1u, status.Status.BundleCount);

        RecordingDispatchProxy<IWorldSession>.Invocation sessionMessage =
            Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        ServerPathSettlerBuildResult result = Assert.IsType<ServerPathSettlerBuildResult>(sessionMessage.Arguments[0]);
        Assert.Equal(1u, result.Result);
        Assert.Equal(9002u, result.PathSettlerImprovementId);
        Assert.Equal(11u, result.PathSettlerImprovementGroupId);

        RecordingDispatchProxy<IPathManager>.Invocation completeCall =
            Assert.Single(pathManagerProxy.GetInvocations(nameof(IPathManager.CompleteMissionBySettlerImprovementGroupId)));
        Assert.Equal(11u, completeCall.Arguments[0]);
    }

    [Fact]
    public void HandleMessage_WithUnknownImprovementGroup_OnlyAttemptsMissionCompletion()
    {
        IGameTableManager gameTableManager = CreateGameTableManager();
        IWorldSession session = CreateSession(
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<IPathManager> pathManagerProxy);
        ClientPathSettlerImprovementBuildTier buildTier = CreateBuildTier(999u, 0u);

        var handler = new ClientPathSettlerImprovementBuildTierHandler(
            NullLogger<ClientPathSettlerImprovementBuildTierHandler>.Instance,
            gameTableManager);

        handler.HandleMessage(session, buildTier);

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.EnqueueToVisible)));
        Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));

        RecordingDispatchProxy<IPathManager>.Invocation completeCall =
            Assert.Single(pathManagerProxy.GetInvocations(nameof(IPathManager.CompleteMissionBySettlerImprovementGroupId)));
        Assert.Equal(999u, completeCall.Arguments[0]);
    }

    [Fact]
    public void HandleMessage_WithOutOfRangeImprovementGroup_DoesNotSendBuildAcknowledgement()
    {
        IGameTableManager gameTableManager = CreateGameTableManager(
            new PathSettlerImprovementGroupEntry
            {
                Id = 0x4000u,
                PathSettlerHubId = 46u,
                PathSettlerImprovementIdTier00 = 9001u
            });
        IWorldSession session = CreateSession(
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<IPathManager> pathManagerProxy);
        ClientPathSettlerImprovementBuildTier buildTier = CreateBuildTier(0x4000u, 0u);

        var handler = new ClientPathSettlerImprovementBuildTierHandler(
            NullLogger<ClientPathSettlerImprovementBuildTierHandler>.Instance,
            gameTableManager);

        handler.HandleMessage(session, buildTier);

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.EnqueueToVisible)));
        Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));

        RecordingDispatchProxy<IPathManager>.Invocation completeCall =
            Assert.Single(pathManagerProxy.GetInvocations(nameof(IPathManager.CompleteMissionBySettlerImprovementGroupId)));
        Assert.Equal(0x4000u, completeCall.Arguments[0]);
    }

    [Theory]
    [InlineData(4u, 9001u)]
    [InlineData(0u, 0u)]
    [InlineData(0u, 0x8000u)]
    public void HandleMessage_WithUnmappedOrUnwritableImprovement_DoesNotSendBuildAcknowledgement(uint buildTier, uint tier00ImprovementId)
    {
        IGameTableManager gameTableManager = CreateGameTableManager(
            new PathSettlerImprovementGroupEntry
            {
                Id = 11u,
                PathSettlerHubId = 46u,
                PathSettlerImprovementIdTier00 = tier00ImprovementId
            });
        IWorldSession session = CreateSession(
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<IPathManager> pathManagerProxy);
        ClientPathSettlerImprovementBuildTier request = CreateBuildTier(11u, buildTier);

        var handler = new ClientPathSettlerImprovementBuildTierHandler(
            NullLogger<ClientPathSettlerImprovementBuildTierHandler>.Instance,
            gameTableManager);

        handler.HandleMessage(session, request);

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.EnqueueToVisible)));
        Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));

        RecordingDispatchProxy<IPathManager>.Invocation completeCall =
            Assert.Single(pathManagerProxy.GetInvocations(nameof(IPathManager.CompleteMissionBySettlerImprovementGroupId)));
        Assert.Equal(11u, completeCall.Arguments[0]);
    }

    private static IWorldSession CreateSession(
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        out RecordingDispatchProxy<IPlayer> playerProxy,
        out RecordingDispatchProxy<IPathManager> pathManagerProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        IPathManager pathManager = RecordingDispatchProxy<IPathManager>.Create(out pathManagerProxy);

        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.PathManager), pathManager);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);

        return session;
    }

    private static ClientPathSettlerImprovementBuildTier CreateBuildTier(uint pathSettlerImprovementGroupId, uint buildTier)
    {
        var message = (ClientPathSettlerImprovementBuildTier)RuntimeHelpers.GetUninitializedObject(
            typeof(ClientPathSettlerImprovementBuildTier));
        SetAutoProperty(
            message,
            nameof(ClientPathSettlerImprovementBuildTier.PathSettlerImprovementGroupId),
            pathSettlerImprovementGroupId);
        SetAutoProperty(message, nameof(ClientPathSettlerImprovementBuildTier.BuildTier), buildTier);
        return message;
    }

    private static IGameTableManager CreateGameTableManager(params PathSettlerImprovementGroupEntry[] improvementGroups)
    {
        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));
        SetAutoProperty(
            gameTableManager,
            nameof(GameTableManager.PathSettlerImprovementGroup),
            CreateGameTable(improvementGroups));
        return gameTableManager;
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
        FieldInfo backingField = instance.GetType().GetField(
            $"<{propertyName}>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(instance, value);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(instance, value);
    }
}
