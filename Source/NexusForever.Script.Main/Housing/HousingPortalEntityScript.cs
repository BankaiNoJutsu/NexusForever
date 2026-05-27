using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Main.Housing
{
    [ScriptFilterCreatureId(26350u)]
    public class HousingPortalEntityScript : IWorldEntityScript, IOwnedScript<ICreatureEntity>
    {
        private const uint HousingDialogSpellId = 39111u;

        private static readonly IReadOnlyList<uint> housingTrainingSpellBaseIds =
        [
            22919u, // Recall - House
            25520u  // Escape House
        ];

        private readonly IFactory<ISpellParameters> spellParametersFactory;
        private readonly ILogger<HousingPortalEntityScript> log;

        private ICreatureEntity owner;

        public static IReadOnlyList<uint> HousingTrainingSpellBaseIds => housingTrainingSpellBaseIds;

        public HousingPortalEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            ILogger<HousingPortalEntityScript> log)
        {
            this.spellParametersFactory = spellParametersFactory;
            this.log                    = log;
        }

        public void OnLoad(ICreatureEntity owner)
        {
            this.owner = owner;
            log.LogDebug("Housing portal script loaded for entity {EntityGuid}: creature={CreatureId}.",
                owner.Guid,
                owner.CreatureId);
        }

        public void OnActivateSuccess(IPlayer activator)
        {
            if (activator == null)
                return;

            ISpellManager spellManager = activator.SpellManager;
            if (spellManager == null)
            {
                log.LogWarning("Housing portal {EntityGuid} could not grant housing spells to player {PlayerGuid}: missing spell manager.",
                    owner?.Guid ?? 0u,
                    activator.Guid);
                return;
            }

            foreach (uint spellBaseId in housingTrainingSpellBaseIds)
            {
                if (spellManager.GetSpell(spellBaseId) == null)
                    spellManager.AddSpell(spellBaseId);
            }

            ISpellParameters spellParameters = spellParametersFactory.Resolve();
            spellParameters.PrimaryTargetId        = activator.Guid;
            spellParameters.UserInitiatedSpellCast = false;
            spellParameters.IgnoreGlobalCooldown   = true;
            spellParameters.CancelActiveTrade      = true;
            spellParameters.ClientRequestSource    = nameof(HousingPortalEntityScript);

            activator.CastSpell(HousingDialogSpellId, spellParameters);
            log.LogDebug("Housing portal {EntityGuid} cast housing dialog spell {SpellId} for player {PlayerGuid}.",
                owner?.Guid ?? 0u,
                HousingDialogSpellId,
                activator.Guid);
        }
    }
}
