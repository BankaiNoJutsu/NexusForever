using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Script.Main.Tutorial;

namespace NexusForever.Game.Tests.Map;

public class TutorialCombatMineEntityScriptTests
{
    [Fact]
    public void Update_BeforeDangerZoneCastTimeElapsed_KeepsTelegraphWarningActive()
    {
        MineHarness harness = CreateHarness(castTimeMs: 250u);

        harness.Script.OnActivateSuccess(harness.Player);
        Thread.Sleep(25);
        harness.Script.Update(0.025d);

        Assert.Single(GetVisibleMessages<ServerSpellStart>(harness.OwnerProxy));
        Assert.Empty(GetVisibleMessages<ServerSpellGo>(harness.OwnerProxy));
        Assert.Empty(GetVisibleMessages<ServerSpellFinish>(harness.OwnerProxy));
        Assert.Empty(harness.OwnerProxy.GetInvocations(nameof(IWorldEntity.RemoveBusy)));
    }

    [Fact]
    public void Update_AfterDangerZoneCastTimeElapsed_DetonatesTelegraph()
    {
        MineHarness harness = CreateHarness(castTimeMs: 1u);

        harness.Script.OnActivateSuccess(harness.Player);
        Thread.Sleep(25);
        harness.Script.Update(0.025d);

        Assert.Single(GetVisibleMessages<ServerSpellStart>(harness.OwnerProxy));
        Assert.Single(GetVisibleMessages<ServerSpellGo>(harness.OwnerProxy));
        Assert.Single(GetVisibleMessages<ServerSpellFinish>(harness.OwnerProxy));
        Assert.Single(harness.OwnerProxy.GetInvocations(nameof(IWorldEntity.RemoveBusy)));
    }

    private static MineHarness CreateHarness(uint castTimeMs)
    {
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 3460u });
        mapProxy.SetMethodHandler(nameof(IBaseMap.Search), _ => Array.Empty<IPlayer>());

        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out RecordingDispatchProxy<IQuestManager> questManagerProxy);
        questManagerProxy.SetMethodHandler(nameof(IQuestManager.GetQuestState), args =>
            (ushort)args[0] == 10518 ? QuestState.Accepted : null);

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 200u);
        playerProxy.SetProperty(nameof(IPlayer.Map), map);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);

        ISimpleCollidableEntity owner = RecordingDispatchProxy<ISimpleCollidableEntity>.Create(out RecordingDispatchProxy<ISimpleCollidableEntity> ownerProxy);
        ownerProxy.SetProperty(nameof(IWorldEntity.Guid), 100u);
        ownerProxy.SetProperty(nameof(IWorldEntity.CreatureId), 73463u);
        ownerProxy.SetProperty(nameof(IWorldEntity.Map), map);
        ownerProxy.SetProperty(nameof(IWorldEntity.Position), Vector3.Zero);
        ownerProxy.SetProperty(nameof(IWorldEntity.Rotation), Vector3.Zero);
        ownerProxy.SetProperty(nameof(IWorldEntity.InWorld), true);
        ownerProxy.SetMethodReturn(nameof(IWorldEntity.RemoveBusy), true);

        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> gameTableManagerProxy);
        gameTableManagerProxy.SetProperty(nameof(IGameTableManager.Spell4), CreateGameTable(new Spell4Entry
        {
            Id       = 85430u,
            CastTime = castTimeMs
        }));

        IGlobalSpellManager globalSpellManager = RecordingDispatchProxy<IGlobalSpellManager>.Create(out RecordingDispatchProxy<IGlobalSpellManager> spellManagerProxy);
        spellManagerProxy.SetProperty(nameof(IGlobalSpellManager.NextCastingId), 900u);
        spellManagerProxy.SetProperty(nameof(IGlobalSpellManager.NextEffectId), 901u);
        spellManagerProxy.SetMethodReturn(
            nameof(IGlobalSpellManager.GetTelegraphDamageEntries),
            new[]
            {
                new TelegraphDamageEntry
                {
                    Id = 1991u
                }
            });

        var script = new TutorialCombatMineEntityScript(
            NullLogger<TutorialCombatMineEntityScript>.Instance,
            gameTableManager,
            globalSpellManager);
        script.OnLoad(owner);

        return new MineHarness(script, player, ownerProxy);
    }

    private static IReadOnlyList<TMessage> GetVisibleMessages<TMessage>(RecordingDispatchProxy<ISimpleCollidableEntity> ownerProxy)
    {
        return ownerProxy
            .GetInvocations(nameof(IWorldEntity.EnqueueToVisible))
            .Select(invocation => invocation.Arguments[0])
            .OfType<TMessage>()
            .ToList();
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        uint maxId = entries
            .Select(entry => (uint)typeof(T).GetField("Id")!.GetValue(entry)!)
            .DefaultIfEmpty()
            .Max();

        int[] lookup = Enumerable.Repeat(-1, checked((int)maxId + 1)).ToArray();
        for (int index = 0; index < entries.Length; index++)
        {
            uint id = (uint)typeof(T).GetField("Id")!.GetValue(entries[index])!;
            lookup[id] = index;
        }

        typeof(GameTable<T>)
            .GetProperty(nameof(GameTable<T>.Entries), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.SetValue(table, entries);
        typeof(GameTable<T>)
            .GetField("lookup", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(table, lookup);
        typeof(GameTable<T>)
            .GetField("header", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(table, new GameTableHeader { MaxId = (ulong)lookup.Length });

        return table;
    }

    private sealed record MineHarness(
        TutorialCombatMineEntityScript Script,
        IPlayer Player,
        RecordingDispatchProxy<ISimpleCollidableEntity> OwnerProxy);
}
