using System.Reflection;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Entity;

public class SimpleEntityArchiveUnlockTests
{
    [Fact]
    public void OnActivate_UnlocksInteractArchiveArticleWithoutGrantingRewards()
    {
        const uint archiveArticleId = 456u;

        SimpleEntity entity = CreateSimpleEntity(new Creature2Entry
        {
            Id                             = 123u,
            ArchiveArticleIdInteractUnlock = archiveArticleId
        });
        IPlayer player = CreatePlayer(out var archiveManagerProxy);

        entity.OnActivate(player);

        RecordingDispatchProxy<IGalacticArchiveManager>.Invocation call =
            Assert.Single(archiveManagerProxy.GetInvocations(nameof(IGalacticArchiveManager.UnlockArticle)));
        Assert.Equal(archiveArticleId, call.Arguments[0]);
        Assert.Equal(false, call.Arguments[1]);
        Assert.Equal(true, call.Arguments[2]);
    }

    [Fact]
    public void OnActivate_WithNoInteractArchiveArticle_DoesNotRequestUnlock()
    {
        SimpleEntity entity = CreateSimpleEntity(new Creature2Entry
        {
            Id = 123u
        });
        IPlayer player = CreatePlayer(out var archiveManagerProxy);

        entity.OnActivate(player);

        Assert.Empty(archiveManagerProxy.GetInvocations(nameof(IGalacticArchiveManager.UnlockArticle)));
    }

    private static SimpleEntity CreateSimpleEntity(Creature2Entry creatureEntry)
    {
        IMovementManager movementManager = RecordingDispatchProxy<IMovementManager>.Create(out _);
        var entity = new SimpleEntity(movementManager);

        typeof(WorldEntity)
            .GetProperty(nameof(WorldEntity.CreatureEntry), BindingFlags.Instance | BindingFlags.Public)!
            .GetSetMethod(nonPublic: true)!
            .Invoke(entity, [creatureEntry]);

        return entity;
    }

    private static IPlayer CreatePlayer(out RecordingDispatchProxy<IGalacticArchiveManager> archiveManagerProxy)
    {
        ICharacterAchievementManager achievementManager = RecordingDispatchProxy<ICharacterAchievementManager>.Create(out _);
        IGalacticArchiveManager archiveManager = RecordingDispatchProxy<IGalacticArchiveManager>.Create(out archiveManagerProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);

        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievementManager);
        playerProxy.SetProperty(nameof(IPlayer.GalacticArchiveManager), archiveManager);

        return player;
    }
}
