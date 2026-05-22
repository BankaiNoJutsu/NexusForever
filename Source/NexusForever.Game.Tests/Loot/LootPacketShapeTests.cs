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
    public void ClientLootRollAction_ReadsOwnerLootAndTwoBitAction()
    {
        byte[] data = WritePacket(writer =>
        {
            writer.Write(0x11111111u);
            writer.Write(0x22222222u);
            writer.Write(LootRollAction.Greed, 2u);
        });

        using var stream = new MemoryStream(data);
        using var reader = new GamePacketReader(stream);
        var packet = new ClientLootRollAction();

        packet.Read(reader);

        Assert.Equal(0x11111111u, packet.OwnerUnitId);
        Assert.Equal(0x22222222u, packet.LootUnitId);
        Assert.Equal(LootRollAction.Greed, packet.Action);
    }

    [Fact]
    public void ClientLootAssignMaster_ReadsOwnerLootAndAssigneeIdentity()
    {
        byte[] data = WritePacket(writer =>
        {
            writer.Write(0x33333333u);
            writer.Write(0x44444444u);
            writer.Write((ushort)17, 14u);
            writer.Write(0x1122334455667788ul);
        });

        using var stream = new MemoryStream(data);
        using var reader = new GamePacketReader(stream);
        var packet = new ClientLootAssignMaster();

        packet.Read(reader);

        Assert.Equal(0x33333333u, packet.OwnerUnitId);
        Assert.Equal(0x44444444u, packet.LootUnitId);
        Assert.Equal((ushort)17, packet.Assignee.RealmId);
        Assert.Equal(0x1122334455667788ul, packet.Assignee.Id);
    }

    [Fact]
    public void LootRollResultPackets_WriteMappedRollLayouts()
    {
        byte[] rollData = WritePacket(new ServerLootRoll
        {
            LootUnitId = 0x01020304u,
            Roller = new Identity
            {
                RealmId = 12,
                Id      = 0x1111222233334444ul
            },
            ItemId = 0x12345u,
            Action = LootRollAction.Need
        });
        using (var stream = new MemoryStream(rollData))
        using (var reader = new GamePacketReader(stream))
        {
            Assert.Equal(0x01020304u, reader.ReadUInt());
            Assert.Equal((ushort)12, reader.ReadUShort(14u));
            Assert.Equal(0x1111222233334444ul, reader.ReadULong());
            Assert.Equal(0x12345u, reader.ReadUInt(18u));
            Assert.Equal((uint)LootRollAction.Need, reader.ReadUInt(32u));
        }

        byte[] winnerData = WritePacket(new ServerLootWinner
        {
            LootUnitId = 0x05060708u,
            WinningRoll = new ServerLootWinner.LootRoll
            {
                Identity = new Identity
                {
                    RealmId = 13,
                    Id      = 0x2222333344445555ul
                },
                Value = 101u
            },
            ItemId = 0x23456u,
            OtherRolls =
            [
                new ServerLootWinner.LootRoll
                {
                    Identity = new Identity
                    {
                        RealmId = 14,
                        Id      = 0x3333444455556666ul
                    },
                    Value = 44u
                }
            ]
        });
        using (var stream = new MemoryStream(winnerData))
        using (var reader = new GamePacketReader(stream))
        {
            Assert.Equal(0x05060708u, reader.ReadUInt());
            Assert.Equal((ushort)13, reader.ReadUShort(14u));
            Assert.Equal(0x2222333344445555ul, reader.ReadULong());
            Assert.Equal(101u, reader.ReadUInt());
            Assert.Equal(0x23456u, reader.ReadUInt(18u));
            Assert.Equal(1u, reader.ReadUInt());
            Assert.Equal((ushort)14, reader.ReadUShort(14u));
            Assert.Equal(0x3333444455556666ul, reader.ReadULong());
            Assert.Equal(44u, reader.ReadUInt());
        }

        byte[] removeData = WritePacket(new ServerLootRemove
        {
            OwnerUnitId = 0x12345678u
        });
        using (var stream = new MemoryStream(removeData))
        using (var reader = new GamePacketReader(stream))
            Assert.Equal(0x12345678u, reader.ReadUInt());
    }

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
    public void ServerLootItemUpdate_WritesMappedLootItemPayload()
    {
        var packet = new ServerLootItemUpdate
        {
            LootItem = new NetworkLootItem
            {
                LootUnitId         = 0x11111111u,
                Type               = LootItemType.StaticItem,
                ItemId             = 0x22222222u,
                Amount             = 2u,
                CanLoot            = true,
                RequiresRoll       = true,
                OnlyMasterLootable = false,
                Explosion          = true,
                Granted            = false,
                RollTime           = 45u,
                RandomCircuitData  = 0x0123456789ABCDEFul,
                RandomGlyphData    = 0x33333333u,
                ItemQuality2Id     = 6u,
                MasterList =
                [
                    new Identity
                    {
                        RealmId = 23,
                        Id      = 0x4444555566667777ul
                    }
                ]
            }
        };

        byte[] data = WritePacket(packet);

        using var stream = new MemoryStream(data);
        using var reader = new GamePacketReader(stream);
        Assert.Equal(0x11111111u, reader.ReadUInt());
        Assert.Equal((uint)LootItemType.StaticItem, reader.ReadUInt(32u));
        Assert.Equal(0x22222222u, reader.ReadUInt());
        Assert.Equal(2u, reader.ReadUInt());
        Assert.True(reader.ReadBit());
        Assert.True(reader.ReadBit());
        Assert.False(reader.ReadBit());
        Assert.True(reader.ReadBit());
        Assert.False(reader.ReadBit());
        Assert.Equal(45u, reader.ReadUInt());
        Assert.Equal(0x0123456789ABCDEFul, reader.ReadULong());
        Assert.Equal(0x33333333u, reader.ReadUInt());
        Assert.Equal(6u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal((ushort)23, reader.ReadUShort(14u));
        Assert.Equal(0x4444555566667777ul, reader.ReadULong());
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
            OwnerUnitId = 10u,
            LootUnitId  = 20u
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
