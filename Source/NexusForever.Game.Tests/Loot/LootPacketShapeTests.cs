using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Loot;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Loot;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Loot;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Shared;
using NetworkLootItem = NexusForever.Network.World.Message.Model.Loot.LootItem;

namespace NexusForever.Game.Tests.Loot;

[Collection(LegacyServiceProviderCollection.Name)]
public class LootPacketShapeTests
{
    [Fact]
    public void LootItem_WritePreservesCurrentBooleanOrderAndTrailingFields()
    {
        var packet = new NetworkLootItem
        {
            LootUnitId         = 0x10203040u,
            Type               = LootItemType.StaticItem,
            ItemId             = 0x55667788u,
            Amount             = 7u,
            CanLoot            = true,
            RequiresRoll       = false,
            OnlyMasterLootable = true,
            Explosion          = false,
            Granted            = true,
            RollTime           = 31u,
            RandomCircuitData  = 0x0102030405060708ul,
            RandomGlyphData    = 0xCAFEBABEu,
            ItemQuality2Id     = 4u,
            MasterList =
            [
                new Identity
                {
                    RealmId = 17,
                    Id      = 4200ul
                }
            ]
        };

        byte[] data = WritePacket(packet);

        using var stream = new MemoryStream(data);
        using var reader = new GamePacketReader(stream);
        Assert.Equal(0x10203040u, reader.ReadUInt());
        Assert.Equal((uint)LootItemType.StaticItem, reader.ReadUInt(32u));
        Assert.Equal(0x55667788u, reader.ReadUInt());
        Assert.Equal(7u, reader.ReadUInt());
        Assert.True(reader.ReadBit());
        Assert.False(reader.ReadBit());
        Assert.True(reader.ReadBit());
        Assert.False(reader.ReadBit());
        Assert.True(reader.ReadBit());
        Assert.Equal(31u, reader.ReadUInt());
        Assert.Equal(0x0102030405060708ul, reader.ReadULong());
        Assert.Equal(0xCAFEBABEu, reader.ReadUInt());
        Assert.Equal(4u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal((ushort)17, reader.ReadUShort(14u));
        Assert.Equal(4200ul, reader.ReadULong());
    }

    [Fact]
    public void ServerLootNotify_WritePreservesOwnerParentExplosionHeaderBeforeEntries()
    {
        var packet = new ServerLootNotify
        {
            OwnerUnitId  = 11u,
            ParentUnitId = 22u,
            Explosion    = true,
            LootItems =
            [
                new NetworkLootItem
                {
                    LootUnitId = 33u,
                    Type       = LootItemType.Cash,
                    ItemId     = 44u,
                    Amount     = 55u
                }
            ]
        };

        byte[] data = WritePacket(packet);

        using var stream = new MemoryStream(data);
        using var reader = new GamePacketReader(stream);
        Assert.Equal(11u, reader.ReadUInt());
        Assert.Equal(22u, reader.ReadUInt());
        Assert.True(reader.ReadBit());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(33u, reader.ReadUInt());
        Assert.Equal((uint)LootItemType.Cash, reader.ReadUInt(32u));
        Assert.Equal(44u, reader.ReadUInt());
        Assert.Equal(55u, reader.ReadUInt());
    }

    [Fact]
    public void UnusedLootFeedbackPackets_WritePreserveCurrentPayloadLayouts()
    {
        byte[] notificationData = WritePacket(new ServerLootNotification
        {
            LootUnitId        = 100u,
            ItemId            = 200u,
            Amount            = 3u,
            LooterUnitId      = 400u,
            Type              = LootItemType.StaticItem,
            RandomCircuitData = 500ul,
            RandomGlyphData   = 600u,
            ItemQuality2Id    = 7u
        });
        using (var stream = new MemoryStream(notificationData))
        using (var reader = new GamePacketReader(stream))
        {
            Assert.Equal(100u, reader.ReadUInt());
            Assert.Equal(200u, reader.ReadUInt());
            Assert.Equal(3u, reader.ReadUInt());
            Assert.Equal(400u, reader.ReadUInt());
            Assert.Equal((uint)LootItemType.StaticItem, reader.ReadUInt(32u));
            Assert.Equal(500ul, reader.ReadULong());
            Assert.Equal(600u, reader.ReadUInt());
            Assert.Equal(7u, reader.ReadUInt());
        }

        byte[] canLootData = WritePacket(new ServerLootCanLoot
        {
            LootUnitId = 1234u
        });
        using (var stream = new MemoryStream(canLootData))
        using (var reader = new GamePacketReader(stream))
            Assert.Equal(1234u, reader.ReadUInt());

        byte[] bindOnPickupData = WritePacket(new ServerLootBindOnPickup
        {
            Unused     = 10u,
            LootUnitId = 20u
        });
        using (var stream = new MemoryStream(bindOnPickupData))
        using (var reader = new GamePacketReader(stream))
        {
            Assert.Equal(10u, reader.ReadUInt());
            Assert.Equal(20u, reader.ReadUInt());
        }
    }

    [Fact]
    public void SendLootNotify_IncludeGrantedItemsPreservesCurrentDeliveryState()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        IGroupStateManager groupStateManager = RecordingDispatchProxy<IGroupStateManager>.Create(out _);
        LegacyServiceProvider.Provider = new ServiceCollection()
            .AddSingleton(new GlobalLootManager(groupStateManager))
            .BuildServiceProvider();

        try
        {
            IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
            IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out var sessionProxy);
            ICurrencyManager currencyManager = RecordingDispatchProxy<ICurrencyManager>.Create(out _);

            playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
            playerProxy.SetProperty(nameof(IPlayer.CurrencyManager), currencyManager);
            playerProxy.SetProperty(nameof(IPlayer.Session), session);
            playerProxy.SetProperty("Guid", 4242u);

            var lootInstance = new LootInstance(
                ownerUnitId: 99u,
                looterIds: new Dictionary<ulong, uint> { [42ul] = 4242u },
                looterType: LooterType.Player,
                lootEntityType: LootEntityType.Creature)
            {
                Explosion = true
            };

            LootInstanceItem delivered = lootInstance.AddLootItem((uint)CurrencyType.Credits, LootItemType.Cash, 7u);
            delivered.SetWinner(player);
            Assert.True(delivered.DeliverItem(player, sendAsGrant: false));

            LootInstanceItem pending = lootInstance.AddLootItem((uint)CurrencyType.Credits, LootItemType.Cash, 3u);

            int beforeCount = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Count;
            lootInstance.SendLootNotify(player, includeGrantedItems: true);

            RecordingDispatchProxy<IGameSession>.Invocation call = Assert.Single(sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Skip(beforeCount));
            var notify = Assert.IsType<ServerLootNotify>(call.Arguments[0]);

            Assert.Equal(99u, notify.OwnerUnitId);
            Assert.Equal(99u, notify.ParentUnitId);
            Assert.True(notify.Explosion);
            Assert.Equal(2, notify.LootItems.Count);

            NetworkLootItem granted = Assert.Single(notify.LootItems, item => item.Granted);
            Assert.Equal(0u, granted.LootUnitId);
            Assert.Equal(LootItemType.Cash, granted.Type);
            Assert.Equal((uint)CurrencyType.Credits, granted.ItemId);
            Assert.Equal(7u, granted.Amount);
            Assert.True(granted.CanLoot);
            Assert.True(granted.Explosion);

            NetworkLootItem active = Assert.Single(notify.LootItems, item => !item.Granted);
            Assert.Equal(pending.Id, active.LootUnitId);
            Assert.Equal(LootItemType.Cash, active.Type);
            Assert.Equal((uint)CurrencyType.Credits, active.ItemId);
            Assert.Equal(3u, active.Amount);
            Assert.True(active.CanLoot);
            Assert.False(active.RequiresRoll);
            Assert.False(active.OnlyMasterLootable);
            Assert.True(active.Explosion);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    private static byte[] WritePacket(NexusForever.Network.Message.IWritable packet)
    {
        using var stream = new MemoryStream();
        using var writer = new GamePacketWriter(stream);
        packet.Write(writer);
        writer.FlushBits();
        return stream.ToArray();
    }
}
