using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Entity;

namespace NexusForever.Game.Tests.Entity;

public class EntityCreateAuxiliaryEmissionTests
{
    [Fact]
    public void BuildPreCreatePackets_ReturnsFivePacketsInRegistrationOrder()
    {
        using ServiceProvider provider = EntityCreatePacketTests.BuildProvider();
        IEntityFactory entityFactory = provider.GetRequiredService<IEntityFactory>();
        IWorldEntity entity = entityFactory.CreateWorldEntity(EntityType.WorldUnit);

        IReadOnlyList<IWritable> packets = entity.BuildEntityCreateAuxPackets();

        Assert.Equal(5, packets.Count);
        Assert.IsType<ServerEntityCreateAuxSingleRow>(packets[0]);
        Assert.IsType<ServerEntityCreateAuxRowList>(packets[1]);
        Assert.IsType<ServerEntityCreateAuxBitPackedRowList>(packets[2]);
        Assert.IsType<ServerEntityCreateAuxSingleBitPackedRow>(packets[3]);
        Assert.IsType<ServerEntityCreateAuxScalarList>(packets[4]);
    }

    [Fact]
    public void BuildPreCreatePackets_CorrelatesUnitIdFields()
    {
        using ServiceProvider provider = EntityCreatePacketTests.BuildProvider();
        IEntityFactory entityFactory = provider.GetRequiredService<IEntityFactory>();
        IWorldEntity entity = entityFactory.CreateWorldEntity(EntityType.StructuredPlug);
        EntityCreatePacketTests.SetWorldSocketId(entity, 0x90A0);

        var singleRow = (ServerEntityCreateAuxSingleRow)entity.BuildEntityCreateAuxPackets()[0];

        Assert.Equal(entity.Guid, singleRow.Value1);
        Assert.Equal(entity.Guid, singleRow.Value2.Value0);
        Assert.Equal(0x90A0u, singleRow.Value5);
    }

    [Fact]
    public void BuildPreCreatePackets_WithLargeActivePropId_Uses17BitSafeBitPackedValue()
    {
        using ServiceProvider provider = EntityCreatePacketTests.BuildProvider();
        IEntityFactory entityFactory = provider.GetRequiredService<IEntityFactory>();
        IWorldEntity entity = entityFactory.CreateWorldEntity(EntityType.StructuredPlug);

        SetActivePropId(entity, 7140154ul);
        SetDisplayInfo(entity, 0x12345u);

        IReadOnlyList<IWritable> packets = entity.BuildEntityCreateAuxPackets();

        var bitPackedList = Assert.IsType<ServerEntityCreateAuxBitPackedRowList>(packets[2]);
        var bitPackedRow = Assert.IsType<ServerEntityCreateAuxSingleBitPackedRow>(packets[3]);

        Assert.Single(bitPackedList.Rows);
        Assert.Equal(entity.DisplayInfo, bitPackedList.Rows[0].Value2);
        Assert.Equal(entity.DisplayInfo, bitPackedRow.Value2);

        Exception listWrite = Record.Exception(() => WritePacket(bitPackedList));
        Exception rowWrite = Record.Exception(() => WritePacket(bitPackedRow));

        Assert.Null(listWrite);
        Assert.Null(rowWrite);
    }

    private static void SetActivePropId(IWorldEntity entity, ulong activePropId)
    {
        PropertyInfo propertyInfo = typeof(WorldEntity).GetProperty(nameof(IWorldEntity.ActivePropId), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new Xunit.Sdk.XunitException($"Unable to find {nameof(IWorldEntity.ActivePropId)} on {nameof(WorldEntity)}.");
        propertyInfo.SetValue(entity, activePropId);
    }

    private static void SetDisplayInfo(IWorldEntity entity, uint displayInfo)
    {
        PropertyInfo propertyInfo = typeof(WorldEntity).GetProperty(nameof(IWorldEntity.CreatureDisplayEntry), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new Xunit.Sdk.XunitException($"Unable to find {nameof(IWorldEntity.CreatureDisplayEntry)} on {nameof(WorldEntity)}.");
        propertyInfo.SetValue(entity, new Creature2DisplayInfoEntry
        {
            Id = displayInfo
        });
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
