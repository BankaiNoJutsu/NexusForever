using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 266: client case <c>0x10a</c> dispatches vtable <c>+0xc8</c> to <c>14049cae0</c>,
    /// which compares entity type <c>+0x80</c> against native ids <c>0x18</c>/<c>0x19</c> (<see cref="EntityType.Pet"/>/<see cref="EntityType.EsperPet"/>).
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.PetOrEsperPetEntity)]
    public class PrerequisiteCheckPetOrEsperPetEntity : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            IWorldEntity entity = parameters.Target as IWorldEntity ?? player;
            uint isPetEntity = entity.Type is EntityType.Pet or EntityType.EsperPet ? 1u : 0u;
            return PrerequisiteCompare.Compare(comparison, isPetEntity, value);
        }
    }
}
