using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Character;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Static.Guild;
using NexusForever.Game.Static.ICComm;
using NexusForever.Network.Message;

internal static class ICCommReconnectCommand
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    internal static void Validate(LoginWorldOptions options, string second, string outsider, ICCommChannelType type, bool allowed)
    {
        string[] users = [options.User, second, outsider];
        if (!allowed || users.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 3
            || users.Any(user => !Regex.IsMatch(user, @"^loadtest[0-9]+@example\.local$", RegexOptions.CultureInvariant))
            || new[] { options.StsHost, options.AuthHost, options.WorldHost }.Any(host => host is not ("127.0.0.1" or "localhost"))
            || type is not (ICCommChannelType.Global or ICCommChannelType.Guild))
            throw new InvalidOperationException("ICComm requires three distinct owned loopback loadtest users, explicit opt-in and Global or Guild scope.");
        var auth = new MySqlConnectionStringBuilder(options.AuthConnectionString);
        var character = new MySqlConnectionStringBuilder(options.CharacterConnectionString);
        if (auth.Server is not ("127.0.0.1" or "localhost") || character.Server != auth.Server || auth.Port != character.Port
            || !Regex.IsMatch(auth.Database, @"^nexus_assessment_[a-zA-Z0-9_]+_auth$", RegexOptions.CultureInvariant)
            || character.Database != auth.Database[..^5] + "_character")
            throw new InvalidOperationException("ICComm requires matching local assessment Auth/Character routes.");
    }

    internal static LoginWorldOptions Options(string path, string user, string password)
    {
        using var world = JsonDocument.Parse(File.ReadAllText(path));
        string directory = Directory.GetParent(Path.GetDirectoryName(Path.GetFullPath(path))!)!.FullName;
        using var auth = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "NexusForever.AuthServer", "AuthServer.json")));
        using var sts = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "NexusForever.StsServer", "StsServer.json")));
        JsonElement db = world.RootElement.GetProperty("Database");
        string Route(string name) { var row = db.GetProperty(name); if (row.ValueKind == JsonValueKind.Array) row = row[0]; return row.GetProperty("ConnectionString").GetString()!; }
        static string Host(JsonDocument doc) => doc.RootElement.GetProperty("Network").GetProperty("Host").GetString()!;
        static int Port(JsonDocument doc) => doc.RootElement.GetProperty("Network").GetProperty("Port").GetInt32();
        return new() { User = user, Password = password, AuthConnectionString = Route("Auth"), CharacterConnectionString = Route("Character"),
            StsHost = Host(sts), StsPort = Port(sts), AuthHost = Host(auth), AuthPort = Port(auth), WorldHost = Host(world), WorldPort = Port(world), ClientEnteredWorldDelayMs = 250 };
    }

    internal static async Task<int> Run(CliArgs args)
    {
        var options = Options(args.GetString("runtime-config", ""), args.GetString("user", ""), args.GetString("password", Defaults.Password));
        string second = args.GetString("second-user", ""), outsider = args.GetString("outsider-user", "");
        if (!Enum.TryParse<ICCommChannelType>(args.GetString("channel-type", "Global"), true, out var type)) throw new ArgumentException("Unknown ICComm scope.");
        Validate(options, second, outsider, type, args.Has("allow-owned-iccomm"));
        string output = Path.GetFullPath(args.GetString("output", ""));
        if (Directory.Exists(output)) throw new InvalidOperationException("Refusing to overwrite ICComm evidence.");
        Directory.CreateDirectory(output);
        var peers = new List<Peer>(); var steps = new List<object>();
        var owners = new Dictionary<string, AccountCharacter>(); var initial = new Dictionary<string, SqlState>(); var states = new List<object>();
        string nonce = Guid.NewGuid().ToString("N"); string alpha = "NF" + nonce + "Alpha界", beta = "NF" + nonce + "Beta界";
        var started = DateTime.UtcNow; uint messageId = 0; bool passed = false; string? failure = null;
        try
        {
            foreach (string user in new[] { options.User, second, outsider })
            {
                var owner = await LoginWorldCommand.LookupAccountCharacter(options, user); owners.Add(user, owner);
                await Offline(user, true); initial.Add(user, await Read(options, owner)); states.Add(new { Phase = "initial", User = user, State = initial[user] });
            }
            ulong guild = 0;
            if (type == ICCommChannelType.Guild)
            {
                using var db = new CharacterContext(Connection(options.CharacterConnectionString));
                async Task<ulong[]> Guilds(string user) => await db.GuildMember.Join(db.Guild.Where(g => g.Type == (byte)GuildType.Guild && g.DeleteTime == null), m => m.Id, g => g.Id,
                    (m, g) => new { m.CharacterId, g.Id }).Where(row => row.CharacterId == owners[user].CharacterId).Select(row => row.Id).ToArrayAsync();
                var a = await Guilds(options.User); var b = await Guilds(second);
                if (a.Length != 1 || !a.SequenceEqual(b) || (await Guilds(outsider)).Length != 0) throw new InvalidOperationException("Prepared same-Guild peers and an unaffiliated outsider required.");
                guild = a[0];
            }
            var alice = await Connect(options.User, "alice-1"); var bob = await Connect(second, "bob-1"); var carol = await Connect(outsider, "outsider-1");
            ulong barrier = await Join(carol, ICCommChannelType.Global, 0, "NF" + nonce + "Barrier");
            await RefuseJoin(carol, ICCommChannelType.Group, 0, alpha, ICCommJoinResult.NoGroup);
            await RefuseJoin(carol, ICCommChannelType.Guild, ulong.MaxValue, alpha, ICCommJoinResult.NoGuild);
            ulong channel = await Join(alice, type, guild, alpha);
            Require(await Join(bob, type, guild, alpha) == channel, "Peers did not share the exact named channel.");
            Require(await Join(bob, type, guild, alpha) == channel, "Repeated join changed the channel.");
            ulong other = await Join(alice, type, guild, beta);
            Require(await Join(bob, type, guild, beta) == other && other != channel, "Named channels collapsed.");
            await Send(alice, channel, "initial-broadcast", [bob], [carol]);
            await Send(bob, channel, "directed", [alice], [carol], alice.Name);
            await bob.World.SendEncrypted(GameMessageOpcode.ClientICCommChannelNotJoined, writer => writer.Write(channel));
            await Send(bob, channel, "after-not-joined", [], [alice, carol], expected: ICCommMessageResult.NotInChannel);
            await Send(alice, channel, "while-detached", [], [bob, carol]);
            await Send(bob, other, "other-name-survives", [alice], [carol]);
            Require(await Join(bob, type, guild, alpha) == channel, "Explicit rejoin lost the still-live channel.");
            await Send(bob, channel, "after-explicit-rejoin", [alice], [carol]);
            foreach (bool abort in new[] { false, true })
            {
                string phase = abort ? "abortive" : "normal";
                var previous = await Read(options, owners[second]);
                await bob.Close(abort); await Offline(second, false);
                await Send(alice, channel, phase + "-offline", [], [carol]);
                bob = await Connect(second, "bob-" + phase + "-reconnect");
                Require((await Read(options, owners[second])).Epoch > previous.Epoch, "Reconnect did not obtain a new World session epoch.");
                await Send(bob, channel, phase + "-before-join", [], [alice, carol], expected: ICCommMessageResult.NotInChannel);
                await Send(alice, channel, phase + "-not-auto-joined", [], [bob, carol]);
                Require(await Join(bob, type, guild, alpha) == channel, "Explicit reconnect join did not reuse the active channel.");
                await Send(bob, channel, phase + "-after-join", [alice], [carol]);
                await Send(alice, channel, phase + "-delivery-once", [bob], [carol]);
            }
            await bob.Close(false); await Offline(second, false);
            await alice.Close(false); await Offline(options.User, false);
            // An observed reply on the still-connected outsider crosses the
            // normal world tick/flush boundary after the other actors' cleanup.
            await Send(carol, barrier, "post-cleanup-tick-barrier", [], []);
            alice = await Connect(options.User, "alice-fresh"); bob = await Connect(second, "bob-fresh");
            ulong fresh = await Join(alice, type, guild, alpha);
            Require(fresh != channel, "The empty transient channel survived completed cleanup and a world tick.");
            Require(await Join(bob, type, guild, alpha) == fresh, "Fresh peers did not share the new channel.");
            await Send(alice, fresh, "fresh-channel", [bob], [carol]);
            foreach (var peer in peers) await peer.Close(false);
            foreach (string user in owners.Keys) { await Offline(user, false); states.Add(new { Phase = "final", User = user, State = await Read(options, owners[user]) }); }
            foreach (string user in owners.Keys)
            {
                var before = initial[user];
                var after = await Read(options, owners[user]);
                Require(before.EconomySHA256 == after.EconomySHA256 && before.EntitlementsSHA256 == after.EntitlementsSHA256, "ICComm changed fixture economy or entitlements.");
            }
            passed = true;

            async Task<Peer> Connect(string user, string label)
            {
                var peer = await Peer.Connect(options with { User = user }, label, owners[user]); peers.Add(peer);
                states.Add(new { Phase = label, User = user, State = await Read(options, owners[user]) }); return peer;
            }
            async Task<ulong> Join(Peer peer, ICCommChannelType scope, ulong ownerGuild, string name)
            {
                int before = peer.Frames.Length;
                await peer.World.SendEncrypted(GameMessageOpcode.ClientICCommChannelJoin, writer => ICCommProtocol.Join(writer, scope, ownerGuild, name));
                await Until(() => peer.Frames.Skip(before).Any(f => f.Opcode == GameMessageOpcode.ServerICCommChannelJoin && f.Type == scope && f.Name == name), "Join reply", peers);
                var row = peer.Frames.Skip(before).Single(f => f.Opcode == GameMessageOpcode.ServerICCommChannelJoin && f.Type == scope && f.Name == name);
                Require(row.Channel != 0, "Join returned a zero channel."); steps.Add(new { Kind = "join", peer.Label, scope, name, row.Channel }); return row.Channel;
            }
            async Task RefuseJoin(Peer peer, ICCommChannelType scope, ulong ownerGuild, string name, ICCommJoinResult result)
            {
                int before = peer.Frames.Length;
                await peer.World.SendEncrypted(GameMessageOpcode.ClientICCommChannelJoin, writer => ICCommProtocol.Join(writer, scope, ownerGuild, name));
                await Until(() => peer.Frames.Skip(before).Any(f => f.Opcode == GameMessageOpcode.ServerICCommChannelJoinResult && f.Type == scope && f.Name == name), "Join rejection", peers);
                Require(peer.Frames.Skip(before).Single(f => f.Opcode == GameMessageOpcode.ServerICCommChannelJoinResult && f.Type == scope && f.Name == name).Result == (int)result, "Wrong join rejection.");
                steps.Add(new { Kind = "join-rejected", scope, name, result });
            }
            async Task Send(Peer sender, ulong id, string phase, Peer[] recipients, Peer[] excluded, string target = "", ICCommMessageResult expected = ICCommMessageResult.Sent)
            {
                uint serial = ++messageId; string text = nonce + ":" + phase;
                await sender.World.SendEncrypted(GameMessageOpcode.ClientICCommMessage, writer => ICCommProtocol.Send(writer, id, serial, text, target));
                await Until(() => sender.Frames.Any(f => f.Opcode == GameMessageOpcode.ServerICCommMessageResult && f.MessageId == serial && f.Channel == id), phase + " result", peers);
                var result = sender.Frames.Single(f => f.Opcode == GameMessageOpcode.ServerICCommMessageResult && f.MessageId == serial && f.Channel == id);
                Require(result.Result == (int)expected, phase + " returned a different result.");
                if (expected == ICCommMessageResult.Sent)
                    await Until(() => sender.Frames.Any(f => f.Opcode == GameMessageOpcode.ServerICCommOrderedMessage && f.MessageId == serial && f.Channel == id)
                        && recipients.All(peer => peer.Frames.Any(f => f.Opcode == GameMessageOpcode.ServerICCommDirectedMessage && f.Text == text && f.Channel == id)), phase + " delivery", peers);
                await Task.Delay(500); foreach (var peer in peers) peer.Check();
                Require(sender.Frames.Count(f => f.Opcode == GameMessageOpcode.ServerICCommOrderedMessage && f.MessageId == serial && f.Text == text) == (expected == ICCommMessageResult.Sent ? 1 : 0), phase + " echo count differs.");
                if (expected == ICCommMessageResult.Sent)
                {
                    var frames = sender.Frames;
                    Require(Array.FindIndex(frames, f => f.Opcode == GameMessageOpcode.ServerICCommMessageResult && f.MessageId == serial && f.Channel == id)
                        < Array.FindIndex(frames, f => f.Opcode == GameMessageOpcode.ServerICCommOrderedMessage && f.MessageId == serial && f.Channel == id), phase + " echo preceded its result.");
                }
                foreach (var peer in recipients) Require(peer.Frames.Count(f => f.Opcode == GameMessageOpcode.ServerICCommDirectedMessage && f.Channel == id && f.Text == text && f.Sender == sender.Name) == 1, phase + " directed row differs.");
                foreach (var peer in excluded) Require(!peer.Frames.Any(f => f.Text == text), phase + " leaked to excluded peer.");
                steps.Add(new { Kind = "send", sender.Label, Channel = id, MessageId = serial, Text = text, Target = target, Expected = expected,
                    Recipients = recipients.Select(p => p.Label).ToArray(), Excluded = excluded.Select(p => p.Label).ToArray(), NegativeObservationMs = 500 });
            }
        }
        catch (Exception error)
        {
            failure = Scrub(error.Message); Console.Error.WriteLine(Scrub(error.ToString()));
        }
        finally
        {
            foreach (var peer in peers)
            {
                try { await peer.Close(false); }
                catch (Exception error) { passed = false; failure ??= Scrub(error.Message); await File.AppendAllTextAsync(Path.Combine(output, "cleanup-errors.log"), Scrub(error.Message) + Environment.NewLine); }
            }
            foreach (string user in owners.Keys)
            {
                try { await Offline(user, false); } catch (Exception error) { passed = false; failure ??= Scrub(error.Message); await File.AppendAllTextAsync(Path.Combine(output, "cleanup-errors.log"), Scrub(error.Message) + Environment.NewLine); }
            }
            await File.WriteAllTextAsync(Path.Combine(output, "transcripts.json"), JsonSerializer.Serialize(peers.Select(p => new { p.Label, p.Owner, p.Sample.Steps, p.Sample.CharacterList, Frames = p.Frames, p.AbortiveClose }), Json));
        }
        await Report(passed, failure);
        if (passed) Console.WriteLine($"ICComm {type} socket PASS: explicit join/message/cleanup, normal and abortive reconnect; no automatic membership replay.");
        return passed ? 0 : 1;
        async Task Offline(string user, bool allowNew)
        {
            var watch = Stopwatch.StartNew();
            while (true)
            {
                var state = await Read(options, owners[user]);
                if (!state.Online && state.Closed && (allowNew || state.Epoch != 0)) { states.Add(new { Phase = "closed", User = user, State = state }); return; }
                if (watch.Elapsed > TimeSpan.FromSeconds(30)) throw new TimeoutException("Saved offline Character/closed presence."); await Task.Delay(100);
            }
        }
        string Scrub(string value)
        {
            foreach (string secret in new[] { options.Password, new MySqlConnectionStringBuilder(options.AuthConnectionString).Password, new MySqlConnectionStringBuilder(options.CharacterConnectionString).Password }.Where(s => !string.IsNullOrEmpty(s)).Distinct()) value = value.Replace(secret, "[REDACTED]", StringComparison.Ordinal);
            return value;
        }
        Task Report(bool passed, string? error) => File.WriteAllTextAsync(Path.Combine(output, "iccomm-reconnect.json"), JsonSerializer.Serialize(new
        {
            Passed = passed, StartedUtc = started, CompletedUtc = DateTime.UtcNow, ChannelType = type, InitialUsers = owners, Steps = steps, States = states, Error = error,
            Scope = "Owned loopback protocol peers; read-only SQL observation; explicit rejoin, no automatic/native callback or retail policy claim. Negative observations bounded to recorded windows."
        }, Json));
    }

    private sealed record SqlState(string Name, bool Online, ulong Epoch, bool Closed, string EconomySHA256, string EntitlementsSHA256);
    private static DatabaseConnectionString Connection(string value) => new() { Provider = DatabaseProvider.MySql, ConnectionString = value };
    private static async Task<SqlState> Read(LoginWorldOptions options, AccountCharacter owner)
    {
        using var db = new CharacterContext(Connection(options.CharacterConnectionString));
        var player = await db.Character.AsNoTracking().SingleAsync(c => c.Id == owner.CharacterId && c.AccountId == owner.AccountId && c.DeleteTime == null);
        if (!player.Name.StartsWith("Load", StringComparison.Ordinal)) throw new InvalidOperationException("Owned Load character required.");
        var authRoute = new MySqlConnectionStringBuilder(options.AuthConnectionString); string friendship = authRoute.Database[..^5] + "_friendship";
        using var auth = new MySqlConnection(options.AuthConnectionString); await auth.OpenAsync(); using var command = auth.CreateCommand();
        command.CommandText = $"SELECT w.epoch,COALESCE(f.presenceSessionEpoch=w.epoch AND f.presenceClosed=1 AND f.activeCharacterId IS NULL AND f.activeRealmId IS NULL,0) FROM account_world_session w LEFT JOIN `{friendship}`.account f ON f.accountId=w.accountId WHERE w.accountId=@id"; command.Parameters.AddWithValue("@id", owner.AccountId);
        ulong epoch = 0; bool closed = true;
        using (var reader = await command.ExecuteReaderAsync()) if (await reader.ReadAsync()) { epoch = reader.GetUInt64(0); closed = reader.GetBoolean(1); }
        string Hash(IEnumerable<string> rows) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', rows.Order(StringComparer.Ordinal)))));
        var economy = new List<string>(); using var connection = new MySqlConnection(options.CharacterConnectionString); await connection.OpenAsync();
        foreach (string table in new[] { "character_currency", "item" })
        {
            using var select = connection.CreateCommand(); select.CommandText = $"SELECT * FROM `{table}` WHERE `{(table == "item" ? "ownerId" : "id")}`=@id"; select.Parameters.AddWithValue("@id", owner.CharacterId);
            using var reader = await select.ExecuteReaderAsync();
            while (await reader.ReadAsync()) economy.Add(table + ":" + string.Join(':', Enumerable.Range(0, reader.FieldCount).Select(i => reader.IsDBNull(i) ? "NULL" : Convert.ToString(reader.GetValue(i), CultureInfo.InvariantCulture))));
        }
        using var entitlements = auth.CreateCommand(); entitlements.CommandText = "SELECT entitlementId,amount FROM account_entitlement WHERE id=@id ORDER BY entitlementId"; entitlements.Parameters.AddWithValue("@id", owner.AccountId);
        var entries = new List<string>(); using (var reader = await entitlements.ExecuteReaderAsync()) while (await reader.ReadAsync()) entries.Add(reader.GetByte(0) + ":" + reader.GetUInt32(1));
        return new(player.Name, player.IsOnline, epoch, closed, Hash(economy), Hash(entries));
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static async Task Until(Func<bool> condition, string label, List<Peer> peers)
    {
        var watch = Stopwatch.StartNew(); while (!condition()) { foreach (var peer in peers) peer.Check(); if (watch.Elapsed > TimeSpan.FromSeconds(15)) throw new TimeoutException(label); await Task.Delay(25); }
    }
    private sealed class Peer
    {
        internal string Label { get; }
        internal string Name { get; }
        internal AccountCharacter Owner { get; }
        internal LoginWorldSample Sample { get; }
        internal GameProtocolClient World { get; }
        internal bool AbortiveClose { get; private set; }
        private readonly List<ICCommWire> frames;
        private readonly CancellationTokenSource cancellation = new();
        private readonly Task receiver;
        private bool closed;
        internal ICCommWire[] Frames { get { lock (frames) return frames.ToArray(); } }
        private Peer(string label, string name, AccountCharacter owner, LoginWorldSample sample, GameProtocolClient world, List<ICCommWire> observations)
        { Label = label; Name = name; Owner = owner; Sample = sample; World = world; frames = observations; receiver = Receive(); }
        internal static async Task<Peer> Connect(LoginWorldOptions options, string label, AccountCharacter owner)
        {
            var frames = new List<ICCommWire>(); var sample = new LoginWorldSample { User = options.User, Label = label, StartedUtc = DateTime.UtcNow };
            var connected = await LoginWorldCommand.ConnectForScenario(options, sample, frame => { var row = ICCommProtocol.Read(frame); if (row != null) lock (frames) frames.Add(row); });
            try
            {
                Require(connected.Character == owner, "Selected Character changed.");
                return new(label, (await Read(options, owner)).Name, owner, sample, connected.World, frames);
            }
            catch { await connected.World.DisposeAsync(); throw; }
        }
        private async Task Receive() { while (!cancellation.IsCancellationRequested) await World.ReadInnerFrame(cancellation.Token); }
        internal void Check() { if (!closed && receiver.IsCompleted) receiver.GetAwaiter().GetResult(); }
        internal async Task Close(bool abort)
        {
            if (closed) return; closed = true; AbortiveClose = abort;
            if (abort) World.Abort();
            cancellation.Cancel(); await World.DisposeAsync();
            try { await receiver; } catch (Exception error) when (error is OperationCanceledException or IOException or ObjectDisposedException or System.Net.Sockets.SocketException) { }
            cancellation.Dispose();
        }
    }
}
