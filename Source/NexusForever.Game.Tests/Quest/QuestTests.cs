using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Currency;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game;
using NexusForever.Game.Entity;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
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

    [Theory]
    [InlineData(10513, 10527)]
    [InlineData(10521, 10532)]
    public void SendInitialPackets_RidersReefMovementCompleteAndHoverboardActive_HidesCompletedMovementRoot(
        ushort movementQuestId,
        ushort hoverboardQuestId)
    {
        IPlayer player = CreatePlayer(out var sessionProxy);
        var manager = new QuestManager(player, new CharacterModel());

        AddCompletedQuest(manager, CreateQuest(movementQuestId, CreateQuestInfo(movementQuestId), QuestState.Completed));
        AddActiveQuest(manager, CreateQuest(hoverboardQuestId, CreateQuestInfo(hoverboardQuestId), QuestState.Accepted, 21321u));

        manager.SendInitialPackets();

        ServerQuestInit init = sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(i => i.Arguments[0])
            .OfType<ServerQuestInit>()
            .Single();

        Assert.Empty(init.Completed);

        ServerQuestInit.QuestActive activeQuest = Assert.Single(init.Active);
        Assert.Equal(hoverboardQuestId, activeQuest.QuestId);
        Assert.Equal(21321u, activeQuest.QuestObjectiveId);
    }

    [Fact]
    public void SendInitialPackets_RidersReefMovementCompleteWithoutHoverboardActive_IncludesCompletedMovementRoot()
    {
        IPlayer player = CreatePlayer(out var sessionProxy);
        var manager = new QuestManager(player, new CharacterModel());

        AddCompletedQuest(manager, CreateQuest(10513, CreateQuestInfo(10513), QuestState.Completed));

        manager.SendInitialPackets();

        ServerQuestInit init = sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(i => i.Arguments[0])
            .OfType<ServerQuestInit>()
            .Single();

        ServerQuestInit.QuestComplete completedQuest = Assert.Single(init.Completed);
        Assert.Equal((ushort)10513, completedQuest.QuestId);
        Assert.Empty(init.Active);
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
    public void SendObjectiveWorldLocationUpdates_WithMissingDirectionTablesSendsZeroWorldLocation()
    {
        IPlayer player = CreatePlayer(out var sessionProxy);
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider(BuildGameTableManager());

        try
        {
            var quest = new NexusForever.Game.Quest.Quest(
                player,
                CreateGuidanceQuestInfo(questDirectionId: 77u));

            quest.SendObjectiveWorldLocationUpdates();

            ServerQuestObjectiveWorldLocation update = Assert.Single(sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .OfType<ServerQuestObjectiveWorldLocation>());

            Assert.Equal((ushort)9002, update.QuestId);
            Assert.Equal((byte)0, update.QuestObjectiveIndex);
            Assert.Equal(0u, update.WorldLocation2Id);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void SendObjectiveWorldLocationUpdates_WithMissingDirectionEntryTableSendsZeroWorldLocation()
    {
        IPlayer player = CreatePlayer(out var sessionProxy);
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider(BuildGameTableManager(
            questDirectionTable: CreateGameTable(new QuestDirectionEntry
            {
                Id                      = 77u,
                QuestDirectionEntryId00 = 88u
            })));

        try
        {
            var quest = new NexusForever.Game.Quest.Quest(
                player,
                CreateGuidanceQuestInfo(questDirectionId: 77u));

            quest.SendObjectiveWorldLocationUpdates();

            ServerQuestObjectiveWorldLocation update = Assert.Single(sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .OfType<ServerQuestObjectiveWorldLocation>());

            Assert.Equal(0u, update.WorldLocation2Id);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void SendObjectiveWorldLocationUpdates_WithSingleDirectionEntrySendsWorldLocation()
    {
        IPlayer player = CreatePlayer(out var sessionProxy);
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider(BuildGameTableManager(
            questDirectionTable: CreateGameTable(new QuestDirectionEntry
            {
                Id                      = 77u,
                QuestDirectionEntryId00 = 88u
            }),
            questDirectionEntryTable: CreateGameTable(new QuestDirectionEntryEntry
            {
                Id               = 88u,
                WorldLocation2Id = 12345u
            })));

        try
        {
            var quest = new NexusForever.Game.Quest.Quest(
                player,
                CreateGuidanceQuestInfo(questDirectionId: 77u));

            quest.SendObjectiveWorldLocationUpdates();

            ServerQuestObjectiveWorldLocation update = Assert.Single(sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(i => i.Arguments[0])
                .OfType<ServerQuestObjectiveWorldLocation>());

            Assert.Equal(12345u, update.WorldLocation2Id);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Theory]
    [InlineData(10528, 53532u, 53533u)]
    [InlineData(10530, 53619u, 53620u)]
    public void QuestComplete_RidersReefFinalQuest_AllowsCompletionWithoutVisibleSurfaceReceiver(
        ushort questId,
        uint receiverA,
        uint receiverB)
    {
        IQuestInfo questInfo = CreateQuestInfo(questId);
        IPlayer player = CreateQuestCompletePlayer();
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildQuestCompleteProvider(
            new Dictionary<ushort, IQuestInfo>
            {
                [questId] = questInfo
            },
            new Dictionary<ushort, ImmutableList<uint>>
            {
                [questId] = ImmutableList.Create(receiverA, receiverB)
            });

        try
        {
            IQuest quest = CreateQuest(questId, questInfo, QuestState.Achieved);
            var manager = new QuestManager(player, new CharacterModel());
            AddActiveQuest(manager, quest);

            manager.QuestComplete(questId, reward: 0, communicator: false);

            Assert.Equal(QuestState.Completed, quest.State);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void QuestComplete_NonReceiverlessQuest_RequiresVisibleReceiver()
    {
        const ushort questId = 10520;

        IQuestInfo questInfo = CreateQuestInfo(questId);
        IPlayer player = CreateQuestCompletePlayer();
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildQuestCompleteProvider(
            new Dictionary<ushort, IQuestInfo>
            {
                [questId] = questInfo
            },
            new Dictionary<ushort, ImmutableList<uint>>
            {
                [questId] = ImmutableList.Create(74812u)
            });

        try
        {
            IQuest quest = CreateQuest(questId, questInfo, QuestState.Achieved);
            var manager = new QuestManager(player, new CharacterModel());
            AddActiveQuest(manager, quest);

            Assert.Throws<QuestException>(() => manager.QuestComplete(questId, reward: 0, communicator: false));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Theory]
    [InlineData(10525, 9494u)]
    [InlineData(10526, 9530u)]
    public void QuestComplete_RidersReefCryopodReward_GrantsOmnibit(
        ushort questId,
        uint rewardId)
    {
        IQuestInfo questInfo = CreateQuestInfo(
            questId,
            new Quest2RewardEntry
            {
                Id                 = rewardId,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.AccountCurrency,
                ObjectId           = (uint)AccountCurrencyType.Omnibit,
                ObjectAmount       = 1u
            });
        IPlayer player = CreateQuestRewardPlayer(
            out _,
            out RecordingDispatchProxy<IAccountCurrencyManager> accountCurrencyProxy,
            visibleReceiverIds: ImmutableHashSet.Create(73421u));
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildQuestCompleteProvider(
            new Dictionary<ushort, IQuestInfo>
            {
                [questId] = questInfo
            },
            new Dictionary<ushort, ImmutableList<uint>>
            {
                [questId] = ImmutableList.Create(73421u)
            });

        try
        {
            IQuest quest = CreateQuest(questId, questInfo, QuestState.Achieved);
            var manager = new QuestManager(player, new CharacterModel());
            AddActiveQuest(manager, quest);

            manager.QuestComplete(questId, reward: 0, communicator: false);

            RecordingDispatchProxy<IAccountCurrencyManager>.Invocation currencyGrant = Assert.Single(
                accountCurrencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencyAddAmount)));
            Assert.Equal(AccountCurrencyType.Omnibit, currencyGrant.Arguments[0]);
            Assert.Equal(1ul, currencyGrant.Arguments[1]);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Theory]
    [InlineData(10528, 9503u)]
    [InlineData(10530, 9504u)]
    public void QuestComplete_RidersReefFinalReward_GrantsEscapePodItem(
        ushort questId,
        uint rewardId)
    {
        IQuestInfo questInfo = CreateQuestInfo(
            questId,
            new Quest2RewardEntry
            {
                Id                 = rewardId,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 80875u,
                ObjectAmount       = 1u
            });
        IPlayer player = CreateQuestRewardPlayer(
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out _);
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildQuestCompleteProvider(
            new Dictionary<ushort, IQuestInfo>
            {
                [questId] = questInfo
            },
            new Dictionary<ushort, ImmutableList<uint>>
            {
                [questId] = questId == 10528
                    ? ImmutableList.Create(53532u, 53533u)
                    : ImmutableList.Create(53619u, 53620u)
            });

        try
        {
            IQuest quest = CreateQuest(questId, questInfo, QuestState.Achieved);
            var manager = new QuestManager(player, new CharacterModel());
            AddActiveQuest(manager, quest);

            manager.QuestComplete(questId, reward: 0, communicator: false);

            RecordingDispatchProxy<IInventory>.Invocation itemGrant = Assert.Single(
                inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
            Assert.Equal(InventoryLocation.Inventory, itemGrant.Arguments[0]);
            Assert.Equal(80875u, itemGrant.Arguments[1]);
            Assert.Equal(1u, itemGrant.Arguments[2]);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Theory]
    [InlineData(0, 84804u, 84805u)]
    [InlineData(1, 84804u, 84805u)]
    [InlineData(2, 84805u, 84804u)]
    [InlineData(201, 84805u, 84804u)]
    [InlineData(19268, 84804u, 84805u)]
    public void QuestComplete_SelectableRewardSelection_GrantsSelectedReward(
        ushort rewardSelection,
        uint selectedItemId,
        uint unselectedItemId)
    {
        const ushort questId = 9002;
        const uint requiredItemId = 84956u;
        const uint firstSelectableItemId = 84804u;
        const uint secondSelectableItemId = 84805u;

        IQuestInfo questInfo = CreateQuestInfo(
            questId,
            new Quest2RewardEntry
            {
                Id                 = 100u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = requiredItemId,
                ObjectAmount       = 1u
            },
            new Quest2RewardEntry
            {
                Id                 = 200u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = firstSelectableItemId,
                ObjectAmount       = 1u,
                Flags              = 1u
            },
            new Quest2RewardEntry
            {
                Id                 = 201u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = secondSelectableItemId,
                ObjectAmount       = 1u,
                Flags              = 1u
            });
        IPlayer player = CreateQuestRewardPlayer(
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out _,
            visibleReceiverIds: ImmutableHashSet.Create(74812u));
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildQuestCompleteProvider(
            new Dictionary<ushort, IQuestInfo>
            {
                [questId] = questInfo
            },
            new Dictionary<ushort, ImmutableList<uint>>
            {
                [questId] = ImmutableList.Create(74812u)
            });

        try
        {
            IQuest quest = CreateQuest(questId, questInfo, QuestState.Achieved);
            var manager = new QuestManager(player, new CharacterModel());
            AddActiveQuest(manager, quest);

            manager.QuestComplete(questId, rewardSelection, communicator: false);

            IReadOnlyList<RecordingDispatchProxy<IInventory>.Invocation> itemGrants =
                inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate));
            Assert.Equal(2, itemGrants.Count);
            Assert.Contains(itemGrants, invocation => (uint)invocation.Arguments[1] == requiredItemId);
            Assert.Contains(itemGrants, invocation => (uint)invocation.Arguments[1] == selectedItemId);
            Assert.DoesNotContain(itemGrants, invocation => (uint)invocation.Arguments[1] == unselectedItemId);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void QuestComplete_InvalidSelectableReward_DoesNotGrantRequiredReward()
    {
        const ushort questId = 9002;
        const uint requiredItemId = 84956u;
        const uint firstSelectableItemId = 84804u;
        const uint secondSelectableItemId = 84805u;

        IQuestInfo questInfo = CreateQuestInfo(
            questId,
            new Quest2RewardEntry
            {
                Id                 = 100u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = requiredItemId,
                ObjectAmount       = 1u
            },
            new Quest2RewardEntry
            {
                Id                 = 200u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = firstSelectableItemId,
                ObjectAmount       = 1u,
                Flags              = 1u
            },
            new Quest2RewardEntry
            {
                Id                 = 201u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = secondSelectableItemId,
                ObjectAmount       = 1u,
                Flags              = 1u
            });
        IPlayer player = CreateQuestRewardPlayer(
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out _,
            visibleReceiverIds: ImmutableHashSet.Create(74812u));
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildQuestCompleteProvider(
            new Dictionary<ushort, IQuestInfo>
            {
                [questId] = questInfo
            },
            new Dictionary<ushort, ImmutableList<uint>>
            {
                [questId] = ImmutableList.Create(74812u)
            });

        try
        {
            IQuest quest = CreateQuest(questId, questInfo, QuestState.Achieved);
            var manager = new QuestManager(player, new CharacterModel());
            AddActiveQuest(manager, quest);

            Assert.Throws<QuestException>(() => manager.QuestComplete(questId, reward: 9999, communicator: false));

            Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void ObjectiveUpdate_WhenQuestIsCompleted_DoesNotRevertToAchieved()
    {
        IPlayer player = CreatePlayer(out _);
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider();

        try
        {
            var quest = new NexusForever.Game.Quest.Quest(
                player,
                CreateSequentialQuestInfo(),
                CreateCompletedSequentialQuestModel());

            quest.ObjectiveUpdate(QuestObjectiveType.CompleteQuest, 9001u, 1u);

            Assert.Equal(QuestState.Completed, quest.State);
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

    private static IPlayer CreateQuestCompletePlayer()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        ICharacterAchievementManager achievementManager = RecordingDispatchProxy<ICharacterAchievementManager>.Create(out _);

        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievementManager);
        playerProxy.SetMethodHandler(nameof(IPlayer.GetVisibleCreature), _ => Array.Empty<WorldEntity>());
        return player;
    }

    private static IPlayer CreateQuestRewardPlayer(
        out RecordingDispatchProxy<IInventory> inventoryProxy,
        out RecordingDispatchProxy<IAccountCurrencyManager> accountCurrencyProxy,
        IReadOnlySet<uint> visibleReceiverIds = null)
    {
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out inventoryProxy);
        inventoryProxy.SetMethodReturn(nameof(IInventory.GetInventorySlotsRemaining), 10u);

        IAccountCurrencyManager accountCurrencyManager = RecordingDispatchProxy<IAccountCurrencyManager>.Create(out accountCurrencyProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
        accountProxy.SetProperty(nameof(IAccount.CurrencyManager), accountCurrencyManager);

        ICharacterAchievementManager achievementManager = RecordingDispatchProxy<ICharacterAchievementManager>.Create(out _);

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievementManager);
        playerProxy.SetMethodHandler(nameof(IPlayer.GetVisibleCreature), args =>
        {
            uint creatureId = (uint)args[0];
            return visibleReceiverIds?.Contains(creatureId) == true
                ? new WorldEntity[] { null }
                : Array.Empty<WorldEntity>();
        });
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

    private static CharacterQuestModel CreateCompletedSequentialQuestModel()
    {
        return new CharacterQuestModel
        {
            Id      = 42ul,
            QuestId = 9001,
            State   = (byte)QuestState.Completed,
            Flags   = (byte)(QuestStateFlags.Tracked | QuestStateFlags.Objective0Complete | QuestStateFlags.Objective1Complete),
            QuestObjective =
            {
                new CharacterQuestObjectiveModel
                {
                    Id       = 42ul,
                    QuestId  = 9001,
                    Index    = 0,
                    Progress = 5u
                },
                new CharacterQuestObjectiveModel
                {
                    Id       = 42ul,
                    QuestId  = 9001,
                    Index    = 1,
                    Progress = 1u
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

    private static IQuestInfo CreateGuidanceQuestInfo(uint questDirectionId)
    {
        IQuestInfo questInfo = RecordingDispatchProxy<IQuestInfo>.Create(out var questInfoProxy);
        questInfoProxy.SetProperty(nameof(IQuestInfo.Entry), new Quest2Entry
        {
            Id = 9002u
        });
        questInfoProxy.SetProperty(nameof(IQuestInfo.Objectives), ImmutableList.Create<IQuestObjectiveInfo>(
            new QuestObjectiveInfo(new QuestObjectiveEntry
            {
                Id               = 201u,
                Type             = (uint)QuestObjectiveType.ActivateEntity,
                Data             = 73464u,
                Count            = 1u,
                QuestDirectionId = questDirectionId
            })));

        return questInfo;
    }

    private static IQuestInfo CreateQuestInfo(ushort questId, params Quest2RewardEntry[] rewards)
    {
        IQuestInfo questInfo = RecordingDispatchProxy<IQuestInfo>.Create(out RecordingDispatchProxy<IQuestInfo> questInfoProxy);
        questInfoProxy.SetProperty(nameof(IQuestInfo.Entry), new Quest2Entry
        {
            Id               = questId,
            PushedItemIds    = new uint[6],
            PushedItemCounts = new uint[6]
        });
        questInfoProxy.SetProperty(
            nameof(IQuestInfo.Rewards),
            rewards.ToImmutableDictionary(r => r.Id));
        return questInfo;
    }

    private static IQuest CreateQuest(ushort questId, IQuestInfo questInfo, QuestState state, uint currentObjectiveId = 0u)
    {
        IQuest quest = RecordingDispatchProxy<IQuest>.Create(out RecordingDispatchProxy<IQuest> questProxy);
        questProxy.SetProperty(nameof(IQuest.Id), questId);
        questProxy.SetProperty(nameof(IQuest.Info), questInfo);
        questProxy.SetProperty(nameof(IQuest.State), state);
        questProxy.SetProperty(nameof(IQuest.Flags), QuestStateFlags.Tracked);
        questProxy.SetMethodHandler("GetEnumerator", _ => Enumerable.Empty<IQuestObjective>().GetEnumerator());
        questProxy.SetMethodReturn(nameof(IQuest.GetCurrentObjectiveId), currentObjectiveId);
        return quest;
    }

    private static IServiceProvider BuildProvider(GameTableManager gameTableManager = null)
    {
        IScriptCollection scriptCollection = RecordingDispatchProxy<IScriptCollection>.Create(out _);
        IScriptManager scriptManager = RecordingDispatchProxy<IScriptManager>.Create(out var scriptManagerProxy);
        scriptManagerProxy.SetMethodReturn(nameof(IScriptManager.InitialiseOwnedScripts), scriptCollection);

        var services = new ServiceCollection()
            .AddSingleton(scriptManager);

        if (gameTableManager != null)
            services.AddSingleton(gameTableManager);

        return services.BuildServiceProvider();
    }

    private static GameTableManager BuildGameTableManager(
        GameTable<QuestDirectionEntry> questDirectionTable = null,
        GameTable<QuestDirectionEntryEntry> questDirectionEntryTable = null)
    {
        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));

        if (questDirectionTable != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.QuestDirection), questDirectionTable);
        if (questDirectionEntryTable != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.QuestDirectionEntry), questDirectionEntryTable);

        return gameTableManager;
    }

    private static IServiceProvider BuildQuestCompleteProvider(
        IReadOnlyDictionary<ushort, IQuestInfo> questInfos,
        IReadOnlyDictionary<ushort, ImmutableList<uint>> questReceivers)
    {
        var globalQuestManager = new GlobalQuestManager();
        SetPrivateField(
            globalQuestManager,
            "questInfoStore",
            questInfos.ToImmutableDictionary());
        SetPrivateField(
            globalQuestManager,
            "questGiverStore",
            ImmutableDictionary<ushort, ImmutableList<uint>>.Empty);
        SetPrivateField(
            globalQuestManager,
            "questReceiverStore",
            questReceivers.ToImmutableDictionary());

        var disableManager = new DisableManager();
        SetPrivateField(
            disableManager,
            "disables",
            ImmutableDictionary<ulong, Disable>.Empty);

        return new ServiceCollection()
            .AddSingleton(globalQuestManager)
            .AddSingleton(disableManager)
            .BuildServiceProvider();
    }

    private static void AddActiveQuest(QuestManager manager, IQuest quest)
    {
        FieldInfo field = typeof(QuestManager)
            .GetField("activeQuests", BindingFlags.Instance | BindingFlags.NonPublic);

        var activeQuests = Assert.IsType<Dictionary<ushort, IQuest>>(field?.GetValue(manager));
        activeQuests.Add(quest.Id, quest);
    }

    private static void AddCompletedQuest(QuestManager manager, IQuest quest)
    {
        FieldInfo field = typeof(QuestManager)
            .GetField("completedQuests", BindingFlags.Instance | BindingFlags.NonPublic);

        var completedQuests = Assert.IsType<Dictionary<ushort, IQuest>>(field?.GetValue(manager));
        completedQuests.Add(quest.Id, quest);
    }

    private static void SetPrivateField<T>(T instance, string fieldName, object value)
    {
        FieldInfo field = typeof(T).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(instance, value);
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);

        FieldInfo idField = typeof(T).GetField("Id", BindingFlags.Instance | BindingFlags.Public)!;
        uint maxId = entries.Select(entry => (uint)idField.GetValue(entry)!).DefaultIfEmpty().Max();
        var lookup = Enumerable.Repeat(-1, (int)maxId + 1).ToArray();
        for (int i = 0; i < entries.Length; i++)
            lookup[(int)(uint)idField.GetValue(entries[i])!] = i;

        SetPrivateField(table, "lookup", lookup);
        SetPrivateField(table, "header", new GameTableHeader
        {
            MaxId = (ulong)lookup.Length
        });

        return table;
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(instance, value);
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
