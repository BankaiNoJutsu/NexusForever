using System.Reflection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Quest;

namespace NexusForever.Game.Tests.Quest;

public class ClientQuestCompleteHandlerTests
{
    [Fact]
    public void HandleMessage_NpcQuestCompletion_EndsDialogAfterQuestComplete()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IQuestManager> questManagerProxy, out RecordingDispatchProxy<IWorldSession> sessionProxy);
        SetActiveDialog(session, dialogUnitId: 790u);
        var handler = new ClientQuestCompleteHandler();

        handler.HandleMessage(session, CreateQuestComplete(3671, rewardSelection: 0, isCommunicatorMsg: false));

        RecordingDispatchProxy<IQuestManager>.Invocation questComplete = Assert.Single(
            questManagerProxy.GetInvocations(nameof(IQuestManager.QuestComplete)));
        Assert.Equal((ushort)3671, questComplete.Arguments[0]);
        Assert.Equal((ushort)0, questComplete.Arguments[1]);
        Assert.Equal(false, questComplete.Arguments[2]);

        RecordingDispatchProxy<IWorldSession>.Invocation sentMessage = Assert.Single(
            sessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)));
        var dialogEnd = Assert.IsType<ServerDialogEnd>(sentMessage.Arguments[0]);
        Assert.Equal(790u, dialogEnd.DialogUnitId);
    }

    [Fact]
    public void HandleMessage_CommunicatorQuestCompletion_DoesNotEndNpcDialog()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IQuestManager> questManagerProxy, out RecordingDispatchProxy<IWorldSession> sessionProxy);
        var handler = new ClientQuestCompleteHandler();

        handler.HandleMessage(session, CreateQuestComplete(3671, rewardSelection: 0, isCommunicatorMsg: true));

        RecordingDispatchProxy<IQuestManager>.Invocation questComplete = Assert.Single(
            questManagerProxy.GetInvocations(nameof(IQuestManager.QuestComplete)));
        Assert.Equal((ushort)3671, questComplete.Arguments[0]);
        Assert.Equal((ushort)0, questComplete.Arguments[1]);
        Assert.Equal(true, questComplete.Arguments[2]);
        Assert.Empty(sessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)));
    }

    [Fact]
    public void HandleMessage_WhenQuestValidationFails_SendsGenericErrorWithoutEndingDialog()
    {
        IWorldSession session = CreateSession(
            out RecordingDispatchProxy<IQuestManager> questManagerProxy,
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out RecordingDispatchProxy<IPlayer> playerProxy);
        questManagerProxy.SetMethodHandler(
            nameof(IQuestManager.QuestComplete),
            _ => throw new QuestException("quest is not complete"));
        SetActiveDialog(session, dialogUnitId: 790u);
        var handler = new ClientQuestCompleteHandler();

        handler.HandleMessage(session, CreateQuestComplete(3480, rewardSelection: 0, isCommunicatorMsg: false));

        RecordingDispatchProxy<IPlayer>.Invocation error = Assert.Single(
            playerProxy.GetInvocations(nameof(IPlayer.SendGenericError)));
        Assert.Equal(GenericError.Params, error.Arguments[0]);
        RecordingDispatchProxy<IQuestManager>.Invocation resend = Assert.Single(
            questManagerProxy.GetInvocations(nameof(IQuestManager.SendQuestState)));
        Assert.Equal((ushort)3480, resend.Arguments[0]);
        Assert.Empty(sessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)));
    }

    private static IWorldSession CreateSession(
        out RecordingDispatchProxy<IQuestManager> questManagerProxy,
        out RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        return CreateSession(out questManagerProxy, out sessionProxy, out _);
    }

    private static IWorldSession CreateSession(
        out RecordingDispatchProxy<IQuestManager> questManagerProxy,
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out questManagerProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);

        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        return session;
    }

    private static void SetActiveDialog(IWorldSession session, uint dialogUnitId)
    {
        Type dialogSessionState = typeof(ClientQuestCompleteHandler).Assembly
            .GetType("NexusForever.WorldServer.Network.Message.Handler.DialogSessionState")!;
        MethodInfo setActiveDialog = dialogSessionState
            .GetMethod("SetActiveDialog", BindingFlags.Static | BindingFlags.Public)!;
        setActiveDialog.Invoke(null, [session, dialogUnitId]);
    }

    private static ClientQuestComplete CreateQuestComplete(ushort questId, ushort rewardSelection, bool isCommunicatorMsg)
    {
        var questComplete = new ClientQuestComplete();
        SetAutoProperty(questComplete, nameof(ClientQuestComplete.QuestId), questId);
        SetAutoProperty(questComplete, nameof(ClientQuestComplete.RewardSelection), rewardSelection);
        SetAutoProperty(questComplete, nameof(ClientQuestComplete.IsCommunicatorMsg), isCommunicatorMsg);
        return questComplete;
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(instance, value);
    }
}
