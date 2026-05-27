using System.Numerics;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Static.Entity.Movement.Spline;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Script.Main.Quests.NorthernWilds;

namespace NexusForever.Game.Tests.Quests;

public class NorthernWildsUltrabotScriptTests
{
    [Fact]
    public void OnAddToMap_UsesCurrentPositionAndDestinationForLinearPath()
    {
        IMovementManager movementManager = RecordingDispatchProxy<IMovementManager>.Create(out RecordingDispatchProxy<IMovementManager> movementProxy);
        ICreatureEntity owner = RecordingDispatchProxy<ICreatureEntity>.Create(out RecordingDispatchProxy<ICreatureEntity> ownerProxy);
        ownerProxy.SetProperty(nameof(ICreatureEntity.MovementManager), movementManager);
        ownerProxy.SetProperty(nameof(ICreatureEntity.Position), new Vector3(1f, 2f, 3f));

        var script = new Q3487UltrabotEntityScript();
        script.OnLoad(owner);
        script.OnAddToMap(RecordingDispatchProxy<IBaseMap>.Create(out _));

        RecordingDispatchProxy<IMovementManager>.Invocation invocation = Assert.Single(movementProxy.GetInvocations(nameof(IMovementManager.SetPositionPath)));
        List<Vector3> path = Assert.IsType<List<Vector3>>(invocation.Arguments[0]);

        Assert.Equal(2, path.Count);
        Assert.Equal(new Vector3(1f, 2f, 3f), path[0]);
        Assert.Equal(new Vector3(4450f, -700f, -5150f), path[1]);
        Assert.Equal(SplineType.Linear, Assert.IsType<SplineType>(invocation.Arguments[1]));
    }

    [Fact]
    public void OnKilled_WhenPlayerHasNotCompletedAchievement_GrantsWarbotAchievement()
    {
        IPlayer player = CreatePlayerWithAchievementState(
            alreadyCompleted: false,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementManagerProxy);
        var script = new Q3487UltrabotEntityScript();

        script.OnLoad(RecordingDispatchProxy<ICreatureEntity>.Create(out _));
        script.OnKilled(player);

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation grant = Assert.Single(
            achievementManagerProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
        Assert.Equal((ushort)1296, grant.Arguments[0]);
    }

    [Fact]
    public void OnKilled_WhenPlayerAlreadyCompletedAchievement_DoesNotGrantAgain()
    {
        IPlayer player = CreatePlayerWithAchievementState(
            alreadyCompleted: true,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementManagerProxy);
        var script = new Q3487UltrabotEntityScript();

        script.OnLoad(RecordingDispatchProxy<ICreatureEntity>.Create(out _));
        script.OnKilled(player);

        Assert.Empty(achievementManagerProxy.GetInvocations(nameof(ICharacterAchievementManager.GrantAchievement)));
    }

    private static IPlayer CreatePlayerWithAchievementState(
        bool alreadyCompleted,
        out RecordingDispatchProxy<ICharacterAchievementManager> achievementManagerProxy)
    {
        ICharacterAchievementManager achievementManager = RecordingDispatchProxy<ICharacterAchievementManager>.Create(out achievementManagerProxy);
        achievementManagerProxy.SetMethodHandler(nameof(ICharacterAchievementManager.HasCompletedAchievement), args =>
        {
            Assert.Equal((ushort)1296, args[0]);
            return alreadyCompleted;
        });

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievementManager);
        return player;
    }
}
