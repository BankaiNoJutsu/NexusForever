using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Story;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Housing
{
    [ScriptFilterCreatureId(54400u, 54401u, 54403u, 54404u, 65296u, 65297u, 65298u, 65299u)]
    public class HousingIntroEntityScript : IWorldEntityScript, IOwnedScript<ICreatureEntity>
    {
        private static readonly IReadOnlyDictionary<uint, uint> creatureStoryPanelIds = new ReadOnlyDictionary<uint, uint>(
            new Dictionary<uint, uint>
            {
                [54400u] = 2291u, // Zen Pond, Thayd
                [65296u] = 2291u, // Zen Pond, Illium
                [54401u] = 2294u, // Windmill, Thayd
                [65297u] = 2294u, // Windmill, Illium
                [54403u] = 2292u, // Power Generator, Thayd
                [65298u] = 2292u, // Power Generator, Illium
                [54404u] = 2293u, // Storage Unit, Thayd
                [65299u] = 2293u  // Storage Unit, Illium
            });

        private readonly IStoryBuilder storyBuilder;
        private readonly ILogger<HousingIntroEntityScript> log;

        private ICreatureEntity owner;

        public static IReadOnlyDictionary<uint, uint> CreatureStoryPanelIds => creatureStoryPanelIds;

        public HousingIntroEntityScript(
            IStoryBuilder storyBuilder,
            ILogger<HousingIntroEntityScript> log)
        {
            this.storyBuilder = storyBuilder;
            this.log          = log;
        }

        public void OnLoad(ICreatureEntity owner)
        {
            this.owner = owner;
            log.LogDebug("Housing intro script loaded for entity {EntityGuid}: creature={CreatureId}.",
                owner.Guid,
                owner.CreatureId);
        }

        public void OnActivateSuccess(IPlayer activator)
        {
            if (activator == null || owner == null)
                return;

            if (!creatureStoryPanelIds.TryGetValue(owner.CreatureId, out uint storyPanelId))
            {
                log.LogWarning("Housing intro entity {EntityGuid} has no story panel mapping for creature {CreatureId}.",
                    owner.Guid,
                    owner.CreatureId);
                return;
            }

            storyBuilder.SendServerStoryPanelShow(activator, storyPanelId);
            log.LogDebug("Housing intro entity {EntityGuid} sent story panel {StoryPanelId} to player {PlayerGuid}.",
                owner.Guid,
                storyPanelId,
                activator.Guid);
        }
    }
}
