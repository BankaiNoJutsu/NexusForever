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

    [Fact]
    public void OnActivateCast_UnlocksInteractArchiveArticleWithoutGrantingRewards()
    {
        const uint archiveArticleId = 789u;

        SimpleEntity entity = CreateSimpleEntity(new Creature2Entry
        {
            Id                             = 123u,
            ArchiveArticleIdInteractUnlock = archiveArticleId
        });
        IPlayer player = CreatePlayer(out var archiveManagerProxy);

        entity.OnActivateCast(player);

        RecordingDispatchProxy<IGalacticArchiveManager>.Invocation call =
            Assert.Single(archiveManagerProxy.GetInvocations(nameof(IGalacticArchiveManager.UnlockArticle)));
        Assert.Equal(archiveArticleId, call.Arguments[0]);
        Assert.Equal(false, call.Arguments[1]);
        Assert.Equal(true, call.Arguments[2]);
    }

    [Fact]
    public void OnActivate_WithDatacube_AddsDatacubeWithFullProgress()
    {
        const uint datacubeId = 321u;

        SimpleEntity entity = CreateSimpleEntity(new Creature2Entry
        {
            Id         = 123u,
            DatacubeId = datacubeId
        });
        IPlayer player = CreatePlayer(out _, out var datacubeManagerProxy);

        entity.OnActivate(player);

        RecordingDispatchProxy<IDatacubeManager>.Invocation call =
            Assert.Single(datacubeManagerProxy.GetInvocations(nameof(IDatacubeManager.AddDatacube)));
        Assert.Equal((ushort)datacubeId, call.Arguments[0]);
        Assert.Equal((uint)int.MaxValue, call.Arguments[1]);
    }

    [Fact]
    public void OnActivateCast_WithMissingDatacube_AddsDatacubeWithChecklistProgress()
    {
        const uint datacubeId = 322u;

        SimpleEntity entity = CreateSimpleEntity(new Creature2Entry
        {
            Id         = 123u,
            DatacubeId = datacubeId
        }, questChecklistIdx: 3);
        IPlayer player = CreatePlayer(out _, out var datacubeManagerProxy);

        entity.OnActivateCast(player);

        RecordingDispatchProxy<IDatacubeManager>.Invocation call =
            Assert.Single(datacubeManagerProxy.GetInvocations(nameof(IDatacubeManager.AddDatacube)));
        Assert.Equal((ushort)datacubeId, call.Arguments[0]);
        Assert.Equal(8u, call.Arguments[1]);
    }

    [Fact]
    public void OnActivateCast_WithMissingDatacubeVolume_AddsVolumeWithChecklistProgress()
    {
        const uint volumeId = 654u;

        SimpleEntity entity = CreateSimpleEntity(new Creature2Entry
        {
            Id               = 123u,
            DatacubeVolumeId = volumeId
        }, questChecklistIdx: 4);
        IPlayer player = CreatePlayer(out _, out var datacubeManagerProxy);

        entity.OnActivateCast(player);

        RecordingDispatchProxy<IDatacubeManager>.Invocation call =
            Assert.Single(datacubeManagerProxy.GetInvocations(nameof(IDatacubeManager.AddDatacubeVolume)));
        Assert.Equal((ushort)volumeId, call.Arguments[0]);
        Assert.Equal(16u, call.Arguments[1]);
    }

    private static SimpleEntity CreateSimpleEntity(Creature2Entry creatureEntry, byte questChecklistIdx = 0)
    {
        IMovementManager movementManager = RecordingDispatchProxy<IMovementManager>.Create(out _);
        var entity = new SimpleEntity(movementManager);

        typeof(WorldEntity)
            .GetProperty(nameof(WorldEntity.CreatureEntry), BindingFlags.Instance | BindingFlags.Public)!
            .GetSetMethod(nonPublic: true)!
            .Invoke(entity, [creatureEntry]);

        typeof(WorldEntity)
            .GetProperty(nameof(WorldEntity.QuestChecklistIdx), BindingFlags.Instance | BindingFlags.Public)!
            .GetSetMethod(nonPublic: true)!
            .Invoke(entity, [questChecklistIdx]);

        return entity;
    }

    private static IPlayer CreatePlayer(out RecordingDispatchProxy<IGalacticArchiveManager> archiveManagerProxy)
    {
        return CreatePlayer(out archiveManagerProxy, out _);
    }

    private static IPlayer CreatePlayer(
        out RecordingDispatchProxy<IGalacticArchiveManager> archiveManagerProxy,
        out RecordingDispatchProxy<IDatacubeManager> datacubeManagerProxy)
    {
        ICharacterAchievementManager achievementManager = RecordingDispatchProxy<ICharacterAchievementManager>.Create(out _);
        IGalacticArchiveManager archiveManager = RecordingDispatchProxy<IGalacticArchiveManager>.Create(out archiveManagerProxy);
        IDatacubeManager datacubeManager = RecordingDispatchProxy<IDatacubeManager>.Create(out datacubeManagerProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);

        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievementManager);
        playerProxy.SetProperty(nameof(IPlayer.GalacticArchiveManager), archiveManager);
        playerProxy.SetProperty(nameof(IPlayer.DatacubeManager), datacubeManager);

        return player;
    }
}
