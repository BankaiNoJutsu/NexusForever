using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Quest;

public class GlobalQuestManagerPartialTableTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Initialise_WithMissingPrimaryTablesUsesEmptyCaches(bool includeEmptyTables)
    {
        var manager = CreateManager(gameTableManager =>
        {
            if (!includeEmptyTables)
                return;

            SetTable(gameTableManager, nameof(GameTableManager.Quest2), CreateGameTable<Quest2Entry>());
            SetTable(gameTableManager, nameof(GameTableManager.Creature2), CreateGameTable<Creature2Entry>());
            SetTable(gameTableManager, nameof(GameTableManager.CommunicatorMessages), CreateGameTable<CommunicatorMessagesEntry>());
        });

        manager.Initialise();

        Assert.Null(manager.GetQuestInfo(42));
        Assert.Empty(manager.GetQuestGivers(42));
        Assert.Empty(manager.GetQuestReceivers(42));
        Assert.Null(manager.GetCommunicatorMessage(200u));
        Assert.Empty(manager.GetQuestCommunicatorMessages(42));
        Assert.Empty(manager.GetQuestCommunicatorQuestStateTriggers(42, QuestState.Accepted));
    }

    [Fact]
    public void Initialise_WithTableBackedRowsCachesQuestRelationsAndCommunicators()
    {
        var manager = CreateManager(gameTableManager =>
        {
            SetTable(gameTableManager, nameof(GameTableManager.Quest2), CreateGameTable(new Quest2Entry
            {
                Id = 42u
            }));
            SetTable(gameTableManager, nameof(GameTableManager.Creature2), CreateGameTable(
                new Creature2Entry
                {
                    Id             = 100u,
                    QuestIdGiven   = QuestIds(42u),
                    QuestIdReceive = QuestIds(43u)
                },
                new Creature2Entry
                {
                    Id             = 101u,
                    QuestIdGiven   = QuestIds(42u, 44u),
                    QuestIdReceive = QuestIds()
                }));
            SetTable(gameTableManager, nameof(GameTableManager.CommunicatorMessages), CreateGameTable(
                new CommunicatorMessagesEntry
                {
                    Id               = 200u,
                    QuestIdDelivered = 42u,
                    Quests           = CommunicatorValues(),
                    States           = CommunicatorValues()
                },
                new CommunicatorMessagesEntry
                {
                    Id     = 201u,
                    Quests = CommunicatorValues(42u),
                    States = CommunicatorValues((uint)QuestState.Accepted)
                },
                new CommunicatorMessagesEntry
                {
                    Id               = 202u,
                    QuestIdDelivered = 44u,
                    Quests           = CommunicatorValues(44u),
                    States           = CommunicatorValues((uint)QuestState.Accepted)
                },
                new CommunicatorMessagesEntry
                {
                    Id     = 203u,
                    Quests = CommunicatorValues(45u),
                    States = CommunicatorValues(11u)
                }));
        });

        manager.Initialise();

        Assert.NotNull(manager.GetQuestInfo(42));
        Assert.Equal([100u, 101u], manager.GetQuestGivers(42));
        Assert.Equal([100u], manager.GetQuestReceivers(43));
        Assert.Equal(200u, manager.GetCommunicatorMessage(200u).Id);
        Assert.Equal([200u], manager.GetQuestCommunicatorMessages(42).Select(c => c.Id));
        Assert.Equal([201u], manager.GetQuestCommunicatorQuestStateTriggers(42, QuestState.Accepted).Select(c => c.Id));
        Assert.Empty(manager.GetQuestCommunicatorQuestStateTriggers(44, QuestState.Accepted));
        Assert.Empty(Enum.GetValues<QuestState>()
            .SelectMany(s => manager.GetQuestCommunicatorQuestStateTriggers(45, s)));
    }

    [Fact]
    public void Initialise_Q4696GalerasDurekIndexesClientStarterCreature()
    {
        var manager = CreateManager(gameTableManager =>
        {
            SetTable(gameTableManager, nameof(GameTableManager.Creature2), CreateGameTable(
                new Creature2Entry
                {
                    Id             = 11061u,
                    QuestIdGiven   = QuestIds(3480u),
                    QuestIdReceive = QuestIds()
                },
                new Creature2Entry
                {
                    Id             = 17175u,
                    QuestIdGiven   = QuestIds(4696u),
                    QuestIdReceive = QuestIds()
                }));
        });

        manager.Initialise();

        Assert.Equal([17175u], manager.GetQuestGivers(4696));
        Assert.DoesNotContain(11061u, manager.GetQuestGivers(4696));
    }

    [Fact]
    public void Initialise_Q4696GalerasDarbyIndexesClientFinisherCreature()
    {
        var manager = CreateManager(gameTableManager =>
        {
            SetTable(gameTableManager, nameof(GameTableManager.Creature2), CreateGameTable(
                new Creature2Entry
                {
                    Id             = 11061u,
                    QuestIdGiven   = QuestIds(3480u),
                    QuestIdReceive = QuestIds()
                },
                new Creature2Entry
                {
                    Id             = 17053u,
                    QuestIdGiven   = QuestIds(4666u),
                    QuestIdReceive = QuestIds(4696u)
                },
                new Creature2Entry
                {
                    Id             = 17175u,
                    QuestIdGiven   = QuestIds(4696u),
                    QuestIdReceive = QuestIds()
                }));
        });

        manager.Initialise();

        Assert.Equal([17053u], manager.GetQuestReceivers(4696));
        Assert.DoesNotContain(17175u, manager.GetQuestReceivers(4696));
        Assert.DoesNotContain(11061u, manager.GetQuestReceivers(4696));
    }

    [Fact]
    public void Initialise_Q3797IndexesReviewedDurekGiverAndReceiverOverrides()
    {
        var manager = CreateManager(gameTableManager =>
        {
            SetTable(gameTableManager, nameof(GameTableManager.Creature2), CreateGameTable(
                new Creature2Entry
                {
                    Id             = 11061u,
                    QuestIdGiven   = QuestIds(3480u),
                    QuestIdReceive = QuestIds(9071u, 9112u)
                }));
        });

        manager.Initialise();

        Assert.Equal([11061u], manager.GetQuestGivers(3480));
        Assert.Equal([11061u], manager.GetQuestGivers(3797));
        Assert.Equal([11061u], manager.GetQuestReceivers(3797));
        Assert.Equal([11061u], manager.GetQuestReceivers(9071));
        Assert.Equal([11061u], manager.GetQuestReceivers(9112));
    }

    [Fact]
    public void Initialise_LandsReachDurekIndexesBuild16042QuestRoutes()
    {
        var manager = CreateManager(gameTableManager =>
        {
            SetTable(gameTableManager, nameof(GameTableManager.Creature2), CreateGameTable(
                new Creature2Entry
                {
                    Id             = 11066u,
                    QuestIdGiven   = QuestIds(3797u, 3886u, 3741u),
                    QuestIdReceive = QuestIds(3783u, 3797u, 3895u, 3741u, 3886u)
                }));
        });

        manager.Initialise();

        Assert.Equal([11066u], manager.GetQuestGivers(3741));
        Assert.Equal([11066u], manager.GetQuestGivers(3797));
        Assert.Equal([11066u], manager.GetQuestGivers(3886));
        Assert.Equal([11066u], manager.GetQuestReceivers(3783));
        Assert.Equal([11066u], manager.GetQuestReceivers(3797));
        Assert.Equal([11066u], manager.GetQuestReceivers(3895));
        Assert.Equal([11066u], manager.GetQuestReceivers(3741));
        Assert.Equal([11066u], manager.GetQuestReceivers(3886));
    }

    [Fact]
    public void Initialise_Q3670IndexesReviewedDeadeyeReceiverOverrides()
    {
        var manager = CreateManager(gameTableManager =>
        {
            SetTable(gameTableManager, nameof(GameTableManager.Creature2), CreateGameTable(
                new Creature2Entry
                {
                    Id             = 11063u,
                    QuestIdGiven   = QuestIds(3670u),
                    QuestIdReceive = QuestIds(3673u)
                },
                new Creature2Entry
                {
                    Id             = 16622u,
                    QuestIdGiven   = QuestIds(4969u, 3770u, 3780u, 3782u),
                    QuestIdReceive = QuestIds(3766u, 3780u, 4540u, 3963u)
                }));
        });

        manager.Initialise();

        Assert.Equal([11063u], manager.GetQuestGivers(3670));
        Assert.Equal([11063u, 16622u], manager.GetQuestReceivers(3670));
        Assert.Equal([11063u], manager.GetQuestReceivers(3673));
        Assert.DoesNotContain(16622u, manager.GetQuestGivers(3670));
        Assert.DoesNotContain(16622u, manager.GetQuestReceivers(3673));
    }

    [Fact]
    public void Initialise_Q3963IndexesClientDeadeyeReceiver()
    {
        var manager = CreateManager(gameTableManager =>
        {
            SetTable(gameTableManager, nameof(GameTableManager.Creature2), CreateGameTable(
                new Creature2Entry
                {
                    Id             = 11063u,
                    QuestIdGiven   = QuestIds(3670u, 3667u, 3671u, 3668u, 3673u),
                    QuestIdReceive = QuestIds(3673u, 3479u, 3671u, 3480u)
                },
                new Creature2Entry
                {
                    Id             = 16622u,
                    QuestIdGiven   = QuestIds(4969u, 3770u, 3780u, 3782u),
                    QuestIdReceive = QuestIds(3766u, 3780u, 4540u, 3963u)
                }));
        });

        manager.Initialise();

        Assert.Equal([16622u], manager.GetQuestReceivers(3963));
        Assert.Empty(manager.GetQuestGivers(3963));
        Assert.DoesNotContain(11063u, manager.GetQuestReceivers(3963));
    }

    [Fact]
    public void Initialise_Q3671IndexesCampIcefuryDeadeyeReceiverOnly()
    {
        var manager = CreateManager(gameTableManager =>
        {
            SetTable(gameTableManager, nameof(GameTableManager.Creature2), CreateGameTable(
                new Creature2Entry
                {
                    Id             = 11063u,
                    QuestIdGiven   = QuestIds(3671u),
                    QuestIdReceive = QuestIds(3671u)
                },
                new Creature2Entry
                {
                    Id             = 12959u,
                    QuestIdGiven   = QuestIds(3487u, 3963u),
                    QuestIdReceive = QuestIds(3670u, 3673u, 3479u, 3671u, 3480u, 3781u, 3487u)
                }));
        });

        manager.Initialise();

        Assert.Equal([12959u], manager.GetQuestReceivers(3671));
        Assert.DoesNotContain(11063u, manager.GetQuestReceivers(3671));
        Assert.Equal([11063u], manager.GetQuestGivers(3671));
    }

    [Fact]
    public void Initialise_Q3479IndexesBosunStarterAndQ3479Q3480IndexesClientDeadeyeReceiver()
    {
        var manager = CreateManager(gameTableManager =>
        {
            SetTable(gameTableManager, nameof(GameTableManager.Creature2), CreateGameTable(
                new Creature2Entry
                {
                    Id             = 11062u,
                    QuestIdGiven   = QuestIds(3479u),
                    QuestIdReceive = QuestIds()
                },
                new Creature2Entry
                {
                    Id             = 11063u,
                    QuestIdGiven   = QuestIds(3670u, 3667u, 3671u, 3668u, 3673u),
                    QuestIdReceive = QuestIds(3673u, 3479u, 3671u, 3480u)
                }));
        });

        manager.Initialise();

        Assert.Equal([11062u], manager.GetQuestGivers(3479));
        Assert.Equal([11063u], manager.GetQuestReceivers(3479));
        Assert.Equal([11063u], manager.GetQuestReceivers(3480));
    }

    [Fact]
    public void Initialise_Q3781IndexesReviewedStarterAndReceiverOverrides()
    {
        var manager = CreateManager(gameTableManager =>
        {
            SetTable(gameTableManager, nameof(GameTableManager.Creature2), CreateGameTable(
                new Creature2Entry
                {
                    Id             = 50668u,
                    QuestIdGiven   = QuestIds(),
                    QuestIdReceive = QuestIds()
                },
                new Creature2Entry
                {
                    Id             = 16622u,
                    QuestIdGiven   = QuestIds(4969u, 3770u, 3780u, 3782u),
                    QuestIdReceive = QuestIds(3766u, 3780u, 4540u, 3963u)
                }));
        });

        manager.Initialise();

        Assert.Equal([50668u], manager.GetQuestGivers(3781));
        Assert.Equal([16622u], manager.GetQuestReceivers(3781));
        Assert.DoesNotContain(16622u, manager.GetQuestGivers(3781));
        Assert.DoesNotContain(50668u, manager.GetQuestReceivers(3781));
    }

    [Fact]
    public void Initialise_CrimsonIsleKezrekIndexesBuild16042DirectAndReviewedReceiverRoutes()
    {
        var manager = CreateManager(gameTableManager =>
        {
            SetTable(gameTableManager, nameof(GameTableManager.Creature2), CreateGameTable(
                new Creature2Entry
                {
                    Id             = 24158u,
                    QuestIdGiven   = QuestIds(5575u, 5594u, 7036u, 8856u),
                    QuestIdReceive = QuestIds(5575u, 5594u, 5595u, 8856u)
                }));
        });

        manager.Initialise();

        Assert.Empty(manager.GetQuestGivers(5580));
        Assert.Empty(manager.GetQuestGivers(5583));
        Assert.Equal([24158u], manager.GetQuestGivers(5575));
        Assert.Equal([24158u], manager.GetQuestReceivers(5575));
        Assert.Equal([24158u], manager.GetQuestGivers(5594));
        Assert.Equal([24158u], manager.GetQuestReceivers(5580));
        Assert.Equal([24158u], manager.GetQuestReceivers(5583));
        Assert.Equal([24158u], manager.GetQuestReceivers(5594));
    }

    [Fact]
    public void Initialise_CrimsonIsleMondoIndexesBuild16042Q5573AndQ5597Routes()
    {
        var manager = CreateManager(gameTableManager =>
        {
            SetTable(gameTableManager, nameof(GameTableManager.Creature2), CreateGameTable(
                new Creature2Entry
                {
                    Id             = 24187u,
                    QuestIdGiven   = QuestIds(5573u, 5604u, 5596u, 5597u, 5814u, 5593u, 8855u),
                    QuestIdReceive = QuestIds(5573u, 5609u, 5814u, 5596u, 5597u, 5604u, 5610u, 7036u, 5593u, 8855u, 5582u, 9127u, 9131u, 9132u)
                }));
        });

        manager.Initialise();

        Assert.Equal([24187u], manager.GetQuestGivers(5573));
        Assert.Equal([24187u], manager.GetQuestReceivers(5573));
        Assert.Equal([24187u], manager.GetQuestGivers(5597));
        Assert.Equal([24187u], manager.GetQuestReceivers(5597));
        Assert.Equal([24187u], manager.GetQuestReceivers(5610));
    }

    private static uint[] QuestIds(params uint[] questIds)
    {
        var values = new uint[25];
        Array.Copy(questIds, values, questIds.Length);
        return values;
    }

    private static uint[] CommunicatorValues(params uint[] values)
    {
        var result = new uint[3];
        Array.Copy(values, result, values.Length);
        return result;
    }

    private static GlobalQuestManager CreateManager(Action<GameTableManager> configure)
    {
        var gameTableManager = (GameTableManager)RuntimeHelpers.GetUninitializedObject(typeof(GameTableManager));
        configure(gameTableManager);

        return new GlobalQuestManager(gameTableManager: gameTableManager);
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        SetPrivateField(table, "header", new GameTableHeader
        {
            MaxId = entries.Length == 0 ? 0ul : entries.Max(GetEntryId) + 1ul
        });
        SetPrivateField(table, "lookup", BuildLookup(entries));
        return table;
    }

    private static int[] BuildLookup<T>(T[] entries) where T : class, new()
    {
        if (entries.Length == 0)
            return [];

        int[] lookup = Enumerable.Repeat(-1, (int)(entries.Max(GetEntryId) + 1ul)).ToArray();
        for (int i = 0; i < entries.Length; i++)
            lookup[(int)GetEntryId(entries[i])] = i;

        return lookup;
    }

    private static ulong GetEntryId<T>(T entry)
    {
        FieldInfo idField = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public)[0];
        return Convert.ToUInt64(idField.GetValue(entry));
    }

    private static void SetTable<T>(GameTableManager gameTableManager, string propertyName, GameTable<T> table) where T : class, new()
    {
        SetAutoProperty(gameTableManager, propertyName, table);
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        backingField.SetValue(instance, value);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(instance, value);
    }
}
