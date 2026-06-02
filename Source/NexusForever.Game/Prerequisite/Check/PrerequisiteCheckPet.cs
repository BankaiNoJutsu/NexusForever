using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 88: handler table <c>Prerequisite_CheckPet</c> (<c>14049f370</c>) walks active pet entities at
    /// entity <c>+0x16a0/+0x198</c>. NF checks visible vanity pet when <see cref="IPlayer.VanityPetGuid"/> is set.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Pet)]
    public class PrerequisiteCheckPet : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint hasPet = 0u;
            if (player.VanityPetGuid != null && player.GetVisible<IPetEntity>(player.VanityPetGuid.Value) != null)
                hasPet = 1u;

            return PrerequisiteCompare.Compare(comparison, hasPet, value);
        }
    }
}
