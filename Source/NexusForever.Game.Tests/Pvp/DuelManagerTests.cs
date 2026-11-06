using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Abstract.Pvp;
using NexusForever.Game.Pvp;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Pvp;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Pvp;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;

namespace NexusForever.Game.Tests.Pvp;

public class DuelManagerTests
{
    [Fact]
    public void DuelingCheckIsRegisteredForNativeType204()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDuelManager>(new DuelManager());
        services.AddGamePrerequisite();
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.IsType<PrerequisiteCheckDueling>(provider.GetRequiredKeyedService<IPrerequisiteCheck>((PrerequisiteType)204));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void DuelingPrerequisiteUsesOnlyActiveSessionAndIgnoresTableValueFields(int phase)
    {
        var manager = new DuelManager();
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out _);
        IPlayer first = CreatePlayer(101, map, out _, out _);
        IPlayer second = CreatePlayer(202, map, out _, out _);
        IPlayer outsider = CreatePlayer(303, map, out _, out _);
        if (phase >= 1) Assert.Null(manager.Initiate(first, second));
        if (phase >= 2) Assert.Null(manager.Accept(second));
        if (phase >= 3) manager.Update(3.1);
        if (phase >= 4) Assert.True(manager.Forfeit(first));
        bool active = phase == 3;
        var check = new PrerequisiteCheckDueling(manager);
        Assert.Equal(active, manager.IsDueling(first));
        Assert.Equal(active, manager.IsDueling(second));
        Assert.False(manager.IsDueling(outsider));
        Assert.False(manager.IsDueling(null));
        foreach (IPlayer player in new[] { first, second })
        {
            Assert.Equal(active, check.Meets(player, PrerequisiteComparison.Equal, uint.MaxValue, 17, new PrerequisiteParameters()));
            Assert.Equal(!active, check.Meets(player, PrerequisiteComparison.NotEqual, 0, uint.MaxValue, new PrerequisiteParameters()));
        }
        Assert.False(check.Meets(outsider, PrerequisiteComparison.Equal, 0, 0, null));
        Assert.True(check.Meets(outsider, PrerequisiteComparison.NotEqual, 0, 0, null));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void DuelingPrerequisiteRejectsMissingEntityAndUnsupportedComparison(int mode)
    {
        var manager = new DuelManager();
        var check = new PrerequisiteCheckDueling(manager);
        Assert.False(check.Meets(null, (PrerequisiteComparison)mode, 0, 0, null));
        if (mode is not 1 and not 2)
        {
            IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
            Assert.False(check.Meets(player, (PrerequisiteComparison)mode, 0, 0, null));
        }
    }

    [Fact]
    public void Accept_TransitionsPendingChallengeThroughCountdownToActive()
    {
        var manager = new DuelManager();
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out _);
        IPlayer challenger = CreatePlayer(101u, map, out RecordingDispatchProxy<IWorldSession> challengerSessionProxy, out _);
        IPlayer opponent = CreatePlayer(202u, map, out RecordingDispatchProxy<IWorldSession> opponentSessionProxy, out _);

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
        IPlayer challenger = CreatePlayer(101u, map, out RecordingDispatchProxy<IWorldSession> challengerSessionProxy, out _);
        IPlayer opponent = CreatePlayer(202u, map, out RecordingDispatchProxy<IWorldSession> opponentSessionProxy, out _);

        Assert.Null(manager.Initiate(challenger, opponent));

        manager.Update(29.99d);

        Assert.Empty(GetMessages<ServerDuelResult>(challengerSessionProxy));

        manager.Update(0.02d);

        ServerDuelResult result = Assert.Single(GetMessages<ServerDuelResult>(challengerSessionProxy));
        Assert.Equal(0u, result.WinnerUnitId);
        Assert.Equal(0u, result.LoserUnitId);
        Assert.Equal(DuelFinishReason.DuelCancelled, result.Reason);
        Assert.Single(GetMessages<ServerDuelResult>(opponentSessionProxy));
        Assert.Equal(DuelFailureReason.CannotDuelRightNow, manager.Accept(opponent));
    }

    [Fact]
    public void PendingChallenge_DeclineEmitsResultWithoutWinnerOrLoser()
    {
        var manager = new DuelManager();
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out _);
        IPlayer challenger = CreatePlayer(101u, map, out RecordingDispatchProxy<IWorldSession> challengerSessionProxy, out _);
        IPlayer opponent = CreatePlayer(202u, map, out RecordingDispatchProxy<IWorldSession> opponentSessionProxy, out _);

        Assert.Null(manager.Initiate(challenger, opponent));

        Assert.True(manager.Decline(opponent));

        ServerDuelResult result = Assert.Single(GetMessages<ServerDuelResult>(challengerSessionProxy));
        Assert.Equal(0u, result.WinnerUnitId);
        Assert.Equal(0u, result.LoserUnitId);
        Assert.Equal(DuelFinishReason.DeclinedRequest, result.Reason);
        Assert.Single(GetMessages<ServerDuelResult>(opponentSessionProxy));
        Assert.False(manager.AreDueling(challenger, opponent));
    }

    [Fact]
    public void ActiveDisconnectCancelsDuelWithoutWinnerOrLoser()
    {
        var manager = new DuelManager();
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out _);
        IPlayer challenger = CreatePlayer(101u, map, out RecordingDispatchProxy<IWorldSession> challengerSessionProxy, out _);
        IPlayer opponent = CreatePlayer(202u, map, out RecordingDispatchProxy<IWorldSession> opponentSessionProxy, out _);

        Assert.Null(manager.Initiate(challenger, opponent));
        Assert.Null(manager.Accept(opponent));
        manager.Update(3d);

        manager.OnPlayerDisconnect(opponent);

        ServerDuelResult result = Assert.Single(GetMessages<ServerDuelResult>(challengerSessionProxy));
        Assert.Equal(0u, result.WinnerUnitId);
        Assert.Equal(0u, result.LoserUnitId);
        Assert.Equal(DuelFinishReason.DuelCancelled, result.Reason);
        Assert.Single(GetMessages<ServerDuelResult>(opponentSessionProxy));
        Assert.False(manager.AreDueling(challenger, opponent));
        Assert.False(manager.TryFinishDefeat(opponent, challenger));
    }

    [Fact]
    public void ActiveDefeat_EmitsResultAndUpdatesDuelAchievements()
    {
        var manager = new DuelManager();
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out _);
        IPlayer winner = CreatePlayer(101u, map, out RecordingDispatchProxy<IWorldSession> winnerSessionProxy, out RecordingDispatchProxy<ICharacterAchievementManager> winnerAchievementProxy);
        IPlayer loser = CreatePlayer(202u, map, out RecordingDispatchProxy<IWorldSession> loserSessionProxy, out RecordingDispatchProxy<ICharacterAchievementManager> loserAchievementProxy);

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
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        ICharacterAchievementManager achievementManager =
            RecordingDispatchProxy<ICharacterAchievementManager>.Create(out achievementProxy);

        playerProxy.SetProperty(nameof(IPlayer.Guid), guid);
        playerProxy.SetProperty(nameof(IPlayer.Map), map);
        playerProxy.SetProperty(nameof(IPlayer.InWorld), true);
        playerProxy.SetProperty(nameof(IPlayer.IsAlive), true);
        playerProxy.SetProperty(nameof(IPlayer.InCombat), false);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.Identity), new Identity { Id = guid, RealmId = 1 });
        var account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
        accountProxy.SetProperty(nameof(IAccount.Id), guid);
        accountProxy.SetProperty(nameof(IAccount.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        sessionProxy.SetProperty(nameof(IWorldSession.Account), account);
        sessionProxy.SetProperty(nameof(IWorldSession.Events), new EventQueue());
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievementManager);
        playerProxy.SetMethodReturn(nameof(IPlayer.HasFlag), false);

        return player;
    }

    private static IReadOnlyList<T> GetMessages<T>(RecordingDispatchProxy<IWorldSession> sessionProxy)
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
