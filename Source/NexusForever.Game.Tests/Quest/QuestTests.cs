using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Entity;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Script;
using NexusForever.Script.Template.Collection;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Quest;

[Collection(LegacyServiceProviderCollection.Name)]
public class QuestTests
{
    [Fact]
    public void SendInitialPackets_IncludesCurrentObjectiveIdForActiveQuest()
    {
        IPlayer player = CreatePlayer(out var sessionProxy);
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider();

        try
        {
            var quest = new NexusForever.Game.Quest.Quest(
                player,
                CreateSequentialQuestInfo(),
                CreateAcceptedQuestModel(firstObjectiveProgress: 5u));

            var manager = new QuestManager(player, new CharacterModel());
            AddActiveQuest(manager, quest);

            manager.SendInitialPackets();

            ServerQuestInit init = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .OfType<ServerQuestInit>()
                .Single();

            ServerQuestInit.QuestActive activeQuest = Assert.Single(init.Active);
            Assert.Equal((ushort)9001, activeQuest.QuestId);
            Assert.Equal(QuestState.Accepted, activeQuest.State);
            Assert.Equal(102u, activeQuest.QuestObjectiveId);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void ObjectiveUpdate_WhenSequentialObjectiveUnlocks_SendsCurrentObjectiveId()
    {
        IPlayer player = CreatePlayer(out var sessionProxy);
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider();

        try
        {
            var quest = new NexusForever.Game.Quest.Quest(player, CreateSequentialQuestInfo());

            Assert.Equal(101u, quest.GetCurrentObjectiveId());

            quest.ObjectiveUpdate(QuestObjectiveType.KillCreature, 73464u, 5u);

            Assert.Equal(102u, quest.GetCurrentObjectiveId());

            ServerQuestStateChange stateChange = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .OfType<ServerQuestStateChange>()
                .Single();

            Assert.Equal((ushort)9001, stateChange.QuestId);
            Assert.Equal(QuestState.Accepted, stateChange.QuestState);
            Assert.Equal(102u, stateChange.QuestObjectiveId);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    private static IPlayer CreatePlayer(out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        return player;
    }

    private static CharacterQuestModel CreateAcceptedQuestModel(uint firstObjectiveProgress)
    {
        return new CharacterQuestModel
        {
            Id      = 42ul,
            QuestId = 9001,
            State   = (byte)QuestState.Accepted,
            Flags   = (byte)QuestStateFlags.Tracked,
            QuestObjective =
            {
                new CharacterQuestObjectiveModel
                {
                    Id       = 42ul,
                    QuestId  = 9001,
                    Index    = 0,
                    Progress = firstObjectiveProgress
                },
                new CharacterQuestObjectiveModel
                {
                    Id      = 42ul,
                    QuestId = 9001,
                    Index   = 1
                }
            }
        };
    }

    private static IQuestInfo CreateSequentialQuestInfo()
    {
        IQuestInfo questInfo = RecordingDispatchProxy<IQuestInfo>.Create(out var questInfoProxy);
        questInfoProxy.SetProperty(nameof(IQuestInfo.Entry), new Quest2Entry
        {
            Id = 9001u
        });
        questInfoProxy.SetProperty(nameof(IQuestInfo.Objectives), ImmutableList.Create<IQuestObjectiveInfo>(
            new QuestObjectiveInfo(new QuestObjectiveEntry
            {
                Id    = 101u,
                Type  = (uint)QuestObjectiveType.KillCreature,
                Data  = 73464u,
                Count = 5u,
                Flags = (uint)QuestObjectiveFlags.DisablesDynamicProgress
            }),
            new QuestObjectiveInfo(new QuestObjectiveEntry
            {
                Id    = 102u,
                Type  = (uint)QuestObjectiveType.ActivateEntity,
                Data  = 73463u,
                Count = 1u,
                Flags = (uint)(QuestObjectiveFlags.RequiresPreviousObjectives | QuestObjectiveFlags.Sequential)
            })));

        return questInfo;
    }

    private static IServiceProvider BuildProvider()
    {
        IScriptCollection scriptCollection = RecordingDispatchProxy<IScriptCollection>.Create(out _);
        IScriptManager scriptManager = RecordingDispatchProxy<IScriptManager>.Create(out var scriptManagerProxy);
        scriptManagerProxy.SetMethodReturn(nameof(IScriptManager.InitialiseOwnedScripts), scriptCollection);

        return new ServiceCollection()
            .AddSingleton(scriptManager)
            .BuildServiceProvider();
    }

    private static void AddActiveQuest(QuestManager manager, IQuest quest)
    {
        FieldInfo field = typeof(QuestManager)
            .GetField("activeQuests", BindingFlags.Instance | BindingFlags.NonPublic);

        var activeQuests = Assert.IsType<Dictionary<ushort, IQuest>>(field?.GetValue(manager));
        activeQuests.Add(quest.Id, quest);
    }
}
