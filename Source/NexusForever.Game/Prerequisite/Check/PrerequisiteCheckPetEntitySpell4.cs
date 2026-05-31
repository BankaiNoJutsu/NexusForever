using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.PetEntitySpell4)]
    public class PrerequisiteCheckPetEntitySpell4 : IPrerequisiteCheck
    {
        #region Dependency Injection

        private readonly ILogger<PrerequisiteCheckPetEntitySpell4> log;

        public PrerequisiteCheckPetEntitySpell4(
            ILogger<PrerequisiteCheckPetEntitySpell4> log)
        {
            this.log = log;
        }

        #endregion

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            // Client case 0xbf: SpellService_LookupSpellWrapperByWrapperId on caster entity+0x1600, then vanity pet
            // from FUN_14039df50 + Prerequisite_ResolveSpellOnPetEntity when caster is the local player.
            // Retail tbl keeps objectId0 at 0; value0 is not read by the client dispatch path.
            bool resolved = HasActiveVanityPetSpellReference(player, objectId);
            switch (comparison)
            {
                case PrerequisiteComparison.Equal:
                    return resolved;
                case PrerequisiteComparison.NotEqual:
                    return !resolved;
                default:
                    log.LogWarning($"Unhandled PrerequisiteComparison {comparison} for {PrerequisiteType.PetEntitySpell4}!");
                    return false;
            }
        }

        private static bool HasActiveVanityPetSpellReference(IPlayer player, uint objectId)
        {
            if (player.VanityPetGuid == null)
                return false;

            if (player.GetVisible<IPetEntity>(player.VanityPetGuid.Value) == null)
                return false;

            // Retail keeps objectId0 at 0; require an unlocked vanity-pet spell book entry as a coarse
            // proxy for client SpellService resolve on entity+0x1600 / pet fallback path.
            if (objectId != 0u)
            {
                return player.SpellManager.GetSpellForSpell4Id(objectId) != null
                    || player.SpellManager.GetSpell(objectId) != null;
            }

            return player.SpellManager.GetPets().Count > 0;
        }
    }
}
