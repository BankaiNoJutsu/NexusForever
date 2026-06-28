using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Entity;

namespace NexusForever.Game.Entity
{
    /// <summary>
    /// Builds unresolved entity-create auxiliary packet candidates in client registration order
    /// (<c>0x025F</c>..<c>0x0261</c>, <c>0x0263</c>, <c>0x0264</c>) for evidence work.
    /// Normal visibility and respawn recreation do not emit them until native consumer semantics are verified.
    /// Blocked field semantics currently reuse bounded create-surface values only; the 17-bit row
    /// slots intentionally avoid wide identifiers such as <see cref="IWorldEntity.ActivePropId"/>
    /// until native consumer mapping is verified.
    /// </summary>
    public static class EntityCreateAuxiliaryPacketBuilder
    {
        public static IReadOnlyList<IWritable> BuildPreCreatePackets(IWorldEntity entity)
        {
            ServerEntityCreateAuxRow row = BuildRow(entity);
            ServerEntityCreateAuxBitPackedRow bitPackedRow = BuildBitPackedRow(entity);

            return
            [
                CopyRow(row),
                BuildRowList(row),
                BuildBitPackedRowList(bitPackedRow),
                CopyBitPackedRow(bitPackedRow),
                BuildScalarList(entity),
            ];
        }

        private static ServerEntityCreateAuxSingleRow CopyRow(ServerEntityCreateAuxRow row)
        {
            return new ServerEntityCreateAuxSingleRow
            {
                Value0  = row.Value0,
                Value1  = row.Value1,
                Value2  = CopyTriple(row.Value2),
                Value5  = row.Value5,
                Value6  = CopyTriple(row.Value6),
                Value9  = row.Value9,
                Value10 = row.Value10,
            };
        }

        private static ServerEntityCreateAuxRowList BuildRowList(ServerEntityCreateAuxRow row)
        {
            var list = new ServerEntityCreateAuxRowList();
            list.Rows.Add(CopyRow(row));
            return list;
        }

        private static ServerEntityCreateAuxSingleBitPackedRow CopyBitPackedRow(ServerEntityCreateAuxBitPackedRow row)
        {
            return new ServerEntityCreateAuxSingleBitPackedRow
            {
                Value0 = row.Value0,
                Value1 = row.Value1,
                Value2 = row.Value2,
                Value3 = row.Value3,
                Value4 = row.Value4,
                Value5 = row.Value5,
                Value6 = CopyTriple(row.Value6),
            };
        }

        private static ServerEntityCreateAuxBitPackedRowList BuildBitPackedRowList(ServerEntityCreateAuxBitPackedRow row)
        {
            var list = new ServerEntityCreateAuxBitPackedRowList();
            list.Rows.Add(CopyBitPackedRow(row));
            return list;
        }

        private static ServerEntityCreateAuxRow BuildRow(IWorldEntity entity)
        {
            return new ServerEntityCreateAuxRow
            {
                Value0  = (ushort)((uint)entity.Type & 0xFFFFu),
                Value1  = entity.Guid,
                Value2  = BuildUnitTriple(entity),
                Value5  = entity.WorldSocketId,
                Value6  = BuildUnitTriple(entity),
                Value9  = (uint)(entity.ActivePropId & 0xFFFFFFFFu),
                Value10 = entity.ControllerGuid ?? entity.Guid,
            };
        }

        private static ServerEntityCreateAuxBitPackedRow BuildBitPackedRow(IWorldEntity entity)
        {
            return new ServerEntityCreateAuxBitPackedRow
            {
                Value0 = entity.Guid,
                Value1 = (uint)entity.Type,
                Value2 = entity.DisplayInfo,
                Value3 = entity.WorldSocketId,
                Value4 = (uint)entity.Faction1,
                Value5 = (uint)entity.Faction2,
                Value6 = BuildUnitTriple(entity),
            };
        }

        private static ServerEntityCreateAuxScalarList BuildScalarList(IWorldEntity entity)
        {
            var list = new ServerEntityCreateAuxScalarList
            {
                Value0 = entity.Guid,
                Value1 = (ushort)((uint)entity.Type & 0xFFFFu),
                Value2 = entity.WorldSocketId,
            };

            list.Values.Add((uint)(entity.ActivePropId & 0xFFFFFFFFu));
            if (entity.ControllerGuid.HasValue)
                list.Values.Add(entity.ControllerGuid.Value);

            return list;
        }

        private static ServerEntityCreateAuxUInt32Triple BuildUnitTriple(IWorldEntity entity)
        {
            return new ServerEntityCreateAuxUInt32Triple
            {
                Value0 = entity.Guid,
                Value1 = (uint)(entity.ActivePropId & 0xFFFFFFFFu),
                Value2 = entity.WorldSocketId,
            };
        }

        private static ServerEntityCreateAuxUInt32Triple CopyTriple(ServerEntityCreateAuxUInt32Triple triple)
        {
            return new ServerEntityCreateAuxUInt32Triple
            {
                Value0 = triple.Value0,
                Value1 = triple.Value1,
                Value2 = triple.Value2,
            };
        }
    }
}
