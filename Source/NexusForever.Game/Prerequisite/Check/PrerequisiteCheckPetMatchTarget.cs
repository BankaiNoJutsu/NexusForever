using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 89: live <c>Prerequisite_CheckPetMatchTarget</c> (<c>14049d4f0</c>, vtable <c>+0x160</c>)
    /// compares pet lookup ids between caster and target. NF compares visible vanity-pet
    /// <see cref="IWorldEntity.CreatureId"/> when both sides have an active pet.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.PetMatchTarget)]
    public class PrerequisiteCheckPetMatchTarget : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint playerPetId = GetVanityPetCreatureId(player);
            uint targetPetId = 0u;
            if (parameters.Target is IPlayer targetPlayer)
                targetPetId = GetVanityPetCreatureId(targetPlayer);

            uint match = playerPetId != 0u && playerPetId == targetPetId ? 1u : 0u;
            return PrerequisiteCompare.Compare(comparison, match, value);
        }

        private static uint GetVanityPetCreatureId(IPlayer player)
        {
            if (player.VanityPetGuid == null)
                return 0u;

            IPetEntity pet = player.GetVisible<IPetEntity>(player.VanityPetGuid.Value);
            return pet?.CreatureId ?? 0u;
        }
    }
}
