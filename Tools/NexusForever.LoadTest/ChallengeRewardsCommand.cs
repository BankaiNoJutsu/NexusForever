using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Character;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Static.Entity;
using NexusForever.Network;
using NexusForever.Network.Message;

internal sealed record ChallengePlacement(uint EntityId, uint CreatureId, ushort World, ushort Zone, float X, float Y, float Z);
internal sealed record ChallengeRecipe(uint ChallengeId, uint TrackId, uint Spell4Id, uint MaximumPoints, uint[] Creatures, ChallengePlacement[] Placements);
internal sealed record ChallengeSqlState(bool Online, ulong Epoch, bool Closed, ChallengeWireState? Challenge, string? Attempt,
    uint Points, ulong Cycle, bool TrackActive, uint Claimed, uint[] Rows, int Claims, int Medals, int InventoryItems);

internal static class ChallengeRewardsCommand
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    internal static void ValidateFixture(string user, string authConnection, string characterConnection, bool placementAllowed)
    {
        if (!placementAllowed || !Regex.IsMatch(user, @"^loadtest[0-9]+@example\.local$", RegexOptions.CultureInvariant))
            throw new InvalidOperationException("Challenge scenario requires a prepared loadtest fixture and --allow-fixture-placement.");
        foreach (var pair in new[] { (authConnection, "_auth"), (characterConnection, "_character") })
        {
            var builder = new MySqlConnectionStringBuilder(pair.Item1);
            if (builder.Server is not ("127.0.0.1" or "localhost") || !Regex.IsMatch(builder.Database, @"^nexus_assessment_[a-zA-Z0-9_]+" + pair.Item2 + "$", RegexOptions.CultureInvariant))
                throw new InvalidOperationException("Challenge fixture writes require an owned local assessment schema.");
        }
        var auth = new MySqlConnectionStringBuilder(authConnection); var character = new MySqlConnectionStringBuilder(characterConnection);
        if (auth.Port != character.Port || auth.Database[..^5] != character.Database[..^10])
            throw new InvalidOperationException("Challenge fixture Auth and Character must belong to the same owned stack.");
    }

    internal static void ValidateRecipe(ChallengeRecipe recipe)
    {
        if (recipe.ChallengeId is 0 or > 0x3FFF || recipe.TrackId is 0 or > 0x3FFF || recipe.Spell4Id == 0 || recipe.MaximumPoints == 0
            || recipe.Creatures is not { Length: > 0 } || recipe.Placements is not { Length: > 0 and <= 1000 }
            || recipe.Placements.Any(p => p.EntityId == 0 || p.World == 0 || !recipe.Creatures.Contains(p.CreatureId)
                || !float.IsFinite(p.X) || !float.IsFinite(p.Y) || !float.IsFinite(p.Z)))
            throw new InvalidDataException("Invalid Challenge fixture recipe.");
    }

    internal static async Task<int> Run(CliArgs args)
    {
        string output = Path.GetFullPath(args.GetString("output", Path.Combine("artifacts", "load-tests", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-challenge")));
        if (Directory.Exists(output)) throw new InvalidOperationException("Refusing to overwrite a Challenge evidence directory.");
        Directory.CreateDirectory(output);
        var options = new LoginWorldOptions
        {
            User = args.GetString("user", ""), Password = args.GetString("password", Defaults.Password),
            StsHost = args.GetString("sts-host", "127.0.0.1"), AuthHost = args.GetString("auth-host", "127.0.0.1"), WorldHost = args.GetString("world-host", "127.0.0.1"),
            StsPort = args.GetInt("sts-port", 6600), AuthPort = args.GetInt("auth-port", 23115), WorldPort = args.GetInt("world-port", 24009),
            AuthConnectionString = args.GetString("auth-connection", Defaults.AuthConnectionString), CharacterConnectionString = args.GetString("character-connection", Defaults.CharacterConnectionString),
            ClientEnteredWorldDelayMs = 250
        };
        var sessions = new List<object>(); var started = DateTime.UtcNow; ChallengeSqlState? final = null; string? recipeHash = null;
        try
        {
            ValidateFixture(options.User, options.AuthConnectionString, options.CharacterConnectionString, args.Has("allow-fixture-placement"));
            if (new[] { options.StsHost, options.AuthHost, options.WorldHost }.Any(host => host is not ("127.0.0.1" or "localhost")))
                throw new InvalidOperationException("Challenge fixture protocol endpoints must be loopback.");
            string path = args.GetString("recipe", ""); var recipe = JsonSerializer.Deserialize<ChallengeRecipe>(await File.ReadAllTextAsync(path), Json) ?? throw new InvalidDataException("Missing recipe.");
            ValidateRecipe(recipe); recipeHash = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(path)));
            int maxKills = args.GetInt("max-kills", 100); if (maxKills is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(maxKills));
            int placementOffset = args.GetInt("placement-offset", 0);
            if (placementOffset < 0 || placementOffset >= recipe.Placements.Length) throw new ArgumentOutOfRangeException(nameof(placementOffset));
            var character = await LoginWorldCommand.LookupAccountCharacter(options, options.User);
            var state = await Read(options, character, recipe); int kills = 0;
            while (state.Points < recipe.MaximumPoints)
            {
                if (kills >= maxKills || kills >= recipe.Placements.Length - placementOffset) throw new InvalidOperationException("Earning exceeded the supplied fixture kill/placement bound.");
                var placement = recipe.Placements[placementOffset + kills];
                var prior = state.Challenge;
                await WaitOffline(options, character, recipe);
                await Place(options, character, placement);
                var observer = new ChallengeSocketObserver(recipe.ChallengeId, recipe.Spell4Id); var sample = new LoginWorldSample { User = options.User, Label = "challenge-combat", StartedUtc = DateTime.UtcNow };
                var connected = await LoginWorldCommand.ConnectForScenario(options, sample, observer.Receive);
                if (connected.Character.CharacterId != character.CharacterId) throw new InvalidOperationException("Selected fixture character changed.");
                await using (var session = new ReceivingWorld(connected.World))
                {
                    var before = state.Challenge; uint token = 1; int casts = 0; var watch = Stopwatch.StartNew();
                    while (!ChallengeSocketObserver.Advanced(before, observer.Challenge))
                    {
                        session.Check(); if (watch.Elapsed > TimeSpan.FromSeconds(75))
                            throw new TimeoutException("No real combat Challenge progress: " + JsonSerializer.Serialize(observer.CastResults));
                        var targets = observer.Targets(recipe.Creatures.ToHashSet());
                        if (targets.Length == 0) { await Task.Delay(100); continue; }
                        uint target = targets.Contains(observer.AcceptedTarget) ? observer.AcceptedTarget : targets[casts % targets.Length];
                        observer.SentCast(token, target);
                        await session.World.SendEncrypted(GameMessageOpcode.ClientCastSpellSelected, writer => ChallengeProtocol.WriteSelectedCast(writer,
                            token++, recipe.Spell4Id, target, placement.X, placement.Y, placement.Z));
                        ++casts; await Task.Delay(1100);
                    }
                    var observed = observer.Challenge!;
                    // A terminal/tier or first-activation update is provisional.
                    // Keep polling the socket until SQL and its published reload agree.
                    bool durable = before?.Active != true || observed.Completions > (before?.Completions ?? 0) || observed.Tier > (before?.Tier ?? 0);
                    if (durable)
                        await Until(async () =>
                        {
                            session.Check(); var sql = await Read(options, character, recipe); var wire = observer.Reward(recipe.TrackId);
                            return sql.Challenge?.Completions == observed.Completions && sql.Challenge.Active == observed.Active
                                && sql.Challenge.Tier == observed.Tier && sql.Challenge.Count >= observed.Count
                                && wire != null && wire.Points == sql.Points && wire.Rows.SequenceEqual(sql.Rows);
                        }, "Durable earning/published reload", TimeSpan.FromSeconds(20));
                    sessions.Add(new { Kind = "combat", Placement = placement, Casts = casts, Results = observer.CastResults, Before = before, Observed = observed, Reloads = observer.Reloads, Steps = sample.Steps });
                }
                await WaitOffline(options, character, recipe); state = await Read(options, character, recipe); ++kills;
                if (!ChallengeSocketObserver.Advanced(prior, state.Challenge))
                    throw new InvalidOperationException("Combat progress did not survive normal cleanup.");
                await File.WriteAllTextAsync(Path.Combine(output, $"kill-{kills:D3}.json"), JsonSerializer.Serialize(state, Json));
                Console.WriteLine($"Challenge kill{kills}: progress={state.Challenge?.Count}, completions={state.Challenge?.Completions}, points={state.Points}");
            }

            ulong cycle = state.Cycle; uint earned = state.Points; uint[] selected = state.Rows; int initialClaims = state.Claims;
            // Split the claim sequence across a real logout/login to verify its
            // selected rows and claim bit survive without resampling.
            for (int phase = 0; phase < 3; ++phase)
            {
                var observer = new ChallengeSocketObserver(recipe.ChallengeId); var sample = new LoginWorldSample { User = options.User, Label = "challenge-claim-relog", StartedUtc = DateTime.UtcNow };
                var connected = await LoginWorldCommand.ConnectForScenario(options, sample, observer.Receive);
                await using (var session = new ReceivingWorld(connected.World))
                {
                    await Until(() => Task.FromResult(observer.Reward(recipe.TrackId) != null), "Relog reward reload", TimeSpan.FromSeconds(15));
                    state = await Read(options, character, recipe); AssertWire(state, observer.Reward(recipe.TrackId)!);
                    if (phase < 2 && (state.Cycle != cycle || !state.Rows.SequenceEqual(selected))) throw new InvalidOperationException("Relog resampled the retained cycle.");
                    if (phase == 0)
                    {
                        long beforeChoose = observer.Reloads;
                        await session.World.SendEncrypted(GameMessageOpcode.ClientRewardTrackChooseTrack, w => w.Write(recipe.TrackId, 14));
                        await Until(async () => { session.Check(); return observer.ChoicePublishedSince(recipe.TrackId, beforeChoose)
                            && (await Read(options, character, recipe)).TrackActive; }, "Choose track published reload", TimeSpan.FromSeconds(15));
                    }
                    int end = phase == 0 ? Math.Min(1, selected.Length) : phase == 1 ? selected.Length : 0;
                    int begin = phase == 1 ? 1 : 0;
                    for (int ordinal = begin; ordinal < end; ++ordinal)
                    {
                        int expected = initialClaims + ordinal + 1; uint row = selected[ordinal];
                        int beforeItems = (await Read(options, character, recipe)).InventoryItems;
                        await session.World.SendEncrypted(GameMessageOpcode.ClientRewardTrackClaimRewardPoint, w => { w.Write(row, 14); w.Write(0u); });
                        await Until(async () =>
                        {
                            session.Check(); var sql = await Read(options, character, recipe); var wire = observer.Reward(recipe.TrackId);
                            return sql.Claims == expected && sql.InventoryItems == beforeItems + 1
                                && wire != null && wire.Points == sql.Points && wire.Claimed == sql.Claimed && wire.Rows.SequenceEqual(sql.Rows);
                        }, "Claim receipt/inventory/reload", TimeSpan.FromSeconds(20));
                    }
                    sessions.Add(new { Kind = "claim-relog", Phase = phase, Reloads = observer.Reloads, Wire = observer.Reward(recipe.TrackId), Steps = sample.Steps });
                }
                await WaitOffline(options, character, recipe);
            }
            final = await Read(options, character, recipe);
            if (final.Cycle != cycle + 1 || final.Points != earned - recipe.MaximumPoints || final.Claims != initialClaims + selected.Length || final.Claimed != 0)
                throw new InvalidOperationException("Final cycle reset/carry differs from the accepted policy.");
            await WriteReport(true, null); Console.WriteLine($"Challenge socket PASS: {kills} genuine combat kills, choose/claims/relog/reset; cycle{cycle}->{final.Cycle}."); return 0;
        }
        catch (Exception error)
        {
            string message = error.GetType().Name + ": " + error.Message;
            foreach (string secret in new[] { options.Password, new MySqlConnectionStringBuilder(options.AuthConnectionString).Password, new MySqlConnectionStringBuilder(options.CharacterConnectionString).Password }.Where(v => !string.IsNullOrEmpty(v)).Distinct())
                message = message.Replace(secret, "[REDACTED]", StringComparison.Ordinal);
            await WriteReport(false, message); Console.Error.WriteLine(message); return 1;
        }
        async Task WriteReport(bool passed, string? error) => await File.WriteAllTextAsync(Path.Combine(output, "challenge-rewards.json"), JsonSerializer.Serialize(new
        {
            Passed = passed, StartedUtc = started, CompletedUtc = DateTime.UtcNow, options.User, RecipeSHA256 = recipeHash, Sessions = sessions, Final = final, Error = error,
            Scope = "Genuine socket casts and rewards; explicit offline placement of an owned fixture between combat sessions. No reward SQL writes or forced completion. No native UI or walking proof."
        }, Json));
    }

    private static void AssertWire(ChallengeSqlState sql, RewardWireState wire)
    {
        if (sql.Points != wire.Points || sql.Claimed != wire.Claimed || sql.TrackActive != wire.Active || !sql.Rows.SequenceEqual(wire.Rows))
            throw new InvalidOperationException("Published reward reload differs from durable state.");
    }
    private static DatabaseConnectionString Connection(string value) => new() { Provider = DatabaseProvider.MySql, ConnectionString = value };
    private static async Task<ChallengeSqlState> Read(LoginWorldOptions options, AccountCharacter owner, ChallengeRecipe recipe)
    {
        await using var db = new CharacterContext(Connection(options.CharacterConnectionString));
        var player = await db.Character.AsNoTracking().SingleAsync(row => row.Id == owner.CharacterId && row.AccountId == owner.AccountId);
        var challenge = await db.CharacterChallenge.AsNoTracking().SingleOrDefaultAsync(row => row.Id == owner.CharacterId && row.ChallengeId == recipe.ChallengeId);
        var track = await db.CharacterChallengeRewardTrack.AsNoTracking().SingleOrDefaultAsync(row => row.Id == owner.CharacterId && row.TrackId == recipe.TrackId);
        var slots = track == null ? [] : await db.CharacterChallengeRewardSlot.AsNoTracking().Where(row => row.Id == owner.CharacterId && row.TrackId == recipe.TrackId && row.CycleId == track.CycleId).OrderBy(row => row.Ordinal).ToArrayAsync();
        uint mask = 0; foreach (var slot in slots.Where(row => row.Claimed)) mask |= 1u << slot.Ordinal;
        var authBuilder = new MySqlConnectionStringBuilder(options.AuthConnectionString); string friendship = authBuilder.Database[..^5] + "_friendship";
        await using var auth = new MySqlConnection(authBuilder.ConnectionString); await auth.OpenAsync();
        await using var command = auth.CreateCommand(); command.CommandText = $"SELECT w.epoch,COALESCE(f.presenceSessionEpoch=w.epoch AND f.presenceClosed=1 AND f.activeCharacterId IS NULL AND f.activeRealmId IS NULL,0) FROM account_world_session w LEFT JOIN `{friendship}`.account f ON f.accountId=w.accountId WHERE w.accountId=@id"; command.Parameters.AddWithValue("@id", owner.AccountId);
        ulong epoch = 0; bool closed = true; await using (var reader = await command.ExecuteReaderAsync()) if (await reader.ReadAsync()) { epoch = reader.GetUInt64(0); closed = reader.GetBoolean(1); }
        return new(player.IsOnline, epoch, closed, challenge == null ? null : new(challenge.ChallengeId, challenge.CurrentCount, 0, challenge.CurrentTier, challenge.LastRewardTier, challenge.CompletionCount, challenge.Activated), challenge?.ActiveAttemptId,
            track?.Points ?? 0, track?.CycleId ?? 0, track?.Active ?? false, mask, slots.Select(row => row.RewardRowId).ToArray(),
            await db.CharacterChallengeRewardClaim.CountAsync(row => row.Id == owner.CharacterId && row.TrackId == recipe.TrackId),
            await db.CharacterChallengeMedalReceipt.CountAsync(row => row.Id == owner.CharacterId), await db.Item.CountAsync(row => row.OwnerId == owner.CharacterId && row.Location == (ushort)InventoryLocation.Inventory));
    }
    private static async Task WaitOffline(LoginWorldOptions options, AccountCharacter owner, ChallengeRecipe recipe)
        => await Until(async () => { var state = await Read(options, owner, recipe); return !state.Online && state.Closed; }, "Normal offline cleanup/presence", TimeSpan.FromSeconds(30));
    private static async Task Place(LoginWorldOptions options, AccountCharacter owner, ChallengePlacement placement)
    {
        await using var db = new CharacterContext(Connection(options.CharacterConnectionString));
        var player = await db.Character.SingleAsync(row => row.Id == owner.CharacterId && row.AccountId == owner.AccountId);
        if (player.IsOnline || !player.Name.StartsWith("Load", StringComparison.Ordinal)) throw new InvalidOperationException("Fixture placement requires an offline owned Load character.");
        player.WorldId = placement.World; player.WorldZoneId = placement.Zone; player.LocationX = placement.X; player.LocationY = placement.Y; player.LocationZ = placement.Z + 2;
        player.RotationX = 0; player.RotationY = 0; player.RotationZ = 0;
        await db.SaveChangesAsync();
    }
    private static async Task Until(Func<Task<bool>> condition, string label, TimeSpan timeout)
    {
        var watch = Stopwatch.StartNew(); while (!await condition()) { if (watch.Elapsed > timeout) throw new TimeoutException(label); await Task.Delay(100); }
    }
    private sealed class ReceivingWorld : IAsyncDisposable
    {
        internal GameProtocolClient World { get; }
        private readonly CancellationTokenSource cancellation = new();
        private readonly Task receiver;
        internal ReceivingWorld(GameProtocolClient world)
        {
            World = world; receiver = Receive();
        }
        private async Task Receive() { while (!cancellation.IsCancellationRequested) _ = await World.ReadInnerFrame(cancellation.Token); }
        internal void Check() { if (receiver.IsCompleted) receiver.GetAwaiter().GetResult(); }
        public async ValueTask DisposeAsync()
        {
            cancellation.Cancel(); await World.DisposeAsync();
            try { await receiver; } catch (Exception error) when (error is OperationCanceledException or IOException or ObjectDisposedException) { }
            cancellation.Dispose();
        }
    }
}
