using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Pvp;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Pvp;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Pvp;

namespace NexusForever.Game.Tests.Pvp;

public class DuelManagerTests
{
    [Fact]
    public void Accept_TransitionsPendingChallengeThroughCountdownToActive()
    {
        var manager = new DuelManager();
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out _);
        IPlayer challenger = CreatePlayer(101u, map, out RecordingDispatchProxy<IGameSession> challengerSessionProxy, out _);
        IPlayer opponent = CreatePlayer(202u, map, out RecordingDispatchProxy<IGameSession> opponentSessionProxy, out _);

        Assert.Null(manager.Initiate(challenger, opponent));

        ServerDuelChallenge challenge = Assert.Single(GetMessages<ServerDuelChallenge>(challengerSessionProxy));
        Assert.Equal(101u, challenge.ChallengerUnitId);
        Assert.Equal(202u, challenge.OpponentUnitId);
        Assert.Single(GetMessages<ServerDuelChallenge>(opponentSessionProxy));
        Assert.False(manager.AreDueling(challenger, opponent));

        Assert.Null(manager.Accept(opponent));

        ServerDuelCountdown countdown = Assert.Single(GetMessages<ServerDuelCountdown>(challengerSessionProxy));
        Assert.Equal(101u, countdown.ChallengerUnitId);
        Assert.Equal(202u, countdown.OpponentUnitId);
        Assert.Single(GetMessages<ServerDuelCountdown>(opponentSessionProxy));

        manager.Update(2.99d);

        Assert.Empty(GetMessages<ServerDuelStart>(challengerSessionProxy));
        Assert.False(manager.AreDueling(challenger, opponent));

        manager.Update(0.02d);

        ServerDuelStart start = Assert.Single(GetMessages<ServerDuelStart>(challengerSessionProxy));
        Assert.Equal(101u, start.ChallengerUnitId);
        Assert.Equal(202u, start.OpponentUnitId);
        Assert.Single(GetMessages<ServerDuelStart>(opponentSessionProxy));
        Assert.True(manager.AreDueling(challenger, opponent));
    }

    [Fact]
    public void PendingChallenge_TimesOutWithCancelledResult()
    {
        var manager = new DuelManager();
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out _);
        IPlayer challenger = CreatePlayer(101u, map, out RecordingDispatchProxy<IGameSession> challengerSessionProxy, out _);
        IPlayer opponent = CreatePlayer(202u, map, out RecordingDispatchProxy<IGameSession> opponentSessionProxy, out _);

        Assert.Null(manager.Initiate(challenger, opponent));

        manager.Update(29.99d);

        Assert.Empty(GetMessages<ServerDuelResult>(challengerSessionProxy));

        manager.Update(0.02d);

        ServerDuelResult result = Assert.Single(GetMessages<ServerDuelResult>(challengerSessionProxy));
        Assert.Equal(101u, result.WinnerUnitId);
        Assert.Equal(202u, result.LoserUnitId);
        Assert.Equal(DuelFinishReason.DuelCancelled, result.Reason);
        Assert.Single(GetMessages<ServerDuelResult>(opponentSessionProxy));
        Assert.Equal(DuelFailureReason.CannotDuelRightNow, manager.Accept(opponent));
    }

    [Fact]
    public void ActiveDefeat_EmitsResultAndUpdatesDuelAchievements()
    {
        var manager = new DuelManager();
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out _);
        IPlayer winner = CreatePlayer(101u, map, out RecordingDispatchProxy<IGameSession> winnerSessionProxy, out RecordingDispatchProxy<ICharacterAchievementManager> winnerAchievementProxy);
        IPlayer loser = CreatePlayer(202u, map, out RecordingDispatchProxy<IGameSession> loserSessionProxy, out RecordingDispatchProxy<ICharacterAchievementManager> loserAchievementProxy);

        Assert.Null(manager.Initiate(winner, loser));
        Assert.Null(manager.Accept(loser));
        manager.Update(3d);

        Assert.True(manager.TryFinishDefeat(loser, winner));

        ServerDuelResult result = Assert.Single(GetMessages<ServerDuelResult>(winnerSessionProxy));
        Assert.Equal(101u, result.WinnerUnitId);
        Assert.Equal(202u, result.LoserUnitId);
        Assert.Equal(DuelFinishReason.Defeated, result.Reason);
        Assert.Single(GetMessages<ServerDuelResult>(loserSessionProxy));
        Assert.False(manager.AreDueling(winner, loser));

        IReadOnlyList<RecordingDispatchProxy<ICharacterAchievementManager>.Invocation> winnerAchievements =
            winnerAchievementProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements));
        IReadOnlyList<RecordingDispatchProxy<ICharacterAchievementManager>.Invocation> loserAchievements =
            loserAchievementProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements));

        Assert.Collection(winnerAchievements,
            call => Assert.Equal(AchievementType.DuelParticipate, (AchievementType)call.Arguments[1]),
            call => Assert.Equal(AchievementType.DuelWin, (AchievementType)call.Arguments[1]));
        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation loserAchievement = Assert.Single(loserAchievements);
        Assert.Equal(AchievementType.DuelParticipate, (AchievementType)loserAchievement.Arguments[1]);
    }

    private static IPlayer CreatePlayer(
        uint guid,
        IBaseMap map,
        out RecordingDispatchProxy<IGameSession> sessionProxy,
        out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
        ICharacterAchievementManager achievementManager =
            RecordingDispatchProxy<ICharacterAchievementManager>.Create(out achievementProxy);

        playerProxy.SetProperty(nameof(IPlayer.Guid), guid);
        playerProxy.SetProperty(nameof(IPlayer.Map), map);
        playerProxy.SetProperty(nameof(IPlayer.InWorld), true);
        playerProxy.SetProperty(nameof(IPlayer.IsAlive), true);
        playerProxy.SetProperty(nameof(IPlayer.InCombat), false);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievementManager);
        playerProxy.SetMethodReturn(nameof(IPlayer.HasFlag), false);

        return player;
    }

    private static IReadOnlyList<T> GetMessages<T>(RecordingDispatchProxy<IGameSession> sessionProxy)
        where T : class, IWritable
    {
        return sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Where(i => i.Arguments.Length == 1)
            .Select(i => i.Arguments[0])
            .OfType<T>()
            .ToList();
    }
}
