using NexusForever.Game.Static.Entity;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.GalacticArchive;
using NexusForever.Network.World.Message.Model.Guild;
using NexusForever.Network.World.Message.Model.Marketplace;
using NexusForever.Network.World.Message.Model.Pregame;
using Xunit;

// Wire fixtures derived from merged upstream #587 and #591. Keep the fork's
// handler-facing APIs while verifying widths, order and following-bit alignment.
namespace NexusForever.Game.Tests.Network;

public class UpstreamPacketCompatibilityTests
{
    private static byte[] Encode(Action<GamePacketWriter> write)
    {
        using var stream = new MemoryStream();
        using var writer = new GamePacketWriter(stream);
        write(writer);
        writer.FlushBits();
        return stream.ToArray();
    }

    [Fact]
    public void ArchiveUnlockAcceptsFourteenBitArticleId()
    {
        byte[] data = Encode(w => w.Write((ushort)123, 14u));
        using var reader = new GamePacketReader(new MemoryStream(data));
        var message = new ClientGalacticArchiveUnlock();
        message.Read(reader);
        Assert.Equal(123u, (uint)message.ArchiveArticleId);
    }

    [Fact]
    public void ArchiveViewedAcceptsFourteenBitArticleId()
    {
        byte[] data = Encode(w => w.Write((ushort)123, 14u));
        using var reader = new GamePacketReader(new MemoryStream(data));
        var message = new ClientGalacticArchiveViewed();
        message.Read(reader);
        Assert.Equal(123u, (uint)message.ArchiveArticleId);
    }

    [Fact]
    public void ArchiveUpdateUsesSeventyEightBitsBeforePadding()
    {
        var message = new ServerGalacticArchiveUpdate
        {
            ArchiveArticleId = 123,
            UnlockedFlags = 0x80000001,
            ViewedFlags = 1
        };
        byte[] data = Encode(message.Write);
        Assert.Equal(10, data.Length);
        using var reader = new GamePacketReader(new MemoryStream(data));
        Assert.Equal(123u, (uint)reader.ReadUShort(14u));
        Assert.Equal(0x80000001u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
    }

    [Fact]
    public void AuctionCancelReadsItemBeforeAuctionId()
    {
        byte[] data = Encode(w => { w.Write(12345u, 18u); w.Write(0x0102030405060708ul); });
        using var reader = new GamePacketReader(new MemoryStream(data));
        var message = new ClientAuctionCancel();
        message.Read(reader);
        Assert.Equal(12345u, message.Item2Id);
        Assert.Equal(0x0102030405060708ul, message.AuctionId);
    }

    [Theory]
    [InlineData(ResurrectionType.WakeHere, 0)]
    [InlineData(ResurrectionType.WakeHereServiceToken, 127)]
    [InlineData(ResurrectionType.SpellCasterLocation, 42)]
    [InlineData(ResurrectionType.Holocrypt, 1)]
    public void ResurrectAcceptReadsSevenBitsBeforeType(ResurrectionType type, byte unknown)
    {
        byte[] data = Encode(w => { w.Write(123u); w.Write(unknown, 7u); w.Write((uint)type); });
        using var reader = new GamePacketReader(new MemoryStream(data));
        var message = new ClientResurrectAccept();
        message.Read(reader);
        Assert.Equal(123u, message.UnitId);
        Assert.Equal(unknown, message.Unknown1);
        Assert.Equal(type, message.RezType);
    }

    [Fact]
    public void GuildBankMoveReadsEighteenBitItemIdBeforeLocations()
    {
        byte[] data = Encode(w =>
        {
            w.Write((ushort)321, 14u);
            w.Write(0x0102030405060708ul);
            w.Write(0x1112131415161718ul);
            w.Write(0x2122232425262728ul);
            w.Write(0x3FFFFu, 18u);
            w.Write(10u, 9u); // Preserve the fork's realm-bank wire mapping.
            w.Write(0x01020304u);
            w.Write((uint)InventoryLocation.Ability, 9u);
            w.Write(3u);
            w.Write(true);
            w.Write(17u);
            w.Write((byte)0xA5);
        });
        using var reader = new GamePacketReader(new MemoryStream(data));
        var message = new ClientGuildBankTransaction2();
        message.Read(reader);

        Assert.Equal((ushort)321, message.GuildIdentity.RealmId);
        Assert.Equal(0x0102030405060708ul, message.GuildIdentity.Id);
        Assert.Equal(0x1112131415161718ul, message.SourceItemGuid);
        Assert.Equal(0x2122232425262728ul, message.DestinationItemGuid);
        Assert.Equal(0x3FFFFu, message.SourceItem2Id);
        Assert.Equal(InventoryLocation.RealmBank, message.Destination.Location);
        Assert.Equal(0x01020304u, message.Destination.BagIndex);
        Assert.Equal(InventoryLocation.Ability, message.Source.Location);
        Assert.Equal(3u, message.Source.BagIndex);
        Assert.True(message.UseFirstFreeSlot);
        Assert.Equal(17u, message.Count);
        Assert.Equal((byte)0xA5, reader.ReadByte());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RealmTransferRequestConsumesOnlyItsUnknownBit(bool value)
    {
        byte[] data = Encode(w => { w.Write(value); w.Write((ushort)0xCAFE); });
        using var reader = new GamePacketReader(new MemoryStream(data));
        var message = new ClientGetRealmTransferDestinations();
        message.Read(reader);

        Assert.Equal(value, message.Unknown0);
        Assert.Equal((ushort)0xCAFE, reader.ReadUShort());
    }
}
