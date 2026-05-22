using System.Numerics;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Game.Static.Housing;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Abstract.Housing
{
    public interface IDecor : IDatabaseCharacter, IDatabaseState, INetworkBuildable<ServerHousingResidenceDecor.Decor>
    {
        ulong Id { get; }
        ulong DecorId { get; }
        uint DecorInfoId { get; }
        HousingDecorInfoEntry Entry { get; }
        DecorType Type { get; set; }
        uint DecorData { get; set; }
        uint HookBagIndex { get; set; }
        uint HookIndex { get; set; }
        uint PlotIndex { get; set; }
        Vector3 Position { get; set; }
        Quaternion Rotation { get; set; }
        float Scale { get; set; }
        uint ActivePropUnitId { get; set; }
        ulong DecorParentId { get; set; }
        ushort ColourShiftId { get; set; }
        
        IResidence Residence { get; }

        /// <summary>
        /// Move <see cref="IDecor"/> to supplied position.
        /// </summary>
        void Move(DecorType type, Vector3 position, Quaternion rotation, float scale, uint plotIndex);

        /// <summary>
        /// Replace the backing decor entry for a retail decor update that changes the decor template in-place.
        /// </summary>
        void UpdateEntry(HousingDecorInfoEntry entry);

        /// <summary>
        /// Replace the raw decor info id for decor records backed by a non-decor client table.
        /// </summary>
        void UpdateDecorInfoId(uint decorInfoId);

        /// <summary>
        /// Move <see cref="IDecor"/> to the crate.
        /// </summary>
        void Crate();
    }
}
