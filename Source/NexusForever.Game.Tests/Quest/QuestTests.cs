using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Currency;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Abstract.Reputation;
using NexusForever.Game;
using NexusForever.Game.Achievement;
using NexusForever.Game.Entity;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Reputation;
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

namespace NexusForever.Game.Tests.Quest;

public class QuestTests
{
    [Fact]
    public void SendInitialPackets_IncludesCurrentObjectiveIdForActiveQuest()
    {
        IPlayer player = CreatePlayer(out var sessionProxy);

        var quest = new NexusForever.Game.Quest.Quest(
            player,
            CreateSequentialQuestInfo(),
            CreateAcceptedQuestModel(firstObjectiveProgress: 5u),
            scriptManager: CreateScriptManager());

        var manager = CreateQuestManager(player);
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

    [Fact]
    public void SendInitialPackets_ReplaysQuestStateAndObjectiveUpdatesForActiveQuest()
    {
        IPlayer player = CreatePlayer(out var sessionProxy);

        var quest = new NexusForever.Game.Quest.Quest(
            player,
            CreateSequentialQuestInfo(),
            CreateAcceptedQuestModel(firstObjectiveProgress: 5u),
            scriptManager: CreateScriptManager());

        var manager = CreateQuestManager(player);
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

    [Fact]
    public void SendQuestState_ReplaysQuestStateAndObjectiveUpdatesForActiveQuest()
    {
        IPlayer player = CreatePlayer(out var sessionProxy);

        var quest = new NexusForever.Game.Quest.Quest(
            player,
            CreateSequentialQuestInfo(),
            CreateAcceptedQuestModel(firstObjectiveProgress: 5u),
            scriptManager: CreateScriptManager());

        var manager = CreateQuestManager(player);
        AddActiveQuest(manager, quest);

        manager.SendQuestState(9001);

        object[] messages = sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(i => i.Arguments[0])
            .ToArray();

        Assert.DoesNotContain(messages, m => m is ServerQuestInit);

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

    [Fact]
    public void SendInitialPackets_UsesObjectiveCompletionFlagsFromStoredProgress()
    {
        IPlayer player = CreatePlayer(out var sessionProxy);

        var quest = new NexusForever.Game.Quest.Quest(
            player,
            CreateSequentialQuestInfo(),
            CreateAcceptedQuestModel(firstObjectiveProgress: 5u),
            scriptManager: CreateScriptManager());

        var manager = CreateQuestManager(player);
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

    [Theory]
    [InlineData(10513, 10527)]
    [InlineData(10521, 10532)]
    public void SendInitialPackets_RidersReefMovementCompleteAndHoverboardActive_HidesCompletedMovementRoot(
        ushort movementQuestId,
        ushort hoverboardQuestId)
    {
        IPlayer player = CreatePlayer(out var sessionProxy);
        var manager = CreateQuestManager(player);

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
        var manager = CreateQuestManager(player);

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

        var quest = new NexusForever.Game.Quest.Quest(player, CreateSequentialQuestInfo(), scriptManager: CreateScriptManager());

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

    [Fact]
    public void ObjectiveUpdate_SyncsObjectiveCompletionFlags()
    {
        IPlayer player = CreatePlayer(out _);

        var quest = new NexusForever.Game.Quest.Quest(player, CreateSequentialQuestInfo(), scriptManager: CreateScriptManager());
        quest.Flags = QuestStateFlags.Tracked;

        quest.ObjectiveUpdate(QuestObjectiveType.KillCreature, 73464u, 5u);

        Assert.Equal(QuestStateFlags.Tracked | QuestStateFlags.Objective0Complete, quest.Flags);
    }

    [Theory]
    [InlineData(17189u)]
    [InlineData(19595u)]
    public void ObjectiveUpdate_Q4696RewardPaneTargetGroup_CreditsTargetGroupMember(uint creatureId)
    {
        IPlayer player = CreatePlayer(out var sessionProxy);
        IAssetManager assetManager = RecordingDispatchProxy<IAssetManager>.Create(out var assetManagerProxy);
        assetManagerProxy.SetMethodHandler(nameof(IAssetManager.GetQuestObjectiveTargetIds), args =>
        {
            Assert.Equal(6508u, (uint)args[0]);
            return ImmutableList.Create(17189u, 19595u);
        });

        var quest = new NexusForever.Game.Quest.Quest(
            player,
            CreateQ4696QuestInfo(),
            assetManager: assetManager,
            scriptManager: CreateScriptManager());

        quest.ObjectiveUpdate(QuestObjectiveType.ActivateEntity, creatureId, 1u);

        ServerQuestObjectiveUpdate update = Assert.Single(sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(i => i.Arguments[0])
            .OfType<ServerQuestObjectiveUpdate>());

        Assert.Equal((ushort)4696, update.QuestId);
        Assert.Equal((byte)0, update.QuestObjectiveIndex);
        Assert.Equal(1u, update.Completed);
        Assert.Equal(QuestState.Accepted, quest.State);
    }

    [Fact]
    public void ObjectiveUpdate_Q3777LoftiteCliffsEnterZone_CreditsTravelObjective()
    {
        IPlayer player = CreatePlayer(out var sessionProxy);

        var quest = new NexusForever.Game.Quest.Quest(
            player,
            CreateQ3777ByLeapsAndBoundsQuestInfo(),
            scriptManager: CreateScriptManager());

        quest.ObjectiveUpdate(QuestObjectiveType.EnterZone, 219u, 1u);

        ServerQuestObjectiveUpdate update = Assert.Single(sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(i => i.Arguments[0])
            .OfType<ServerQuestObjectiveUpdate>());

        Assert.Equal((ushort)3777, update.QuestId);
        Assert.Equal((byte)0, update.QuestObjectiveIndex);
        Assert.Equal(1u, update.Completed);
        Assert.Equal(QuestState.Accepted, quest.State);
    }

    [Theory]
    [InlineData(3479, 4564u, 11945u)]
    [InlineData(3479, 4564u, 11948u)]
    [InlineData(3479, 4564u, 12844u)]
    [InlineData(3479, 4564u, 13116u)]
    [InlineData(3479, 4564u, 13117u)]
    [InlineData(3479, 4564u, 13959u)]
    [InlineData(3479, 4564u, 36331u)]
    [InlineData(3479, 4564u, 36335u)]
    [InlineData(3479, 4564u, 51126u)]
    [InlineData(3480, 4565u, 12844u)]
    [InlineData(3480, 4565u, 11945u)]
    [InlineData(3480, 4565u, 11948u)]
    [InlineData(3480, 4565u, 13116u)]
    [InlineData(3480, 4565u, 13117u)]
    [InlineData(3480, 4565u, 13959u)]
    [InlineData(3480, 4565u, 51126u)]
    public void ObjectiveUpdate_Q3479Q3480SharedYetiKillTargetGroup_CreditsTargetGroupMember(
        ushort questId,
        uint objectiveId,
        uint creatureId)
    {
        IPlayer player = CreatePlayer(out var sessionProxy);
        IAssetManager assetManager = RecordingDispatchProxy<IAssetManager>.Create(out var assetManagerProxy);
        assetManagerProxy.SetMethodHandler(nameof(IAssetManager.GetQuestObjectiveTargetIds), args =>
        {
            Assert.Equal(objectiveId, (uint)args[0]);
            return questId == 3479
                ? ImmutableList.Create(11945u, 11948u, 12844u, 13116u, 13117u, 13959u, 36331u, 36335u, 51126u)
                : ImmutableList.Create(12844u, 11945u, 11948u, 13116u, 13117u, 13959u, 51126u);
        });

        var quest = new NexusForever.Game.Quest.Quest(
            player,
            CreateQ3479Q3480YetiKillQuestInfo(questId, objectiveId),
            assetManager: assetManager,
            scriptManager: CreateScriptManager());

        quest.ObjectiveUpdate(QuestObjectiveType.KillTargetGroups, creatureId, 1u);

        ServerQuestObjectiveUpdate update = Assert.Single(sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(i => i.Arguments[0])
            .OfType<ServerQuestObjectiveUpdate>());

        Assert.Equal(questId, update.QuestId);
        Assert.Equal((byte)0, update.QuestObjectiveIndex);
        Assert.Equal(125u, update.Completed);
        Assert.Equal(QuestState.Accepted, quest.State);
    }

    [Theory]
    [InlineData(3479, 4467u)]
    [InlineData(3480, 4470u)]
    public void ObjectiveUpdate_Q3479Q3480TrappedSurvivorSucceedCsi_CompletesSurvivorObjective(
        ushort questId,
        uint objectiveId)
    {
        IPlayer player = CreatePlayer(out var sessionProxy);

        var quest = new NexusForever.Game.Quest.Quest(
            player,
            CreateQ3479Q3480TrappedSurvivorQuestInfo(questId, objectiveId),
            globalQuestManager: CreateGlobalQuestManager(),
            scriptManager: CreateScriptManager());

        quest.ObjectiveUpdate(QuestObjectiveType.SucceedCSI, 99999u, 1u);

        Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        Assert.Equal(QuestState.Accepted, quest.State);

        quest.ObjectiveUpdate(QuestObjectiveType.SucceedCSI, 11070u, 1u);
        quest.ObjectiveUpdate(QuestObjectiveType.SucceedCSI, 11070u, 1u);
        quest.ObjectiveUpdate(QuestObjectiveType.SucceedCSI, 11070u, 1u);

        ServerQuestObjectiveUpdate[] updates = sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(i => i.Arguments[0])
            .OfType<ServerQuestObjectiveUpdate>()
            .ToArray();
        Assert.Equal(3, updates.Length);
        Assert.All(updates, update =>
        {
            Assert.Equal(questId, update.QuestId);
            Assert.Equal((byte)0, update.QuestObjectiveIndex);
        });
        Assert.Equal(1u, updates[0].Completed);
        Assert.Equal(2u, updates[1].Completed);
        Assert.Equal(3u, updates[2].Completed);
        Assert.Equal(QuestState.Achieved, quest.State);
        Assert.Equal(QuestStateFlags.Objective0Complete, quest.Flags);
    }

    [Fact]
    public void QuestAdd_Q3479RequiresVisibleBosunRedmark()
    {
        IPlayer player = CreateQuestAcceptPlayer(
            visibleCreatureIds: ImmutableHashSet.Create(11062u),
            faction: Faction.Exile,
            level: 3u);
        IQuestInfo q3479 = CreateQ3479Q3480TrappedSurvivorQuestInfo(3479, 4467u);
        var manager = CreateQuestManager(
            player,
            CreateGlobalQuestManager(
                new Dictionary<ushort, IQuestInfo>
                {
                    [3479] = q3479
                },
                questGivers: new Dictionary<ushort, ImmutableList<uint>>
                {
                    [3479] = ImmutableList.Create(11062u)
                }));

        manager.QuestAdd(3479, item: null);

        Assert.Equal(QuestState.Accepted, manager.GetQuestState(3479));
    }

    [Fact]
    public void QuestAdd_Q3479RejectsMissingBosunRedmark()
    {
        IPlayer player = CreateQuestAcceptPlayer(
            visibleCreatureIds: ImmutableHashSet<uint>.Empty,
            faction: Faction.Exile,
            level: 3u);
        IQuestInfo q3479 = CreateQ3479Q3480TrappedSurvivorQuestInfo(3479, 4467u);
        var manager = CreateQuestManager(
            player,
            CreateGlobalQuestManager(
                new Dictionary<ushort, IQuestInfo>
                {
                    [3479] = q3479
                },
                questGivers: new Dictionary<ushort, ImmutableList<uint>>
                {
                    [3479] = ImmutableList.Create(11062u)
                }));

        Assert.Throws<QuestException>(() => manager.QuestAdd(3479, item: null));
        Assert.Null(manager.GetQuestState(3479));
    }

    [Fact]
    public void QuestAdd_Q3480RequiresVisibleCommanderDurek()
    {
        IPlayer player = CreateQuestAcceptPlayer(
            visibleCreatureIds: ImmutableHashSet.Create(11061u),
            faction: Faction.Exile,
            level: 3u);
        IQuestInfo q3480 = CreateQ3479Q3480TrappedSurvivorQuestInfo(3480, 4470u);
        var manager = CreateQuestManager(
            player,
            CreateGlobalQuestManager(
                new Dictionary<ushort, IQuestInfo>
                {
                    [3480] = q3480
                },
                questGivers: new Dictionary<ushort, ImmutableList<uint>>
                {
                    [3480] = ImmutableList.Create(11061u)
                }));

        manager.QuestAdd(3480, item: null);

        Assert.Equal(QuestState.Accepted, manager.GetQuestState(3480));
    }

    [Fact]
    public void QuestAdd_Q3480RejectsMissingCommanderDurek()
    {
        IPlayer player = CreateQuestAcceptPlayer(
            visibleCreatureIds: ImmutableHashSet<uint>.Empty,
            faction: Faction.Exile,
            level: 3u);
        IQuestInfo q3480 = CreateQ3479Q3480TrappedSurvivorQuestInfo(3480, 4470u);
        var manager = CreateQuestManager(
            player,
            CreateGlobalQuestManager(
                new Dictionary<ushort, IQuestInfo>
                {
                    [3480] = q3480
                },
                questGivers: new Dictionary<ushort, ImmutableList<uint>>
                {
                    [3480] = ImmutableList.Create(11061u)
                }));

        Assert.Throws<QuestException>(() => manager.QuestAdd(3480, item: null));
        Assert.Null(manager.GetQuestState(3480));
    }

    [Theory]
    [InlineData(QuestState.Accepted)]
    [InlineData(QuestState.Completed)]
    public void QuestAdd_Q3480RejectsQ3479Exclusion(QuestState excludedQuestState)
    {
        IPlayer player = CreateQuestAcceptPlayer(
            visibleCreatureIds: ImmutableHashSet.Create(11061u),
            faction: Faction.Exile,
            level: 3u);
        IQuestInfo q3479 = CreateQ3479Q3480TrappedSurvivorQuestInfo(3479, 4467u);
        IQuestInfo q3480 = CreateQ3479Q3480TrappedSurvivorQuestInfo(3480, 4470u);
        var manager = CreateQuestManager(
            player,
            CreateGlobalQuestManager(
                new Dictionary<ushort, IQuestInfo>
                {
                    [3479] = q3479,
                    [3480] = q3480
                },
                questGivers: new Dictionary<ushort, ImmutableList<uint>>
                {
                    [3480] = ImmutableList.Create(11061u)
                }));
        IQuest excludedQuest = CreateQuest(3479, q3479, excludedQuestState);
        if (excludedQuestState == QuestState.Completed)
            AddCompletedQuest(manager, excludedQuest);
        else
            AddActiveQuest(manager, excludedQuest);

        Assert.Throws<QuestException>(() => manager.QuestAdd(3480, item: null));
        Assert.Null(manager.GetQuestState(3480));
    }

    [Theory]
    [InlineData(11945u)]
    [InlineData(11948u)]
    [InlineData(11962u)]
    [InlineData(11963u)]
    [InlineData(12212u)]
    [InlineData(12213u)]
    public void ObjectiveUpdate_Q3797SecuringTheAreaKillTargetGroups_CreditsTargetGroupMember(uint creatureId)
    {
        IPlayer player = CreatePlayer(out var sessionProxy);
        IAssetManager assetManager = RecordingDispatchProxy<IAssetManager>.Create(out var assetManagerProxy);
        assetManagerProxy.SetMethodHandler(nameof(IAssetManager.GetQuestObjectiveTargetIds), args =>
        {
            Assert.Equal(4918u, (uint)args[0]);
            return ImmutableList.Create(11945u, 11948u, 11962u, 11963u, 12212u, 12213u);
        });

        var quest = new NexusForever.Game.Quest.Quest(
            player,
            CreateQ3797SecuringTheAreaQuestInfo(),
            assetManager: assetManager,
            scriptManager: CreateScriptManager());

        quest.ObjectiveUpdate(QuestObjectiveType.KillTargetGroups, creatureId, 1u);

        ServerQuestObjectiveUpdate update = Assert.Single(sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(i => i.Arguments[0])
            .OfType<ServerQuestObjectiveUpdate>());

        Assert.Equal((ushort)3797, update.QuestId);
        Assert.Equal((byte)0, update.QuestObjectiveIndex);
        Assert.Equal(125u, update.Completed);
        Assert.Equal(QuestState.Accepted, quest.State);
    }

    [Theory]
    [InlineData(14054u)]
    [InlineData(11913u)]
    public void ObjectiveUpdate_Q3668IndigenousIntelligenceKillTargetGroup_CreditsBlockedTargetGroupMember(uint creatureId)
    {
        IPlayer player = CreatePlayer(out var sessionProxy);
        IAssetManager assetManager = RecordingDispatchProxy<IAssetManager>.Create(out var assetManagerProxy);
        assetManagerProxy.SetMethodHandler(nameof(IAssetManager.GetQuestObjectiveTargetIds), args =>
        {
            Assert.Equal(4791u, (uint)args[0]);
            return ImmutableList.Create(11907u, 11910u, 11912u, 11913u, 11917u, 14054u, 17545u, 36429u, 36884u);
        });

        var quest = new NexusForever.Game.Quest.Quest(
            player,
            CreateQ3668IndigenousIntelligenceQuestInfo(),
            assetManager: assetManager,
            scriptManager: CreateScriptManager());

        quest.ObjectiveUpdate(QuestObjectiveType.KillTargetGroup, creatureId, 1u);

        ServerQuestObjectiveUpdate update = Assert.Single(sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(i => i.Arguments[0])
            .OfType<ServerQuestObjectiveUpdate>());

        Assert.Equal((ushort)3668, update.QuestId);
        Assert.Equal((byte)0, update.QuestObjectiveIndex);
        Assert.Equal(200u, update.Completed);
        Assert.Equal(QuestState.Accepted, quest.State);
    }

    [Fact]
    public void QuestAdd_Q3886CommunicatorDeliveredFromRootZone_AllowsAcceptFromChildWorldZone()
    {
        IPlayer player = CreateQuestAcceptPlayer(
            visibleCreatureIds: ImmutableHashSet<uint>.Empty,
            faction: Faction.Exile,
            level: 3u);
        RecordingDispatchProxy<IPlayer> playerProxy = (RecordingDispatchProxy<IPlayer>)(object)player;

        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.Entry), new WorldEntry
        {
            Id = 426u
        });
        playerProxy.SetProperty(nameof(IPlayer.Map), map);
        playerProxy.SetProperty(nameof(IPlayer.Zone), new WorldZoneEntry
        {
            Id           = 597u,
            ParentZoneId = 590u
        });

        GameTableManager gameTableManager = BuildGameTableManager(
            quest2Table: CreateGameTable(
                CreateCommunicatorQuestEntry(3668u, 4791u),
                CreateCommunicatorQuestEntry(3886u, 5052u)),
            questObjectiveTable: CreateGameTable(
                new QuestObjectiveEntry
                {
                    Id    = 4791u,
                    Type  = (uint)QuestObjectiveType.KillTargetGroup,
                    Data  = 7293u,
                    Count = 5u
                },
                new QuestObjectiveEntry
                {
                    Id    = 5052u,
                    Type  = (uint)QuestObjectiveType.ActivateTargetGroupChecklist,
                    Data  = 13623u,
                    Count = 3u
                }),
            communicatorMessagesTable: CreateGameTable(
                new CommunicatorMessagesEntry
                {
                    Id               = 1373u,
                    WorldId          = 426u,
                    WorldZoneId      = 35u,
                    QuestIdDelivered = 3886u,
                    Quests           = [3668u, 0u, 0u],
                    States           = [(uint)QuestState.Achieved, 0u, 0u]
                }),
            worldZoneTable: CreateGameTable(
                new WorldZoneEntry { Id = 35u },
                new WorldZoneEntry { Id = 590u, ParentZoneId = 35u },
                new WorldZoneEntry { Id = 597u, ParentZoneId = 590u }));

        var globalQuestManager = new GlobalQuestManager(gameTableManager: gameTableManager);
        globalQuestManager.Initialise();
        var manager = CreateQuestManager(player, globalQuestManager);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), manager);
        AddActiveQuest(manager, CreateQuest(3668, globalQuestManager.GetQuestInfo(3668), QuestState.Achieved));

        manager.QuestAdd(3886, item: null);

        Assert.Equal(QuestState.Accepted, manager.GetQuestState(3886));
    }

    [Fact]
    public void QuestAdd_Q4696RequiresVisibleGalerasDurekAndCompletedPrerequisite()
    {
        IPlayer player = CreateQuestAcceptPlayer(
            visibleCreatureIds: ImmutableHashSet.Create(17175u),
            faction: Faction.Exile,
            level: 16u);
        IQuestInfo q4696 = CreateQ4696QuestAcceptInfo();
        var manager = CreateQuestManager(
            player,
            CreateGlobalQuestManager(
                new Dictionary<ushort, IQuestInfo>
                {
                    [4696] = q4696
                },
                questGivers: new Dictionary<ushort, ImmutableList<uint>>
                {
                    [4696] = ImmutableList.Create(17175u)
                }));
        AddCompletedQuest(manager, CreateQuest(4667, CreateQuestInfo(4667), QuestState.Completed));

        manager.QuestAdd(4696, item: null);

        Assert.Equal(QuestState.Accepted, manager.GetQuestState(4696));
    }

    [Fact]
    public void QuestAdd_Q3797RequiresVisibleDurekAndCompletedQ3486()
    {
        IPlayer player = CreateQuestAcceptPlayer(
            visibleCreatureIds: ImmutableHashSet.Create(11066u),
            faction: Faction.Exile,
            level: 4u);
        IQuestInfo q3797 = CreateQ3797QuestAcceptInfo();
        var manager = CreateQuestManager(
            player,
            CreateGlobalQuestManager(
                new Dictionary<ushort, IQuestInfo>
                {
                    [3797] = q3797
                },
                questGivers: new Dictionary<ushort, ImmutableList<uint>>
                {
                    [3797] = ImmutableList.Create(11066u)
                }));
        AddCompletedQuest(manager, CreateQuest(3486, CreateQuestInfo(3486), QuestState.Completed));

        manager.QuestAdd(3797, item: null);

        Assert.Equal(QuestState.Accepted, manager.GetQuestState(3797));
    }

    [Fact]
    public void QuestAdd_Q5597RequiresVisibleMondoAndCompletedQ5596()
    {
        IPlayer player = CreateQuestAcceptPlayer(
            visibleCreatureIds: ImmutableHashSet.Create(24187u),
            faction: Faction.Dominion,
            level: 4u);
        IQuestInfo q5597 = CreateQ5597QuestAcceptInfo();
        var manager = CreateQuestManager(
            player,
            CreateGlobalQuestManager(
                new Dictionary<ushort, IQuestInfo>
                {
                    [5597] = q5597
                },
                questGivers: new Dictionary<ushort, ImmutableList<uint>>
                {
                    [5597] = ImmutableList.Create(24187u)
                }));
        AddCompletedQuest(manager, CreateQuest(5596, CreateQuestInfo(5596), QuestState.Completed));

        manager.QuestAdd(5597, item: null);

        Assert.Equal(QuestState.Accepted, manager.GetQuestState(5597));
    }

    [Fact]
    public void QuestAdd_Q5573RequiresVisibleMondo()
    {
        IPlayer player = CreateQuestAcceptPlayer(
            visibleCreatureIds: ImmutableHashSet.Create(24187u),
            faction: Faction.Dominion,
            level: 3u);
        IQuestInfo q5573 = CreateQ5573QuestAcceptInfo();
        var manager = CreateQuestManager(
            player,
            CreateGlobalQuestManager(
                new Dictionary<ushort, IQuestInfo>
                {
                    [5573] = q5573
                },
                questGivers: new Dictionary<ushort, ImmutableList<uint>>
                {
                    [5573] = ImmutableList.Create(24187u)
                }));
        AddCompletedQuest(manager, CreateQuest(5593, CreateQuestInfo(5593), QuestState.Completed));

        manager.QuestAdd(5573, item: null);

        Assert.Equal(QuestState.Accepted, manager.GetQuestState(5573));
    }

    [Fact]
    public void QuestAdd_Q3781RequiresVisibleDeadExileSoldier()
    {
        IPlayer player = CreateQuestAcceptPlayer(
            visibleCreatureIds: ImmutableHashSet.Create(50668u),
            faction: Faction.Exile,
            level: 5u);
        IQuestInfo q3781 = CreateQ3781QuestAcceptInfo();
        var manager = CreateQuestManager(
            player,
            CreateGlobalQuestManager(
                new Dictionary<ushort, IQuestInfo>
                {
                    [3781] = q3781
                },
                questGivers: new Dictionary<ushort, ImmutableList<uint>>
                {
                    [3781] = ImmutableList.Create(50668u)
                }));

        manager.QuestAdd(3781, item: null);

        Assert.Equal(QuestState.Accepted, manager.GetQuestState(3781));
    }

    [Theory]
    [InlineData(3670)]
    [InlineData(3671)]
    [InlineData(3783)]
    [InlineData(5610)]
    public void QuestAdd_NoObjectiveQuestStartsAchieved(ushort questId)
    {
        IPlayer player = CreateQuestAcceptPlayer(
            visibleCreatureIds: ImmutableHashSet<uint>.Empty,
            faction: Faction.Exile,
            level: 5u);
        IQuestInfo questInfo = CreateNoObjectiveQuestInfo(questId);
        var manager = CreateQuestManager(
            player,
            CreateGlobalQuestManager(new Dictionary<ushort, IQuestInfo>
            {
                [questId] = questInfo
            }));

        manager.QuestAdd(questInfo);

        Assert.Equal(QuestState.Achieved, manager.GetQuestState(questId));
    }

    [Theory]
    [InlineData(3670, 16622u, 7004u, 19041u)]
    [InlineData(3671, 12959u, 6662u, 19131u)]
    [InlineData(3783, 11066u, 8830u, 17756u)]
    [InlineData(5610, 24187u, 0u, 0u)]
    public void QuestComplete_NoObjectiveQuestCompletesAtVisibleReceiverAndGrantsReward(
        ushort questId,
        uint receiverId,
        uint rewardId,
        uint rewardItemId)
    {
        Quest2RewardEntry[] rewards = rewardId == 0u ? [] :
        [
            new Quest2RewardEntry
            {
                Id                 = rewardId,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = rewardItemId,
                ObjectAmount       = 1u
            }
        ];
        IQuestInfo questInfo = CreateNoObjectiveQuestInfo(questId, rewards);
        IPlayer player = CreateQuestLifecyclePlayer(
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
            visibleCreatureIds: ImmutableHashSet.Create(receiverId));
        var manager = CreateQuestManager(
            player,
            CreateGlobalQuestManager(
                new Dictionary<ushort, IQuestInfo>
                {
                    [questId] = questInfo
                },
                questReceivers: new Dictionary<ushort, ImmutableList<uint>>
                {
                    [questId] = ImmutableList.Create(receiverId)
                }));

        manager.QuestAdd(questInfo);
        Assert.Equal(QuestState.Achieved, manager.GetQuestState(questId));

        manager.QuestComplete(questId, reward: 0, communicator: false);

        Assert.Equal(QuestState.Completed, manager.GetQuestState(questId));
        IReadOnlyList<RecordingDispatchProxy<IInventory>.Invocation> itemGrants =
            inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate));
        if (rewardItemId == 0u)
        {
            Assert.Empty(itemGrants);
        }
        else
        {
            RecordingDispatchProxy<IInventory>.Invocation itemGrant = Assert.Single(itemGrants);
            Assert.Equal(InventoryLocation.Inventory, itemGrant.Arguments[0]);
            Assert.Equal(rewardItemId, itemGrant.Arguments[1]);
            Assert.Equal(1u, itemGrant.Arguments[2]);
        }

        IReadOnlyList<RecordingDispatchProxy<ICharacterAchievementManager>.Invocation> achievementChecks =
            achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements));
        Assert.Contains(achievementChecks, invocation =>
            (AchievementType)invocation.Arguments[1] == AchievementType.QuestComplete &&
            (uint)invocation.Arguments[2] == questId);
        Assert.Contains(achievementChecks, invocation =>
            (AchievementType)invocation.Arguments[1] == AchievementType.QuestCompleteChecklist &&
            (uint)invocation.Arguments[2] == questId);
        Assert.Contains(achievementChecks, invocation =>
            (AchievementType)invocation.Arguments[1] == AchievementType.QuestCompleteChecklistCount &&
            (uint)invocation.Arguments[2] == questId);
    }

    [Fact]
    public void QuestComplete_Q3671RejectsVisibleLandingSiteDeadeyeReceiver()
    {
        const ushort questId = 3671;
        const uint landingSiteDeadeye = 11063u;
        const uint campIcefuryDeadeye = 12959u;

        IQuestInfo questInfo = CreateNoObjectiveQuestInfo(questId);
        IPlayer player = CreateQuestLifecyclePlayer(
            out _,
            out _,
            visibleCreatureIds: ImmutableHashSet.Create(landingSiteDeadeye));
        var manager = CreateQuestManager(
            player,
            CreateGlobalQuestManager(
                new Dictionary<ushort, IQuestInfo>
                {
                    [questId] = questInfo
                },
                questReceivers: new Dictionary<ushort, ImmutableList<uint>>
                {
                    [questId] = ImmutableList.Create(campIcefuryDeadeye)
                }));

        manager.QuestAdd(questInfo);
        Assert.Equal(QuestState.Achieved, manager.GetQuestState(questId));

        Assert.Throws<QuestException>(() => manager.QuestComplete(questId, reward: 0, communicator: false));
    }

    [Fact]
    public void QuestLifecycle_Q9880DownInTheDregsContract_AllowsReceiverlessAcceptObjectiveCompleteRewardAndAchievements()
    {
        const ushort questId = 9880;
        const uint objectiveId = 18869u;
        const uint targetGroupId = 12511u;
        const uint scarhideRaiderCreature2Id = 24140u;
        const uint palehuskHowlerCreature2Id = 36027u;
        const uint quest2RewardId = 6867u;
        const uint contractCommissionItem2Id = 92281u;
        const uint periodicQuestGroupId = 52u;

        GameTableManager gameTableManager = BuildGameTableManager(
            quest2Table: CreateGameTable(CreateQ9880DownInTheDregsContractQuestEntry()),
            questObjectiveTable: CreateGameTable(new QuestObjectiveEntry
            {
                Id    = objectiveId,
                Type  = (uint)QuestObjectiveType.KillTargetGroups,
                Flags = 1540u,
                Data  = targetGroupId,
                Count = 30u
            }),
            quest2RewardTable: CreateGameTable(new Quest2RewardEntry
            {
                Id                 = quest2RewardId,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = contractCommissionItem2Id,
                ObjectAmount       = 1u,
                Flags              = 0u
            }),
            itemTable: CreateGameTable(new Item2Entry
            {
                Id = contractCommissionItem2Id
            }),
            periodicQuestGroupTable: CreateGameTable(new PeriodicQuestGroupEntry
            {
                Id                       = periodicQuestGroupId,
                PeriodicQuestSetId       = 37u,
                PeriodicQuestsOffered    = uint.MaxValue,
                MaxPeriodicQuestsAllowed = uint.MaxValue,
                Weight                   = 1u,
                ContractTypeEnum         = 2u,
                ContractQualityEnum      = 1u
            }));
        IQuestInfo questInfo = CreateQ9880DownInTheDregsContractQuestInfo(gameTableManager);
        IAssetManager assetManager = RecordingDispatchProxy<IAssetManager>.Create(out RecordingDispatchProxy<IAssetManager> assetProxy);
        assetProxy.SetMethodHandler(nameof(IAssetManager.GetQuestObjectiveTargetIds), args =>
        {
            Assert.Equal(objectiveId, (uint)args[0]);
            return ImmutableList.Create(scarhideRaiderCreature2Id, palehuskHowlerCreature2Id);
        });

        IPlayer player = CreateQ9880ContractLifecyclePlayer(
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy);
        var manager = new QuestManager(
            player,
            new CharacterModel(),
            assetManager: assetManager,
            globalQuestManager: CreateGlobalQuestManager(new Dictionary<ushort, IQuestInfo>
            {
                [questId] = questInfo
            }),
            scriptManager: CreateScriptManager(),
            gameTableManager: gameTableManager,
            contractManager: new ContractManager(gameTableManager));

        manager.QuestAdd(questId, item: null);

        IQuest quest = Assert.Single(manager.GetActiveQuests());
        IQuestObjective objective = Assert.Single(quest);
        Assert.Equal(QuestState.Accepted, manager.GetQuestState(questId));
        Assert.Equal(objectiveId, objective.ObjectiveInfo.Id);
        Assert.Equal(QuestObjectiveType.KillTargetGroups, objective.ObjectiveInfo.Type);
        Assert.Equal(targetGroupId, objective.ObjectiveInfo.Entry.Data);

        manager.ObjectiveUpdate(QuestObjectiveType.KillTargetGroups, scarhideRaiderCreature2Id, 29u);

        Assert.Equal(QuestState.Accepted, manager.GetQuestState(questId));
        Assert.Equal(29u, objective.Progress);

        manager.ObjectiveUpdate(QuestObjectiveType.KillTargetGroups, palehuskHowlerCreature2Id, 1u);

        Assert.Equal(QuestState.Achieved, manager.GetQuestState(questId));
        Assert.Equal(30u, objective.Progress);

        manager.QuestComplete(questId, reward: 0, communicator: false);

        Assert.Equal(QuestState.Completed, manager.GetQuestState(questId));
        RecordingDispatchProxy<IInventory>.Invocation itemGrant = Assert.Single(
            inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
        Assert.Equal(InventoryLocation.Inventory, itemGrant.Arguments[0]);
        Assert.Equal(contractCommissionItem2Id, itemGrant.Arguments[1]);
        Assert.Equal(1u, itemGrant.Arguments[2]);

        IReadOnlyList<RecordingDispatchProxy<ICharacterAchievementManager>.Invocation> achievementChecks =
            achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements));
        Assert.Contains(achievementChecks, invocation =>
            (AchievementType)invocation.Arguments[1] == AchievementType.QuestComplete &&
            (uint)invocation.Arguments[2] == questId);
        Assert.Contains(achievementChecks, invocation =>
            (AchievementType)invocation.Arguments[1] == AchievementType.ContractComplete &&
            (uint)invocation.Arguments[2] == 13u);
        Assert.Contains(achievementChecks, invocation =>
            (AchievementType)invocation.Arguments[1] == AchievementType.ContractQualityComplete &&
            (uint)invocation.Arguments[2] == 1u);
        Assert.Contains(achievementChecks, invocation =>
            (AchievementType)invocation.Arguments[1] == AchievementType.ContractTypeComplete &&
            (uint)invocation.Arguments[2] == 2u);
    }

    [Fact]
    public void SendContractAvailability_EmitsOnlyOfferedValidatedGoodQualityContracts()
    {
        const uint contractCommissionItem2Id = 92281u;

        GameTableManager gameTableManager = BuildGameTableManager(
            quest2Table: CreateGameTable(
                CreateContractQuestEntry(9880, 18869u, 52u, 1u),
                CreateContractQuestEntry(9881, 18871u, 52u, 2u),
                CreateContractQuestEntry(9883, 18873u, 52u, 3u),
                CreateContractQuestEntry(9891, 18891u, 53u, 1u)),
            questObjectiveTable: CreateGameTable(
                CreateKillContractObjectiveEntry(18869u, 12511u),
                CreateKillContractObjectiveEntry(18871u, 12516u),
                CreateKillContractObjectiveEntry(18873u, 12522u),
                CreateKillContractObjectiveEntry(18891u, 12542u)),
            quest2RewardTable: CreateGameTable(
                CreateItemQuestReward(6867u, 9880u, contractCommissionItem2Id),
                CreateItemQuestReward(6868u, 9881u, contractCommissionItem2Id),
                CreateItemQuestReward(6869u, 9883u, 999999u),
                CreateItemQuestReward(6877u, 9891u, contractCommissionItem2Id)),
            itemTable: CreateGameTable(new Item2Entry
            {
                Id = contractCommissionItem2Id
            }),
            periodicQuestGroupTable: CreateGameTable(
                new PeriodicQuestGroupEntry
                {
                    Id                       = 52u,
                    PeriodicQuestSetId       = 37u,
                    PeriodicQuestsOffered    = 2u,
                    MaxPeriodicQuestsAllowed = uint.MaxValue,
                    Weight                   = 1u,
                    ContractTypeEnum         = 2u,
                    ContractQualityEnum      = 1u
                },
                new PeriodicQuestGroupEntry
                {
                    Id                       = 53u,
                    PeriodicQuestSetId       = 37u,
                    PeriodicQuestsOffered    = 1u,
                    MaxPeriodicQuestsAllowed = uint.MaxValue,
                    Weight                   = 1u,
                    ContractTypeEnum         = 2u,
                    ContractQualityEnum      = 3u
                }));

        IPlayer player = CreatePlayer(out RecordingDispatchProxy<IGameSession> sessionProxy);
        var manager = new QuestManager(
            player,
            new CharacterModel(),
            gameTableManager: gameTableManager,
            contractManager: new ContractManager(gameTableManager));

        manager.SendContractAvailability();

        RecordingDispatchProxy<IGameSession>.Invocation packetSend = Assert.Single(
            sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        var packet = Assert.IsType<ServerQuestContractGoodQualityChanged>(packetSend.Arguments[0]);
        Assert.Equal([9880u, 9881u, 0u], packet.QuestIds);
    }

    [Fact]
    public void QuestAdd_Quest2BackedContractRejectsWhenPeriodicGroupMaxReached()
    {
        const ushort activeQuestId = 9880;
        const ushort blockedQuestId = 9881;
        const uint contractCommissionItem2Id = 92281u;

        GameTableManager gameTableManager = BuildGameTableManager(
            quest2Table: CreateGameTable(
                CreateContractQuestEntry(activeQuestId, 18869u, 52u, 1u),
                CreateContractQuestEntry(blockedQuestId, 18871u, 52u, 2u)),
            questObjectiveTable: CreateGameTable(
                CreateKillContractObjectiveEntry(18869u, 12511u),
                CreateKillContractObjectiveEntry(18871u, 12516u)),
            quest2RewardTable: CreateGameTable(
                CreateItemQuestReward(6867u, activeQuestId, contractCommissionItem2Id),
                CreateItemQuestReward(6868u, blockedQuestId, contractCommissionItem2Id)),
            itemTable: CreateGameTable(new Item2Entry
            {
                Id = contractCommissionItem2Id
            }),
            periodicQuestGroupTable: CreateGameTable(new PeriodicQuestGroupEntry
            {
                Id                       = 52u,
                PeriodicQuestSetId       = 37u,
                PeriodicQuestsOffered    = 2u,
                MaxPeriodicQuestsAllowed = 1u,
                Weight                   = 1u,
                ContractTypeEnum         = 2u,
                ContractQualityEnum      = 1u
            }));
        IQuestInfo activeQuestInfo = new QuestInfo(CreateContractQuestEntry(activeQuestId, 18869u, 52u, 1u), CreateGlobalQuestManager(), gameTableManager);
        IQuestInfo blockedQuestInfo = new QuestInfo(CreateContractQuestEntry(blockedQuestId, 18871u, 52u, 2u), CreateGlobalQuestManager(), gameTableManager);
        IPlayer player = CreateQ9880ContractLifecyclePlayer(
            out _,
            out _);
        var manager = new QuestManager(
            player,
            new CharacterModel(),
            globalQuestManager: CreateGlobalQuestManager(new Dictionary<ushort, IQuestInfo>
            {
                [activeQuestId] = activeQuestInfo,
                [blockedQuestId] = blockedQuestInfo
            }),
            scriptManager: CreateScriptManager(),
            gameTableManager: gameTableManager,
            contractManager: new ContractManager(gameTableManager));
        AddActiveQuest(manager, CreateQuest(activeQuestId, activeQuestInfo, QuestState.Accepted));

        Assert.Throws<QuestException>(() => manager.QuestAdd(blockedQuestId, item: null));
        Assert.Equal(QuestState.Accepted, manager.GetQuestState(activeQuestId));
        Assert.Null(manager.GetQuestState(blockedQuestId));
    }

    [Theory]
    [InlineData((uint)QuestObjectiveType.KillCreature, 9801u, 18801u, 24140u)]
    [InlineData((uint)QuestObjectiveType.ActivateEntity, 9802u, 18802u, 34567u)]
    [InlineData((uint)QuestObjectiveType.KillTargetGroups, 9803u, 18803u, 12511u)]
    [InlineData((uint)QuestObjectiveType.VirtualCollect, 9804u, 18804u, 776u)]
    [InlineData((uint)QuestObjectiveType.CompleteMaxLevelQuests, 9805u, 18805u, 0u)]
    [InlineData((uint)QuestObjectiveType.PvPKills, 9806u, 18806u, 0u)]
    [InlineData((uint)QuestObjectiveType.EarnCurrency, 9807u, 18807u, 1703u)]
    [InlineData((uint)QuestObjectiveType.CombatMomentum, 9808u, 18808u, 5001u)]
    [InlineData((uint)QuestObjectiveType.KillCreature2, 9809u, 18809u, 2u)]
    public void ContractManager_ValidatesAllBuild16042ContractObjectiveFamilies(uint objectiveType, uint questId, uint objectiveId, uint objectiveData)
    {
        const uint contractCommissionItem2Id = 92281u;
        const uint periodicQuestGroupId = 52u;

        Quest2Entry questEntry = CreateContractQuestEntry((ushort)questId, objectiveId, periodicQuestGroupId, 1u);
        GameTableManager gameTableManager = BuildGameTableManager(
            quest2Table: CreateGameTable(questEntry),
            questObjectiveTable: CreateGameTable(CreateContractObjectiveEntry(objectiveId, (QuestObjectiveType)objectiveType, objectiveData)),
            quest2RewardTable: CreateGameTable(CreateItemQuestReward(6867u, questId, contractCommissionItem2Id)),
            itemTable: CreateGameTable(new Item2Entry
            {
                Id = contractCommissionItem2Id
            }),
            periodicQuestGroupTable: CreateGameTable(CreateContractPeriodicQuestGroup(periodicQuestGroupId, offered: uint.MaxValue, maxAllowed: uint.MaxValue)));

        IQuestInfo questInfo = new QuestInfo(questEntry, CreateGlobalQuestManager(), gameTableManager);
        var contractManager = new ContractManager(gameTableManager);

        Assert.True(contractManager.CanUseReceiverlessLifecycle(questInfo));
        Assert.True(contractManager.CanAccept(questInfo, [], [], DateTime.UtcNow));
        Assert.NotNull(contractManager.GetPeriodicQuestGroup(questInfo));
        Assert.Equal([questId, 0u, 0u], contractManager.GetGoodQualityContractQuestIds([], [], DateTime.UtcNow));
    }

    [Fact]
    public void ContractManager_RejectsCatalogRowsMissingRuntimeOwnedObjectiveRewardOrItem()
    {
        const ushort validQuestId = 9880;
        const ushort missingObjectiveQuestId = 9881;
        const ushort missingRewardQuestId = 9882;
        const ushort missingItemQuestId = 9883;
        const ushort zeroAmountRewardQuestId = 9884;
        const uint contractCommissionItem2Id = 92281u;

        GameTableManager gameTableManager = BuildGameTableManager(
            quest2Table: CreateGameTable(
                CreateContractQuestEntry(validQuestId, 18869u, 52u, 1u),
                CreateContractQuestEntry(missingObjectiveQuestId, 18870u, 52u, 2u),
                CreateContractQuestEntry(missingRewardQuestId, 18871u, 52u, 3u),
                CreateContractQuestEntry(missingItemQuestId, 18872u, 52u, 4u),
                CreateContractQuestEntry(zeroAmountRewardQuestId, 18873u, 52u, 5u)),
            questObjectiveTable: CreateGameTable(
                CreateKillContractObjectiveEntry(18869u, 12511u),
                CreateKillContractObjectiveEntry(18871u, 12516u),
                CreateKillContractObjectiveEntry(18872u, 12522u),
                CreateKillContractObjectiveEntry(18873u, 12542u)),
            quest2RewardTable: CreateGameTable(
                CreateItemQuestReward(6867u, validQuestId, contractCommissionItem2Id),
                CreateItemQuestReward(6868u, missingObjectiveQuestId, contractCommissionItem2Id),
                CreateItemQuestReward(6870u, missingItemQuestId, 999999u),
                CreateItemQuestReward(6871u, zeroAmountRewardQuestId, contractCommissionItem2Id, objectAmount: 0u)),
            itemTable: CreateGameTable(new Item2Entry
            {
                Id = contractCommissionItem2Id
            }),
            periodicQuestGroupTable: CreateGameTable(CreateContractPeriodicQuestGroup(52u, offered: uint.MaxValue, maxAllowed: uint.MaxValue)));

        var contractManager = new ContractManager(gameTableManager);
        IQuestInfo validQuestInfo = new QuestInfo(CreateContractQuestEntry(validQuestId, 18869u, 52u, 1u), CreateGlobalQuestManager(), gameTableManager);

        Assert.True(contractManager.CanUseReceiverlessLifecycle(validQuestInfo));
        Assert.Equal([validQuestId, 0u, 0u], contractManager.GetGoodQualityContractQuestIds([], [], DateTime.UtcNow));

        foreach (IQuestInfo invalidQuestInfo in new[]
        {
            new QuestInfo(CreateContractQuestEntry(missingObjectiveQuestId, 18870u, 52u, 2u), CreateGlobalQuestManager(), gameTableManager),
            new QuestInfo(CreateContractQuestEntry(missingRewardQuestId, 18871u, 52u, 3u), CreateGlobalQuestManager(), gameTableManager),
            new QuestInfo(CreateContractQuestEntry(missingItemQuestId, 18872u, 52u, 4u), CreateGlobalQuestManager(), gameTableManager),
            new QuestInfo(CreateContractQuestEntry(zeroAmountRewardQuestId, 18873u, 52u, 5u), CreateGlobalQuestManager(), gameTableManager)
        })
        {
            Assert.False(contractManager.CanUseReceiverlessLifecycle(invalidQuestInfo));
            Assert.False(contractManager.CanAccept(invalidQuestInfo, [], [], DateTime.UtcNow));
            Assert.Null(contractManager.GetPeriodicQuestGroup(invalidQuestInfo));
        }
    }

    [Fact]
    public void ContractManager_CanAcceptCountsActiveAndUnexpiredCompletedContractsAgainstPeriodicGroupLimit()
    {
        const ushort activeQuestId = 9880;
        const ushort completedQuestId = 9881;
        const ushort nextQuestId = 9883;
        const uint contractCommissionItem2Id = 92281u;
        DateTime now = new(2026, 6, 16, 12, 0, 0, DateTimeKind.Utc);

        GameTableManager gameTableManager = BuildGameTableManager(
            quest2Table: CreateGameTable(
                CreateContractQuestEntry(activeQuestId, 18869u, 52u, 1u),
                CreateContractQuestEntry(completedQuestId, 18871u, 52u, 2u),
                CreateContractQuestEntry(nextQuestId, 18873u, 52u, 3u)),
            questObjectiveTable: CreateGameTable(
                CreateKillContractObjectiveEntry(18869u, 12511u),
                CreateKillContractObjectiveEntry(18871u, 12516u),
                CreateKillContractObjectiveEntry(18873u, 12522u)),
            quest2RewardTable: CreateGameTable(
                CreateItemQuestReward(6867u, activeQuestId, contractCommissionItem2Id),
                CreateItemQuestReward(6868u, completedQuestId, contractCommissionItem2Id),
                CreateItemQuestReward(6869u, nextQuestId, contractCommissionItem2Id)),
            itemTable: CreateGameTable(new Item2Entry
            {
                Id = contractCommissionItem2Id
            }),
            periodicQuestGroupTable: CreateGameTable(CreateContractPeriodicQuestGroup(52u, offered: 3u, maxAllowed: 2u)));

        IQuestInfo activeQuestInfo = new QuestInfo(CreateContractQuestEntry(activeQuestId, 18869u, 52u, 1u), CreateGlobalQuestManager(), gameTableManager);
        IQuestInfo completedQuestInfo = new QuestInfo(CreateContractQuestEntry(completedQuestId, 18871u, 52u, 2u), CreateGlobalQuestManager(), gameTableManager);
        IQuestInfo nextQuestInfo = new QuestInfo(CreateContractQuestEntry(nextQuestId, 18873u, 52u, 3u), CreateGlobalQuestManager(), gameTableManager);
        var contractManager = new ContractManager(gameTableManager);

        IQuest activeQuest = CreateQuest(activeQuestId, activeQuestInfo, QuestState.Accepted);
        IQuest unexpiredCompletedQuest = CreateQuest(completedQuestId, completedQuestInfo, QuestState.Completed, reset: now.AddHours(1));
        IQuest expiredCompletedQuest = CreateQuest(completedQuestId, completedQuestInfo, QuestState.Completed, reset: now.AddHours(-1));

        Assert.False(contractManager.CanAccept(nextQuestInfo, [activeQuest], [unexpiredCompletedQuest], now));
        Assert.True(contractManager.CanAccept(nextQuestInfo, [activeQuest], [expiredCompletedQuest], now));
    }

    [Fact]
    public void ContractManager_RejectsReceiverlessLifecycleForContractFlagRowsWithoutPeriodicGroup()
    {
        const ushort questId = 10514;
        const uint contractCommissionItem2Id = 53684u;

        Quest2Entry questEntry = CreateContractQuestEntry(questId, 19801u, periodicQuestGroupId: 0u, periodicQuestWeight: 0u);
        GameTableManager gameTableManager = BuildGameTableManager(
            quest2Table: CreateGameTable(questEntry),
            questObjectiveTable: CreateGameTable(CreateContractObjectiveEntry(19801u, QuestObjectiveType.ActivateEntity, 36027u)),
            quest2RewardTable: CreateGameTable(CreateItemQuestReward(7001u, questId, contractCommissionItem2Id)),
            itemTable: CreateGameTable(new Item2Entry
            {
                Id = contractCommissionItem2Id
            }));

        IQuestInfo questInfo = new QuestInfo(questEntry, CreateGlobalQuestManager(), gameTableManager);
        var contractManager = new ContractManager(gameTableManager);

        Assert.True(questInfo.IsContract());
        Assert.False(contractManager.CanUseReceiverlessLifecycle(questInfo));
        Assert.False(contractManager.CanAccept(questInfo, [], [], DateTime.UtcNow));
        Assert.Null(contractManager.GetPeriodicQuestGroup(questInfo));
        Assert.Equal([0u, 0u, 0u], contractManager.GetGoodQualityContractQuestIds([], [], DateTime.UtcNow));
    }

    [Theory]
    [InlineData(false, true, 16u)]
    [InlineData(true, false, 16u)]
    [InlineData(true, true, 15u)]
    public void QuestAdd_Q4696RejectsMissingRetailStarterPrerequisites(
        bool hasVisibleDurek,
        bool hasCompletedPrerequisite,
        uint level)
    {
        IPlayer player = CreateQuestAcceptPlayer(
            visibleCreatureIds: hasVisibleDurek ? ImmutableHashSet.Create(17175u) : ImmutableHashSet<uint>.Empty,
            faction: Faction.Exile,
            level: level);
        IQuestInfo q4696 = CreateQ4696QuestAcceptInfo();
        var manager = CreateQuestManager(
            player,
            CreateGlobalQuestManager(
                new Dictionary<ushort, IQuestInfo>
                {
                    [4696] = q4696
                },
                questGivers: new Dictionary<ushort, ImmutableList<uint>>
                {
                    [4696] = ImmutableList.Create(17175u)
                }));

        if (hasCompletedPrerequisite)
            AddCompletedQuest(manager, CreateQuest(4667, CreateQuestInfo(4667), QuestState.Completed));

        Assert.Throws<QuestException>(() => manager.QuestAdd(4696, item: null));
        Assert.Null(manager.GetQuestState(4696));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void QuestAdd_Q3797RejectsMissingDurekOrQ3486Prerequisite(
        bool hasVisibleDurek,
        bool hasCompletedPrerequisite)
    {
        IPlayer player = CreateQuestAcceptPlayer(
            visibleCreatureIds: hasVisibleDurek ? ImmutableHashSet.Create(11066u) : ImmutableHashSet<uint>.Empty,
            faction: Faction.Exile,
            level: 4u);
        IQuestInfo q3797 = CreateQ3797QuestAcceptInfo();
        var manager = CreateQuestManager(
            player,
            CreateGlobalQuestManager(
                new Dictionary<ushort, IQuestInfo>
                {
                    [3797] = q3797
                },
                questGivers: new Dictionary<ushort, ImmutableList<uint>>
                {
                    [3797] = ImmutableList.Create(11066u)
                }));

        if (hasCompletedPrerequisite)
            AddCompletedQuest(manager, CreateQuest(3486, CreateQuestInfo(3486), QuestState.Completed));

        Assert.Throws<QuestException>(() => manager.QuestAdd(3797, item: null));
        Assert.Null(manager.GetQuestState(3797));
    }

    [Theory]
    [InlineData(false, true, Faction.Dominion, 4u)]
    [InlineData(true, false, Faction.Dominion, 4u)]
    [InlineData(true, true, Faction.Exile, 4u)]
    [InlineData(true, true, Faction.Dominion, 0u)]
    public void QuestAdd_Q5597RejectsMissingMondoPrerequisiteFactionOrLevel(
        bool hasVisibleMondo,
        bool hasCompletedPrerequisite,
        Faction faction,
        uint level)
    {
        IPlayer player = CreateQuestAcceptPlayer(
            visibleCreatureIds: hasVisibleMondo ? ImmutableHashSet.Create(24187u) : ImmutableHashSet<uint>.Empty,
            faction: faction,
            level: level);
        IQuestInfo q5597 = CreateQ5597QuestAcceptInfo();
        var manager = CreateQuestManager(
            player,
            CreateGlobalQuestManager(
                new Dictionary<ushort, IQuestInfo>
                {
                    [5597] = q5597
                },
                questGivers: new Dictionary<ushort, ImmutableList<uint>>
                {
                    [5597] = ImmutableList.Create(24187u)
                }));

        if (hasCompletedPrerequisite)
            AddCompletedQuest(manager, CreateQuest(5596, CreateQuestInfo(5596), QuestState.Completed));

        Assert.Throws<QuestException>(() => manager.QuestAdd(5597, item: null));
        Assert.Null(manager.GetQuestState(5597));
    }

    [Fact]
    public void SendObjectiveWorldLocationUpdates_WithMissingDirectionTablesSendsZeroWorldLocation()
    {
        IPlayer player = CreatePlayer(out var sessionProxy);
        GameTableManager gameTableManager = BuildGameTableManager();

        var quest = new NexusForever.Game.Quest.Quest(
            player,
            CreateGuidanceQuestInfo(questDirectionId: 77u),
            scriptManager: CreateScriptManager(),
            gameTableManager: gameTableManager);

        quest.SendObjectiveWorldLocationUpdates();

        ServerQuestObjectiveWorldLocation update = Assert.Single(sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(i => i.Arguments[0])
            .OfType<ServerQuestObjectiveWorldLocation>());

        Assert.Equal((ushort)9002, update.QuestId);
        Assert.Equal((byte)0, update.QuestObjectiveIndex);
        Assert.Equal(0u, update.WorldLocation2Id);
    }

    [Fact]
    public void SendObjectiveWorldLocationUpdates_WithMissingDirectionEntryTableSendsZeroWorldLocation()
    {
        IPlayer player = CreatePlayer(out var sessionProxy);
        GameTableManager gameTableManager = BuildGameTableManager(
            questDirectionTable: CreateGameTable(new QuestDirectionEntry
            {
                Id                      = 77u,
                QuestDirectionEntryId00 = 88u
            }));

        var quest = new NexusForever.Game.Quest.Quest(
            player,
            CreateGuidanceQuestInfo(questDirectionId: 77u),
            scriptManager: CreateScriptManager(),
            gameTableManager: gameTableManager);

        quest.SendObjectiveWorldLocationUpdates();

        ServerQuestObjectiveWorldLocation update = Assert.Single(sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(i => i.Arguments[0])
            .OfType<ServerQuestObjectiveWorldLocation>());

        Assert.Equal(0u, update.WorldLocation2Id);
    }

    [Fact]
    public void SendObjectiveWorldLocationUpdates_WithMissingDirectionEntryRowSendsZeroWorldLocation()
    {
        IPlayer player = CreatePlayer(out var sessionProxy);
        GameTableManager gameTableManager = BuildGameTableManager(
            questDirectionTable: CreateGameTable(new QuestDirectionEntry
            {
                Id                      = 77u,
                QuestDirectionEntryId00 = 88u
            }),
            questDirectionEntryTable: CreateGameTable<QuestDirectionEntryEntry>());

        var quest = new NexusForever.Game.Quest.Quest(
            player,
            CreateGuidanceQuestInfo(questDirectionId: 77u),
            scriptManager: CreateScriptManager(),
            gameTableManager: gameTableManager);

        quest.SendObjectiveWorldLocationUpdates();

        ServerQuestObjectiveWorldLocation update = Assert.Single(sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(i => i.Arguments[0])
            .OfType<ServerQuestObjectiveWorldLocation>());

        Assert.Equal(0u, update.WorldLocation2Id);
    }

    [Fact]
    public void SendObjectiveWorldLocationUpdates_WithSingleDirectionEntrySendsWorldLocation()
    {
        IPlayer player = CreatePlayer(out var sessionProxy);
        GameTableManager gameTableManager = BuildGameTableManager(
            questDirectionTable: CreateGameTable(new QuestDirectionEntry
            {
                Id                      = 77u,
                QuestDirectionEntryId00 = 88u
            }),
            questDirectionEntryTable: CreateGameTable(new QuestDirectionEntryEntry
            {
                Id               = 88u,
                WorldLocation2Id = 12345u
            }));

        var quest = new NexusForever.Game.Quest.Quest(
            player,
            CreateGuidanceQuestInfo(questDirectionId: 77u),
            scriptManager: CreateScriptManager(),
            gameTableManager: gameTableManager);

        quest.SendObjectiveWorldLocationUpdates();

        ServerQuestObjectiveWorldLocation update = Assert.Single(sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(i => i.Arguments[0])
            .OfType<ServerQuestObjectiveWorldLocation>());

        Assert.Equal(12345u, update.WorldLocation2Id);
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
        GlobalQuestManager globalQuestManager = CreateGlobalQuestManager(
            new Dictionary<ushort, IQuestInfo>
            {
                [questId] = questInfo
            },
            new Dictionary<ushort, ImmutableList<uint>>
            {
                [questId] = ImmutableList.Create(receiverA, receiverB)
            });

        IQuest quest = CreateQuest(questId, questInfo, QuestState.Achieved);
        var manager = CreateQuestManager(player, globalQuestManager);
        AddActiveQuest(manager, quest);

        manager.QuestComplete(questId, reward: 0, communicator: false);

        Assert.Equal(QuestState.Completed, quest.State);
    }

    [Fact]
    public void QuestComplete_NonReceiverlessQuest_RequiresVisibleReceiver()
    {
        const ushort questId = 10520;

        IQuestInfo questInfo = CreateQuestInfo(questId);
        IPlayer player = CreateQuestCompletePlayer();
        GlobalQuestManager globalQuestManager = CreateGlobalQuestManager(
            new Dictionary<ushort, IQuestInfo>
            {
                [questId] = questInfo
            },
            new Dictionary<ushort, ImmutableList<uint>>
            {
                [questId] = ImmutableList.Create(74812u)
            });

        IQuest quest = CreateQuest(questId, questInfo, QuestState.Achieved);
        var manager = CreateQuestManager(player, globalQuestManager);
        AddActiveQuest(manager, quest);

        Assert.Throws<QuestException>(() => manager.QuestComplete(questId, reward: 0, communicator: false));
    }

    [Fact]
    public void QuestComplete_Q4696RequiresVisibleGalerasDarbyReceiver()
    {
        IQuestInfo questInfo = CreateQuestInfo(4696);
        IPlayer player = CreateQuestRewardPlayer(
            out _,
            out _,
            visibleReceiverIds: ImmutableHashSet.Create(17053u));
        GlobalQuestManager globalQuestManager = CreateGlobalQuestManager(
            new Dictionary<ushort, IQuestInfo>
            {
                [4696] = questInfo
            },
            new Dictionary<ushort, ImmutableList<uint>>
            {
                [4696] = ImmutableList.Create(17053u)
            });

        IQuest quest = CreateQuest(4696, questInfo, QuestState.Achieved);
        var manager = CreateQuestManager(player, globalQuestManager);
        AddActiveQuest(manager, quest);

        manager.QuestComplete(4696, reward: 0, communicator: false);

        Assert.Equal(QuestState.Completed, quest.State);
    }

    [Theory]
    [InlineData(5580)]
    [InlineData(5583)]
    public void QuestComplete_CrimsonIsleBranchQuestRequiresVisibleKezrekReceiver(ushort questId)
    {
        IQuestInfo questInfo = CreateQuestInfo(questId);
        IPlayer player = CreateQuestRewardPlayer(
            out _,
            out _,
            visibleReceiverIds: ImmutableHashSet.Create(24158u));
        GlobalQuestManager globalQuestManager = CreateGlobalQuestManager(
            new Dictionary<ushort, IQuestInfo>
            {
                [questId] = questInfo
            },
            new Dictionary<ushort, ImmutableList<uint>>
            {
                [questId] = ImmutableList.Create(24158u)
            });

        IQuest quest = CreateQuest(questId, questInfo, QuestState.Achieved);
        var manager = CreateQuestManager(player, globalQuestManager);
        AddActiveQuest(manager, quest);

        manager.QuestComplete(questId, reward: 0, communicator: false);

        Assert.Equal(QuestState.Completed, quest.State);
    }

    [Fact]
    public void QuestComplete_Q3781RequiresVisibleGalerasDeadeyeReceiver()
    {
        IQuestInfo questInfo = CreateQuestInfo(3781);
        IPlayer player = CreateQuestRewardPlayer(
            out _,
            out _,
            visibleReceiverIds: ImmutableHashSet.Create(16622u));
        GlobalQuestManager globalQuestManager = CreateGlobalQuestManager(
            new Dictionary<ushort, IQuestInfo>
            {
                [3781] = questInfo
            },
            new Dictionary<ushort, ImmutableList<uint>>
            {
                [3781] = ImmutableList.Create(16622u)
            });

        IQuest quest = CreateQuest(3781, questInfo, QuestState.Achieved);
        var manager = CreateQuestManager(player, globalQuestManager);
        AddActiveQuest(manager, quest);

        manager.QuestComplete(3781, reward: 0, communicator: false);

        Assert.Equal(QuestState.Completed, quest.State);
    }

    [Fact]
    public void QuestComplete_Q3963RequiresVisibleGalerasDeadeyeReceiver()
    {
        IQuestInfo questInfo = CreateQuestInfo(3963);
        IPlayer player = CreateQuestRewardPlayer(
            out _,
            out _,
            visibleReceiverIds: ImmutableHashSet.Create(16622u));
        GlobalQuestManager globalQuestManager = CreateGlobalQuestManager(
            new Dictionary<ushort, IQuestInfo>
            {
                [3963] = questInfo
            },
            new Dictionary<ushort, ImmutableList<uint>>
            {
                [3963] = ImmutableList.Create(16622u)
            });

        IQuest quest = CreateQuest(3963, questInfo, QuestState.Achieved);
        var manager = CreateQuestManager(player, globalQuestManager);
        AddActiveQuest(manager, quest);

        manager.QuestComplete(3963, reward: 0, communicator: false);

        Assert.Equal(QuestState.Completed, quest.State);
    }

    [Theory]
    [InlineData(0, 27872u, 17830u, 17831u)]
    [InlineData(1, 27872u, 17830u, 17831u)]
    [InlineData(2, 17830u, 27872u, 17831u)]
    [InlineData(3, 17831u, 27872u, 17830u)]
    [InlineData(3706, 27872u, 17830u, 17831u)]
    [InlineData(3707, 17830u, 27872u, 17831u)]
    [InlineData(4771, 17831u, 27872u, 17830u)]
    [InlineData(27872, 27872u, 17830u, 17831u)]
    [InlineData(17830, 17830u, 27872u, 17831u)]
    [InlineData(17831, 17831u, 27872u, 17830u)]
    public void QuestComplete_Q3963SelectableRewardsGrantSelectedItemAndCheckAchievements(
        ushort rewardSelection,
        uint selectedItemId,
        uint firstUnselectedItemId,
        uint secondUnselectedItemId)
    {
        const ushort questId = 3963;
        const uint receiverId = 16622u;

        IQuestInfo questInfo = CreateQuestInfo(
            questId,
            new Quest2RewardEntry
            {
                Id                 = 3706u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 27872u,
                ObjectAmount       = 1u,
                Flags              = 1u
            },
            new Quest2RewardEntry
            {
                Id                 = 3707u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 17830u,
                ObjectAmount       = 1u,
                Flags              = 1u
            },
            new Quest2RewardEntry
            {
                Id                 = 4771u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 17831u,
                ObjectAmount       = 1u,
                Flags              = 1u
            });
        IPlayer player = CreateQuestLifecyclePlayer(
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
            visibleCreatureIds: ImmutableHashSet.Create(receiverId));
        GlobalQuestManager globalQuestManager = CreateGlobalQuestManager(
            new Dictionary<ushort, IQuestInfo>
            {
                [questId] = questInfo
            },
            new Dictionary<ushort, ImmutableList<uint>>
            {
                [questId] = ImmutableList.Create(receiverId)
            });

        IQuest quest = CreateQuest(questId, questInfo, QuestState.Achieved);
        var manager = CreateQuestManager(player, globalQuestManager);
        AddActiveQuest(manager, quest);

        manager.QuestComplete(questId, rewardSelection, communicator: false);

        Assert.Equal(QuestState.Completed, quest.State);
        RecordingDispatchProxy<IInventory>.Invocation itemGrant = Assert.Single(
            inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
        Assert.Equal(InventoryLocation.Inventory, itemGrant.Arguments[0]);
        Assert.Equal(selectedItemId, itemGrant.Arguments[1]);
        Assert.Equal(1u, itemGrant.Arguments[2]);

        IReadOnlyList<RecordingDispatchProxy<IInventory>.Invocation> itemGrants =
            inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate));
        Assert.DoesNotContain(itemGrants, invocation => (uint)invocation.Arguments[1] == firstUnselectedItemId);
        Assert.DoesNotContain(itemGrants, invocation => (uint)invocation.Arguments[1] == secondUnselectedItemId);

        IReadOnlyList<RecordingDispatchProxy<ICharacterAchievementManager>.Invocation> achievementChecks =
            achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements));
        Assert.Contains(achievementChecks, invocation =>
            (AchievementType)invocation.Arguments[1] == AchievementType.QuestComplete &&
            (uint)invocation.Arguments[2] == questId);
        Assert.Contains(achievementChecks, invocation =>
            (AchievementType)invocation.Arguments[1] == AchievementType.QuestCompleteChecklist &&
            (uint)invocation.Arguments[2] == questId);
        Assert.Contains(achievementChecks, invocation =>
            (AchievementType)invocation.Arguments[1] == AchievementType.QuestCompleteChecklistCount &&
            (uint)invocation.Arguments[2] == questId);
    }

    [Theory]
    [InlineData(0, 1375u, 13335u, 13327u)]
    [InlineData(1, 1375u, 13335u, 13327u)]
    [InlineData(2, 13335u, 1375u, 13327u)]
    [InlineData(3, 13327u, 1375u, 13335u)]
    [InlineData(2476, 1375u, 13335u, 13327u)]
    [InlineData(2479, 13335u, 1375u, 13327u)]
    [InlineData(8569, 13327u, 1375u, 13335u)]
    [InlineData(1375, 1375u, 13335u, 13327u)]
    [InlineData(13335, 13335u, 1375u, 13327u)]
    [InlineData(13327, 13327u, 1375u, 13335u)]
    public void QuestComplete_Q3479SelectableRewardsGrantSelectedItemAndCheckAchievements(
        ushort rewardSelection,
        uint selectedItemId,
        uint firstUnselectedItemId,
        uint secondUnselectedItemId)
    {
        const ushort questId = 3479;
        const uint receiverId = 11063u;

        IQuestInfo questInfo = CreateQuestInfo(
            questId,
            rewardMoney: 83u,
            new Quest2RewardEntry
            {
                Id                 = 2476u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 1375u,
                ObjectAmount       = 1u,
                Flags              = 1u
            },
            new Quest2RewardEntry
            {
                Id                 = 2479u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 13335u,
                ObjectAmount       = 1u,
                Flags              = 1u
            },
            new Quest2RewardEntry
            {
                Id                 = 8569u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 13327u,
                ObjectAmount       = 1u,
                Flags              = 1u
            });
        IPlayer player = CreateQuestLifecyclePlayer(
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
            out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
            visibleCreatureIds: ImmutableHashSet.Create(receiverId));
        GlobalQuestManager globalQuestManager = CreateGlobalQuestManager(
            new Dictionary<ushort, IQuestInfo>
            {
                [questId] = questInfo
            },
            new Dictionary<ushort, ImmutableList<uint>>
            {
                [questId] = ImmutableList.Create(receiverId)
            });

        IQuest quest = CreateQuest(questId, questInfo, QuestState.Achieved);
        var manager = CreateQuestManager(player, globalQuestManager);
        AddActiveQuest(manager, quest);

        manager.QuestComplete(questId, rewardSelection, communicator: false);

        Assert.Equal(QuestState.Completed, quest.State);
        RecordingDispatchProxy<IInventory>.Invocation itemGrant = Assert.Single(
            inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
        Assert.Equal(InventoryLocation.Inventory, itemGrant.Arguments[0]);
        Assert.Equal(selectedItemId, itemGrant.Arguments[1]);
        Assert.Equal(1u, itemGrant.Arguments[2]);

        IReadOnlyList<RecordingDispatchProxy<IInventory>.Invocation> itemGrants =
            inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate));
        Assert.DoesNotContain(itemGrants, invocation => (uint)invocation.Arguments[1] == firstUnselectedItemId);
        Assert.DoesNotContain(itemGrants, invocation => (uint)invocation.Arguments[1] == secondUnselectedItemId);

        RecordingDispatchProxy<ICurrencyManager>.Invocation currencyGrant = Assert.Single(
            currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
        Assert.Equal(CurrencyType.Credits, currencyGrant.Arguments[0]);
        Assert.Equal(83ul, currencyGrant.Arguments[1]);

        IReadOnlyList<RecordingDispatchProxy<ICharacterAchievementManager>.Invocation> achievementChecks =
            achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements));
        Assert.Contains(achievementChecks, invocation =>
            (AchievementType)invocation.Arguments[1] == AchievementType.QuestComplete &&
            (uint)invocation.Arguments[2] == questId);
        Assert.Contains(achievementChecks, invocation =>
            (AchievementType)invocation.Arguments[1] == AchievementType.QuestCompleteChecklist &&
            (uint)invocation.Arguments[2] == questId);
        Assert.Contains(achievementChecks, invocation =>
            (AchievementType)invocation.Arguments[1] == AchievementType.QuestCompleteChecklistCount &&
            (uint)invocation.Arguments[2] == questId);
    }

    [Theory]
    [InlineData(0, 30459u, 30460u, 30461u)]
    [InlineData(1, 30459u, 30460u, 30461u)]
    [InlineData(2, 30460u, 30459u, 30461u)]
    [InlineData(3, 30461u, 30459u, 30460u)]
    [InlineData(1676, 30459u, 30460u, 30461u)]
    [InlineData(1677, 30460u, 30459u, 30461u)]
    [InlineData(1678, 30461u, 30459u, 30460u)]
    [InlineData(30459, 30459u, 30460u, 30461u)]
    [InlineData(30460, 30460u, 30459u, 30461u)]
    [InlineData(30461, 30461u, 30459u, 30460u)]
    public void QuestComplete_Q3480SelectableRewardsGrantSelectedItemAndCheckAchievements(
        ushort rewardSelection,
        uint selectedItemId,
        uint firstUnselectedItemId,
        uint secondUnselectedItemId)
    {
        const ushort questId = 3480;
        const uint receiverId = 11063u;

        IQuestInfo questInfo = CreateQuestInfo(
            questId,
            rewardMoney: 83u,
            new Quest2RewardEntry
            {
                Id                 = 1676u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 30459u,
                ObjectAmount       = 1u,
                Flags              = 1u
            },
            new Quest2RewardEntry
            {
                Id                 = 1677u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 30460u,
                ObjectAmount       = 1u,
                Flags              = 1u
            },
            new Quest2RewardEntry
            {
                Id                 = 1678u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 30461u,
                ObjectAmount       = 1u,
                Flags              = 1u
            });
        IPlayer player = CreateQuestLifecyclePlayer(
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
            out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
            visibleCreatureIds: ImmutableHashSet.Create(receiverId));
        GlobalQuestManager globalQuestManager = CreateGlobalQuestManager(
            new Dictionary<ushort, IQuestInfo>
            {
                [questId] = questInfo
            },
            new Dictionary<ushort, ImmutableList<uint>>
            {
                [questId] = ImmutableList.Create(receiverId)
            });

        IQuest quest = CreateQuest(questId, questInfo, QuestState.Achieved);
        var manager = CreateQuestManager(player, globalQuestManager);
        AddActiveQuest(manager, quest);

        manager.QuestComplete(questId, rewardSelection, communicator: false);

        Assert.Equal(QuestState.Completed, quest.State);
        RecordingDispatchProxy<IInventory>.Invocation itemGrant = Assert.Single(
            inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
        Assert.Equal(InventoryLocation.Inventory, itemGrant.Arguments[0]);
        Assert.Equal(selectedItemId, itemGrant.Arguments[1]);
        Assert.Equal(1u, itemGrant.Arguments[2]);

        IReadOnlyList<RecordingDispatchProxy<IInventory>.Invocation> itemGrants =
            inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate));
        Assert.DoesNotContain(itemGrants, invocation => (uint)invocation.Arguments[1] == firstUnselectedItemId);
        Assert.DoesNotContain(itemGrants, invocation => (uint)invocation.Arguments[1] == secondUnselectedItemId);

        RecordingDispatchProxy<ICurrencyManager>.Invocation currencyGrant = Assert.Single(
            currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
        Assert.Equal(CurrencyType.Credits, currencyGrant.Arguments[0]);
        Assert.Equal(83ul, currencyGrant.Arguments[1]);

        IReadOnlyList<RecordingDispatchProxy<ICharacterAchievementManager>.Invocation> achievementChecks =
            achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements));
        Assert.Contains(achievementChecks, invocation =>
            (AchievementType)invocation.Arguments[1] == AchievementType.QuestComplete &&
            (uint)invocation.Arguments[2] == questId);
        Assert.Contains(achievementChecks, invocation =>
            (AchievementType)invocation.Arguments[1] == AchievementType.QuestCompleteChecklist &&
            (uint)invocation.Arguments[2] == questId);
        Assert.Contains(achievementChecks, invocation =>
            (AchievementType)invocation.Arguments[1] == AchievementType.QuestCompleteChecklistCount &&
            (uint)invocation.Arguments[2] == questId);
    }

    [Theory]
    [InlineData(0, 12654u)]
    [InlineData(1, 12654u)]
    [InlineData(2, 12655u)]
    [InlineData(3, 12656u)]
    [InlineData(4, 13328u)]
    [InlineData(5, 14571u)]
    [InlineData(6, 17924u)]
    [InlineData(2187, 12654u)]
    [InlineData(2188, 12655u)]
    [InlineData(2189, 12656u)]
    [InlineData(2481, 13328u)]
    [InlineData(3001, 14571u)]
    [InlineData(3846, 17924u)]
    [InlineData(12654, 12654u)]
    [InlineData(12655, 12655u)]
    [InlineData(12656, 12656u)]
    [InlineData(13328, 13328u)]
    [InlineData(14571, 14571u)]
    [InlineData(17924, 17924u)]
    public void QuestComplete_Q3673SelectableRewardsGrantSelectedItemAndCheckAchievements(
        ushort rewardSelection,
        uint selectedItemId)
    {
        const ushort questId = 3673;
        const uint receiverId = 11063u;

        IQuestInfo questInfo = CreateQuestInfo(
            questId,
            new Quest2RewardEntry
            {
                Id                 = 2187u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 12654u,
                ObjectAmount       = 1u,
                Flags              = 1u
            },
            new Quest2RewardEntry
            {
                Id                 = 2188u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 12655u,
                ObjectAmount       = 1u,
                Flags              = 1u
            },
            new Quest2RewardEntry
            {
                Id                 = 2189u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 12656u,
                ObjectAmount       = 1u,
                Flags              = 1u
            },
            new Quest2RewardEntry
            {
                Id                 = 2481u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 13328u,
                ObjectAmount       = 1u,
                Flags              = 1u
            },
            new Quest2RewardEntry
            {
                Id                 = 3001u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 14571u,
                ObjectAmount       = 1u,
                Flags              = 1u
            },
            new Quest2RewardEntry
            {
                Id                 = 3846u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 17924u,
                ObjectAmount       = 1u,
                Flags              = 1u
            });
        IPlayer player = CreateQuestLifecyclePlayer(
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
            visibleCreatureIds: ImmutableHashSet.Create(receiverId));
        GlobalQuestManager globalQuestManager = CreateGlobalQuestManager(
            new Dictionary<ushort, IQuestInfo>
            {
                [questId] = questInfo
            },
            new Dictionary<ushort, ImmutableList<uint>>
            {
                [questId] = ImmutableList.Create(receiverId)
            });

        IQuest quest = CreateQuest(questId, questInfo, QuestState.Achieved);
        var manager = CreateQuestManager(player, globalQuestManager);
        AddActiveQuest(manager, quest);

        manager.QuestComplete(questId, rewardSelection, communicator: false);

        Assert.Equal(QuestState.Completed, quest.State);
        RecordingDispatchProxy<IInventory>.Invocation itemGrant = Assert.Single(
            inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
        Assert.Equal(InventoryLocation.Inventory, itemGrant.Arguments[0]);
        Assert.Equal(selectedItemId, itemGrant.Arguments[1]);
        Assert.Equal(1u, itemGrant.Arguments[2]);

        IReadOnlyList<RecordingDispatchProxy<ICharacterAchievementManager>.Invocation> achievementChecks =
            achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements));
        Assert.Contains(achievementChecks, invocation =>
            (AchievementType)invocation.Arguments[1] == AchievementType.QuestComplete &&
            (uint)invocation.Arguments[2] == questId);
        Assert.Contains(achievementChecks, invocation =>
            (AchievementType)invocation.Arguments[1] == AchievementType.QuestCompleteChecklist &&
            (uint)invocation.Arguments[2] == questId);
        Assert.Contains(achievementChecks, invocation =>
            (AchievementType)invocation.Arguments[1] == AchievementType.QuestCompleteChecklistCount &&
            (uint)invocation.Arguments[2] == questId);
    }

    [Fact]
    public void QuestComplete_Q3673CommunicatorFlaggedReceiverCompletionAllowsVisibleDeadeye()
    {
        const ushort questId = 3673;
        const uint receiverId = 11063u;

        IQuestInfo questInfo = CreateQuestInfo(questId);
        IPlayer player = CreateQuestLifecyclePlayer(
            out _,
            out _,
            visibleCreatureIds: ImmutableHashSet.Create(receiverId));
        GlobalQuestManager globalQuestManager = CreateGlobalQuestManager(
            new Dictionary<ushort, IQuestInfo>
            {
                [questId] = questInfo
            },
            new Dictionary<ushort, ImmutableList<uint>>
            {
                [questId] = ImmutableList.Create(receiverId)
            });

        IQuest quest = CreateQuest(questId, questInfo, QuestState.Achieved);
        var manager = CreateQuestManager(player, globalQuestManager);
        AddActiveQuest(manager, quest);

        manager.QuestComplete(questId, reward: 0, communicator: true);

        Assert.Equal(QuestState.Completed, quest.State);
    }

    [Fact]
    public void QuestComplete_CommunicatorFlaggedReceiverCompletionWithoutVisibleReceiverStillFails()
    {
        const ushort questId = 3673;
        const uint receiverId = 11063u;

        IQuestInfo questInfo = CreateQuestInfo(questId);
        IPlayer player = CreateQuestLifecyclePlayer(
            out _,
            out _,
            visibleCreatureIds: ImmutableHashSet<uint>.Empty);
        GlobalQuestManager globalQuestManager = CreateGlobalQuestManager(
            new Dictionary<ushort, IQuestInfo>
            {
                [questId] = questInfo
            },
            new Dictionary<ushort, ImmutableList<uint>>
            {
                [questId] = ImmutableList.Create(receiverId)
            });

        IQuest quest = CreateQuest(questId, questInfo, QuestState.Achieved);
        var manager = CreateQuestManager(player, globalQuestManager);
        AddActiveQuest(manager, quest);

        Assert.Throws<QuestException>(() => manager.QuestComplete(questId, reward: 0, communicator: true));
        Assert.Equal(QuestState.Achieved, quest.State);
    }

    [Theory]
    [InlineData(0, 13329u, 1377u, 14102u)]
    [InlineData(1, 13329u, 1377u, 14102u)]
    [InlineData(2, 1377u, 13329u, 14102u)]
    [InlineData(3, 14102u, 13329u, 1377u)]
    [InlineData(1706, 13329u, 1377u, 14102u)]
    [InlineData(1707, 1377u, 13329u, 14102u)]
    [InlineData(2147, 14102u, 13329u, 1377u)]
    [InlineData(13329, 13329u, 1377u, 14102u)]
    [InlineData(1377, 1377u, 13329u, 14102u)]
    [InlineData(14102, 14102u, 13329u, 1377u)]
    public void QuestComplete_Q3486SelectableRewardsGrantSelectedItemCashAndCheckAchievements(
        ushort rewardSelection,
        uint selectedItemId,
        uint firstUnselectedItemId,
        uint secondUnselectedItemId)
    {
        const ushort questId = 3486;
        const uint receiverId = 11194u;

        IQuestInfo questInfo = CreateQuestInfo(
            questId,
            rewardMoney: 1625u,
            new Quest2RewardEntry
            {
                Id                 = 1706u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 13329u,
                ObjectAmount       = 1u,
                Flags              = 1u
            },
            new Quest2RewardEntry
            {
                Id                 = 1707u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 1377u,
                ObjectAmount       = 1u,
                Flags              = 1u
            },
            new Quest2RewardEntry
            {
                Id                 = 2147u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 14102u,
                ObjectAmount       = 1u,
                Flags              = 1u
            });
        IPlayer player = CreateQuestLifecyclePlayer(
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
            out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
            visibleCreatureIds: ImmutableHashSet.Create(receiverId));
        GlobalQuestManager globalQuestManager = CreateGlobalQuestManager(
            new Dictionary<ushort, IQuestInfo>
            {
                [questId] = questInfo
            },
            new Dictionary<ushort, ImmutableList<uint>>
            {
                [questId] = ImmutableList.Create(receiverId)
            });

        IQuest quest = CreateQuest(questId, questInfo, QuestState.Achieved);
        var manager = CreateQuestManager(player, globalQuestManager);
        AddActiveQuest(manager, quest);

        manager.QuestComplete(questId, rewardSelection, communicator: false);

        Assert.Equal(QuestState.Completed, quest.State);
        RecordingDispatchProxy<IInventory>.Invocation itemGrant = Assert.Single(
            inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
        Assert.Equal(InventoryLocation.Inventory, itemGrant.Arguments[0]);
        Assert.Equal(selectedItemId, itemGrant.Arguments[1]);
        Assert.Equal(1u, itemGrant.Arguments[2]);

        IReadOnlyList<RecordingDispatchProxy<IInventory>.Invocation> itemGrants =
            inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate));
        Assert.DoesNotContain(itemGrants, invocation => (uint)invocation.Arguments[1] == firstUnselectedItemId);
        Assert.DoesNotContain(itemGrants, invocation => (uint)invocation.Arguments[1] == secondUnselectedItemId);

        RecordingDispatchProxy<ICurrencyManager>.Invocation currencyGrant = Assert.Single(
            currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
        Assert.Equal(CurrencyType.Credits, currencyGrant.Arguments[0]);
        Assert.Equal(1625ul, currencyGrant.Arguments[1]);

        IReadOnlyList<RecordingDispatchProxy<ICharacterAchievementManager>.Invocation> achievementChecks =
            achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements));
        Assert.Contains(achievementChecks, invocation =>
            (AchievementType)invocation.Arguments[1] == AchievementType.QuestComplete &&
            (uint)invocation.Arguments[2] == questId);
        Assert.Contains(achievementChecks, invocation =>
            (AchievementType)invocation.Arguments[1] == AchievementType.QuestCompleteChecklist &&
            (uint)invocation.Arguments[2] == questId);
        Assert.Contains(achievementChecks, invocation =>
            (AchievementType)invocation.Arguments[1] == AchievementType.QuestCompleteChecklistCount &&
            (uint)invocation.Arguments[2] == questId);
    }

    [Fact]
    public void QuestComplete_Q3486UpdatesArrivalAchievementChecklistRows()
    {
        const ushort questId = 3486;
        const uint receiverId = 11194u;

        IQuestInfo questInfo = CreateQuestInfo(questId);
        IPlayer player = CreateQuestLifecyclePlayer(
            out _,
            out _,
            out _,
            CreateQ3486ArrivalAchievementManager,
            visibleCreatureIds: ImmutableHashSet.Create(receiverId));
        ICharacterAchievementManager achievementManager = player.AchievementManager;
        GlobalQuestManager globalQuestManager = CreateGlobalQuestManager(
            new Dictionary<ushort, IQuestInfo>
            {
                [questId] = questInfo
            },
            new Dictionary<ushort, ImmutableList<uint>>
            {
                [questId] = ImmutableList.Create(receiverId)
            });

        IQuest quest = CreateQuest(questId, questInfo, QuestState.Achieved);
        var manager = CreateQuestManager(player, globalQuestManager);
        AddActiveQuest(manager, quest);

        manager.QuestComplete(questId, reward: 0, communicator: false);

        IAchievement achievement3469 = GetAchievement(achievementManager, 3469);
        Assert.Equal(1u, achievement3469.CompletedChecklistMask);
        Assert.False(achievement3469.IsComplete());

        IAchievement achievement5327 = GetAchievement(achievementManager, 5327);
        Assert.Equal(1u, achievement5327.CompletedChecklistMask);
        Assert.False(achievement5327.IsComplete());
    }

    [Theory]
    [InlineData(0, 27868u, 13332u, 27869u)]
    [InlineData(1, 27868u, 13332u, 27869u)]
    [InlineData(2, 13332u, 27868u, 27869u)]
    [InlineData(3, 27869u, 27868u, 13332u)]
    [InlineData(2192, 27868u, 13332u, 27869u)]
    [InlineData(2430, 13332u, 27868u, 27869u)]
    [InlineData(4767, 27869u, 27868u, 13332u)]
    [InlineData(27868, 27868u, 13332u, 27869u)]
    [InlineData(13332, 13332u, 27868u, 27869u)]
    [InlineData(27869, 27869u, 27868u, 13332u)]
    public void QuestComplete_Q3886SelectableRewardsGrantSelectedItemAndCheckAchievements(
        ushort rewardSelection,
        uint selectedItemId,
        uint firstUnselectedItemId,
        uint secondUnselectedItemId)
    {
        const ushort questId = 3886;
        const uint receiverId = 11066u;

        IQuestInfo questInfo = CreateQuestInfo(
            questId,
            new Quest2RewardEntry
            {
                Id                 = 2192u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 27868u,
                ObjectAmount       = 1u,
                Flags              = 1u
            },
            new Quest2RewardEntry
            {
                Id                 = 2430u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 13332u,
                ObjectAmount       = 1u,
                Flags              = 1u
            },
            new Quest2RewardEntry
            {
                Id                 = 4767u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 27869u,
                ObjectAmount       = 1u,
                Flags              = 1u
            });
        IPlayer player = CreateQuestLifecyclePlayer(
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
            visibleCreatureIds: ImmutableHashSet.Create(receiverId));
        GlobalQuestManager globalQuestManager = CreateGlobalQuestManager(
            new Dictionary<ushort, IQuestInfo>
            {
                [questId] = questInfo
            },
            new Dictionary<ushort, ImmutableList<uint>>
            {
                [questId] = ImmutableList.Create(receiverId)
            });

        IQuest quest = CreateQuest(questId, questInfo, QuestState.Achieved);
        var manager = CreateQuestManager(player, globalQuestManager);
        AddActiveQuest(manager, quest);

        manager.QuestComplete(questId, rewardSelection, communicator: false);

        Assert.Equal(QuestState.Completed, quest.State);
        RecordingDispatchProxy<IInventory>.Invocation itemGrant = Assert.Single(
            inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
        Assert.Equal(InventoryLocation.Inventory, itemGrant.Arguments[0]);
        Assert.Equal(selectedItemId, itemGrant.Arguments[1]);
        Assert.Equal(1u, itemGrant.Arguments[2]);

        IReadOnlyList<RecordingDispatchProxy<IInventory>.Invocation> itemGrants =
            inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate));
        Assert.DoesNotContain(itemGrants, invocation => (uint)invocation.Arguments[1] == firstUnselectedItemId);
        Assert.DoesNotContain(itemGrants, invocation => (uint)invocation.Arguments[1] == secondUnselectedItemId);

        IReadOnlyList<RecordingDispatchProxy<ICharacterAchievementManager>.Invocation> achievementChecks =
            achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements));
        Assert.Contains(achievementChecks, invocation =>
            (AchievementType)invocation.Arguments[1] == AchievementType.QuestComplete &&
            (uint)invocation.Arguments[2] == questId);
        Assert.Contains(achievementChecks, invocation =>
            (AchievementType)invocation.Arguments[1] == AchievementType.QuestCompleteChecklist &&
            (uint)invocation.Arguments[2] == questId);
        Assert.Contains(achievementChecks, invocation =>
            (AchievementType)invocation.Arguments[1] == AchievementType.QuestCompleteChecklistCount &&
            (uint)invocation.Arguments[2] == questId);
    }

    [Theory]
    [InlineData(0, 13333u, 27870u, 27871u)]
    [InlineData(1, 13333u, 27870u, 27871u)]
    [InlineData(2, 27870u, 13333u, 27871u)]
    [InlineData(3, 27871u, 13333u, 27870u)]
    [InlineData(2480, 13333u, 27870u, 27871u)]
    [InlineData(2482, 27870u, 13333u, 27871u)]
    [InlineData(4770, 27871u, 13333u, 27870u)]
    [InlineData(13333, 13333u, 27870u, 27871u)]
    [InlineData(27870, 27870u, 13333u, 27871u)]
    [InlineData(27871, 27871u, 13333u, 27870u)]
    public void QuestComplete_Q3797SelectableRewardsGrantSelectedItemCashAndCheckAchievements(
        ushort rewardSelection,
        uint selectedItemId,
        uint firstUnselectedItemId,
        uint secondUnselectedItemId)
    {
        const ushort questId = 3797;
        const uint receiverId = 11066u;

        IQuestInfo questInfo = CreateQuestInfo(
            questId,
            rewardMoney: 145u,
            new Quest2RewardEntry
            {
                Id                 = 2480u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 13333u,
                ObjectAmount       = 1u,
                Flags              = 1u
            },
            new Quest2RewardEntry
            {
                Id                 = 2482u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 27870u,
                ObjectAmount       = 1u,
                Flags              = 1u
            },
            new Quest2RewardEntry
            {
                Id                 = 4770u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 27871u,
                ObjectAmount       = 1u,
                Flags              = 1u
            });
        IPlayer player = CreateQuestLifecyclePlayer(
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
            out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
            visibleCreatureIds: ImmutableHashSet.Create(receiverId));
        GlobalQuestManager globalQuestManager = CreateGlobalQuestManager(
            new Dictionary<ushort, IQuestInfo>
            {
                [questId] = questInfo
            },
            new Dictionary<ushort, ImmutableList<uint>>
            {
                [questId] = ImmutableList.Create(receiverId)
            });

        IQuest quest = CreateQuest(questId, questInfo, QuestState.Achieved);
        var manager = CreateQuestManager(player, globalQuestManager);
        AddActiveQuest(manager, quest);

        manager.QuestComplete(questId, rewardSelection, communicator: false);

        Assert.Equal(QuestState.Completed, quest.State);
        RecordingDispatchProxy<IInventory>.Invocation itemGrant = Assert.Single(
            inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
        Assert.Equal(InventoryLocation.Inventory, itemGrant.Arguments[0]);
        Assert.Equal(selectedItemId, itemGrant.Arguments[1]);
        Assert.Equal(1u, itemGrant.Arguments[2]);

        IReadOnlyList<RecordingDispatchProxy<IInventory>.Invocation> itemGrants =
            inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate));
        Assert.DoesNotContain(itemGrants, invocation => (uint)invocation.Arguments[1] == firstUnselectedItemId);
        Assert.DoesNotContain(itemGrants, invocation => (uint)invocation.Arguments[1] == secondUnselectedItemId);

        RecordingDispatchProxy<ICurrencyManager>.Invocation currencyGrant = Assert.Single(
            currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
        Assert.Equal(CurrencyType.Credits, currencyGrant.Arguments[0]);
        Assert.Equal(145ul, currencyGrant.Arguments[1]);

        IReadOnlyList<RecordingDispatchProxy<ICharacterAchievementManager>.Invocation> achievementChecks =
            achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements));
        Assert.Contains(achievementChecks, invocation =>
            (AchievementType)invocation.Arguments[1] == AchievementType.QuestComplete &&
            (uint)invocation.Arguments[2] == questId);
        Assert.Contains(achievementChecks, invocation =>
            (AchievementType)invocation.Arguments[1] == AchievementType.QuestCompleteChecklist &&
            (uint)invocation.Arguments[2] == questId);
        Assert.Contains(achievementChecks, invocation =>
            (AchievementType)invocation.Arguments[1] == AchievementType.QuestCompleteChecklistCount &&
            (uint)invocation.Arguments[2] == questId);
    }

    [Theory]
    [InlineData(0, 81334u, 81331u, 81329u)]
    [InlineData(1, 81334u, 81331u, 81329u)]
    [InlineData(2, 81331u, 81334u, 81329u)]
    [InlineData(3, 81329u, 81334u, 81331u)]
    [InlineData(2190, 81334u, 81331u, 81329u)]
    [InlineData(2191, 81331u, 81334u, 81329u)]
    [InlineData(4768, 81329u, 81334u, 81331u)]
    [InlineData(15798, 81334u, 81331u, 81329u)]
    [InlineData(15795, 81331u, 81334u, 81329u)]
    [InlineData(15793, 81329u, 81334u, 81331u)]
    public void QuestComplete_Q3668SelectableRewardsGrantSelectedItemAndCheckAchievements(
        ushort rewardSelection,
        uint selectedItemId,
        uint firstUnselectedItemId,
        uint secondUnselectedItemId)
    {
        const ushort questId = 3668;
        const uint receiverId = 12737u;

        IQuestInfo questInfo = CreateQuestInfo(
            questId,
            new Quest2RewardEntry
            {
                Id                 = 2190u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 81334u,
                ObjectAmount       = 1u,
                Flags              = 1u
            },
            new Quest2RewardEntry
            {
                Id                 = 2191u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 81331u,
                ObjectAmount       = 1u,
                Flags              = 1u
            },
            new Quest2RewardEntry
            {
                Id                 = 4768u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 81329u,
                ObjectAmount       = 1u,
                Flags              = 1u
            });
        IPlayer player = CreateQuestLifecyclePlayer(
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
            visibleCreatureIds: ImmutableHashSet.Create(receiverId));
        GlobalQuestManager globalQuestManager = CreateGlobalQuestManager(
            new Dictionary<ushort, IQuestInfo>
            {
                [questId] = questInfo
            },
            new Dictionary<ushort, ImmutableList<uint>>
            {
                [questId] = ImmutableList.Create(receiverId)
            });

        IQuest quest = CreateQuest(questId, questInfo, QuestState.Achieved);
        var manager = CreateQuestManager(player, globalQuestManager);
        AddActiveQuest(manager, quest);

        manager.QuestComplete(questId, rewardSelection, communicator: false);

        Assert.Equal(QuestState.Completed, quest.State);
        RecordingDispatchProxy<IInventory>.Invocation itemGrant = Assert.Single(
            inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
        Assert.Equal(InventoryLocation.Inventory, itemGrant.Arguments[0]);
        Assert.Equal(selectedItemId, itemGrant.Arguments[1]);
        Assert.Equal(1u, itemGrant.Arguments[2]);

        IReadOnlyList<RecordingDispatchProxy<IInventory>.Invocation> itemGrants =
            inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate));
        Assert.DoesNotContain(itemGrants, invocation => (uint)invocation.Arguments[1] == firstUnselectedItemId);
        Assert.DoesNotContain(itemGrants, invocation => (uint)invocation.Arguments[1] == secondUnselectedItemId);

        IReadOnlyList<RecordingDispatchProxy<ICharacterAchievementManager>.Invocation> achievementChecks =
            achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements));
        Assert.Contains(achievementChecks, invocation =>
            (AchievementType)invocation.Arguments[1] == AchievementType.QuestComplete &&
            (uint)invocation.Arguments[2] == questId);
        Assert.Contains(achievementChecks, invocation =>
            (AchievementType)invocation.Arguments[1] == AchievementType.QuestCompleteChecklist &&
            (uint)invocation.Arguments[2] == questId);
        Assert.Contains(achievementChecks, invocation =>
            (AchievementType)invocation.Arguments[1] == AchievementType.QuestCompleteChecklistCount &&
            (uint)invocation.Arguments[2] == questId);
    }

    [Fact]
    public void QuestComplete_Q3668InventoryFullRejectsCompletionBeforeItemExperienceAndAchievements()
    {
        const ushort questId = 3668;
        const uint receiverId = 12737u;

        IQuestInfo questInfo = CreateQuestInfoWithRewardExperience(
            questId,
            152u,
            new Quest2RewardEntry
            {
                Id                 = 2190u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 81334u,
                ObjectAmount       = 1u,
                Flags              = 1u
            });
        IPlayer player = CreateQuestLifecyclePlayer(
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
            out RecordingDispatchProxy<IXpManager> xpProxy,
            visibleCreatureIds: ImmutableHashSet.Create(receiverId));
        inventoryProxy.SetMethodReturn(nameof(IInventory.CanCreateItem), false);
        GlobalQuestManager globalQuestManager = CreateGlobalQuestManager(
            new Dictionary<ushort, IQuestInfo>
            {
                [questId] = questInfo
            },
            new Dictionary<ushort, ImmutableList<uint>>
            {
                [questId] = ImmutableList.Create(receiverId)
            });

        IQuest quest = CreateQuest(questId, questInfo, QuestState.Achieved);
        var manager = CreateQuestManager(player, globalQuestManager);
        AddActiveQuest(manager, quest);

        manager.QuestComplete(questId, reward: 0, communicator: false);

        Assert.Equal(QuestState.Achieved, quest.State);
        RecordingDispatchProxy<IInventory>.Invocation capacityCheck = Assert.Single(
            inventoryProxy.GetInvocations(nameof(IInventory.CanCreateItem)));
        Assert.Equal(InventoryLocation.Inventory, capacityCheck.Arguments[0]);
        Assert.Equal(81334u, capacityCheck.Arguments[1]);
        Assert.Equal(1u, capacityCheck.Arguments[2]);
        Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
        Assert.Empty(xpProxy.GetInvocations(nameof(IXpManager.GrantXp)));
        Assert.Empty(achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements)));
    }

    [Theory]
    [InlineData(0, 28048u)]
    [InlineData(1, 28048u)]
    [InlineData(2, 28031u)]
    [InlineData(3, 76391u)]
    [InlineData(4, 76392u)]
    [InlineData(5, 76393u)]
    [InlineData(6, 76484u)]
    [InlineData(7, 28015u)]
    [InlineData(2244, 28048u)]
    [InlineData(2388, 28031u)]
    [InlineData(7191, 76391u)]
    [InlineData(7192, 76392u)]
    [InlineData(7193, 76393u)]
    [InlineData(7284, 76484u)]
    [InlineData(8867, 28015u)]
    [InlineData(28048, 28048u)]
    [InlineData(28031, 28031u)]
    [InlineData(10855, 76391u)]
    [InlineData(10856, 76392u)]
    [InlineData(10857, 76393u)]
    [InlineData(10948, 76484u)]
    [InlineData(28015, 28015u)]
    public void QuestComplete_Q3777SelectableRewardsGrantSelectedItemAndCheckAchievements(
        ushort rewardSelection,
        uint selectedItemId)
    {
        const ushort questId = 3777;
        const uint receiverId = 15759u;

        IQuestInfo questInfo = CreateQuestInfo(
            questId,
            new Quest2RewardEntry
            {
                Id                 = 2244u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 28048u,
                ObjectAmount       = 1u,
                Flags              = 1u
            },
            new Quest2RewardEntry
            {
                Id                 = 2388u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 28031u,
                ObjectAmount       = 1u,
                Flags              = 1u
            },
            new Quest2RewardEntry
            {
                Id                 = 7191u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 76391u,
                ObjectAmount       = 1u,
                Flags              = 1u
            },
            new Quest2RewardEntry
            {
                Id                 = 7192u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 76392u,
                ObjectAmount       = 1u,
                Flags              = 1u
            },
            new Quest2RewardEntry
            {
                Id                 = 7193u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 76393u,
                ObjectAmount       = 1u,
                Flags              = 1u
            },
            new Quest2RewardEntry
            {
                Id                 = 7284u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 76484u,
                ObjectAmount       = 1u,
                Flags              = 1u
            },
            new Quest2RewardEntry
            {
                Id                 = 8867u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 28015u,
                ObjectAmount       = 1u,
                Flags              = 1u
            });
        IPlayer player = CreateQuestLifecyclePlayer(
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
            visibleCreatureIds: ImmutableHashSet.Create(receiverId));
        GlobalQuestManager globalQuestManager = CreateGlobalQuestManager(
            new Dictionary<ushort, IQuestInfo>
            {
                [questId] = questInfo
            },
            new Dictionary<ushort, ImmutableList<uint>>
            {
                [questId] = ImmutableList.Create(receiverId)
            });

        IQuest quest = CreateQuest(questId, questInfo, QuestState.Achieved);
        var manager = CreateQuestManager(player, globalQuestManager);
        AddActiveQuest(manager, quest);

        manager.QuestComplete(questId, rewardSelection, communicator: false);

        Assert.Equal(QuestState.Completed, quest.State);
        RecordingDispatchProxy<IInventory>.Invocation itemGrant = Assert.Single(
            inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
        Assert.Equal(InventoryLocation.Inventory, itemGrant.Arguments[0]);
        Assert.Equal(selectedItemId, itemGrant.Arguments[1]);
        Assert.Equal(1u, itemGrant.Arguments[2]);

        IReadOnlyList<RecordingDispatchProxy<ICharacterAchievementManager>.Invocation> achievementChecks =
            achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements));
        Assert.Contains(achievementChecks, invocation =>
            (AchievementType)invocation.Arguments[1] == AchievementType.QuestComplete &&
            (uint)invocation.Arguments[2] == questId);
        Assert.Contains(achievementChecks, invocation =>
            (AchievementType)invocation.Arguments[1] == AchievementType.QuestCompleteChecklist &&
            (uint)invocation.Arguments[2] == questId);
        Assert.Contains(achievementChecks, invocation =>
            (AchievementType)invocation.Arguments[1] == AchievementType.QuestCompleteChecklistCount &&
            (uint)invocation.Arguments[2] == questId);
    }

    [Fact]
    public void QuestComplete_Q3777InlineReputationRewardUsesDifficultyDefault()
    {
        const ushort questId = 3777;
        const uint receiverId = 15759u;

        IQuestInfo questInfo = CreateQuestInfo(
            questId,
            entry =>
            {
                entry.Faction2IdRewardReputation00 = 260u;
                entry.RewardReputationOverride00   = 0f;
            },
            188f,
            new Quest2RewardEntry
            {
                Id                 = 2244u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 28048u,
                ObjectAmount       = 1u,
                Flags              = 1u
            });
        IPlayer player = CreateQuestLifecyclePlayer(
            out _,
            out _,
            out RecordingDispatchProxy<IReputationManager> reputationProxy,
            visibleCreatureIds: ImmutableHashSet.Create(receiverId));
        GlobalQuestManager globalQuestManager = CreateGlobalQuestManager(
            new Dictionary<ushort, IQuestInfo>
            {
                [questId] = questInfo
            },
            new Dictionary<ushort, ImmutableList<uint>>
            {
                [questId] = ImmutableList.Create(receiverId)
            });

        IQuest quest = CreateQuest(questId, questInfo, QuestState.Achieved);
        var manager = CreateQuestManager(player, globalQuestManager);
        AddActiveQuest(manager, quest);

        manager.QuestComplete(questId, reward: 0, communicator: false);

        RecordingDispatchProxy<IReputationManager>.Invocation reputationUpdate = Assert.Single(
            reputationProxy.GetInvocations(nameof(IReputationManager.UpdateReputation)));
        Assert.Equal((Faction)260u, reputationUpdate.Arguments[0]);
        Assert.Equal(188f, reputationUpdate.Arguments[1]);
    }

    [Fact]
    public void QuestComplete_Q3741FixedRewardsGrantAllItemsAndCheckAchievements()
    {
        const ushort questId = 3741;
        const uint receiverId = 11066u;

        IQuestInfo questInfo = CreateQuestInfo(
            questId,
            new Quest2RewardEntry
            {
                Id                 = 1712u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 81917u,
                ObjectAmount       = 3u,
                Flags              = 0u
            },
            new Quest2RewardEntry
            {
                Id                 = 3476u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 29614u,
                ObjectAmount       = 1u,
                Flags              = 0u
            });
        IPlayer player = CreateQuestLifecyclePlayer(
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
            visibleCreatureIds: ImmutableHashSet.Create(receiverId));
        GlobalQuestManager globalQuestManager = CreateGlobalQuestManager(
            new Dictionary<ushort, IQuestInfo>
            {
                [questId] = questInfo
            },
            new Dictionary<ushort, ImmutableList<uint>>
            {
                [questId] = ImmutableList.Create(receiverId)
            });

        IQuest quest = CreateQuest(questId, questInfo, QuestState.Achieved);
        var manager = CreateQuestManager(player, globalQuestManager);
        AddActiveQuest(manager, quest);

        manager.QuestComplete(questId, reward: 0, communicator: false);

        Assert.Equal(QuestState.Completed, quest.State);
        IReadOnlyList<RecordingDispatchProxy<IInventory>.Invocation> itemGrants =
            inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate));
        Assert.Equal(2, itemGrants.Count);
        Assert.Contains(itemGrants, invocation =>
            (InventoryLocation)invocation.Arguments[0] == InventoryLocation.Inventory &&
            (uint)invocation.Arguments[1] == 81917u &&
            (uint)invocation.Arguments[2] == 3u);
        Assert.Contains(itemGrants, invocation =>
            (InventoryLocation)invocation.Arguments[0] == InventoryLocation.Inventory &&
            (uint)invocation.Arguments[1] == 29614u &&
            (uint)invocation.Arguments[2] == 1u);

        IReadOnlyList<RecordingDispatchProxy<ICharacterAchievementManager>.Invocation> achievementChecks =
            achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements));
        Assert.Contains(achievementChecks, invocation =>
            (AchievementType)invocation.Arguments[1] == AchievementType.QuestComplete &&
            (uint)invocation.Arguments[2] == questId);
        Assert.Contains(achievementChecks, invocation =>
            (AchievementType)invocation.Arguments[1] == AchievementType.QuestCompleteChecklist &&
            (uint)invocation.Arguments[2] == questId);
        Assert.Contains(achievementChecks, invocation =>
            (AchievementType)invocation.Arguments[1] == AchievementType.QuestCompleteChecklistCount &&
            (uint)invocation.Arguments[2] == questId);
    }

    [Fact]
    public void QuestComplete_Q5597FixedRewardsGrantAllItemsAndUpdatesBloodstoneAchievement()
    {
        const ushort questId = 5597;
        const uint receiverId = 24187u;

        IQuestInfo questInfo = CreateQuestInfo(
            questId,
            new Quest2RewardEntry
            {
                Id                 = 3000u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 81917u,
                ObjectAmount       = 1u,
                Flags              = 0u
            },
            new Quest2RewardEntry
            {
                Id                 = 5236u,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.Item,
                ObjectId           = 29664u,
                ObjectAmount       = 1u,
                Flags              = 0u
            });
        IPlayer player = CreateQuestLifecyclePlayer(
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out _,
            out _,
            CreateQ5597BloodstoneAchievementManager,
            visibleCreatureIds: ImmutableHashSet.Create(receiverId));
        ICharacterAchievementManager achievementManager = player.AchievementManager;
        GlobalQuestManager globalQuestManager = CreateGlobalQuestManager(
            new Dictionary<ushort, IQuestInfo>
            {
                [questId] = questInfo
            },
            new Dictionary<ushort, ImmutableList<uint>>
            {
                [questId] = ImmutableList.Create(receiverId)
            });

        IQuest quest = CreateQuest(questId, questInfo, QuestState.Achieved);
        var manager = CreateQuestManager(player, globalQuestManager);
        AddActiveQuest(manager, quest);

        manager.QuestComplete(questId, reward: 0, communicator: false);

        Assert.Equal(QuestState.Completed, quest.State);
        IReadOnlyList<RecordingDispatchProxy<IInventory>.Invocation> itemGrants =
            inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate));
        Assert.Equal(2, itemGrants.Count);
        Assert.Contains(itemGrants, invocation =>
            (InventoryLocation)invocation.Arguments[0] == InventoryLocation.Inventory &&
            (uint)invocation.Arguments[1] == 81917u &&
            (uint)invocation.Arguments[2] == 1u);
        Assert.Contains(itemGrants, invocation =>
            (InventoryLocation)invocation.Arguments[0] == InventoryLocation.Inventory &&
            (uint)invocation.Arguments[1] == 29664u &&
            (uint)invocation.Arguments[2] == 1u);

        IAchievement achievement = GetAchievement(achievementManager, 4134);
        Assert.Equal(2u, achievement.CompletedChecklistMask);
        Assert.True(achievement.IsComplete());
    }

    [Fact]
    public void QuestComplete_Q5597RejectsMissingMondoReceiver()
    {
        IQuestInfo questInfo = CreateQuestInfo(5597);
        IPlayer player = CreateQuestRewardPlayer(
            out _,
            out _,
            visibleReceiverIds: ImmutableHashSet<uint>.Empty);
        GlobalQuestManager globalQuestManager = CreateGlobalQuestManager(
            new Dictionary<ushort, IQuestInfo>
            {
                [5597] = questInfo
            },
            new Dictionary<ushort, ImmutableList<uint>>
            {
                [5597] = ImmutableList.Create(24187u)
            });

        IQuest quest = CreateQuest(5597, questInfo, QuestState.Achieved);
        var manager = CreateQuestManager(player, globalQuestManager);
        AddActiveQuest(manager, quest);

        Assert.Throws<QuestException>(() => manager.QuestComplete(5597, reward: 0, communicator: false));
        Assert.Equal(QuestState.Achieved, quest.State);
    }

    [Fact]
    public void QuestComplete_Q5573RequiresVisibleMondoReceiver()
    {
        IQuestInfo questInfo = CreateQuestInfo(5573);
        IPlayer player = CreateQuestRewardPlayer(
            out _,
            out _,
            visibleReceiverIds: ImmutableHashSet.Create(24187u));
        GlobalQuestManager globalQuestManager = CreateGlobalQuestManager(
            new Dictionary<ushort, IQuestInfo>
            {
                [5573] = questInfo
            },
            new Dictionary<ushort, ImmutableList<uint>>
            {
                [5573] = ImmutableList.Create(24187u)
            });

        IQuest quest = CreateQuest(5573, questInfo, QuestState.Achieved);
        var manager = CreateQuestManager(player, globalQuestManager);
        AddActiveQuest(manager, quest);

        manager.QuestComplete(5573, reward: 0, communicator: false);

        Assert.Equal(QuestState.Completed, quest.State);
    }

    [Fact]
    public void QuestComplete_Q4696RejectsVisibleStarterWithoutDarbyReceiver()
    {
        IQuestInfo questInfo = CreateQuestInfo(4696);
        IPlayer player = CreateQuestRewardPlayer(
            out _,
            out _,
            visibleReceiverIds: ImmutableHashSet.Create(17175u));
        GlobalQuestManager globalQuestManager = CreateGlobalQuestManager(
            new Dictionary<ushort, IQuestInfo>
            {
                [4696] = questInfo
            },
            new Dictionary<ushort, ImmutableList<uint>>
            {
                [4696] = ImmutableList.Create(17053u)
            });

        IQuest quest = CreateQuest(4696, questInfo, QuestState.Achieved);
        var manager = CreateQuestManager(player, globalQuestManager);
        AddActiveQuest(manager, quest);

        Assert.Throws<QuestException>(() => manager.QuestComplete(4696, reward: 0, communicator: false));
        Assert.Equal(QuestState.Achieved, quest.State);
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
        GlobalQuestManager globalQuestManager = CreateGlobalQuestManager(
            new Dictionary<ushort, IQuestInfo>
            {
                [questId] = questInfo
            },
            new Dictionary<ushort, ImmutableList<uint>>
            {
                [questId] = ImmutableList.Create(73421u)
            });
        GameTableManager gameTableManager = BuildGameTableManager(
            accountCurrencyTypeTable: CreateGameTable(new AccountCurrencyTypeEntry
            {
                Id = (uint)AccountCurrencyType.Omnibit
            }));

        IQuest quest = CreateQuest(questId, questInfo, QuestState.Achieved);
        var manager = CreateQuestManager(player, globalQuestManager, gameTableManager);
        AddActiveQuest(manager, quest);

        manager.QuestComplete(questId, reward: 0, communicator: false);

        RecordingDispatchProxy<IAccountCurrencyManager>.Invocation currencyGrant = Assert.Single(
            accountCurrencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencyAddAmount)));
        Assert.Equal(AccountCurrencyType.Omnibit, currencyGrant.Arguments[0]);
        Assert.Equal(1ul, currencyGrant.Arguments[1]);
    }

    [Fact]
    public void QuestComplete_AccountCurrencyReward_UsesAccountCurrencyTypeTableForValidIds()
    {
        const ushort questId = 9100;
        const uint rewardId = 9101u;
        const uint tableBackedCurrencyId = 20u;
        IQuestInfo questInfo = CreateQuestInfo(
            questId,
            new Quest2RewardEntry
            {
                Id                 = rewardId,
                Quest2Id           = questId,
                Quest2RewardTypeId = (uint)QuestRewardType.AccountCurrency,
                ObjectId           = tableBackedCurrencyId,
                ObjectAmount       = 3u
            });
        IPlayer player = CreateQuestRewardPlayer(
            out _,
            out RecordingDispatchProxy<IAccountCurrencyManager> accountCurrencyProxy,
            visibleReceiverIds: ImmutableHashSet.Create(73421u));
        GlobalQuestManager globalQuestManager = CreateGlobalQuestManager(
            new Dictionary<ushort, IQuestInfo>
            {
                [questId] = questInfo
            },
            new Dictionary<ushort, ImmutableList<uint>>
            {
                [questId] = ImmutableList.Create(73421u)
            });
        GameTableManager gameTableManager = BuildGameTableManager(
            accountCurrencyTypeTable: CreateGameTable(new AccountCurrencyTypeEntry
            {
                Id = tableBackedCurrencyId
            }));

        IQuest quest = CreateQuest(questId, questInfo, QuestState.Achieved);
        var manager = CreateQuestManager(player, globalQuestManager, gameTableManager);
        AddActiveQuest(manager, quest);

        manager.QuestComplete(questId, reward: 0, communicator: false);

        RecordingDispatchProxy<IAccountCurrencyManager>.Invocation currencyGrant = Assert.Single(
            accountCurrencyProxy.GetInvocations(nameof(IAccountCurrencyManager.CurrencyAddAmount)));
        Assert.Equal((AccountCurrencyType)tableBackedCurrencyId, currencyGrant.Arguments[0]);
        Assert.Equal(3ul, currencyGrant.Arguments[1]);
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
        GlobalQuestManager globalQuestManager = CreateGlobalQuestManager(
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

        IQuest quest = CreateQuest(questId, questInfo, QuestState.Achieved);
        var manager = CreateQuestManager(player, globalQuestManager);
        AddActiveQuest(manager, quest);

        manager.QuestComplete(questId, reward: 0, communicator: false);

        RecordingDispatchProxy<IInventory>.Invocation itemGrant = Assert.Single(
            inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
        Assert.Equal(InventoryLocation.Inventory, itemGrant.Arguments[0]);
        Assert.Equal(80875u, itemGrant.Arguments[1]);
        Assert.Equal(1u, itemGrant.Arguments[2]);
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
        GlobalQuestManager globalQuestManager = CreateGlobalQuestManager(
            new Dictionary<ushort, IQuestInfo>
            {
                [questId] = questInfo
            },
            new Dictionary<ushort, ImmutableList<uint>>
            {
                [questId] = ImmutableList.Create(74812u)
            });

        IQuest quest = CreateQuest(questId, questInfo, QuestState.Achieved);
        var manager = CreateQuestManager(player, globalQuestManager);
        AddActiveQuest(manager, quest);

        manager.QuestComplete(questId, rewardSelection, communicator: false);

        IReadOnlyList<RecordingDispatchProxy<IInventory>.Invocation> itemGrants =
            inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate));
        Assert.Equal(2, itemGrants.Count);
        Assert.Contains(itemGrants, invocation => (uint)invocation.Arguments[1] == requiredItemId);
        Assert.Contains(itemGrants, invocation => (uint)invocation.Arguments[1] == selectedItemId);
        Assert.DoesNotContain(itemGrants, invocation => (uint)invocation.Arguments[1] == unselectedItemId);
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
        GlobalQuestManager globalQuestManager = CreateGlobalQuestManager(
            new Dictionary<ushort, IQuestInfo>
            {
                [questId] = questInfo
            },
            new Dictionary<ushort, ImmutableList<uint>>
            {
                [questId] = ImmutableList.Create(74812u)
            });

        IQuest quest = CreateQuest(questId, questInfo, QuestState.Achieved);
        var manager = CreateQuestManager(player, globalQuestManager);
        AddActiveQuest(manager, quest);

        Assert.Throws<QuestException>(() => manager.QuestComplete(questId, reward: 9999, communicator: false));

        Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
    }

    [Fact]
    public void ObjectiveUpdate_WhenQuestIsCompleted_DoesNotRevertToAchieved()
    {
        IPlayer player = CreatePlayer(out _);

        var quest = new NexusForever.Game.Quest.Quest(
            player,
            CreateSequentialQuestInfo(),
            CreateCompletedSequentialQuestModel(),
            scriptManager: CreateScriptManager());

        quest.ObjectiveUpdate(QuestObjectiveType.CompleteQuest, 9001u, 1u);

        Assert.Equal(QuestState.Completed, quest.State);
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
        inventoryProxy.SetMethodReturn(nameof(IInventory.CanCreateItem), true);

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

    private static IPlayer CreateQuestAcceptPlayer(
        IReadOnlySet<uint> visibleCreatureIds,
        Faction faction,
        uint level)
    {
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out RecordingDispatchProxy<IInventory> inventoryProxy);
        inventoryProxy.SetMethodReturn(nameof(IInventory.GetInventorySlotsRemaining), 10u);
        inventoryProxy.SetMethodReturn(nameof(IInventory.CanCreateItem), true);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out _);

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.Faction1), faction);
        playerProxy.SetProperty(nameof(IPlayer.Level), level);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        playerProxy.SetMethodHandler(nameof(IPlayer.GetVisibleCreature), args =>
        {
            uint creatureId = (uint)args[0];
            return visibleCreatureIds.Contains(creatureId)
                ? new WorldEntity[] { null }
                : Array.Empty<WorldEntity>();
        });
        return player;
    }

    private static IPlayer CreateQuestLifecyclePlayer(
        out RecordingDispatchProxy<IInventory> inventoryProxy,
        out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
        IReadOnlySet<uint> visibleCreatureIds)
    {
        return CreateQuestLifecyclePlayer(
            out inventoryProxy,
            out achievementProxy,
            out _,
            out _,
            visibleCreatureIds);
    }

    private static IPlayer CreateQuestLifecyclePlayer(
        out RecordingDispatchProxy<IInventory> inventoryProxy,
        out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
        out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
        IReadOnlySet<uint> visibleCreatureIds)
    {
        return CreateQuestLifecyclePlayer(
            out inventoryProxy,
            out achievementProxy,
            out _,
            out currencyProxy,
            visibleCreatureIds);
    }

    private static IPlayer CreateQuestLifecyclePlayer(
        out RecordingDispatchProxy<IInventory> inventoryProxy,
        out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
        out RecordingDispatchProxy<IXpManager> xpProxy,
        IReadOnlySet<uint> visibleCreatureIds)
    {
        return CreateQuestLifecyclePlayer(
            out inventoryProxy,
            out achievementProxy,
            out _,
            out _,
            out xpProxy,
            visibleCreatureIds);
    }

    private static IPlayer CreateQuestLifecyclePlayer(
        out RecordingDispatchProxy<IInventory> inventoryProxy,
        out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
        out RecordingDispatchProxy<IReputationManager> reputationProxy,
        IReadOnlySet<uint> visibleCreatureIds)
    {
        return CreateQuestLifecyclePlayer(
            out inventoryProxy,
            out achievementProxy,
            out reputationProxy,
            out _,
            out _,
            visibleCreatureIds);
    }

    private static IPlayer CreateQuestLifecyclePlayer(
        out RecordingDispatchProxy<IInventory> inventoryProxy,
        out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
        out RecordingDispatchProxy<IReputationManager> reputationProxy,
        out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
        IReadOnlySet<uint> visibleCreatureIds)
    {
        return CreateQuestLifecyclePlayer(
            out inventoryProxy,
            out achievementProxy,
            out reputationProxy,
            out currencyProxy,
            out _,
            visibleCreatureIds);
    }

    private static IPlayer CreateQuestLifecyclePlayer(
        out RecordingDispatchProxy<IInventory> inventoryProxy,
        out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
        out RecordingDispatchProxy<IReputationManager> reputationProxy,
        out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
        out RecordingDispatchProxy<IXpManager> xpProxy,
        IReadOnlySet<uint> visibleCreatureIds)
    {
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out inventoryProxy);
        inventoryProxy.SetMethodReturn(nameof(IInventory.GetInventorySlotsRemaining), 10u);
        inventoryProxy.SetMethodReturn(nameof(IInventory.CanCreateItem), true);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out _);
        ICharacterAchievementManager achievementManager = RecordingDispatchProxy<ICharacterAchievementManager>.Create(out achievementProxy);
        IReputationManager reputationManager = RecordingDispatchProxy<IReputationManager>.Create(out reputationProxy);
        ICurrencyManager currencyManager = RecordingDispatchProxy<ICurrencyManager>.Create(out currencyProxy);
        IXpManager xpManager = RecordingDispatchProxy<IXpManager>.Create(out xpProxy);

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.Level), 5u);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievementManager);
        playerProxy.SetProperty(nameof(IPlayer.ReputationManager), reputationManager);
        playerProxy.SetProperty(nameof(IPlayer.CurrencyManager), currencyManager);
        playerProxy.SetProperty(nameof(IPlayer.XpManager), xpManager);
        playerProxy.SetMethodHandler(nameof(IPlayer.GetVisibleCreature), args =>
        {
            uint creatureId = (uint)args[0];
            return visibleCreatureIds.Contains(creatureId)
                ? new WorldEntity[] { null }
                : Array.Empty<WorldEntity>();
        });
        return player;
    }

    private static IPlayer CreateQ9880ContractLifecyclePlayer(
        out RecordingDispatchProxy<IInventory> inventoryProxy,
        out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy)
    {
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out inventoryProxy);
        inventoryProxy.SetMethodReturn(nameof(IInventory.GetInventorySlotsRemaining), 10u);
        inventoryProxy.SetMethodReturn(nameof(IInventory.CanCreateItem), true);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out _);
        ICharacterAchievementManager achievementManager = RecordingDispatchProxy<ICharacterAchievementManager>.Create(out achievementProxy);
        IReputationManager reputationManager = RecordingDispatchProxy<IReputationManager>.Create(out _);
        ICurrencyManager currencyManager = RecordingDispatchProxy<ICurrencyManager>.Create(out _);

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.Level), 50u);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievementManager);
        playerProxy.SetProperty(nameof(IPlayer.ReputationManager), reputationManager);
        playerProxy.SetProperty(nameof(IPlayer.CurrencyManager), currencyManager);
        playerProxy.SetMethodHandler(nameof(IPlayer.GetVisibleCreature), _ => Array.Empty<WorldEntity>());
        return player;
    }

    private static IPlayer CreateQuestLifecyclePlayer(
        out RecordingDispatchProxy<IInventory> inventoryProxy,
        out RecordingDispatchProxy<IReputationManager> reputationProxy,
        out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
        Func<IPlayer, ICharacterAchievementManager> createAchievementManager,
        IReadOnlySet<uint> visibleCreatureIds)
    {
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out inventoryProxy);
        inventoryProxy.SetMethodReturn(nameof(IInventory.GetInventorySlotsRemaining), 10u);
        inventoryProxy.SetMethodReturn(nameof(IInventory.CanCreateItem), true);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out _);
        IReputationManager reputationManager = RecordingDispatchProxy<IReputationManager>.Create(out reputationProxy);
        ICurrencyManager currencyManager = RecordingDispatchProxy<ICurrencyManager>.Create(out currencyProxy);
        IGuildManager guildManager = RecordingDispatchProxy<IGuildManager>.Create(out _);

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.Level), 5u);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        playerProxy.SetProperty(nameof(IPlayer.ReputationManager), reputationManager);
        playerProxy.SetProperty(nameof(IPlayer.CurrencyManager), currencyManager);
        playerProxy.SetProperty(nameof(IPlayer.GuildManager), guildManager);
        playerProxy.SetMethodHandler(nameof(IPlayer.GetVisibleCreature), args =>
        {
            uint creatureId = (uint)args[0];
            return visibleCreatureIds.Contains(creatureId)
                ? new WorldEntity[] { null }
                : Array.Empty<WorldEntity>();
        });

        ICharacterAchievementManager achievementManager = createAchievementManager(player);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievementManager);
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

    private static IQuestInfo CreateQ4696QuestInfo()
    {
        IQuestInfo questInfo = RecordingDispatchProxy<IQuestInfo>.Create(out var questInfoProxy);
        questInfoProxy.SetProperty(nameof(IQuestInfo.Entry), new Quest2Entry
        {
            Id = 4696u
        });
        questInfoProxy.SetProperty(nameof(IQuestInfo.Objectives), ImmutableList.Create<IQuestObjectiveInfo>(
            new QuestObjectiveInfo(new QuestObjectiveEntry
            {
                Id                      = 6508u,
                Type                    = (uint)QuestObjectiveType.ActivateEntity,
                Data                    = 0u,
                Count                   = 5u,
                TargetGroupIdRewardPane = 4323u
            })));

        return questInfo;
    }

    private static IQuestInfo CreateQ3777ByLeapsAndBoundsQuestInfo()
    {
        IQuestInfo questInfo = RecordingDispatchProxy<IQuestInfo>.Create(out var questInfoProxy);
        questInfoProxy.SetProperty(nameof(IQuestInfo.Entry), new Quest2Entry
        {
            Id = 3777u
        });
        questInfoProxy.SetProperty(nameof(IQuestInfo.Objectives), ImmutableList.Create<IQuestObjectiveInfo>(
            new QuestObjectiveInfo(new QuestObjectiveEntry
            {
                Id    = 5075u,
                Type  = (uint)QuestObjectiveType.EnterZone,
                Data  = 219u,
                Count = 1u
            }),
            new QuestObjectiveInfo(new QuestObjectiveEntry
            {
                Id                      = 5076u,
                Type                    = (uint)QuestObjectiveType.ActivateEntity,
                Data                    = 6952u,
                Count                   = 1u,
                Flags                   = 3u,
                TargetGroupIdRewardPane = 7534u
            }),
            new QuestObjectiveInfo(new QuestObjectiveEntry
            {
                Id                      = 4859u,
                Type                    = (uint)QuestObjectiveType.CollectItem,
                Data                    = 6998u,
                Count                   = 8u,
                Flags                   = 3u,
                TargetGroupIdRewardPane = 7534u
            })));

        return questInfo;
    }

    private static IQuestInfo CreateQ3479Q3480YetiKillQuestInfo(ushort questId, uint objectiveId)
    {
        IQuestInfo questInfo = RecordingDispatchProxy<IQuestInfo>.Create(out var questInfoProxy);
        questInfoProxy.SetProperty(nameof(IQuestInfo.Entry), new Quest2Entry
        {
            Id                     = questId,
            PushedItemIds          = new uint[6],
            PushedItemCounts       = new uint[6],
            QuestPlayerFactionEnum = 0u,
            PrerequisiteLevel      = 1u,
            QuestIdExclusionPreq0  = questId == 3479 ? 3480u : 3479u
        });
        questInfoProxy.SetProperty(nameof(IQuestInfo.PrerequisiteQuests), ImmutableList<Quest2Entry>.Empty);
        questInfoProxy.SetProperty(nameof(IQuestInfo.Objectives), ImmutableList.Create<IQuestObjectiveInfo>(
            new QuestObjectiveInfo(new QuestObjectiveEntry
            {
                Id    = objectiveId,
                Type  = (uint)QuestObjectiveType.KillTargetGroups,
                Data  = questId == 3479 ? 7288u : 1463u,
                Count = 8u,
                Flags = 4u
            })));

        return questInfo;
    }

    private static IQuestInfo CreateQ3479Q3480TrappedSurvivorQuestInfo(ushort questId, uint objectiveId)
    {
        IQuestInfo questInfo = RecordingDispatchProxy<IQuestInfo>.Create(out var questInfoProxy);
        questInfoProxy.SetProperty(nameof(IQuestInfo.Entry), new Quest2Entry
        {
            Id                     = questId,
            PushedItemIds          = new uint[6],
            PushedItemCounts       = new uint[6],
            QuestPlayerFactionEnum = 0u,
            PrerequisiteLevel      = 1u,
            QuestIdExclusionPreq0  = questId == 3479 ? 3480u : 3479u
        });
        questInfoProxy.SetProperty(nameof(IQuestInfo.PrerequisiteQuests), ImmutableList<Quest2Entry>.Empty);
        questInfoProxy.SetProperty(nameof(IQuestInfo.Objectives), ImmutableList.Create<IQuestObjectiveInfo>(
            new QuestObjectiveInfo(new QuestObjectiveEntry
            {
                Id    = objectiveId,
                Type  = (uint)QuestObjectiveType.SucceedCSI,
                Data  = 11070u,
                Count = 3u,
                Flags = 16u
            })));

        return questInfo;
    }

    private static IQuestInfo CreateQ3797SecuringTheAreaQuestInfo()
    {
        IQuestInfo questInfo = RecordingDispatchProxy<IQuestInfo>.Create(out var questInfoProxy);
        questInfoProxy.SetProperty(nameof(IQuestInfo.Entry), new Quest2Entry
        {
            Id = 3797u
        });
        questInfoProxy.SetProperty(nameof(IQuestInfo.Objectives), ImmutableList.Create<IQuestObjectiveInfo>(
            new QuestObjectiveInfo(new QuestObjectiveEntry
            {
                Id    = 4918u,
                Type  = (uint)QuestObjectiveType.KillTargetGroups,
                Data  = 1177u,
                Count = 8u,
                Flags = 4u
            })));

        return questInfo;
    }

    private static IQuestInfo CreateQ3668IndigenousIntelligenceQuestInfo()
    {
        IQuestInfo questInfo = RecordingDispatchProxy<IQuestInfo>.Create(out var questInfoProxy);
        questInfoProxy.SetProperty(nameof(IQuestInfo.Entry), new Quest2Entry
        {
            Id = 3668u
        });
        questInfoProxy.SetProperty(nameof(IQuestInfo.Objectives), ImmutableList.Create<IQuestObjectiveInfo>(
            new QuestObjectiveInfo(new QuestObjectiveEntry
            {
                Id    = 4791u,
                Type  = (uint)QuestObjectiveType.KillTargetGroup,
                Data  = 7293u,
                Count = 5u,
                Flags = 5u
            })));

        return questInfo;
    }

    private static IQuestInfo CreateQ9880DownInTheDregsContractQuestInfo(GameTableManager gameTableManager)
    {
        return new QuestInfo(CreateQ9880DownInTheDregsContractQuestEntry(), CreateGlobalQuestManager(), gameTableManager);
    }

    private static Quest2Entry CreateQ9880DownInTheDregsContractQuestEntry()
    {
        return CreateContractQuestEntry(9880, 18869u, 52u, 1u);
    }

    private static Quest2Entry CreateContractQuestEntry(ushort questId, uint objectiveId, uint periodicQuestGroupId, uint periodicQuestWeight)
    {
        return new Quest2Entry
        {
            Id                     = questId,
            Flags                  = 0xC0020u,
            ConLevel               = 50u,
            Type                   = 13u,
            PrerequisiteLevel      = 50u,
            PrerequisiteQuests     = [0u, 0u, 0u],
            QuestPlayerFactionEnum = 2u,
            WorldZoneId            = 12u,
            PushedItemIds          = new uint[6],
            PushedItemCounts       = new uint[6],
            Objectives             = [objectiveId, 0u, 0u, 0u, 0u, 0u],
            Quest2SubTypeId        = 122u,
            PeriodicQuestGroupId   = periodicQuestGroupId,
            PeriodicQuestWeight    = periodicQuestWeight,
            QuestRepeatPeriodEnum  = 1u
        };
    }

    private static QuestObjectiveEntry CreateKillContractObjectiveEntry(uint objectiveId, uint targetGroupId)
    {
        return CreateContractObjectiveEntry(objectiveId, QuestObjectiveType.KillTargetGroups, targetGroupId, flags: 1540u, count: 30u);
    }

    private static QuestObjectiveEntry CreateContractObjectiveEntry(
        uint objectiveId,
        QuestObjectiveType objectiveType,
        uint data,
        uint flags = 0u,
        uint count = 1u)
    {
        return new QuestObjectiveEntry
        {
            Id    = objectiveId,
            Type  = (uint)objectiveType,
            Flags = flags,
            Data  = data,
            Count = count
        };
    }

    private static Quest2Entry CreateCommunicatorQuestEntry(uint questId, uint objectiveId)
    {
        return new Quest2Entry
        {
            Id                     = questId,
            PrerequisiteLevel      = 1u,
            QuestPlayerFactionEnum = 0u,
            PushedItemIds          = new uint[6],
            PushedItemCounts       = new uint[6],
            Objectives             = [objectiveId, 0u, 0u, 0u, 0u, 0u]
        };
    }

    private static Quest2RewardEntry CreateItemQuestReward(uint rewardId, uint questId, uint itemId, uint objectAmount = 1u)
    {
        return new Quest2RewardEntry
        {
            Id                 = rewardId,
            Quest2Id           = questId,
            Quest2RewardTypeId = (uint)QuestRewardType.Item,
            ObjectId           = itemId,
            ObjectAmount       = objectAmount,
            Flags              = 0u
        };
    }

    private static PeriodicQuestGroupEntry CreateContractPeriodicQuestGroup(uint id, uint offered, uint maxAllowed)
    {
        return new PeriodicQuestGroupEntry
        {
            Id                       = id,
            PeriodicQuestSetId       = 37u,
            PeriodicQuestsOffered    = offered,
            MaxPeriodicQuestsAllowed = maxAllowed,
            Weight                   = 1u,
            ContractTypeEnum         = 2u,
            ContractQualityEnum      = 1u
        };
    }

    private static IQuestInfo CreateQ3781QuestAcceptInfo()
    {
        IQuestInfo questInfo = RecordingDispatchProxy<IQuestInfo>.Create(out RecordingDispatchProxy<IQuestInfo> questInfoProxy);
        questInfoProxy.SetProperty(nameof(IQuestInfo.Entry), new Quest2Entry
        {
            Id                    = 3781u,
            QuestPlayerFactionEnum = 0u,
            PrerequisiteLevel     = 2u,
            PushedItemIds         = new uint[6],
            PushedItemCounts      = new uint[6]
        });
        questInfoProxy.SetProperty(
            nameof(IQuestInfo.PrerequisiteQuests),
            ImmutableList<Quest2Entry>.Empty);
        questInfoProxy.SetProperty(
            nameof(IQuestInfo.Objectives),
            ImmutableList.Create<IQuestObjectiveInfo>(
                new QuestObjectiveInfo(new QuestObjectiveEntry
                {
                    Id                      = 4880u,
                    Type                    = (uint)QuestObjectiveType.ActivateEntity,
                    Data                    = 2143u,
                    Count                   = 3u,
                    Flags                   = 4u,
                    TargetGroupIdRewardPane = 2143u
                })));
        questInfoProxy.SetProperty(
            nameof(IQuestInfo.Rewards),
            ImmutableDictionary<uint, Quest2RewardEntry>.Empty);
        return questInfo;
    }

    private static IQuestInfo CreateQ3797QuestAcceptInfo()
    {
        IQuestInfo questInfo = RecordingDispatchProxy<IQuestInfo>.Create(out RecordingDispatchProxy<IQuestInfo> questInfoProxy);
        questInfoProxy.SetProperty(nameof(IQuestInfo.Entry), new Quest2Entry
        {
            Id                     = 3797u,
            QuestPlayerFactionEnum = 0u,
            PrerequisiteQuests     = [3486u, 0u, 0u],
            PushedItemIds          = new uint[6],
            PushedItemCounts       = new uint[6]
        });
        questInfoProxy.SetProperty(
            nameof(IQuestInfo.PrerequisiteQuests),
            ImmutableList.Create(new Quest2Entry { Id = 3486u }));
        questInfoProxy.SetProperty(
            nameof(IQuestInfo.Objectives),
            ImmutableList.Create<IQuestObjectiveInfo>(
                new QuestObjectiveInfo(new QuestObjectiveEntry
                {
                    Id    = 4918u,
                    Type  = (uint)QuestObjectiveType.KillTargetGroups,
                    Data  = 1177u,
                    Count = 8u,
                    Flags = 4u
                })));
        questInfoProxy.SetProperty(
            nameof(IQuestInfo.Rewards),
            ImmutableDictionary<uint, Quest2RewardEntry>.Empty);
        return questInfo;
    }

    private static IQuestInfo CreateQ5597QuestAcceptInfo()
    {
        IQuestInfo questInfo = RecordingDispatchProxy<IQuestInfo>.Create(out RecordingDispatchProxy<IQuestInfo> questInfoProxy);
        questInfoProxy.SetProperty(nameof(IQuestInfo.Entry), new Quest2Entry
        {
            Id                     = 5597u,
            QuestPlayerFactionEnum = 1u,
            PrerequisiteLevel      = 1u,
            PrerequisiteQuests     = [5596u, 0u, 0u],
            PushedItemIds          = new uint[6],
            PushedItemCounts       = new uint[6]
        });
        questInfoProxy.SetProperty(
            nameof(IQuestInfo.PrerequisiteQuests),
            ImmutableList.Create(new Quest2Entry { Id = 5596u }));
        questInfoProxy.SetProperty(
            nameof(IQuestInfo.Objectives),
            ImmutableList.Create<IQuestObjectiveInfo>(
                new QuestObjectiveInfo(new QuestObjectiveEntry
                {
                    Id                      = 8256u,
                    Type                    = (uint)QuestObjectiveType.VirtualCollect,
                    Data                    = 364u,
                    Count                   = 6u,
                    Flags                   = 4u,
                    TargetGroupIdRewardPane = 4373u
                })));
        questInfoProxy.SetProperty(
            nameof(IQuestInfo.Rewards),
            ImmutableDictionary<uint, Quest2RewardEntry>.Empty);
        return questInfo;
    }

    private static IQuestInfo CreateQ5573QuestAcceptInfo()
    {
        IQuestInfo questInfo = RecordingDispatchProxy<IQuestInfo>.Create(out RecordingDispatchProxy<IQuestInfo> questInfoProxy);
        questInfoProxy.SetProperty(nameof(IQuestInfo.Entry), new Quest2Entry
        {
            Id                     = 5573u,
            QuestPlayerFactionEnum = 1u,
            PrerequisiteLevel      = 3u,
            PrerequisiteQuests     = [5593u, 0u, 0u],
            PushedItemIds          = new uint[6],
            PushedItemCounts       = new uint[6]
        });
        questInfoProxy.SetProperty(
            nameof(IQuestInfo.PrerequisiteQuests),
            ImmutableList.Create(new Quest2Entry { Id = 5593u }));
        questInfoProxy.SetProperty(
            nameof(IQuestInfo.Objectives),
            ImmutableList.Create<IQuestObjectiveInfo>(
                new QuestObjectiveInfo(new QuestObjectiveEntry
                {
                    Id    = 8229u,
                    Type  = (uint)QuestObjectiveType.ActivateTargetGroupChecklist,
                    Data  = 2541u,
                    Count = 3u,
                    Flags = 4u
                })));
        questInfoProxy.SetProperty(
            nameof(IQuestInfo.Rewards),
            ImmutableDictionary<uint, Quest2RewardEntry>.Empty);
        return questInfo;
    }

    private static IQuestInfo CreateQ4696QuestAcceptInfo()
    {
        IQuestInfo questInfo = RecordingDispatchProxy<IQuestInfo>.Create(out RecordingDispatchProxy<IQuestInfo> questInfoProxy);
        questInfoProxy.SetProperty(nameof(IQuestInfo.Entry), new Quest2Entry
        {
            Id                    = 4696u,
            QuestPlayerFactionEnum = 0u,
            PrerequisiteLevel     = 16u,
            PrerequisiteQuests    = [4667u, 0u, 0u],
            PushedItemIds         = new uint[6],
            PushedItemCounts      = new uint[6]
        });
        questInfoProxy.SetProperty(
            nameof(IQuestInfo.PrerequisiteQuests),
            ImmutableList.Create(new Quest2Entry { Id = 4667u }));
        questInfoProxy.SetProperty(
            nameof(IQuestInfo.Objectives),
            ImmutableList.Create<IQuestObjectiveInfo>(
                new QuestObjectiveInfo(new QuestObjectiveEntry
                {
                    Id                      = 6508u,
                    Type                    = (uint)QuestObjectiveType.ActivateEntity,
                    Data                    = 0u,
                    Count                   = 5u,
                    TargetGroupIdRewardPane = 4323u
                })));
        questInfoProxy.SetProperty(
            nameof(IQuestInfo.Rewards),
            ImmutableDictionary<uint, Quest2RewardEntry>.Empty);
        return questInfo;
    }

    private static IQuestInfo CreateNoObjectiveQuestInfo(ushort questId, params Quest2RewardEntry[] rewards)
    {
        IQuestInfo questInfo = RecordingDispatchProxy<IQuestInfo>.Create(out RecordingDispatchProxy<IQuestInfo> questInfoProxy);
        questInfoProxy.SetProperty(nameof(IQuestInfo.Entry), new Quest2Entry
        {
            Id               = questId,
            PushedItemIds    = new uint[6],
            PushedItemCounts = new uint[6]
        });
        questInfoProxy.SetProperty(
            nameof(IQuestInfo.Objectives),
            ImmutableList<IQuestObjectiveInfo>.Empty);
        questInfoProxy.SetProperty(
            nameof(IQuestInfo.Rewards),
            rewards.ToImmutableDictionary(r => r.Id));
        return questInfo;
    }

    private static IQuestInfo CreateQuestInfo(ushort questId, params Quest2RewardEntry[] rewards)
    {
        return CreateQuestInfoCore(questId, null, null, null, null, rewards);
    }

    private static IQuestInfo CreateQuestInfo(ushort questId, uint rewardMoney, params Quest2RewardEntry[] rewards)
    {
        return CreateQuestInfoCore(questId, null, null, rewardMoney, null, rewards);
    }

    private static IQuestInfo CreateQuestInfo(ushort questId, Action<Quest2Entry> configureEntry, float rewardReputation, params Quest2RewardEntry[] rewards)
    {
        return CreateQuestInfoCore(questId, configureEntry, rewardReputation, null, null, rewards);
    }

    private static IQuestInfo CreateQuestInfoWithRewardExperience(ushort questId, uint rewardExperience, params Quest2RewardEntry[] rewards)
    {
        return CreateQuestInfoCore(questId, null, null, null, rewardExperience, rewards);
    }

    private static IQuestInfo CreateQuestInfoCore(ushort questId, Action<Quest2Entry> configureEntry, float? rewardReputation, uint? rewardMoney, uint? rewardExperience, params Quest2RewardEntry[] rewards)
    {
        IQuestInfo questInfo = RecordingDispatchProxy<IQuestInfo>.Create(out RecordingDispatchProxy<IQuestInfo> questInfoProxy);
        var entry = new Quest2Entry
        {
            Id               = questId,
            PushedItemIds    = new uint[6],
            PushedItemCounts = new uint[6]
        };
        configureEntry?.Invoke(entry);
        questInfoProxy.SetProperty(nameof(IQuestInfo.Entry), entry);
        questInfoProxy.SetProperty(
            nameof(IQuestInfo.Rewards),
        rewards.ToImmutableDictionary(r => r.Id));
        if (rewardReputation.HasValue)
            questInfoProxy.SetMethodReturn(nameof(IQuestInfo.GetRewardReputation), rewardReputation.Value);
        if (rewardMoney.HasValue)
            questInfoProxy.SetMethodReturn(nameof(IQuestInfo.GetRewardMoney), rewardMoney.Value);
        if (rewardExperience.HasValue)
            questInfoProxy.SetMethodReturn(nameof(IQuestInfo.GetRewardExperience), rewardExperience.Value);
        return questInfo;
    }

    private static IQuest CreateQuest(ushort questId, IQuestInfo questInfo, QuestState state, uint currentObjectiveId = 0u, DateTime? reset = null)
    {
        IQuest quest = RecordingDispatchProxy<IQuest>.Create(out RecordingDispatchProxy<IQuest> questProxy);
        questProxy.SetProperty(nameof(IQuest.Id), questId);
        questProxy.SetProperty(nameof(IQuest.Info), questInfo);
        questProxy.SetProperty(nameof(IQuest.State), state);
        questProxy.SetProperty(nameof(IQuest.Flags), QuestStateFlags.Tracked);
        questProxy.SetProperty(nameof(IQuest.Reset), reset);
        questProxy.SetMethodHandler("GetEnumerator", _ => Enumerable.Empty<IQuestObjective>().GetEnumerator());
        questProxy.SetMethodReturn(nameof(IQuest.GetCurrentObjectiveId), currentObjectiveId);
        return quest;
    }

    private static QuestManager CreateQuestManager(
        IPlayer player,
        IGlobalQuestManager globalQuestManager = null,
        IGameTableManager gameTableManager = null)
    {
        return new QuestManager(
            player,
            new CharacterModel(),
            globalQuestManager: globalQuestManager ?? CreateGlobalQuestManager(),
            scriptManager: CreateScriptManager(),
            gameTableManager: gameTableManager);
    }

    private static IScriptManager CreateScriptManager()
    {
        IScriptCollection scriptCollection = RecordingDispatchProxy<IScriptCollection>.Create(out _);
        IScriptManager scriptManager = RecordingDispatchProxy<IScriptManager>.Create(out var scriptManagerProxy);
        scriptManagerProxy.SetMethodReturn(nameof(IScriptManager.InitialiseOwnedScripts), scriptCollection);
        return scriptManager;
    }

    private static GameTableManager BuildGameTableManager(
        GameTable<Quest2Entry> quest2Table = null,
        GameTable<QuestDirectionEntry> questDirectionTable = null,
        GameTable<QuestDirectionEntryEntry> questDirectionEntryTable = null,
        GameTable<QuestObjectiveEntry> questObjectiveTable = null,
        GameTable<Quest2RewardEntry> quest2RewardTable = null,
        GameTable<Item2Entry> itemTable = null,
        GameTable<PeriodicQuestGroupEntry> periodicQuestGroupTable = null,
        GameTable<CommunicatorMessagesEntry> communicatorMessagesTable = null,
        GameTable<WorldZoneEntry> worldZoneTable = null,
        GameTable<AccountCurrencyTypeEntry> accountCurrencyTypeTable = null)
    {
        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));

        if (quest2Table != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.Quest2), quest2Table);
        if (questDirectionTable != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.QuestDirection), questDirectionTable);
        if (questDirectionEntryTable != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.QuestDirectionEntry), questDirectionEntryTable);
        if (questObjectiveTable != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.QuestObjective), questObjectiveTable);
        if (quest2RewardTable != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.Quest2Reward), quest2RewardTable);
        if (itemTable != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.Item), itemTable);
        if (periodicQuestGroupTable != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.PeriodicQuestGroup), periodicQuestGroupTable);
        if (communicatorMessagesTable != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.CommunicatorMessages), communicatorMessagesTable);
        if (worldZoneTable != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.WorldZone), worldZoneTable);
        if (accountCurrencyTypeTable != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.AccountCurrencyType), accountCurrencyTypeTable);

        return gameTableManager;
    }

    private static GlobalQuestManager CreateGlobalQuestManager(
        IReadOnlyDictionary<ushort, IQuestInfo> questInfos = null,
        IReadOnlyDictionary<ushort, ImmutableList<uint>> questReceivers = null,
        IReadOnlyDictionary<ushort, ImmutableList<uint>> questGivers = null)
    {
        var globalQuestManager = new GlobalQuestManager();
        SetPrivateField(
            globalQuestManager,
            "questInfoStore",
            questInfos?.ToImmutableDictionary() ?? ImmutableDictionary<ushort, IQuestInfo>.Empty);
        SetPrivateField(
            globalQuestManager,
            "questGiverStore",
            questGivers?.ToImmutableDictionary() ?? ImmutableDictionary<ushort, ImmutableList<uint>>.Empty);
        SetPrivateField(
            globalQuestManager,
            "questReceiverStore",
            questReceivers?.ToImmutableDictionary() ?? ImmutableDictionary<ushort, ImmutableList<uint>>.Empty);
        SetPrivateField(
            globalQuestManager,
            "communicatorStore",
            ImmutableDictionary<uint, ICommunicatorMessage>.Empty);
        SetPrivateField(
            globalQuestManager,
            "communicatorQuestStore",
            ImmutableDictionary<ushort, ImmutableList<ICommunicatorMessage>>.Empty);
        SetPrivateField(
            globalQuestManager,
            "communicatorQuestStateTriggerStore",
            ImmutableDictionary<(ushort, QuestState), ImmutableList<ICommunicatorMessage>>.Empty);

        return globalQuestManager;
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

    private static ICharacterAchievementManager CreateQ3486ArrivalAchievementManager(IPlayer player)
    {
        var globalAchievementManager = new TestGlobalAchievementManager(
            new TestAchievementInfo(
                new AchievementEntry
                {
                    Id                = 3469u,
                    AchievementTypeId = (uint)AchievementType.QuestCompleteChecklist
                },
                new AchievementChecklistEntry { Id = 4245u, AchievementId = 3469u, Bit = 0u, ObjectId = 3486u },
                new AchievementChecklistEntry { Id = 4246u, AchievementId = 3469u, Bit = 1u, ObjectId = 3667u }),
            new TestAchievementInfo(
                new AchievementEntry
                {
                    Id                = 5327u,
                    AchievementTypeId = (uint)AchievementType.QuestCompleteChecklist
                },
                new AchievementChecklistEntry { Id = 6907u, AchievementId = 5327u, Bit = 0u, ObjectId = 3486u },
                new AchievementChecklistEntry { Id = 6908u, AchievementId = 5327u, Bit = 1u, ObjectId = 3667u },
                new AchievementChecklistEntry { Id = 6910u, AchievementId = 5327u, Bit = 2u, ObjectId = 3480u }));

        return new CharacterAchievementManager(player, new CharacterModel
        {
            Id = player.CharacterId
        }, globalAchievementManager: globalAchievementManager);
    }

    private static ICharacterAchievementManager CreateQ5597BloodstoneAchievementManager(IPlayer player)
    {
        var globalAchievementManager = new TestGlobalAchievementManager(
            new TestAchievementInfo(
                new AchievementEntry
                {
                    Id                     = 4134u,
                    AchievementTypeId      = (uint)AchievementType.QuestCompleteChecklist,
                    PrerequisiteId        = 18u,
                    PrerequisiteIdServer  = 18u
                },
                new AchievementChecklistEntry
                {
                    Id            = 5495u,
                    AchievementId = 4134u,
                    Bit           = 1u,
                    ObjectId      = 5597u
                }));

        IPrerequisiteManager prerequisiteManager = RecordingDispatchProxy<IPrerequisiteManager>.Create(
            out RecordingDispatchProxy<IPrerequisiteManager> prerequisiteProxy);
        prerequisiteProxy.SetMethodHandler(nameof(IPrerequisiteManager.Meets), args => (uint)args[1] == 18u);

        return new CharacterAchievementManager(player, new CharacterModel
        {
            Id = player.CharacterId
        }, globalAchievementManager: globalAchievementManager, prerequisiteManager: prerequisiteManager);
    }

    private static IAchievement GetAchievement(ICharacterAchievementManager manager, ushort achievementId)
    {
        FieldInfo field = typeof(BaseAchievementManager<CharacterAchievementModel>)
            .GetField("achievements", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);

        var achievements = Assert.IsType<Dictionary<ushort, IAchievement>>(field.GetValue(manager));
        return achievements[achievementId];
    }

    private sealed class TestGlobalAchievementManager : IGlobalAchievementManager
    {
        private readonly Dictionary<ushort, IAchievementInfo> achievements;

        public TestGlobalAchievementManager(params IAchievementInfo[] achievements)
        {
            this.achievements = achievements.ToDictionary(a => a.Id);
        }

        public void Initialise()
        {
        }

        public IAchievementInfo GetAchievement(ushort id)
        {
            return achievements.TryGetValue(id, out IAchievementInfo achievement) ? achievement : null;
        }

        public IEnumerable<IAchievementInfo> GetCharacterAchievements(AchievementType type)
        {
            return achievements.Values.Where(achievement =>
                achievement.IsPlayerAchievement &&
                (AchievementType)achievement.Entry.AchievementTypeId == type);
        }

        public IEnumerable<IAchievementInfo> GetGuildAchievements(AchievementType type)
        {
            return Enumerable.Empty<IAchievementInfo>();
        }

        public bool TryClaimRealmFirstAchievement(IAchievementInfo info, bool isGuildAchievement)
        {
            return false;
        }
    }

    private sealed class TestAchievementInfo : IAchievementInfo
    {
        public TestAchievementInfo(AchievementEntry entry, params AchievementChecklistEntry[] checklistEntries)
        {
            Entry            = entry;
            ChecklistEntries = checklistEntries.ToList();
        }

        public ushort Id => (ushort)Entry.Id;
        public AchievementEntry Entry { get; }
        public List<AchievementChecklistEntry> ChecklistEntries { get; }
        public bool IsPlayerAchievement => true;
        public bool IsRealmFirst => false;
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
