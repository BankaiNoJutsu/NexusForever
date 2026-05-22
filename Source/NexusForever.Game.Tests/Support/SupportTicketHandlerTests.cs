using System.Numerics;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Static;
using NexusForever.Game.Static.Support;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Support;
using NexusForever.Network.World.Message.Model.Shared;
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

    [Fact]
    public void IncidentReport_StoresValidReportThroughSubmissionStore()
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out var sessionProxy);
        var store = new RecordingSubmissionStore(true);
        var handler = new ClientIncidentReportHandler(NullLogger<ClientIncidentReportHandler>.Instance, store);
        ClientIncidentReport report = CreateIncidentReport(ReportPlayerReason.Bot, ReportPlayerSource.Chat);

        handler.HandleMessage(session, report);

        RecordingSubmissionStore.Submission submission = Assert.Single(store.Submissions);
        Assert.Equal("incident", submission.Type);
        Assert.Same(session, submission.Session);
        Assert.Equal(ReportPlayerReason.Bot, GetPayloadValue<ReportPlayerReason>(submission.Payload, nameof(ClientIncidentReport.Reason)));
        Assert.Equal(ReportPlayerSource.Chat, GetPayloadValue<ReportPlayerSource>(submission.Payload, nameof(ClientIncidentReport.Source)));
        Assert.Equal(0xABCDul, GetPayloadValue<ulong>(submission.Payload, nameof(ClientIncidentReport.ObjectId)));
        Assert.Equal(1.5f, GetPayloadValue<float>(submission.Payload, nameof(ClientIncidentReport.DaysAgo)));
        Assert.True(GetPayloadValue<bool>(submission.Payload, nameof(ClientIncidentReport.PermanentIgnore)));
        Assert.Equal("Player note", GetPayloadValue<string>(submission.Payload, nameof(ClientIncidentReport.Note)));
        Identity identity = GetPayloadValue<Identity>(submission.Payload, nameof(ClientIncidentReport.Identity));
        Assert.Equal(1, identity.RealmId);
        Assert.Equal(2002ul, identity.Id);
        Assert.Empty(GetEncryptedMessages(sessionProxy));
    }

    [Theory]
    [InlineData(999u, (uint)ReportPlayerSource.Chat)]
    [InlineData((uint)ReportPlayerReason.Spam, 999u)]
    public void IncidentReport_WithInvalidEnumDoesNotStore(uint reason, uint source)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out var sessionProxy);
        var store = new RecordingSubmissionStore(true);
        var handler = new ClientIncidentReportHandler(NullLogger<ClientIncidentReportHandler>.Instance, store);

        handler.HandleMessage(session, CreateIncidentReport((ReportPlayerReason)reason, (ReportPlayerSource)source));

        Assert.Empty(store.Submissions);
        Assert.Empty(GetEncryptedMessages(sessionProxy));
    }

    [Fact]
    public void ReportBug_StoresValidBugReportThroughSubmissionStore()
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out var sessionProxy);
        var store = new RecordingSubmissionStore(true);
        var handler = new ClientReportBugHandler(NullLogger<ClientReportBugHandler>.Instance, store);

        handler.HandleMessage(session, CreateBugReport(BugCategory.Development));

        RecordingSubmissionStore.Submission submission = Assert.Single(store.Submissions);
        Assert.Equal("bug", submission.Type);
        Assert.Same(session, submission.Session);
        Assert.Equal(BugCategory.Development, GetPayloadValue<BugCategory>(submission.Payload, nameof(ClientReportBug.BugCategoryId)));
        Assert.Equal(1234u, GetPayloadValue<uint>(submission.Payload, nameof(ClientReportBug.SelectedUnitId)));
        Assert.Equal(5678u, GetPayloadValue<uint>(submission.Payload, nameof(ClientReportBug.Quest2Id)));
        Assert.Equal("Bug text", GetPayloadValue<string>(submission.Payload, nameof(ClientReportBug.Description)));
        Assert.Empty(GetEncryptedMessages(sessionProxy));
    }

    [Fact]
    public void ReportBug_WithInvalidCategoryDoesNotStore()
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out var sessionProxy);
        var store = new RecordingSubmissionStore(true);
        var handler = new ClientReportBugHandler(NullLogger<ClientReportBugHandler>.Instance, store);

        handler.HandleMessage(session, CreateBugReport((BugCategory)999u));

        Assert.Empty(store.Submissions);
        Assert.Empty(GetEncryptedMessages(sessionProxy));
    }

    [Fact]
    public void Suggest_StoresSuggestionThroughSubmissionStore()
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out var sessionProxy);
        var store = new RecordingSubmissionStore(true);
        var handler = new ClientSuggestHandler(NullLogger<ClientSuggestHandler>.Instance, store);

        handler.HandleMessage(session, CreateSuggestion("Please add more hoverboards"));

        RecordingSubmissionStore.Submission submission = Assert.Single(store.Submissions);
        Assert.Equal("suggestion", submission.Type);
        Assert.Same(session, submission.Session);
        Assert.Equal("Please add more hoverboards", GetPayloadValue<string>(submission.Payload, nameof(ClientSuggest.SuggestionText)));
        Assert.Empty(GetEncryptedMessages(sessionProxy));
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

    private static ClientIncidentReport CreateIncidentReport(ReportPlayerReason reason, ReportPlayerSource source)
    {
        var report = (ClientIncidentReport)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ClientIncidentReport));
        typeof(ClientIncidentReport).GetProperty(nameof(ClientIncidentReport.Identity))!.SetValue(report, new Identity
        {
            RealmId = 1,
            Id      = 2002ul
        });
        typeof(ClientIncidentReport).GetProperty(nameof(ClientIncidentReport.Reason))!.SetValue(report, reason);
        typeof(ClientIncidentReport).GetProperty(nameof(ClientIncidentReport.Source))!.SetValue(report, source);
        typeof(ClientIncidentReport).GetProperty(nameof(ClientIncidentReport.Note))!.SetValue(report, "Player note");
        typeof(ClientIncidentReport).GetProperty(nameof(ClientIncidentReport.ObjectId))!.SetValue(report, 0xABCDul);
        typeof(ClientIncidentReport).GetProperty(nameof(ClientIncidentReport.DaysAgo))!.SetValue(report, 1.5f);
        typeof(ClientIncidentReport).GetProperty(nameof(ClientIncidentReport.PermanentIgnore))!.SetValue(report, true);
        return report;
    }

    private static ClientReportBug CreateBugReport(BugCategory category)
    {
        var report = (ClientReportBug)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ClientReportBug));
        typeof(ClientReportBug).GetProperty(nameof(ClientReportBug.BugCategoryId))!.SetValue(report, category);
        typeof(ClientReportBug).GetProperty(nameof(ClientReportBug.SelectedUnitId))!.SetValue(report, 1234u);
        typeof(ClientReportBug).GetProperty(nameof(ClientReportBug.Quest2Id))!.SetValue(report, 5678u);
        typeof(ClientReportBug).GetProperty(nameof(ClientReportBug.Description))!.SetValue(report, "Bug text");
        return report;
    }

    private static ClientSuggest CreateSuggestion(string text)
    {
        var suggestion = (ClientSuggest)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ClientSuggest));
        typeof(ClientSuggest).GetProperty(nameof(ClientSuggest.SuggestionText))!.SetValue(suggestion, text);
        return suggestion;
    }

    private static T GetPayloadValue<T>(object payload, string propertyName)
    {
        return (T)payload.GetType().GetProperty(propertyName)!.GetValue(payload)!;
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
