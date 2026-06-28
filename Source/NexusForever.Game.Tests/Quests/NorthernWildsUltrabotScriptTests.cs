using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Script.Main.Quests.NorthernWilds;
using NexusForever.Script.Template;

namespace NexusForever.Game.Tests.Quests;

public class NorthernWildsUltrabotScriptTests
{
    [Fact]
    public void OnAddToMap_DoesNotLaunchGuessedPathFromStaticSpawn()
    {
        IMovementManager movementManager = RecordingDispatchProxy<IMovementManager>.Create(out RecordingDispatchProxy<IMovementManager> movementProxy);
        ICreatureEntity owner = RecordingDispatchProxy<ICreatureEntity>.Create(out RecordingDispatchProxy<ICreatureEntity> ownerProxy);
        ownerProxy.SetProperty(nameof(ICreatureEntity.MovementManager), movementManager);

        var script = new Q3487UltrabotEntityScript();
        script.OnLoad(owner);
        ((IUnitScript)script).OnAddToMap(RecordingDispatchProxy<IBaseMap>.Create(out _));

        Assert.Empty(movementProxy.GetInvocations(nameof(IMovementManager.SetPositionPath)));
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
