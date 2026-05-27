using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.World.Message.Model.PlayerPath;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Path;

namespace NexusForever.Game.Tests.Entity;

public class ClientPathExplorerPowerMapProgressHandlerTests
{
    [Fact]
    public void HandleMessage_UsesExplorerPowerMapCompletion()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IPathManager> pathManagerProxy);
        ClientPathExplorerPowerMapProgress powerMapProgress = CreatePowerMapProgress(77u);

        var handler = new ClientPathExplorerPowerMapProgressHandler(
            NullLogger<ClientPathExplorerPowerMapProgressHandler>.Instance);

        handler.HandleMessage(session, powerMapProgress);

        RecordingDispatchProxy<IPathManager>.Invocation completeCall =
            Assert.Single(pathManagerProxy.GetInvocations(nameof(IPathManager.CompleteExplorerPowerMapMission)));
        Assert.Equal(77u, completeCall.Arguments[0]);
        Assert.Empty(pathManagerProxy.GetInvocations(nameof(IPathManager.CompleteMissionByObjectId)));
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

    private static ClientPathExplorerPowerMapProgress CreatePowerMapProgress(uint pathExplorerPowerMapId)
    {
        var message = (ClientPathExplorerPowerMapProgress)RuntimeHelpers.GetUninitializedObject(
            typeof(ClientPathExplorerPowerMapProgress));
        SetAutoProperty(message, nameof(ClientPathExplorerPowerMapProgress.PathExplorerPowerMapId), pathExplorerPowerMapId);
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
