using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Leaderboard;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.WorldServer.Leaderboard;
using NexusForever.WorldServer.Service;

namespace NexusForever.Game.Tests.Leaderboard;

public class LeaderboardScoreIngestionTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "leaderboard-ingestion-" + Guid.NewGuid().ToString("N"));
    private readonly CharacterDatabase database = new();
    private readonly InlineRunner runner = new();
    private readonly LeaderboardScoreIngestion ingestion;
    private readonly DatabaseLeaderboardStore store;

    public LeaderboardScoreIngestionTests()
    {
        Directory.CreateDirectory(directory);
        database.Initialise(new DatabaseConnectionString
        {
            Provider = DatabaseProvider.Sqlite,
            ConnectionString = $"Data Source={Path.Combine(directory, "character.sqlite")};Pooling=False"
        });
        using (var context = database.CreateContext()) context.Database.EnsureCreated();

        IDatabaseManager manager = RecordingDispatchProxy<IDatabaseManager>.Create(out var managerProxy);
        managerProxy.SetMethodReturn(nameof(IDatabaseManager.GetDatabase), database);
        IRealmContext realm = RecordingDispatchProxy<IRealmContext>.Create(out var realmProxy);
        realmProxy.SetProperty(nameof(IRealmContext.RealmId), (ushort)7);
        store = new DatabaseLeaderboardStore(manager, realm);
        ingestion = new LeaderboardScoreIngestion(manager, realm, store, runner);
    }

    public void Dispose()
    {
        try { Directory.Delete(directory, true); } catch (IOException) { }
    }

    [Fact]
    public void RecordPveCompletion_ZeroTimeOrMissingPlayer_RecordsNothing()
    {
        ingestion.RecordPveCompletion(Player(1ul), LeaderboardType.PveDungeon, 5u, 0u, 0u, 1u);
        ingestion.RecordPveCompletion(null, LeaderboardType.PveDungeon, 5u, 0u, 100u, 1u);
        ingestion.RecordPvpRating(Player(1ul), LeaderboardType.Arena3v3, 0u);

        using var context = database.CreateContext();
        Assert.Empty(context.LeaderboardPveScore);
        Assert.Empty(context.LeaderboardPvpScore);
        Assert.Equal(0, runner.Observed);
    }

    [Fact]
    public void RecordPveCompletion_PersistsScoreWithSequentialIdsAndInvalidatesCache()
    {
        ingestion.RecordPveCompletion(Player(11ul), LeaderboardType.PveDungeon, 5u, 2u, 300u, 3u,
            [new LeaderboardTeamMemberRecord()]);
        ingestion.RecordPveCompletion(Player(12ul), LeaderboardType.PveDungeon, 5u, 2u, 250u, 2u);

        using var context = database.CreateContext();
        var rows = context.LeaderboardPveScore.AsNoTracking().AsEnumerable().OrderBy(row => row.Id).ToArray();
        Assert.Equal(2, rows.Length);
        Assert.Equal(rows[0].Id + 1ul, rows[1].Id);
        Assert.Equal(11ul, rows[0].CharacterId);
        Assert.Equal((ushort)7, rows[0].RealmId);
        Assert.Equal(300u, rows[0].CompletionTime);
        Assert.Equal(3u, rows[0].RewardedTier);
        Assert.NotNull(rows[0].TeamMembersJson);
        Assert.Null(rows[1].TeamMembersJson);
        Assert.Equal(2, runner.Completed);
    }

    [Fact]
    public void RecordPvpRating_PersistsScore()
    {
        ingestion.RecordPvpRating(Player(21ul), LeaderboardType.Arena3v3, 1750u);

        using var context = database.CreateContext();
        var row = Assert.Single(context.LeaderboardPvpScore.AsNoTracking());
        Assert.Equal(21ul, row.CharacterId);
        Assert.Equal(1750u, row.Rating);
        Assert.Equal(1, runner.Completed);
    }

    [Fact]
    public void ScoreIdsContinueAfterExistingRowsOnRestart()
    {
        ingestion.RecordPvpRating(Player(31ul), LeaderboardType.Arena3v3, 1500u);
        ulong first;
        using (var context = database.CreateContext()) first = context.LeaderboardPvpScore.Single().Id;

        IDatabaseManager manager = RecordingDispatchProxy<IDatabaseManager>.Create(out var managerProxy);
        managerProxy.SetMethodReturn(nameof(IDatabaseManager.GetDatabase), database);
        IRealmContext realm = RecordingDispatchProxy<IRealmContext>.Create(out var realmProxy);
        realmProxy.SetProperty(nameof(IRealmContext.RealmId), (ushort)7);
        var restarted = new LeaderboardScoreIngestion(manager, realm, new DatabaseLeaderboardStore(manager, realm), runner);
        restarted.RecordPvpRating(Player(32ul), LeaderboardType.Arena3v3, 1600u);

        using var read = database.CreateContext();
        Assert.Equal([first, first + 1ul], read.LeaderboardPvpScore.AsNoTracking().Select(row => row.Id).AsEnumerable().OrderBy(id => id).ToArray());
    }

    private static IPlayer Player(ulong id)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetProperty(nameof(IPlayer.CharacterId), id);
        proxy.SetProperty(nameof(IPlayer.Name), "Player" + id);
        proxy.SetProperty(nameof(IPlayer.Class), Class.Esper);
        IGuildManager guilds = RecordingDispatchProxy<IGuildManager>.Create(out _);
        proxy.SetProperty(nameof(IPlayer.GuildManager), guilds);
        return player;
    }

    private sealed class InlineRunner : IBackgroundTaskRunner
    {
        internal int Observed, Completed;
        public void Queue(Func<Task> operation, Action<Exception> onException = null) => operation().GetAwaiter().GetResult();
        public void Observe(Task operation, Action onCompleted = null, Action<Exception> onException = null)
        {
            Observed++;
            try { operation.GetAwaiter().GetResult(); Completed++; onCompleted?.Invoke(); }
            catch (Exception error) { onException?.Invoke(error); }
        }
    }
}
