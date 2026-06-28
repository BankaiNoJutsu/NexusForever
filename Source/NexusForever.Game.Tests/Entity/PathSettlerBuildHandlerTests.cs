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
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Path;

namespace NexusForever.Game.Tests.Entity;

public class PathSettlerBuildHandlerTests
{
    [Fact]
    public void HandleMessage_WithKnownImprovementGroup_SendsBuildResultAndStatusAndConsumesResources()
    {
        IGameTableManager gameTableManager = CreateGameTableManager(
        [
            new PathSettlerImprovementGroupEntry
            {
                Id = 11u,
                PathSettlerHubId = 46u,
                DurationPerBundleMs = 45000u,
                PathSettlerImprovementIdTier00 = 450u
            }
        ],
        [
            new PathSettlerImprovementEntry
            {
                Id = 450u,
                CountResource00 = 5u
            }
        ],
        [
            new PathSettlerHubEntry
            {
                Id = 46u,
                Item2IdResource00 = 14400u
            }
        ]);
        IWorldSession session = CreateSession(
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<IPathManager> pathManagerProxy,
            out RecordingDispatchProxy<IInventory> inventoryProxy);
        ClientPathSettlerImprovementBuildTier buildTier = CreateBuildTier(11u, 0u);

        var handler = new ClientPathSettlerImprovementBuildTierHandler(
            NullLogger<ClientPathSettlerImprovementBuildTierHandler>.Instance,
            gameTableManager);

        handler.HandleMessage(session, buildTier);

        RecordingDispatchProxy<IInventory>.Invocation deleteCall =
            Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemDelete)));
        Assert.Equal(14400u, deleteCall.Arguments[0]);
        Assert.Equal(5u, deleteCall.Arguments[1]);
        Assert.Equal(ItemUpdateReason.SettlerImprovementConsumeResource, deleteCall.Arguments[2]);

        RecordingDispatchProxy<IPlayer>.Invocation visibleMessage =
            Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.EnqueueToVisible)));
        ServerPathSettlerBuildStatus status = Assert.IsType<ServerPathSettlerBuildStatus>(visibleMessage.Arguments[0]);
        Assert.True((bool)visibleMessage.Arguments[1]);
        Assert.Equal(46, status.PathSettlerHubId);
        Assert.Equal(11, status.Status.PathSettlerImprovementGroupId);
        Assert.Equal(0, status.Status.Tier);
        Assert.Equal(45000u, status.Status.RemainingTimeMs);
        Assert.Equal(1u, status.Status.BundleCount);

        RecordingDispatchProxy<IWorldSession>.Invocation sessionMessage =
            Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        ServerPathSettlerBuildResult result = Assert.IsType<ServerPathSettlerBuildResult>(sessionMessage.Arguments[0]);
        Assert.Equal(1u, result.Result);
        Assert.Equal(450u, result.PathSettlerImprovementId);
        Assert.Equal(11u, result.PathSettlerImprovementGroupId);

        RecordingDispatchProxy<IPathManager>.Invocation statusCall =
            Assert.Single(pathManagerProxy.GetInvocations(nameof(IPathManager.ApplySettlerImprovementGroupStatus)));
        Assert.Equal(11u, statusCall.Arguments[0]);
        Assert.Equal(0, statusCall.Arguments[1]);
        Assert.Equal(1u, statusCall.Arguments[2]);

        RecordingDispatchProxy<IPathManager>.Invocation completeCall =
            Assert.Single(pathManagerProxy.GetInvocations(nameof(IPathManager.CompleteMissionBySettlerImprovementGroupId)));
        Assert.Equal(11u, completeCall.Arguments[0]);
    }

    [Fact]
    public void HandleMessage_WithUnknownImprovementGroup_DoesNotSendBuildAcknowledgementOrProgress()
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
        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.ApplySettlerImprovementGroupStatus)));
        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.CompleteMissionBySettlerImprovementGroupId)));
    }

    [Fact]
    public void HandleMessage_WithMissingImprovementGroupTable_DoesNotSendBuildAcknowledgementOrProgress()
    {
        IGameTableManager gameTableManager = CreateGameTableManager(includeImprovementGroupTable: false);
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
        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.ApplySettlerImprovementGroupStatus)));
        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.CompleteMissionBySettlerImprovementGroupId)));
    }

    [Fact]
    public void HandleMessage_WithOutOfRangeImprovementGroup_DoesNotSendBuildAcknowledgementOrProgress()
    {
        IGameTableManager gameTableManager = CreateGameTableManager([
            new PathSettlerImprovementGroupEntry
            {
                Id = 0x4000u,
                PathSettlerHubId = 46u,
                PathSettlerImprovementIdTier00 = 9001u
            }]);
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
        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.ApplySettlerImprovementGroupStatus)));
        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.CompleteMissionBySettlerImprovementGroupId)));
    }

    [Theory]
    [InlineData(4u, 9001u)]
    [InlineData(0u, 0u)]
    [InlineData(0u, 0x8000u)]
    public void HandleMessage_WithUnmappedOrUnwritableImprovement_DoesNotSendBuildAcknowledgementOrProgress(uint buildTier, uint tier00ImprovementId)
    {
        IGameTableManager gameTableManager = CreateGameTableManager([
            new PathSettlerImprovementGroupEntry
            {
                Id = 11u,
                PathSettlerHubId = 46u,
                PathSettlerImprovementIdTier00 = tier00ImprovementId
            }]);
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
        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.ApplySettlerImprovementGroupStatus)));
        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.CompleteMissionBySettlerImprovementGroupId)));
    }

    [Fact]
    public void HandleMessage_WithMissingImprovementTable_DoesNotSendBuildAcknowledgementOrProgress()
    {
        IGameTableManager gameTableManager = CreateGameTableManager(
            [
                new PathSettlerImprovementGroupEntry
                {
                    Id = 11u,
                    PathSettlerHubId = 46u,
                    PathSettlerImprovementIdTier00 = 450u
                }
            ],
            includeImprovementTable: false);
        IWorldSession session = CreateSession(
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<IPathManager> pathManagerProxy);
        ClientPathSettlerImprovementBuildTier request = CreateBuildTier(11u, 0u);

        var handler = new ClientPathSettlerImprovementBuildTierHandler(
            NullLogger<ClientPathSettlerImprovementBuildTierHandler>.Instance,
            gameTableManager);

        handler.HandleMessage(session, request);

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.EnqueueToVisible)));
        Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.ApplySettlerImprovementGroupStatus)));
        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.CompleteMissionBySettlerImprovementGroupId)));
    }

    [Fact]
    public void HandleMessage_WithInsufficientSettlerResources_DoesNotSendBuildAcknowledgementOrProgress()
    {
        IGameTableManager gameTableManager = CreateGameTableManager(
        [
            new PathSettlerImprovementGroupEntry
            {
                Id = 11u,
                PathSettlerHubId = 46u,
                PathSettlerImprovementIdTier00 = 450u
            }
        ],
        [
            new PathSettlerImprovementEntry
            {
                Id = 450u,
                CountResource00 = 5u
            }
        ],
        [
            new PathSettlerHubEntry
            {
                Id = 46u,
                Item2IdResource00 = 14400u
            }
        ]);
        IWorldSession session = CreateSession(
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<IPathManager> pathManagerProxy,
            out RecordingDispatchProxy<IInventory> inventoryProxy);
        inventoryProxy.SetMethodReturn(nameof(IInventory.HasItemCount), false);
        ClientPathSettlerImprovementBuildTier request = CreateBuildTier(11u, 0u);

        var handler = new ClientPathSettlerImprovementBuildTierHandler(
            NullLogger<ClientPathSettlerImprovementBuildTierHandler>.Instance,
            gameTableManager);

        handler.HandleMessage(session, request);

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.EnqueueToVisible)));
        Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemDelete)));
        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.ApplySettlerImprovementGroupStatus)));
        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.CompleteMissionBySettlerImprovementGroupId)));
    }

    private static IWorldSession CreateSession(
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        out RecordingDispatchProxy<IPlayer> playerProxy,
        out RecordingDispatchProxy<IPathManager> pathManagerProxy)
    {
        return CreateSession(out sessionProxy, out playerProxy, out pathManagerProxy, out _);
    }

    private static IWorldSession CreateSession(
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        out RecordingDispatchProxy<IPlayer> playerProxy,
        out RecordingDispatchProxy<IPathManager> pathManagerProxy,
        out RecordingDispatchProxy<IInventory> inventoryProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        IPathManager pathManager = RecordingDispatchProxy<IPathManager>.Create(out pathManagerProxy);
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out inventoryProxy);

        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.PathManager), pathManager);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        inventoryProxy.SetMethodReturn(nameof(IInventory.HasItemCount), true);
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

    private static IGameTableManager CreateGameTableManager(
        PathSettlerImprovementGroupEntry[] improvementGroups = null,
        PathSettlerImprovementEntry[] improvements = null,
        PathSettlerHubEntry[] settlerHubs = null,
        bool includeImprovementGroupTable = true,
        bool includeImprovementTable = true,
        bool includeHubTable = true)
    {
        PathSettlerImprovementGroupEntry[] groups = improvementGroups ?? [];
        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));
        if (includeImprovementGroupTable)
        {
            SetAutoProperty(
                gameTableManager,
                nameof(GameTableManager.PathSettlerImprovementGroup),
                CreateGameTable(groups));
        }
        if (includeImprovementTable)
        {
            SetAutoProperty(
                gameTableManager,
                nameof(GameTableManager.PathSettlerImprovement),
                CreateGameTable(improvements ?? CreateDefaultImprovementEntries(groups)));
        }
        if (includeHubTable)
        {
            SetAutoProperty(
                gameTableManager,
                nameof(GameTableManager.PathSettlerHub),
                CreateGameTable(settlerHubs ?? CreateDefaultHubEntries(groups)));
        }
        return gameTableManager;
    }

    private static PathSettlerImprovementEntry[] CreateDefaultImprovementEntries(IEnumerable<PathSettlerImprovementGroupEntry> improvementGroups)
    {
        return improvementGroups
            .SelectMany(GetImprovementIds)
            .Where(id => id != 0u && id <= 0x7FFFu)
            .Distinct()
            .Select(id => new PathSettlerImprovementEntry { Id = id })
            .ToArray();
    }

    private static IEnumerable<uint> GetImprovementIds(PathSettlerImprovementGroupEntry improvementGroup)
    {
        yield return improvementGroup.PathSettlerImprovementIdTier00;
        yield return improvementGroup.PathSettlerImprovementIdTier01;
        yield return improvementGroup.PathSettlerImprovementIdTier02;
        yield return improvementGroup.PathSettlerImprovementIdTier03;
    }

    private static PathSettlerHubEntry[] CreateDefaultHubEntries(IEnumerable<PathSettlerImprovementGroupEntry> improvementGroups)
    {
        return improvementGroups
            .Select(group => group.PathSettlerHubId)
            .Where(id => id != 0u)
            .Distinct()
            .Select(id => new PathSettlerHubEntry
            {
                Id = id,
                Item2IdResource00 = 14400u
            })
            .ToArray();
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
