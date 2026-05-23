using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
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

}
