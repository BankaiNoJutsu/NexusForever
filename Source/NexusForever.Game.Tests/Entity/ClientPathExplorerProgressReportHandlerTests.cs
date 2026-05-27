using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.World.Message.Model.PlayerPath;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Path;

namespace NexusForever.Game.Tests.Entity;

public class ClientPathExplorerProgressReportHandlerTests
{
    [Fact]
    public void HandleMessage_WithMissionId_UsesExplorerProgressCompletion()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IPathManager> pathManagerProxy);
        ClientPathExplorerProgressReport progressReport = CreateProgressReport(35u, 2u);

        var handler = new ClientPathExplorerProgressReportHandler(
            NullLogger<ClientPathExplorerProgressReportHandler>.Instance);

        handler.HandleMessage(session, progressReport);

        RecordingDispatchProxy<IPathManager>.Invocation completeCall =
            Assert.Single(pathManagerProxy.GetInvocations(nameof(IPathManager.CompleteExplorerProgressMission)));
        Assert.Equal((ushort)35, completeCall.Arguments[0]);
        Assert.Equal(2u, completeCall.Arguments[1]);
        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.CompleteActiveMission)));
        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.CompleteMission)));
    }

    [Fact]
    public void HandleMessage_WithOutOfRangeMissionId_DoesNotCompleteMission()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IPathManager> pathManagerProxy);
        ClientPathExplorerProgressReport progressReport = CreateProgressReport(ushort.MaxValue + 1u, 2u);

        var handler = new ClientPathExplorerProgressReportHandler(
            NullLogger<ClientPathExplorerProgressReportHandler>.Instance);

        handler.HandleMessage(session, progressReport);

        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.CompleteExplorerProgressMission)));
        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.CompleteActiveMission)));
        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.CompleteMission)));
    }

    private static IWorldSession CreateSession(out RecordingDispatchProxy<IPathManager> pathManagerProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out var sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IPathManager pathManager = RecordingDispatchProxy<IPathManager>.Create(out pathManagerProxy);

        playerProxy.SetProperty(nameof(IPlayer.PathManager), pathManager);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);

        return session;
    }

    private static ClientPathExplorerProgressReport CreateProgressReport(uint pathMissionId, uint explorerNodeIndex)
    {
        var message = (ClientPathExplorerProgressReport)RuntimeHelpers.GetUninitializedObject(
            typeof(ClientPathExplorerProgressReport));
        SetAutoProperty(message, nameof(ClientPathExplorerProgressReport.PathMissionId), pathMissionId);
        SetAutoProperty(message, nameof(ClientPathExplorerProgressReport.ExplorerNodeIndex), explorerNodeIndex);
        return message;
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType().GetField(
            $"<{propertyName}>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(instance, value);
    }
}
