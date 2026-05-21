using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.Paths
{
    /// <summary>
    /// Map script for Exile path hub (world 1658 — Tempest Refuge area).
    /// DB is fully populated for this world; no dynamic spawning needed.
    /// Logs zone readiness on load.
    /// </summary>
    [ScriptFilterOwnerId(1658)]
    public class ExilePathHubMapScript : IMapScript, IOwnedScript<IBaseMap>
    {
        private readonly ILogger<ExilePathHubMapScript> log;
        private IBaseMap owner;
        private bool initialised;

        public ExilePathHubMapScript(ILogger<ExilePathHubMapScript> log) => this.log = log;

        public void OnLoad(IBaseMap owner)
        {
            this.owner = owner;
            if (!initialised)
            {
                initialised = true;
                log.LogInformation("Exile path hub (world {WorldId}) loaded — DB entities in use, no dynamic spawns needed.", owner.Entry.Id);
            }
        }
        public void Update(double _) { }
        public void OnAddToMap(IGridEntity _) { }
        public void OnRemoveFromMap(IGridEntity _) { }
    }

    /// <summary>
    /// Map script for Exile path zone (world 2997 — Venombite Pass area).
    /// DB is fully populated for this world; no dynamic spawning needed.
    /// </summary>
    [ScriptFilterOwnerId(2997)]
    public class ExilePathZoneMapScript : IMapScript, IOwnedScript<IBaseMap>
    {
        private readonly ILogger<ExilePathZoneMapScript> log;
        private IBaseMap owner;
        private bool initialised;

        public ExilePathZoneMapScript(ILogger<ExilePathZoneMapScript> log) => this.log = log;

        public void OnLoad(IBaseMap owner)
        {
            this.owner = owner;
            if (!initialised)
            {
                initialised = true;
                log.LogInformation("Exile path zone (world {WorldId}) loaded — DB entities in use, no dynamic spawns needed.", owner.Entry.Id);
            }
        }
        public void Update(double _) { }
        public void OnAddToMap(IGridEntity _) { }
        public void OnRemoveFromMap(IGridEntity _) { }
    }

    /// <summary>
    /// Map script for Dominion path zone (world 2979 — Deradune area).
    /// DB is fully populated for this world; no dynamic spawning needed.
    /// </summary>
    [ScriptFilterOwnerId(2979)]
    public class DominionPathZoneMapScript : IMapScript, IOwnedScript<IBaseMap>
    {
        private readonly ILogger<DominionPathZoneMapScript> log;
        private IBaseMap owner;
        private bool initialised;

        public DominionPathZoneMapScript(ILogger<DominionPathZoneMapScript> log) => this.log = log;

        public void OnLoad(IBaseMap owner)
        {
            this.owner = owner;
            if (!initialised)
            {
                initialised = true;
                log.LogInformation("Dominion path zone (world {WorldId}) loaded — DB entities in use, no dynamic spawns needed.", owner.Entry.Id);
            }
        }
        public void Update(double _) { }
        public void OnAddToMap(IGridEntity _) { }
        public void OnRemoveFromMap(IGridEntity _) { }
    }
}
