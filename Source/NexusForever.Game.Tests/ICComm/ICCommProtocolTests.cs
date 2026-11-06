using System.Text.Json;
using NexusForever.Game.Static.ICComm;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.ICComm;

namespace NexusForever.Game.Tests.ICComm;

public sealed class ICCommProtocolTests
{
    [Theory]
    [InlineData(ICCommChannelType.Global)]
    [InlineData(ICCommChannelType.Guild)]
    public void JoinWriterUsesTheProductionReaderAndPreservesWideNames(ICCommChannelType type)
    {
        using var reader = Reader(writer => ICCommProtocol.Join(writer, type, 123456789123ul, "  Addon界  "));
        var request = new ClientICCommChannelJoin(); request.Read(reader);
        Assert.Equal(type, request.Type); Assert.Equal(123456789123ul, request.GuildId); Assert.Equal("  Addon界  ", request.Name);
        Assert.Equal(0u, reader.BytesRemaining);
    }
    [Fact]
    public void MessageWriterUsesProductionMixedStringFields()
    {
        using var reader = Reader(writer => ICCommProtocol.Send(writer, 987654321012ul, 77, "payload", "Recipient界"));
        var request = new ClientICCommMessage(); request.Read(reader);
        Assert.Equal(987654321012ul, request.IccommId); Assert.Equal(77u, request.MessageId);
        Assert.Equal("payload", request.Message); Assert.Equal("Recipient界", request.RecipientName); Assert.Equal(0u, reader.BytesRemaining);
    }
    [Fact]
    public void ObserverReadsActualServerWritersForJoinAndRejection()
    {
        var joined = ICCommProtocol.Read(Frame(GameMessageOpcode.ServerICCommChannelJoin,
            new ServerICCommChannelJoin { Type = ICCommChannelType.Guild, IccomId = 123, ChannelName = "Addon界" }))!;
        Assert.Equal(123ul, joined.Channel); Assert.Equal(ICCommChannelType.Guild, joined.Type); Assert.Equal("Addon界", joined.Name);
        var denied = ICCommProtocol.Read(Frame(GameMessageOpcode.ServerICCommChannelJoinResult,
            new ServerICCommChannelJoinResult { Type = ICCommChannelType.Group, Result = ICCommJoinResult.NoGroup, ChannelName = "Exact name" }))!;
        Assert.Equal((int)ICCommJoinResult.NoGroup, denied.Result); Assert.Equal("Exact name", denied.Name);
    }
    [Fact]
    public void ObserverReadsActualResultEchoDeliveryAndLeftWriters()
    {
        var result = ICCommProtocol.Read(Frame(GameMessageOpcode.ServerICCommMessageResult,
            new ServerICCommMessageResult { IccomId = 123, MessageId = 7, Result = ICCommMessageResult.NotInChannel }))!;
        Assert.Equal((123ul, 7u, (int?)ICCommMessageResult.NotInChannel), (result.Channel, result.MessageId, result.Result));
        var echo = ICCommProtocol.Read(Frame(GameMessageOpcode.ServerICCommOrderedMessage,
            new ServerICCommOrderedMessage { IccomId = 123, MessageId = 8, Message = "wide界" }))!;
        Assert.Equal("wide界", echo.Text); Assert.Equal(8u, echo.MessageId);
        var delivered = ICCommProtocol.Read(Frame(GameMessageOpcode.ServerICCommDirectedMessage,
            new ServerICCommDirectedMessage { IccomId = 123, Message = "ASCII payload", SenderName = "Sender界" }))!;
        Assert.Equal("ASCII payload", delivered.Text); Assert.Equal("Sender界", delivered.Sender);
        Assert.Equal(123ul, ICCommProtocol.Read(Frame(GameMessageOpcode.ServerICCommChannelLeft, new ServerICCommChannelLeft { IccomId = 123 }))!.Channel);
        Assert.NotEmpty(Convert.FromBase64String(delivered.Body)); Assert.Equal(64, delivered.SHA256.Length);
    }
    [Fact]
    public void ObserverRejectsTrailingAndTruncatedFieldsAndIgnoresOtherPackets()
    {
        var frame = Frame(GameMessageOpcode.ServerICCommMessageResult, new ServerICCommMessageResult { IccomId = 123, MessageId = 7, Result = ICCommMessageResult.Sent });
        Assert.Throws<InvalidDataException>(() => ICCommProtocol.Read(frame with { Body = [.. frame.Body, 0] }));
        Assert.ThrowsAny<Exception>(() => ICCommProtocol.Read(frame with { Body = frame.Body[..3] }));
        Assert.Null(ICCommProtocol.Read(new(GameMessageOpcode.ServerRealmInfo, [])));
    }
    [Theory]
    [InlineData("external", "host")]
    [InlineData("duplicates", "users")]
    [InlineData("ordinary", "users")]
    [InlineData("production", "database")]
    [InlineData("different", "database")]
    [InlineData("group", "scope")]
    [InlineData("missing", "opt-in")]
    public void QualificationRejectsUnownedOrUnsupportedBoundaries(string value, string boundary)
    {
        var options = Valid(); string second = "loadtest9822@example.local"; bool allowed = true; var type = ICCommChannelType.Global;
        if (boundary == "host") options = options with { WorldHost = value + ".example" };
        if (value == "duplicates") second = options.User;
        if (value == "ordinary") second = "player@example.local";
        if (value == "production") options = options with { AuthConnectionString = "server=127.0.0.1;database=nexus_forever_auth" };
        if (value == "different") options = options with { CharacterConnectionString = "server=127.0.0.1;database=nexus_assessment_other_character" };
        if (boundary == "scope") type = ICCommChannelType.Group;
        if (boundary == "opt-in") allowed = false;
        Assert.Throws<InvalidOperationException>(() => ICCommReconnectCommand.Validate(options, second, "loadtest9823@example.local", type, allowed));
    }
    [Theory]
    [InlineData(ICCommChannelType.Global)]
    [InlineData(ICCommChannelType.Guild)]
    public void QualificationAllowsMatchingOwnedLoopbackRoutes(ICCommChannelType type)
        => ICCommReconnectCommand.Validate(Valid(), "loadtest9822@example.local", "loadtest9823@example.local", type, true);
    [Fact]
    public void RuntimeOptionsReadConfiguredPortsAndArrayRoutesWithoutDefaults()
    {
        string parent = Path.GetFullPath(Path.GetTempPath()); string directory = Path.Combine(parent, "NexusForever-ICComm-Options-" + Guid.NewGuid().ToString("N"));
        try
        {
            foreach (var project in new[] { "WorldServer", "AuthServer", "StsServer" }) Directory.CreateDirectory(Path.Combine(directory, "NexusForever." + project));
            foreach (var (project, port) in new[] { ("WorldServer", 24009), ("AuthServer", 23119), ("StsServer", 6609) })
            {
                var data = new { Network = new { Host = "127.0.0.1", Port = port }, Database = new { Auth = new[] { new { ConnectionString = Valid().AuthConnectionString } }, Character = new[] { new { ConnectionString = Valid().CharacterConnectionString } } } };
                File.WriteAllText(Path.Combine(directory, "NexusForever." + project, project + ".json"), JsonSerializer.Serialize(data));
            }
            var options = ICCommReconnectCommand.Options(Path.Combine(directory, "NexusForever.WorldServer", "WorldServer.json"), "loadtest9821@example.local", "test-password");
            Assert.Equal((6609, 23119, 24009), (options.StsPort, options.AuthPort, options.WorldPort));
            Assert.Equal(Valid().CharacterConnectionString, options.CharacterConnectionString);
        }
        finally
        {
            string absolute = Path.GetFullPath(directory);
            Assert.StartsWith(Path.Combine(parent, "NexusForever-ICComm-Options-"), absolute, StringComparison.OrdinalIgnoreCase);
            if (Directory.Exists(absolute)) Directory.Delete(absolute, true);
        }
    }
    private static LoginWorldOptions Valid() => new() { User = "loadtest9821@example.local", StsHost = "127.0.0.1", AuthHost = "127.0.0.1", WorldHost = "127.0.0.1",
        AuthConnectionString = "server=127.0.0.1;database=nexus_assessment_test_auth", CharacterConnectionString = "server=127.0.0.1;database=nexus_assessment_test_character" };
    private static GameFrame Frame(GameMessageOpcode opcode, IWritable packet)
    { using var stream = new MemoryStream(); using var writer = new GamePacketWriter(stream); packet.Write(writer); writer.FlushBits(); return new(opcode, stream.ToArray()); }
    private static GamePacketReader Reader(Action<GamePacketWriter> write)
    { using var stream = new MemoryStream(); using var writer = new GamePacketWriter(stream); write(writer); writer.FlushBits(); return new(new MemoryStream(stream.ToArray())); }
}
