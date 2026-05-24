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
using NexusForever.Network;
using NexusForever.Network.Message;
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
    public void SendInitialPackets_ReplaysQuestStateAndObjectiveUpdatesForActiveQuest()
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

            object[] messages = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .ToArray();

            Assert.IsType<ServerQuestInit>(messages[0]);

            ServerQuestStateChange stateChange = Assert.Single(messages.OfType<ServerQuestStateChange>());
            Assert.Equal((ushort)9001, stateChange.QuestId);
            Assert.Equal(QuestState.Accepted, stateChange.QuestState);
            Assert.Equal(102u, stateChange.QuestObjectiveId);

            ServerQuestObjectiveUpdate[] objectiveUpdates = messages
                .OfType<ServerQuestObjectiveUpdate>()
                .OrderBy(m => m.QuestObjectiveIndex)
                .ToArray();

            Assert.Equal(2, objectiveUpdates.Length);

            Assert.Equal((ushort)9001, objectiveUpdates[0].QuestId);
            Assert.Equal((byte)0, objectiveUpdates[0].QuestObjectiveIndex);
            Assert.Equal(5u, objectiveUpdates[0].Completed);

            Assert.Equal((ushort)9001, objectiveUpdates[1].QuestId);
            Assert.Equal((byte)1, objectiveUpdates[1].QuestObjectiveIndex);
            Assert.Equal(0u, objectiveUpdates[1].Completed);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void SendInitialPackets_UsesObjectiveCompletionFlagsFromStoredProgress()
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
            Assert.Equal(QuestStateFlags.Tracked | QuestStateFlags.Objective0Complete, activeQuest.Flags);
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

    [Fact]
    public void ObjectiveUpdate_SyncsObjectiveCompletionFlags()
    {
        IPlayer player = CreatePlayer(out _);
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider();

        try
        {
            var quest = new NexusForever.Game.Quest.Quest(player, CreateSequentialQuestInfo());
            quest.Flags = QuestStateFlags.Tracked;

            quest.ObjectiveUpdate(QuestObjectiveType.KillCreature, 73464u, 5u);

            Assert.Equal(QuestStateFlags.Tracked | QuestStateFlags.Objective0Complete, quest.Flags);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void ServerQuestInit_WritesInactiveAndActiveQuestStatesAsFourBitValues()
    {
        var packet = new ServerQuestInit
        {
            Inactive =
            {
                new ServerQuestInit.QuestInactive
                {
                    QuestId = 7001,
                    State = QuestState.Mentioned
                }
            },
            Active =
            {
                new ServerQuestInit.QuestActive
                {
                    QuestId = 7002,
                    State = QuestState.Accepted,
                    QuestObjectiveId = 123456u,
                    Flags = QuestStateFlags.Tracked,
                    QuestTimeElapsed = 45u,
                    Objectives =
                    {
                        new ServerQuestInit.QuestActive.Objective
                        {
                            Progress = 3u,
                            TimeElapsed = 7u
                        }
                    }
                }
            },
            DailyRandomSeed = 99ul
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));

        Assert.Equal(0u, reader.ReadUInt());

        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal((ushort)7001, reader.ReadUShort(15u));
        Assert.Equal(QuestState.Mentioned, reader.ReadEnum<QuestState>(4u));

        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal((ushort)7002, reader.ReadUShort(15u));
        Assert.Equal(QuestState.Accepted, reader.ReadEnum<QuestState>(4u));
        Assert.Equal(123456u, reader.ReadUInt());
        Assert.Equal(QuestStateFlags.Tracked, reader.ReadEnum<QuestStateFlags>(32u));
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(3u, reader.ReadUInt());
        Assert.Equal(45u, reader.ReadUInt());
        Assert.Equal(7u, reader.ReadUInt());
        Assert.Equal(99ul, reader.ReadULong());
        Assert.Equal(0u, reader.BytesRemaining);
    }

    [Fact]
    public void ServerQuestInit_WritesActiveObjectiveProgressAndTimersAsGroupedArrays()
    {
        var packet = new ServerQuestInit
        {
            Active =
            {
                new ServerQuestInit.QuestActive
                {
                    QuestId = 7002,
                    State = QuestState.Accepted,
                    QuestObjectiveId = 123456u,
                    Flags = QuestStateFlags.Tracked,
                    QuestTimeElapsed = 45u,
                    Objectives =
                    {
                        new ServerQuestInit.QuestActive.Objective
                        {
                            Progress = 3u,
                            TimeElapsed = 7u
                        },
                        new ServerQuestInit.QuestActive.Objective
                        {
                            Progress = 9u,
                            TimeElapsed = 11u
                        }
                    }
                }
            },
            DailyRandomSeed = 99ul
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));

        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(0u, reader.ReadUInt());

        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal((ushort)7002, reader.ReadUShort(15u));
        Assert.Equal(QuestState.Accepted, reader.ReadEnum<QuestState>(4u));
        Assert.Equal(123456u, reader.ReadUInt());
        Assert.Equal(QuestStateFlags.Tracked, reader.ReadEnum<QuestStateFlags>(32u));
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(3u, reader.ReadUInt());
        Assert.Equal(9u, reader.ReadUInt());
        Assert.Equal(45u, reader.ReadUInt());
        Assert.Equal(7u, reader.ReadUInt());
        Assert.Equal(11u, reader.ReadUInt());
        Assert.Equal(99ul, reader.ReadULong());
        Assert.Equal(0u, reader.BytesRemaining);
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

    private static byte[] WritePacket(IWritable packet)
    {
        using var stream = new MemoryStream();
        using var writer = new GamePacketWriter(stream);

        packet.Write(writer);
        writer.FlushBits();

        return stream.ToArray();
    }
}
