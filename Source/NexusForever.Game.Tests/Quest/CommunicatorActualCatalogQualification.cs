using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Abstract.Reputation;
using NexusForever.Game.Quest;
using NexusForever.Game.Reputation;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Quest;

namespace NexusForever.Game.Tests.Quest;

public static class CommunicatorActualCatalogQualification
{
    public static Task RunAsync(string recoveryRoot, string outputDirectory)
        => CommunicatorLifecycleTests.RunActualCatalogAsync(recoveryRoot, outputDirectory);
}

public partial class CommunicatorLifecycleTests
{
    // Explicit offline entry point: actual data is not a discovery-time fixture.
    internal static async Task RunActualCatalogAsync(string recoveryRoot, string outputDirectory)
    {
        recoveryRoot = Path.GetFullPath(recoveryRoot); outputDirectory = Path.GetFullPath(outputDirectory);
        if (Directory.Exists(outputDirectory)) throw new IOException("Retain prior evidence; choose a new output directory.");
        Directory.CreateDirectory(outputDirectory);
        var json = new JsonSerializerOptions { WriteIndented = true, NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals };
        var started = DateTime.UtcNow;
        try
        {
            var (manifest, files) = ContractActualCatalogQualification.VerifyInputs(recoveryRoot);
            var tables = new GameTableManager(Options.Create(new GameTableConfig { GameTablePath = Path.Combine(recoveryRoot, "recovered-tables") }));
            await tables.Initialise(new GameTableLoader().AddGameTable<CommunicatorMessagesEntry>()
                .AddGameTable<Quest2Entry>().AddGameTable<QuestObjectiveEntry>().AddGameTable<Quest2RewardEntry>()
                .AddGameTable<Quest2DifficultyEntry>().AddGameTable<WorldZoneEntry>()
                .AddGameTable<Faction2Entry>().AddGameTable<GameFormulaEntry>());
            var rows = tables.CommunicatorMessages.Entries;
            Assert.Equal(4105, rows.Length);
            var index = CommunicatorIndex.Build(rows, id =>
            {
                var entry = tables.Quest2.GetEntry(id);
                return entry == null ? null : new CommunicatorQuestInfo((byte)entry.Objectives.Count(id => id != 0),
                    (entry.Flags & 0x20u) != 0 || entry.QuestRepeatPeriodEnum != 0, entry.Flags);
            });
            Assert.Equal(rows.Length, index.Messages.Count);
            var reputationRows = rows.Where(row => row.FactionIdReputation != 0).OrderBy(row => row.Id).ToArray();
            Assert.Equal(23, reputationRows.Length);
            Assert.Equal(8, reputationRows.Count(row => row.ReputationMin == 0 && row.ReputationMax == 0));
            File.WriteAllText(Path.Combine(outputDirectory, "inputs.json"), JsonSerializer.Serialize(new
            {
                ManifestSha256 = Hash(manifest), Files = files,
                ReaderSha256 = Hash(File.ReadAllBytes(typeof(GameTable<>).Assembly.Location)),
                RuntimeSha256 = Hash(File.ReadAllBytes(typeof(CommunicatorMessage).Assembly.Location)),
                ProbeSha256 = Hash(File.ReadAllBytes(typeof(CommunicatorActualCatalogQualification).Assembly.Location))
            }, json));
            File.WriteAllText(Path.Combine(outputDirectory, "classification.json"), JsonSerializer.Serialize(new
            {
                Rows = rows.Length, Kinds = index.Messages.Values.GroupBy(row => row.Kind).OrderBy(group => group.Key)
                    .Select(group => new { Kind = group.Key.ToString(), Count = group.Count() }),
                ChainTargets = index.ChainTargets.Count, TriggerArms = index.Triggers.Sum(pair => pair.Value.Length),
                MissingQuestReferences = rows.SelectMany(row => row.Quests.Append(row.QuestIdDelivered))
                    .Where(id => id != 0 && tables.Quest2.GetEntry(id) == null).Distinct().Order(),
                MissingChainTargets = index.ChainTargets.Where(id => !index.Messages.ContainsKey(id)).Order(),
                ReputationRows = reputationRows.Select(row => new { row.Id, row.ReputationMin, row.ReputationMax,
                    row.FactionIdReputation, row.QuestIdDelivered, Kind = index.Messages[row.Id].Kind.ToString() })
            }, json));

            var boundaries = new List<object>(); int boundaryFailures = 0;
            foreach (var row in reputationRows)
            foreach (float amount in new float[] { -1, 0, row.ReputationMin - 1f, row.ReputationMin,
                         row.ReputationMax, row.ReputationMax + 1f, float.NaN, float.NegativeInfinity, float.PositiveInfinity }.Distinct())
            {
                // The native finite comparisons bypass zero bounds. Retain the
                // existing server's NaN/-infinity guard and +infinity policy.
                bool rejected = float.IsNaN(amount) || float.IsNegativeInfinity(amount)
                    || row.ReputationMin != 0 && amount < row.ReputationMin
                    || row.ReputationMax != 0 && amount > row.ReputationMax;
                var actual = index.Messages[row.Id].Evaluate(new ReputationView(row, amount));
                bool passed = rejected == (actual == CommunicatorResult.Reputation);
                if (!passed) ++boundaryFailures;
                boundaries.Add(new { row.Id, Amount = amount, ExpectedReputationRejection = rejected, Actual = actual.ToString(), Passed = passed });
            }

            var route = Assert.Single(rows, row => row.QuestIdDelivered == 9159);
            Assert.Equal(5346u, route.Id);
            var reports = new List<object>(); int reportFailures = 0;
            foreach (var sample in new (float Amount, bool Engaged, bool Expected)[] { (-1f, true, true), (0f, true, true),
                         (float.NaN, true, false), (float.NegativeInfinity, true, false),
                         (float.PositiveInfinity, true, true), (-1f, false, false) })
            {
                using var f = new Fixture(tables, index, route);
                f.PlayerProxy.SetProperty(nameof(IPlayer.Level), 14u);
                f.PlayerProxy.SetProperty(nameof(IPlayer.Faction1), Faction.Dominion);
                f.PlayerProxy.SetProperty(nameof(IPlayer.Zone), tables.WorldZone.GetEntry(route.WorldZoneId));
                var factions = RecordingDispatchProxy<IFactionManager>.Create(out var factionManager);
                factionManager.SetMethodReturn(nameof(IFactionManager.GetFaction), new FactionNode(tables.Faction2.GetEntry(route.FactionIdReputation), tables));
                var model = new CharacterModel { Id = f.Player.CharacterId };
                model.Reputation.Add(new CharacterReputation { Id = model.Id, FactionId = route.FactionIdReputation, Amount = sample.Amount });
                var reputations = new ReputationManager(f.Player, model, factions);
                f.PlayerProxy.SetProperty(nameof(IPlayer.ReputationManager), reputations);
                Assert.IsType<NexusForever.Game.Reputation.Reputation>(reputations.GetReputation((Faction)route.FactionIdReputation));
                Assert.Equal(CommunicatorOfferBlock.None, f.Manager.GetCallOfferBlock(9159));
                Report(f, 9159, sample.Engaged);
                bool mentioned = f.Manager.GetQuestState(9159) == QuestState.Mentioned;
                bool passed = mentioned == sample.Expected;
                if (!passed) ++reportFailures;
                if (mentioned) await ReloadMention(f, 9159);
                reports.Add(new { sample.Amount, sample.Engaged, ExpectedMention = sample.Expected, Mentioned = mentioned, Passed = passed, ReloadVerified = mentioned });
            }
            File.WriteAllText(Path.Combine(outputDirectory, "result.json"), JsonSerializer.Serialize(new
            {
                ElapsedSeconds = (DateTime.UtcNow - started).TotalSeconds, BoundaryFailures = boundaryFailures, ReportFailures = reportFailures,
                Boundaries = boundaries, PublicReports = reports,
                Limit = "Full index classification, reputation predicate isolation and selected row5346/Q9159 parsed report/SQLite mention reload. Other gates use prepared player/map/services; no full4105-row delivery, physical/native/retail or provider qualification."
            }, json));
            Assert.Equal(0, boundaryFailures); Assert.Equal(0, reportFailures);
        }
        catch (Exception error)
        {
            File.WriteAllText(Path.Combine(outputDirectory, "failure.json"), JsonSerializer.Serialize(new { Error = error.ToString() }, json));
            throw;
        }
    }

    private static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
    private static void Report(Fixture f, ushort quest, bool engaged)
    {
        using var output = new MemoryStream();
        using (var writer = new GamePacketWriter(output)) { writer.Write(quest, 15); writer.Write(engaged); writer.FlushBits(); }
        using var reader = new GamePacketReader(new MemoryStream(output.ToArray()));
        var report = new ClientCommunicatorAction(); report.Read(reader);
        new ClientCommunicatorActionHandler(NullLogger<ClientCommunicatorActionHandler>.Instance).HandleMessage((IWorldSession)f.Player.Session, report);
    }

    private static async Task ReloadMention(Fixture f, ushort quest)
    {
        using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<CharacterContext>().UseSqlite(connection).Options;
        using var context = new CharacterContext(options);
        await context.Database.EnsureCreatedAsync();
        context.Character.Add(new CharacterModel { Id = 100, AccountId = 12, Name = "ActualCommunicator", OriginalName = "ActualCommunicator", CreateTime = DateTime.UtcNow, PathActivatedTimestamp = DateTime.UtcNow });
        await context.SaveChangesAsync();
        f.Manager.Save(context); await context.SaveChangesAsync();
        using var read = new CharacterContext(options);
        var restored = read.Character.AsNoTracking().Include(row => row.Quest).ThenInclude(row => row.QuestObjective).Single();
        Assert.Equal(quest, Assert.Single(restored.Quest).QuestId);
        Assert.Equal((byte)QuestState.Mentioned, Assert.Single(restored.Quest).State);
        f.Reload(restored); Report(f, quest, true);
        Assert.Equal(QuestState.Mentioned, f.Manager.GetQuestState(quest));
        Assert.Empty(f.Packets<NexusForever.Network.World.Message.Model.Story.ServerCommunicatorMessage>());
    }

    private sealed record ReputationView(CommunicatorMessagesEntry Row, float Amount) : ICommunicatorPlayerView
    {
        public uint WorldId => Row.WorldId;
        public Class Class => (Class)Row.ClassId;
        public Race Race => (Race)Row.RaceId;
        public Faction Faction => (Faction)Row.FactionId;
        public uint Level => Row.MinLevel;
        public bool IsInZone(uint zone) => true;
        public float GetReputation(Faction faction) => Amount;
        public CommunicatorQuestSnapshot? GetQuest(ushort quest) => null;
        public bool MeetsPrerequisite(uint prerequisite) => true;
        public CommunicatorOfferBlock CheckOffer(ushort quest) => CommunicatorOfferBlock.None;
    }
}
