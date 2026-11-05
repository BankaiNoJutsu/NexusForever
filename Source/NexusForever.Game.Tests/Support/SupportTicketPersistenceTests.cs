using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Support;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Support;
using NexusForever.WorldServer.Service;
using NexusForever.WorldServer.Support;

namespace NexusForever.Game.Tests.Support;

public sealed class SupportTicketPersistenceTests
{
    [Fact]
    public async Task PublicTicketCannotAcknowledgeBeforeItsQueuedFileWriteCompletes()
    {
        using var f = new Frame();
        var request = f.Request(); f.Handler.HandleMessage(f.Session, request);
        typeof(ClientSupportTicket).GetProperty(nameof(ClientSupportTicket.Body))!.SetValue(request, "Changed after admission");
        Assert.Empty(f.Results); Assert.Empty(Directory.GetFiles(f.Root.FullName));
        Assert.Single(f.Runner.Operations);
        await f.Runner.Run(); Assert.Empty(f.Results);
        f.Events.Update(.01);
        Assert.True(Assert.Single(f.Results).Success);
        using var line = JsonDocument.Parse(Assert.Single(File.ReadAllLines(Assert.Single(Directory.GetFiles(f.Root.FullName)))));
        Assert.Equal("ticket", line.RootElement.GetProperty("Type").GetString());
        Assert.Equal(42ul, line.RootElement.GetProperty("CharacterId").GetUInt64());
        Assert.Equal("Body", line.RootElement.GetProperty("Payload").GetProperty("Body").GetString());
    }

    [Fact]
    public async Task FailedQueuedFileWriteReturnsFailureInsteadOfAnAcknowledgedLostTicket()
    {
        using var f = new Frame(blockedDirectory: true);
        f.Handler.HandleMessage(f.Session, f.Request()); Assert.Empty(f.Results);
        await f.Runner.Run(); Assert.Empty(f.Results); f.Events.Update(.01);
        Assert.False(Assert.Single(f.Results).Success); Assert.Empty(f.Runner.Operations);
        Assert.Equal("obstruction", File.ReadAllText(Path.Combine(f.Root.FullName, "blocked")));
    }

    [Fact]
    public async Task FailedResultEnqueueRetriesWithoutWritingAnotherSubmission()
    {
        using var f = new Frame(); int attempts = 0; var accepted = new List<bool>();
        f.Messages.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            var result = Assert.IsType<ServerSupportTicketResult>(args[0]); Assert.True(result.Success);
            if (++attempts == 1) { result.Success = false; throw new IOException("Owned result enqueue rejected before acceptance."); }
            accepted.Add(result.Success); return null;
        });
        f.Handler.HandleMessage(f.Session, f.Request()); await f.Runner.Run();
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while (f.Events.PendingEvents) { Assert.True(watch.Elapsed < TimeSpan.FromSeconds(5)); f.Events.Update(.01); await Task.Delay(5); }
        Assert.Equal(2, attempts); Assert.True(Assert.Single(accepted)); Assert.Empty(f.Runner.Operations);
        Assert.Single(File.ReadAllLines(Assert.Single(Directory.GetFiles(f.Root.FullName))));
    }

    [Theory]
    [InlineData("player")] [InlineData("account")] [InlineData("queue")]
    public async Task ReplacedOwnerCannotReceiveTheOldSubmissionResult(string replacement)
    {
        using var f = new Frame(); f.Handler.HandleMessage(f.Session, f.Request());
        if (replacement == "player") f.Messages.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
        else if (replacement == "account") f.Messages.SetProperty(nameof(IWorldSession.Account), RecordingDispatchProxy<IAccount>.Create(out _));
        else f.Messages.SetProperty(nameof(IWorldSession.Events), new EventQueue());
        await f.Runner.Run(); f.Events.Update(.01); Assert.Empty(f.Results);
        using var stored = JsonDocument.Parse(Assert.Single(File.ReadAllLines(Assert.Single(Directory.GetFiles(f.Root.FullName)))));
        Assert.Equal(42ul, stored.RootElement.GetProperty("CharacterId").GetUInt64());
        Assert.Equal(12u, stored.RootElement.GetProperty("AccountId").GetUInt32());
    }

    [Fact]
    public void RejectedWorkerQueueReturnsFailureWithoutWritingAFile()
    {
        using var f = new Frame(); f.Runner.RejectQueue = true;
        f.Handler.HandleMessage(f.Session, f.Request());
        Assert.False(Assert.Single(f.Results).Success); Assert.Empty(f.Runner.Operations); Assert.Empty(Directory.GetFiles(f.Root.FullName));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void FaultedOrCanceledStorageCannotProduceSuccess(bool canceled)
    {
        using var f = new Frame(store: new BrokenStore(canceled));
        f.Handler.HandleMessage(f.Session, f.Request());
        Assert.False(Assert.Single(f.Results).Success); Assert.Empty(Directory.GetFiles(f.Root.FullName));
    }

    [Fact]
    public async Task UnacknowledgedReportsKeepTheirExistingQueuedWriter()
    {
        using var f = new Frame(); Assert.True(f.Store.TryAppend(f.Session, "incident", new { Note = "Original" }));
        Assert.Empty(Directory.GetFiles(f.Root.FullName)); await f.Runner.Run(); Assert.Empty(f.Results);
        using var line = JsonDocument.Parse(Assert.Single(File.ReadAllLines(Assert.Single(Directory.GetFiles(f.Root.FullName)))));
        Assert.Equal("incident", line.RootElement.GetProperty("Type").GetString());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task PartialAppendPreservesEarlierRecordsAndRequiresRepairBeforeFurtherSuccess(bool repairFails)
    {
        int opened = 0;
        using var f = new Frame(openFile: path => ++opened == 2 ? new BrokenAppend(path, repairFails)
            : new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read));
        f.Handler.HandleMessage(f.Session, f.Request()); await f.Runner.Run(); f.Events.Update(.01);
        Assert.True(Assert.Single(f.Results).Success);
        string path = Assert.Single(Directory.GetFiles(f.Root.FullName)); byte[] original = File.ReadAllBytes(path);
        f.Handler.HandleMessage(f.Session, f.Request()); await f.Runner.Run(); f.Events.Update(.01);
        Assert.False(f.Results.Last().Success);
        if (!repairFails) Assert.Equal(original, File.ReadAllBytes(path));
        f.Handler.HandleMessage(f.Session, f.Request()); await f.Runner.Run(); f.Events.Update(.01);
        Assert.Equal(!repairFails, f.Results.Last().Success);
        Assert.Equal(repairFails ? 2 : 3, opened);
        if (repairFails) Assert.Equal(original, File.ReadAllBytes(path).Take(original.Length));
        else Assert.Equal(2, File.ReadAllLines(path).Length);
        Assert.Equal(3, f.Results.Count());
    }

    private sealed class BrokenAppend(string path, bool repairFails) : FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read)
    {
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            base.Write(buffer[..(buffer.Length / 2)]);
            throw new IOException("Owned partial append failure.");
        }
        public override void SetLength(long value)
        {
            if (repairFails) throw new IOException("Owned tail repair failure.");
            base.SetLength(value);
        }
    }

    private sealed class BrokenStore(bool canceled) : ISupportSubmissionStore
    {
        public bool TryAppend(IWorldSession session, string type, object payload) => throw new NotSupportedException();
        public Task<bool> AppendAsync(IWorldSession session, string type, object payload) => canceled
            ? Task.FromCanceled<bool>(new CancellationToken(true)) : Task.FromException<bool>(new IOException("Owned storage failure."));
    }

    private sealed class DeferredRunner : IBackgroundTaskRunner
    {
        internal readonly Queue<(Func<Task> Operation, Action<Exception> Failed)> Operations = new();
        internal bool RejectQueue;
        public void Queue(Func<Task> operation, Action<Exception> onException = null)
        {
            if (RejectQueue) throw new IOException("Owned worker admission failure.");
            Operations.Enqueue((operation, onException));
        }
        public void Observe(Task operation, Action onCompleted = null, Action<Exception> onException = null) => throw new NotSupportedException();
        internal async Task Run()
        {
            var work = Operations.Dequeue();
            try { await Task.Run(work.Operation); } catch (Exception error) { work.Failed?.Invoke(error); }
        }
    }

    private sealed class Frame : IDisposable
    {
        internal readonly DirectoryInfo Root = Directory.CreateTempSubdirectory("nf-support-ticket-");
        internal readonly EventQueue Events = new();
        internal readonly DeferredRunner Runner = new();
        internal readonly IWorldSession Session;
        internal readonly RecordingDispatchProxy<IWorldSession> Messages;
        internal readonly ClientSupportTicketHandler Handler;
        internal readonly FileSupportSubmissionStore Store;
        internal IEnumerable<ServerSupportTicketResult> Results => Messages.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(row => row.Arguments[0]).OfType<ServerSupportTicketResult>();
        internal Frame(bool blockedDirectory = false, ISupportSubmissionStore store = null, Func<string, FileStream> openFile = null)
        {
            Session = RecordingDispatchProxy<IWorldSession>.Create(out var messages); Messages = messages;
            messages.SetProperty(nameof(IWorldSession.Events), Events);
            var player = RecordingDispatchProxy<IPlayer>.Create(out var actor);
            actor.SetProperty(nameof(IPlayer.CharacterId), 42ul); actor.SetProperty(nameof(IPlayer.Session), Session);
            var account = RecordingDispatchProxy<IAccount>.Create(out var owner);
            owner.SetProperty(nameof(IAccount.Id), 12u); owner.SetProperty(nameof(IAccount.Session), Session);
            actor.SetProperty(nameof(IPlayer.Account), account);
            messages.SetProperty(nameof(IWorldSession.Player), player); messages.SetProperty(nameof(IWorldSession.Account), account);
            string directory = blockedDirectory ? Path.Combine(Root.FullName, "blocked") : Root.FullName;
            if (blockedDirectory) File.WriteAllText(directory, "obstruction");
            Store = new FileSupportSubmissionStore(NullLogger<FileSupportSubmissionStore>.Instance, Runner, directory, openFile);
            Handler = new(NullLogger<ClientSupportTicketHandler>.Instance, store ?? Store);
        }
        internal ClientSupportTicket Request()
        {
            var request = new ClientSupportTicket();
            foreach (var pair in new (string Name, object Value)[] { (nameof(ClientSupportTicket.Subject), "Subject"),
                (nameof(ClientSupportTicket.Body), "Body"), (nameof(ClientSupportTicket.LanguageId), Language.English) })
                typeof(ClientSupportTicket).GetProperty(pair.Name)!.SetValue(request, pair.Value);
            return request;
        }
        public void Dispose()
        {
            string path = Path.GetFullPath(Root.FullName), temp = Path.GetFullPath(Path.GetTempPath());
            if (!path.StartsWith(temp, StringComparison.OrdinalIgnoreCase) || !Root.Name.StartsWith("nf-support-ticket-", StringComparison.Ordinal))
                throw new InvalidOperationException("Not the owned support fixture directory.");
            Root.Delete(true);
        }
    }
}
