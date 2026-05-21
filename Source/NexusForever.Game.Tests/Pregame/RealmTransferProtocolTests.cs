using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Static.Pregame;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Pregame;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Misc;

namespace NexusForever.Game.Tests.Pregame;

public class RealmTransferProtocolTests
{
    [Fact]
    public void ClientGetRealmTransferDestinationsHandler_ReturnsEmptyCompatibilityList()
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        var handler = new ClientGetRealmTransferDestinationsHandler(
            NullLogger<ClientGetRealmTransferDestinationsHandler>.Instance);

        handler.HandleMessage(session, new ClientGetRealmTransferDestinations());

        ServerTransferDestinationRealmList response = sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0])
            .OfType<ServerTransferDestinationRealmList>()
            .Single();
        Assert.Empty(response.Realms);
    }

    [Fact]
    public void ServerTransferDestinationRealmList_WriteSerializesDestinationRows()
    {
        var message = new ServerTransferDestinationRealmList
        {
            Realms =
            [
                new ServerTransferDestinationRealmList.RealmTransferInfo
                {
                    RealmId = 1001u,
                    RealmName = "Destination",
                    RealmNoteStringId = 2002u,
                    Flags = RealmFlag.FactionRestricted,
                    Type = RealmType.PVP,
                    Status = RealmStatus.Up,
                    Population = RealmPopulation.High,
                    Unused1 = 3003u,
                    Unused2 = Enumerable.Range(0, 16).Select(i => (byte)i).ToArray(),
                    AccountRealmInfo = new RealmInfo.AccountRealmData
                    {
                        RealmId = 44,
                        CharacterCount = 2u,
                        LastPlayedCharacter = "Last",
                        LastPlayedTime = 0x1122334455667788ul
                    },
                    Unused3 = 55,
                    Unused4 = 66,
                    Unused5 = 77,
                    Unused6 = 88,
                    IsFree = true
                }
            ]
        };

        byte[] packetData = WritePacket(message);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(1001u, reader.ReadUInt());
        Assert.Equal("Destination", reader.ReadWideString());
        Assert.Equal(2002u, reader.ReadUInt());
        Assert.Equal(RealmFlag.FactionRestricted, reader.ReadEnum<RealmFlag>(32u));
        Assert.Equal(RealmType.PVP, reader.ReadEnum<RealmType>(2u));
        Assert.Equal(RealmStatus.Up, reader.ReadEnum<RealmStatus>(3u));
        Assert.Equal(RealmPopulation.High, reader.ReadEnum<RealmPopulation>(3u));
        Assert.Equal(3003u, reader.ReadUInt());
        Assert.Equal(Enumerable.Range(0, 16).Select(i => (byte)i).ToArray(), reader.ReadBytes(16u));
        Assert.Equal((ushort)44, reader.ReadUShort(14u));
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal("Last", reader.ReadWideString());
        Assert.Equal(0x1122334455667788ul, reader.ReadULong());
        Assert.Equal((ushort)55, reader.ReadUShort());
        Assert.Equal((ushort)66, reader.ReadUShort());
        Assert.Equal((ushort)77, reader.ReadUShort());
        Assert.Equal((ushort)88, reader.ReadUShort());
        Assert.True(reader.ReadBit());
    }

    private static byte[] WritePacket(ServerTransferDestinationRealmList message)
    {
        using var stream = new MemoryStream();
        using var writer = new GamePacketWriter(stream);
        message.Write(writer);
        writer.FlushBits();
        return stream.ToArray();
    }
}
