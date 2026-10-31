using System.Buffers.Binary;
using NexusForever.Game.Static.Quest;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Network;

public sealed class LoadHarnessContractTests
{
    private static byte[] Encode(IWritable message)
    {
        using var stream = new MemoryStream(); using var writer = new GamePacketWriter(stream);
        message.Write(writer); writer.FlushBits(); return stream.ToArray();
    }

    [Fact]
    public void ReceiverlessAcceptanceRoundTripsThroughTheExistingClientReader()
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream)) { ContractProtocol.WriteAccept(writer, 9880); writer.FlushBits(); }
        using var reader = new GamePacketReader(new MemoryStream(stream.ToArray())); var packet = new ClientQuestAccept(); packet.Read(reader);
        Assert.Equal(9880, packet.QuestId); Assert.Equal(300, (int)packet.ItemLocation.Location); Assert.Equal(0u, packet.ItemLocation.BagIndex);
    }

    [Theory]
    [InlineData(0)] [InlineData(32768)] [InlineData(65535)]
    public void AcceptanceRejectsIdsThatWouldBeTruncatedOnTheWire(ushort quest)
    {
        using var stream = new MemoryStream(); using var writer = new GamePacketWriter(stream);
        Assert.Throws<ArgumentOutOfRangeException>(() => ContractProtocol.WriteAccept(writer, quest)); Assert.Empty(stream.ToArray());
    }

    [Fact]
    public void InitConsumesAllMappedSectionsAndUnalignedObjectiveArrays()
    {
        var bytes = Encode(new ServerQuestInit
        {
            Completed = [new() { QuestId = 13, CompletedToday = true }, new() { QuestId = 14 }],
            Inactive = [new() { QuestId = 15, State = QuestState.Mentioned }],
            Active = [new() { QuestId = 9880, State = QuestState.Accepted, QuestObjectiveId = 77, Flags = (QuestStateFlags)3,
                Objectives = [new() { Progress = 2, TimeElapsed = 5 }, new() { Progress = 3, TimeElapsed = 6 }], QuestTimeElapsed = 11 },
                new() { QuestId = 9881, State = QuestState.Achieved }], DailyRandomSeed = ulong.MaxValue
        });
        var rows = ContractProtocol.ReadInit(bytes); Assert.Equal(5, rows.Count);
        Assert.Equal(QuestState.Completed, rows[13]); Assert.Equal(QuestState.Mentioned, rows[15]);
        Assert.Equal(QuestState.Accepted, rows[9880]); Assert.Equal(QuestState.Achieved, rows[9881]);
        Assert.ThrowsAny<Exception>(() => ContractProtocol.ReadInit(bytes[..^2]));
    }

    [Fact]
    public void InitRejectsDuplicateIdentitiesAcrossSectionsAndUnboundedCollections()
    {
        Assert.Throws<InvalidDataException>(() => ContractProtocol.ReadInit(Encode(new ServerQuestInit
        { Completed = [new() { QuestId = 13 }], Active = [new() { QuestId = 13, State = QuestState.Accepted }] })));
        byte[] huge = new byte[4]; BinaryPrimitives.WriteUInt32LittleEndian(huge, uint.MaxValue);
        Assert.Throws<InvalidDataException>(() => ContractProtocol.ReadInit(huge));
    }

    [Fact]
    public void ObserverReplacesInitialStatesAndRecordsOnlyMappedStateChanges()
    {
        var observer = new ContractSocketObserver();
        observer.Receive(new(GameMessageOpcode.ServerQuestInit, Encode(new ServerQuestInit { Inactive = [new() { QuestId = 9880, State = QuestState.Mentioned }] })));
        Assert.Equal(QuestState.Mentioned, observer.State(9880)); Assert.Equal(0, observer.Changes);
        observer.Receive(new(GameMessageOpcode.ServerQuestStateChange, Encode(new ServerQuestStateChange { QuestId = 9880, QuestState = QuestState.Accepted, QuestObjectiveId = 123 })));
        Assert.Equal(QuestState.Accepted, observer.State(9880)); Assert.Equal(1, observer.Changes);
        observer.Receive(new(GameMessageOpcode.ServerQuestInit, Encode(new ServerQuestInit()))); Assert.Null(observer.State(9880));
    }

    [Theory]
    [InlineData(0, 1)] [InlineData(9880, 99)]
    public void InvalidServerIdentitiesAndStatesAreRejected(ushort id, int state)
        => Assert.Throws<InvalidDataException>(() => ContractProtocol.ReadChange(Encode(new ServerQuestStateChange { QuestId = id, QuestState = (QuestState)state })));

    [Theory]
    [InlineData(null, false)] [InlineData("", false)] [InlineData("00000000000000000000000000000000", false)]
    [InlineData("123456781234123412341234567890ab", true)] [InlineData("123456781234123412341234567890AB", false)]
    [InlineData("12345678-1234-1234-1234-1234567890ab", false)]
    public void RelogVerificationRequiresCanonicalNonemptyAcceptanceIdentities(string value, bool valid)
        => Assert.Equal(valid, ContractAcceptanceCommand.ValidAcceptance(value));

    private static LoginWorldOptions Options() => new()
    {
        User = "loadtest9701@example.local", StsHost = "127.0.0.1", AuthHost = "localhost", WorldHost = "127.0.0.1",
        AuthConnectionString = "server=127.0.0.1;database=nexus_assessment_20261029_auth",
        CharacterConnectionString = "server=127.0.0.1;database=nexus_assessment_20261029_character"
    };

    [Fact]
    public void OnlyAnExplicitOwnedLoopbackFixtureCanIssueAcceptance()
    {
        var options = Options(); ContractAcceptanceCommand.ValidateBoundary(options, 9880, true);
        Assert.Throws<InvalidOperationException>(() => ContractAcceptanceCommand.ValidateBoundary(options, 9880, false));
        Assert.Throws<InvalidOperationException>(() => ContractAcceptanceCommand.ValidateBoundary(options, 32768, true));
        Assert.Throws<InvalidOperationException>(() => ContractAcceptanceCommand.ValidateBoundary(options, 0, true));
    }

    [Theory]
    [InlineData("user")] [InlineData("endpoint")] [InlineData("remote-sql")] [InlineData("schema")] [InlineData("port")]
    public void BoundaryRejectsForeignAccountsEndpointsAndDatabases(string change)
    {
        var normal = Options();
        var options = new LoginWorldOptions
        {
            User = change == "user" ? "real@example.local" : normal.User,
            StsHost = normal.StsHost, AuthHost = normal.AuthHost, WorldHost = change == "endpoint" ? "example.com" : normal.WorldHost,
            AuthConnectionString = change == "remote-sql" ? "server=example.com;database=nexus_assessment_20261029_auth" : normal.AuthConnectionString,
            CharacterConnectionString = change switch
            {
                "schema" => "server=127.0.0.1;database=nexus_forever_character",
                "port" => "server=127.0.0.1;port=3307;database=nexus_assessment_20261029_character",
                _ => normal.CharacterConnectionString
            }
        };
        Assert.Throws<InvalidOperationException>(() => ContractAcceptanceCommand.ValidateBoundary(options, 9880, true));
    }
}
