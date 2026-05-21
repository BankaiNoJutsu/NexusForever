using System.Numerics;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Static;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Support;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Support;
using NexusForever.WorldServer.Support;

namespace NexusForever.Game.Tests.Support;

public class SupportTicketHandlerTests
{
    [Fact]
    public void HandleMessage_StoresTicketThroughSubmissionStoreAndSendsSuccess()
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out var sessionProxy);
        var store = new RecordingSubmissionStore(true);
        var handler = new ClientSupportTicketHandler(NullLogger<ClientSupportTicketHandler>.Instance, store);
        ClientSupportTicket ticket = CreateTicket(Language.English);

        handler.HandleMessage(session, ticket);

        Assert.Single(store.Submissions);
        Assert.Equal("ticket", store.Submissions[0].Type);
        Assert.Same(session, store.Submissions[0].Session);

        ServerSupportTicketResult result = GetEncryptedMessages(sessionProxy)
            .OfType<ServerSupportTicketResult>()
            .Single();
        Assert.True(result.Success);
    }

    [Fact]
    public void HandleMessage_WhenStoreFailsSendsFailure()
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out var sessionProxy);
        var store = new RecordingSubmissionStore(false);
        var handler = new ClientSupportTicketHandler(NullLogger<ClientSupportTicketHandler>.Instance, store);

        handler.HandleMessage(session, CreateTicket(Language.English));

        ServerSupportTicketResult result = GetEncryptedMessages(sessionProxy)
            .OfType<ServerSupportTicketResult>()
            .Single();
        Assert.False(result.Success);
    }

    [Fact]
    public void HandleMessage_WithInvalidLanguageDoesNotStoreTicketAndSendsFailure()
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out var sessionProxy);
        var store = new RecordingSubmissionStore(true);
        var handler = new ClientSupportTicketHandler(NullLogger<ClientSupportTicketHandler>.Instance, store);

        handler.HandleMessage(session, CreateTicket((Language)999u));

        Assert.Empty(store.Submissions);
        ServerSupportTicketResult result = GetEncryptedMessages(sessionProxy)
            .OfType<ServerSupportTicketResult>()
            .Single();
        Assert.False(result.Success);
    }

    private static IEnumerable<object> GetEncryptedMessages(RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        return sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0]);
    }

    private static ClientSupportTicket CreateTicket(Language language)
    {
        var ticket = (ClientSupportTicket)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ClientSupportTicket));
        typeof(ClientSupportTicket).GetProperty(nameof(ClientSupportTicket.TicketCategoryId))!.SetValue(ticket, (ushort)12);
        typeof(ClientSupportTicket).GetProperty(nameof(ClientSupportTicket.TicketSubCategoryId))!.SetValue(ticket, (ushort)34);
        typeof(ClientSupportTicket).GetProperty(nameof(ClientSupportTicket.Position))!.SetValue(ticket, new Vector3(1f, 2f, 3f));
        typeof(ClientSupportTicket).GetProperty(nameof(ClientSupportTicket.Subject))!.SetValue(ticket, "Subject");
        typeof(ClientSupportTicket).GetProperty(nameof(ClientSupportTicket.Body))!.SetValue(ticket, "Body");
        typeof(ClientSupportTicket).GetProperty(nameof(ClientSupportTicket.LanguageId))!.SetValue(ticket, language);
        return ticket;
    }

    private sealed class RecordingSubmissionStore(bool result) : ISupportSubmissionStore
    {
        public List<Submission> Submissions { get; } = [];

        public bool TryAppend(IWorldSession session, string type, object payload)
        {
            Submissions.Add(new Submission(session, type, payload));
            return result;
        }

        public readonly record struct Submission(IWorldSession Session, string Type, object Payload);
    }
}
