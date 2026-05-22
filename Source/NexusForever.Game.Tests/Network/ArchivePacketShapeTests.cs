using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.GalacticArchive;
using NetworkDatacube = NexusForever.Network.World.Message.Model.Shared.Datacube;

namespace NexusForever.Game.Tests.Network;

public class ArchivePacketShapeTests
{
    [Fact]
    public void GalacticArchiveClientPackets_ReadArchiveArticleId()
    {
        byte[] unlockData = WritePacket(writer => writer.Write(0x01020304u));
        byte[] viewedData = WritePacket(writer => writer.Write(0x05060708u));

        using (var reader = new GamePacketReader(new MemoryStream(unlockData)))
        {
            var packet = new ClientGalacticArchiveUnlock();
            packet.Read(reader);
            Assert.Equal(0x01020304u, packet.ArchiveArticleId);
        }

        using (var reader = new GamePacketReader(new MemoryStream(viewedData)))
        {
            var packet = new ClientGalacticArchiveViewed();
            packet.Read(reader);
            Assert.Equal(0x05060708u, packet.ArchiveArticleId);
        }
    }

    [Fact]
    public void ServerGalacticArchivePackets_WriteUpdateAndRefreshPayloads()
    {
        byte[] updateData = WritePacket(new ServerGalacticArchiveUpdate
        {
            ArchiveArticleId = 0x11111111u,
            UnlockedFlags    = 0x22222222u,
            ViewedFlags      = 0x33333333u
        });

        using (var reader = new GamePacketReader(new MemoryStream(updateData)))
        {
            Assert.Equal(0x11111111u, reader.ReadUInt());
            Assert.Equal(0x22222222u, reader.ReadUInt());
            Assert.Equal(0x33333333u, reader.ReadUInt());
        }

        Assert.Empty(WritePacket(new ServerGalacticArchiveRefresh()));
    }

    [Fact]
    public void ServerDatacubeUpdatePackets_WriteDatacubeAndVolumeRows()
    {
        byte[] datacubeData = WritePacket(new ServerDatacubeUpdate
        {
            DatacubeData = new NetworkDatacube
            {
                DatacubeId = 0x0123,
                Progress   = 0xABCDEF01u
            }
        });
        byte[] volumeData = WritePacket(new ServerDatacubeVolumeUpdate
        {
            DatacubeVolumeData = new NetworkDatacube
            {
                DatacubeId = 0x0234,
                Progress   = 0x10203040u
            }
        });

        AssertDatacube(datacubeData, 0x0123, 0xABCDEF01u);
        AssertDatacube(volumeData, 0x0234, 0x10203040u);
    }

    [Fact]
    public void ServerDatacubeUpdateList_WritesSeparateDatacubeAndVolumeArrays()
    {
        var packet = new ServerDatacubeUpdateList
        {
            DatacubeData =
            [
                new NetworkDatacube
                {
                    DatacubeId = 0x0345,
                    Progress   = 0x01010101u
                }
            ],
            DatacubeVolumeData =
            [
                new NetworkDatacube
                {
                    DatacubeId = 0x0456,
                    Progress   = 0x02020202u
                }
            ]
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(0x0345u, reader.ReadUShort(14u));
        Assert.Equal(0x01010101u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(0x0456u, reader.ReadUShort(14u));
        Assert.Equal(0x02020202u, reader.ReadUInt());
    }

    private static void AssertDatacube(byte[] packetData, ushort datacubeId, uint progress)
    {
        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(datacubeId, reader.ReadUShort(14u));
        Assert.Equal(progress, reader.ReadUInt());
    }

    private static byte[] WritePacket(IWritable packet)
    {
        return WritePacket(packet.Write);
    }

    private static byte[] WritePacket(Action<GamePacketWriter> write)
    {
        using var stream = new MemoryStream();
        using var writer = new GamePacketWriter(stream);
        write(writer);
        writer.FlushBits();
        return stream.ToArray();
    }
}
