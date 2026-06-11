using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Reputation;
using NexusForever.Game.Reputation;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Reputation;

namespace NexusForever.Game.Tests.Reputation;

public class ReputationManagerTests
{
    [Fact]
    public void UpdateReputation_NewHighStandingAwardsEveryReachedFactionLevel()
    {
        ICharacterAchievementManager achievementManager = RecordingDispatchProxy<ICharacterAchievementManager>.Create(out var achievementManagerProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out var sessionProxy);
        IPlayer player = TestPlayerBuilder.Create()
            .WithAchievementManager(achievementManager)
            .WithSession(session)
            .Build();

        IFactionNode factionNode = RecordingDispatchProxy<IFactionNode>.Create(out var factionNodeProxy);
        factionNodeProxy.SetProperty(nameof(IFactionNode.FactionId), Faction.Dominion);
        IFactionManager factionManager = BuildFactionManager(factionNode);

        var manager = new ReputationManager(player, new CharacterModel { Id = 42ul }, factionManager);

        manager.UpdateReputation(Faction.Dominion, 40000f);

        Assert.Equal(40000f, manager.GetReputation(Faction.Dominion)?.Amount);

        IReadOnlyList<RecordingDispatchProxy<ICharacterAchievementManager>.Invocation> calls = achievementManagerProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements));
        Assert.Collection(calls,
            call => AssertAchievementCall(call, player, AchievementType.ReputationLevel, (uint)Faction.Dominion, (uint)FactionLevel.Liked),
            call => AssertAchievementCall(call, player, AchievementType.ReputationLevel, (uint)Faction.Dominion, (uint)FactionLevel.Accepted),
            call => AssertAchievementCall(call, player, AchievementType.ReputationLevel, (uint)Faction.Dominion, (uint)FactionLevel.Popular),
            call => AssertAchievementCall(call, player, AchievementType.ReputationLevel, (uint)Faction.Dominion, (uint)FactionLevel.Esteemed),
            call => AssertAchievementCall(call, player, AchievementType.ReputationLevel, (uint)Faction.Dominion, (uint)FactionLevel.Beloved),
            call => AssertAchievementCall(call, player, AchievementType.GuildBelovedReputation, (uint)FactionLevel.Beloved, 0u));

        RecordingDispatchProxy<IGameSession>.Invocation sessionCall = Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        var update = Assert.IsType<ServerReputationUpdate>(sessionCall.Arguments[0]);
        Assert.Equal(Faction.Dominion, update.FactionId);
        Assert.Equal(40000f, update.ReputationDelta);
    }

    [Fact]
    public void UpdateReputation_LosingStandingDoesNotAwardProgressionAchievements()
    {
        ICharacterAchievementManager achievementManager = RecordingDispatchProxy<ICharacterAchievementManager>.Create(out var achievementManagerProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out var sessionProxy);
        IPlayer player = TestPlayerBuilder.Create()
            .WithAchievementManager(achievementManager)
            .WithSession(session)
            .Build();

        IFactionNode factionNode = RecordingDispatchProxy<IFactionNode>.Create(out var factionNodeProxy);
        factionNodeProxy.SetProperty(nameof(IFactionNode.FactionId), Faction.Dominion);
        IFactionManager factionManager = BuildFactionManager(factionNode);

        var model = new CharacterModel { Id = 42ul };
        model.Reputation.Add(new CharacterReputation
        {
            Id = 42ul,
            FactionId = (uint)Faction.Dominion,
            Amount = 5000f
        });

        var manager = new ReputationManager(player, model, factionManager);

        manager.UpdateReputation(Faction.Dominion, -4000f);

        Assert.Equal(1000f, manager.GetReputation(Faction.Dominion)?.Amount);
        Assert.Empty(achievementManagerProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements)));

        RecordingDispatchProxy<IGameSession>.Invocation sessionCall = Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        var update = Assert.IsType<ServerReputationUpdate>(sessionCall.Arguments[0]);
        Assert.Equal(-4000f, update.ReputationDelta);
    }

    private static IFactionManager BuildFactionManager(IFactionNode node)
    {
        IFactionManager factionManager = RecordingDispatchProxy<IFactionManager>.Create(out var factionManagerProxy);
        factionManagerProxy.SetMethodReturn(nameof(IFactionManager.GetFaction), node);
        return factionManager;
    }

    private static void AssertAchievementCall(
        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation invocation,
        IPlayer expectedTarget,
        AchievementType expectedType,
        uint expectedObjectId,
        uint expectedObjectIdAlt)
    {
        Assert.Same(expectedTarget, invocation.Arguments[0]);
        Assert.Equal(expectedType, invocation.Arguments[1]);
        Assert.Equal(expectedObjectId, invocation.Arguments[2]);
        Assert.Equal(expectedObjectIdAlt, invocation.Arguments[3]);
        Assert.Equal(1u, invocation.Arguments[4]);
    }
}
