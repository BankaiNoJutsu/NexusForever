using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Matching;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Matching;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Arena.TheCryoPlex
{
    /// <summary>
    /// WIP-guessed from LaughingWS Instances-and-more and current Slaughterdome
    /// behavior. Door release, death stats, and holocrypt resurrection are scoped
    /// compatibility scaffolds pending Cryo-Plex queue/match smoke.
    /// </summary>
    [ScriptFilterOwnerId(581)]
    public class TheCryoPlexEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private IPublicEvent publicEvent;

        private const double AutoResurrectDelaySeconds = 5d;

        private readonly List<uint> doorEntities = [];
        private readonly Dictionary<ulong, double> autoResurrectTimers = [];
        private readonly Dictionary<ulong, uint> deathCounts = [];

        #region Dependency Injection

        private readonly IMatchingDataManager matchingDataManager;
        private readonly IPlayerManager playerManager;

        public TheCryoPlexEventScript(
            IMatchingDataManager matchingDataManager,
            IPlayerManager playerManager)
        {
            this.matchingDataManager = matchingDataManager;
            this.playerManager       = playerManager;
        }

        #endregion

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            publicEvent.SetPhase(PublicEventPhase.Preperation);
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.Fight:
                    PhaseFight();
                    break;
                case PublicEventPhase.Finished:
                    PhaseFinished();
                    break;
            }
        }

        private void PhaseFight()
        {
            foreach (uint doorGuid in doorEntities)
            {
                IDoorEntity door = publicEvent?.Map.GetEntity<IDoorEntity>(doorGuid);
                door?.OpenDoor();
            }
        }

        private void PhaseFinished()
        {
        }

        /// <summary>
        /// Invoked when a PvP match <see cref="PvpGameState"/> changes on the same map the public event is on.
        /// </summary>
        public void OnMatchState(PvpGameState state)
        {
            switch (state)
            {
                case PvpGameState.InProgress:
                    publicEvent.SetPhase(PublicEventPhase.Fight);
                    break;
                case PvpGameState.Finished:
                    publicEvent.SetPhase(PublicEventPhase.Finished);
                    break;
            }
        }

        /// <summary>
        /// Invoked when a <see cref="IGridEntity"/> is added to the map the public event is on.
        /// </summary>
        public void OnAddToMap(IGridEntity entity)
        {
            if (entity is not IWorldEntity worldEntity)
                return;

            switch ((PublicEventCreature)worldEntity.CreatureId)
            {
                case PublicEventCreature.PvpForcefieldDoor:
                case PublicEventCreature.PvpForcefieldDoor2:
                    doorEntities.Add(worldEntity.Guid);
                    break;
            }
        }

        public void Update(double lastTick)
        {
            foreach ((ulong characterId, double timer) in autoResurrectTimers.ToArray())
            {
                IPlayer player = playerManager.GetPlayer(characterId);
                if (player == null || player.IsAlive)
                {
                    autoResurrectTimers.Remove(characterId);
                    continue;
                }

                double remaining = timer - lastTick;
                if (remaining > 0d)
                {
                    autoResurrectTimers[characterId] = remaining;
                    continue;
                }

                autoResurrectTimers.Remove(characterId);
                player.ResurrectionManager.Resurrect(ResurrectionType.Holocrypt);
            }
        }

        public void OnDeath(IUnitEntity entity)
        {
            if (entity is not IPlayer player)
                return;

            deathCounts.TryGetValue(player.CharacterId, out uint deaths);
            deaths++;
            deathCounts[player.CharacterId] = deaths;

            publicEvent.UpdateStat(player, PublicEventStat.Deaths, deaths);
            autoResurrectTimers[player.CharacterId] = AutoResurrectDelaySeconds;
        }

        public void OnResurrection(IPlayer player)
        {
            autoResurrectTimers.Remove(player.CharacterId);
            MoveToTeamSpawn(player);
        }

        private void MoveToTeamSpawn(IPlayer player)
        {
            if (publicEvent.Map is not IContentPvpMapInstance pvpMap || pvpMap.Match == null)
                return;

            IMatchTeam team = pvpMap.Match.GetTeam(player.Identity);
            if (team == null)
                return;

            IMapEntrance entrance = matchingDataManager.GetMapEntrance(publicEvent.Map.Entry.Id, (byte)team.Team);
            if (entrance == null)
                return;

            player.Rotation = entrance.Rotation;
            player.TeleportToLocal(entrance.Position, false);
        }
    }
}
