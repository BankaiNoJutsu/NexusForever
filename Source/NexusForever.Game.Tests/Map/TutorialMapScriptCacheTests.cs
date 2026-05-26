using System.Collections.Immutable;
using System.Numerics;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Map;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Script.Main.Tutorial;

namespace NexusForever.Game.Tests.Map;

public class TutorialMapScriptCacheTests
{
    [Fact]
    public void HasActiveCombatLaneAnchors_UsesCachedAnchorWhenActiveEntityIsNotYetPresent()
    {
        TutorialMapScript script = CreateScriptWithCachedEntities(
            new EntityModel
            {
                World = 3460,
                Creature = 73463u,
                X = -120f,
                Y = 0f,
                Z = 440f
            });

        bool result = InvokeHasActiveCombatLaneAnchors(script, (new Vector3(-120f, 0f, 440f), 5f, 73463u));

        Assert.True(result);
    }

    [Fact]
    public void HasActiveCombatLaneAnchors_ReturnsFalseWhenCacheDoesNotContainAnchor()
    {
        TutorialMapScript script = CreateScriptWithCachedEntities();

        bool result = InvokeHasActiveCombatLaneAnchors(script, (new Vector3(-120f, 0f, 440f), 5f, 73463u));

        Assert.False(result);
    }

    private static TutorialMapScript CreateScriptWithCachedEntities(params EntityModel[] cachedEntities)
    {
        ICinematicFactory cinematicFactory = RecordingDispatchProxy<ICinematicFactory>.Create(out _);
        IEntityFactory entityFactory = RecordingDispatchProxy<IEntityFactory>.Create(out _);
        IGlobalQuestManager globalQuestManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out _);
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out _);
        IEntityCacheManager entityCacheManager = RecordingDispatchProxy<IEntityCacheManager>.Create(out RecordingDispatchProxy<IEntityCacheManager> cacheManagerProxy);

        cacheManagerProxy.SetMethodReturn(
            nameof(IEntityCacheManager.GetEntityCache),
            new EntityCache(cachedEntities.ToImmutableList()));

        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 3460u });
        mapProxy.SetMethodReturnFactory(nameof(IBaseMap.Search), () => Array.Empty<IWorldEntity>());

        var script = new TutorialMapScript(
            NullLogger<TutorialMapScript>.Instance,
            cinematicFactory,
            entityCacheManager,
            entityFactory,
            globalQuestManager,
            gameTableManager);

        typeof(TutorialMapScript)
            .GetField("owner", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(script, map);

        return script;
    }

    private static bool InvokeHasActiveCombatLaneAnchors(
        TutorialMapScript script,
        params (Vector3 Position, float Radius, uint CreatureId)[] anchors)
    {
        MethodInfo method = typeof(TutorialMapScript).GetMethod(
            "HasActiveCombatLaneAnchors",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);

        return Assert.IsType<bool>(method.Invoke(script, [anchors]));
    }
}