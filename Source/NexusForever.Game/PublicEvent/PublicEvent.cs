using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Static.Matching;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.PublicEvent;
using NexusForever.Script;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Collection;
using NexusForever.Shared;
using NexusForever.Shared.Game;

namespace NexusForever.Game.PublicEvent
{
    public class PublicEvent : IPublicEvent
    {
        public Guid Guid { get; } = Guid.NewGuid();
        public uint Id => template.Entry.Id;
        public bool IsFinalised { get; private set; }
        public uint Phase { get; private set; }
        public bool IsBusy { get; set; }
        public IReadOnlyList<uint> ChildEventIds => template?.ChildEventIds ?? [];

        /// <summary>
        /// Map the <see cref="IPublicEvent"/> is on.
        /// </summary>
        public IBaseMap Map { get; private set; }
        
        private IPublicEventManager publicEventManager;
        private IPublicEventTemplate template;

        private IScriptCollection scriptCollection;

        private readonly Dictionary<Static.PublicEvent.PublicEventTeam, IPublicEventTeam> teams = [];
        private readonly Dictionary<ulong, Membership> memberships = [];
        private sealed record Membership(IPlayer Owner, IPublicEventTeam Team, IPublicEventTeamMember Member);

        private double elapsedTimer;
        private UpdateTimer liveStatsTimer;
        private bool isFinishing;
        private bool CanChangeProgress => !IsFinalised && !isFinishing;

        #region Dependency Injection

        private readonly ILogger<PublicEvent> log;

        private readonly IScriptManager scriptManager;
        private readonly IFactory<IPublicEventTeam> teamFactory;
        private readonly IPublicEventEntityFactory entityFactory;

        public PublicEvent(
            ILogger<PublicEvent> log,
            IScriptManager scriptManager,
            IFactory<IPublicEventTeam> teamFactory,
            IPublicEventEntityFactory entityFactory)
        {
            this.log           = log;
            this.scriptManager = scriptManager;
            this.teamFactory   = teamFactory;
            this.entityFactory = entityFactory;
        }

        #endregion

        public void Dispose()
        {
            if (scriptCollection != null)
                scriptManager.Unload(scriptCollection);

            scriptCollection = null;
        }

        /// <summary>
        /// Invoked each world tick with the delta since the previous tick occurred.
        /// </summary>
        public void Update(double lastTick)
        {
            if (!CanChangeProgress)
                return;

            if (liveStatsTimer != null)
            {
                liveStatsTimer.Update(lastTick);
                if (liveStatsTimer.HasElapsed)
                {
                    SendServerPublicEventStatsUpdate();
                    liveStatsTimer.Reset();
                }
            }

            if (IsBusy || !CanChangeProgress)
                return;

            elapsedTimer += lastTick;

            foreach (IPublicEventTeam publicEventTeam in teams.Values)
            {
                if (!CanChangeProgress)
                    break;
                publicEventTeam.Update(lastTick);
            }
        }

        private ServerPublicEventStatsUpdate BuildStatsUpdate()
        {
            // WildStar64.exe 14007bde0 consumes this scoreboard/update shape after
            // ClientPublicEventRequestScoreboard (0x06FA); reward tiers remain gated
            // to ServerPublicEventEnd until retail delivery semantics are captured.
            return new ServerPublicEventStatsUpdate
            {
                PublicEventId = Id,
                TeamStats     = teams.Values
                    .Select(t => t.BuildTeamStats())
                    .ToList(),
                ParticipantStats = teams.Values
                    .SelectMany(t => t.BuildParticipantStats())
                    .ToList()
            };
        }

        private void SendServerPublicEventStatsUpdate()
        {
            ServerPublicEventStatsUpdate publicEventStatsUpdate = BuildStatsUpdate();

            Broadcast(publicEventStatsUpdate);
        }

        /// <summary>
        /// Initialise the <see cref="PublicEvent"/> with the supplied <see cref="IPublicEventManager"/>, <see cref="IPublicEventTemplate"/> and <see cref="IBaseMap"/>.
        /// </summary>
        public void Initialise(IPublicEventManager publicEventManager, IPublicEventTemplate template, IBaseMap map)
        {
            if (this.publicEventManager != null)
                throw new InvalidOperationException();

            this.publicEventManager = publicEventManager;
            this.template           = template;

            Map = map;

            foreach (PublicEventTeamEntry entry in template.Teams)
            {
                IPublicEventTeam team = teamFactory.Resolve();
                team.Initialise(this, template, entry);
                teams.Add(entry.Id, team);
            }

            entityFactory.Initialise(this);

            scriptCollection = scriptManager.InitialiseOwnedScripts<IPublicEvent>(this, Id);

            if (template.HasLiveStats())
                liveStatsTimer = new(TimeSpan.FromSeconds(5));

            log.LogTrace($"Public event {Guid} has been initialised.");
        }

        /// <summary>
        /// Return a collection of all <see cref="IPublicEventTeam"/>'s participating in the <see cref="IPublicEvent"/>.
        /// </summary>
        public IEnumerable<IPublicEventTeam> GetTeams()
        {
            return teams.Values;
        }

        /*public void SetBusy(bool busy)
        {
            IsBusy = busy;
        }*/

        /// <summary>
        /// Set the current phase of the <see cref="IPublicEvent"/>.
        /// </summary>
        /// <remarks>
        /// This will spawn any entities that are part of the phase.
        /// </remarks>
        public void SetPhase<T>(T phase) where T : Enum
        {
            SetPhase(phase.As<T, uint>());
        }

        /// <summary>
        /// Set the current phase of the <see cref="IPublicEvent"/>.
        /// </summary>
        /// <remarks>
        /// This will spawn any entities that are part of the phase.
        /// </remarks>
        public void SetPhase(uint phase)
        {
            if (!CanChangeProgress)
                return;

            Phase = phase;

            if (Map != null)
                entityFactory.SpawnEntities(phase);

            InvokeScriptCollection<IPublicEventScript>(e => e.OnPublicEventPhase(Phase));

            log.LogTrace($"Public event {Guid} has entered phase {Phase}.");
        }

        /// <summary>
        /// Add <see cref="IPlayer"/> to the <see cref="IPublicEvent"/> on <see cref="Static.PublicEvent.PublicEventTeam"/>.
        /// </summary>
        public void JoinEvent(IPlayer player, Static.PublicEvent.PublicEventTeam team)
        {
            if (!CanChangeProgress)
                return;

            /*if (player.Level < template.Entry.MinPlayerLevel)
                return;*/

            if (!teams.TryGetValue(team, out IPublicEventTeam publicEventTeam))
                return;

            if (memberships.ContainsKey(player.CharacterId))
                return;

            IPublicEventTeamMember member = publicEventTeam.JoinTeam(player);
            memberships.Add(player.CharacterId, new Membership(player, publicEventTeam, member));

            publicEventManager.AddEvent(player.CharacterId, this);

            SendServerPublicEventStart(player, member, publicEventTeam);

            if (CanChangeProgress && template.HasLiveStats())
                SendServerPublicEventStatsUpdate();

            log.LogTrace($"Character {player.CharacterId} has joined public event {Guid} on team {team}.");
        }

        private void SendServerPublicEventStart(IPlayer player, IPublicEventTeamMember member, IPublicEventTeam publicEventTeam)
        {
            IEnumerable<IPublicEventObjective> objectives = publicEventTeam.GetObjectives();

            // some objectives are shown to all teams, a good example is the Walatiki mask objectives for blue and red teams
            foreach (IPublicEventTeam otherPublicEventTeam in teams.Values.Where(t => t.Team != publicEventTeam.Team))
                objectives = objectives.Concat(otherPublicEventTeam.GetOtherObjectives());

            member.SendStart(new ServerPublicEventStart
            {
                PublicEventId     = Id,
                PublicEventTeamId = publicEventTeam.Team,
                Objectives        = objectives
                    .Select(o => o.Build())
                    .ToList(),
                Locations = template.Locations.ToList(),
                Busy = IsBusy
            }, () => CanChangeProgress && ReferenceEquals(GetCurrentMembership(player)?.Member, member));
        }

        /// <summary>
        /// Remove <see cref="IPlayer"/> from the <see cref="IPublicEvent"/> with the supplied reason.
        /// </summary>
        public void LeaveEvent(IPlayer player, PublicEventRemoveReason reason)
        {
            if (!memberships.TryGetValue(player.CharacterId, out Membership membership)
                || !ReferenceEquals(membership.Owner, player) || !RemoveCharacter(player.CharacterId))
                return;

            membership.Member.Send(new ServerPublicEventLeave { PublicEventId = Id, Reason = reason });

            log.LogTrace($"Character {player.CharacterId} left public event {Guid} with reason {reason}.");

            if (IsLastPlayer())
                Finish(null);
        }

        public bool RemoveCharacter(ulong characterId)
        {
            if (!memberships.TryGetValue(characterId, out Membership membership))
                return false;

            membership.Team.LeaveTeam(characterId);
            memberships.Remove(characterId);

            publicEventManager.RemoveEvent(characterId, this);

            return true;
        }

        private bool IsLastPlayer()
        {
            return teams.Values.All(t => !t.GetMembers().Any());
        }

        private Membership GetCurrentMembership(IPlayer player)
        {
            if (player == null || !ReferenceEquals(player.Map, Map)
                || !memberships.TryGetValue(player.CharacterId, out Membership membership)
                || !ReferenceEquals(membership.Owner, player)
                || !ReferenceEquals(membership.Member.GetCurrentPlayer(), player))
                return null;

            return membership;
        }

        /// <summary>
        /// Invoked when the <see cref="IPvpMatch"/> changes <see cref="PvpGameState"/> for the map the public event is part of.
        /// </summary>
        public void OnPvpMatchState(PvpGameState state)
        {
            InvokeScriptCollection<IPublicEventScript>(e => e.OnMatchState(state));
        }

        /// <summary>
        /// Update any objective for the team the <see cref="IPlayer"/> is on that meets the supplied type, objectId and count.
        /// </summary>
        public void UpdateObjective(IPlayer player, PublicEventObjectiveType type, uint objectId, int count)
        {
            if (GetCurrentMembership(player) is { } membership)
                UpdateObjective(membership.Team, type, objectId, count);
        }

        /// <summary>
        /// Update any objective for any team that meets the supplied type, objectId and count.
        /// </summary>
        public void UpdateObjective(PublicEventObjectiveType type, uint objectId, int count)
        {
            foreach (IPublicEventTeam publicEventTeam in teams.Values)
                UpdateObjective(publicEventTeam, type, objectId, count);
        }

        private void UpdateObjective(IPublicEventTeam publicEventTeam, PublicEventObjectiveType type, uint objectId, int count)
        {
            if (!CanChangeProgress)
                return;

            publicEventTeam.UpdateObjective(type, objectId, count);
            if (publicEventTeam.IsFinialised)
                Finish(publicEventTeam.Team);
        }

        /// <summary>
        /// Update a specific objective with the supplied objectiveId and count.
        /// </summary>
        public void UpdateObjective<T>(T objectiveId, int count) where T : Enum
        {
            UpdateObjective(objectiveId.As<T, uint>(), count);
        }

        /// <summary>
        /// Update a specific objective with the supplied objectiveId and count.
        /// </summary>
        public void UpdateObjective(uint objectiveId, int count)
        {
            if (!CanChangeProgress || !template.Objectives.TryGetValue(objectiveId, out PublicEventObjectiveEntry entry))
                return;

            if (!teams.TryGetValue(entry.PublicEventTeamId, out IPublicEventTeam publicEventTeam))
                return;

            publicEventTeam.UpdateObjective(entry.Id, count);
            if (publicEventTeam.IsFinialised)
                Finish(publicEventTeam.Team);
        }

        /// <summary>
        /// Activate objective with the supplied objectiveId and max count.
        /// </summary>
        /// <remarks>
        /// Max value is used for dynamic objectives where the max count can change during the event.
        /// </remarks>
        public void ActivateObjective<T>(T objectiveId, uint max = 0u) where T : Enum
        {
            ActivateObjective(objectiveId.As<T, uint>(), max);
        }

        /// <summary>
        /// Activate objective with the supplied objectiveId and max count.
        /// </summary>
        /// <remarks>
        /// Max value is used for dynamic objectives where the max count can change during the event.
        /// </remarks>
        public void ActivateObjective(uint objectiveId, uint max = 0u)
        {
            if (!CanChangeProgress || !template.Objectives.TryGetValue(objectiveId, out PublicEventObjectiveEntry entry))
                return;

            if (!teams.TryGetValue(entry.PublicEventTeamId, out IPublicEventTeam publicEventTeam))
                return;

            publicEventTeam.ActivateObjective(entry.Id, max);
        }

        /// <summary>
        /// Reset objective with the supplied objectiveId.
        /// </summary>
        public void ResetObjective<T>(T objectiveId) where T : Enum
        {
            ResetObjective(objectiveId.As<T, uint>());
        }

        /// <summary>
        /// Reset objective with the supplied objectiveId.
        /// </summary>
        public void ResetObjective(uint objectiveId)
        {
            if (!CanChangeProgress || !template.Objectives.TryGetValue(objectiveId, out PublicEventObjectiveEntry entry))
                return;

            if (!teams.TryGetValue(entry.PublicEventTeamId, out IPublicEventTeam publicEventTeam))
                return;

            publicEventTeam.ResetObjective(objectiveId);
        }

        /// <summary>
        /// Update stat for the <see cref="IPlayer"/> with the supplied <see cref="PublicEventStat"/> and value.
        /// </summary>
        public void UpdateStat(IPlayer player, PublicEventStat stat, uint value)
        {
            GetCurrentMembership(player)?.Team.UpdateStat(player.CharacterId, stat, value);
        }

        /// <summary>
        /// Increment stat for the <see cref="IPlayer"/> with the supplied <see cref="PublicEventStat"/> and value.
        /// </summary>
        public void IncrementStat(IPlayer player, PublicEventStat stat, uint value)
        {
            GetCurrentMembership(player)?.Team.IncrementStat(player.CharacterId, stat, value);
        }

        /// <summary>
        /// Update custom stat for the <see cref="IPlayer"/> with the supplied index and value.
        /// </summary>
        public void UpdateCustomStat(IPlayer player, uint index, uint value)
        {
            if (GetCurrentMembership(player) is not { } membership)
                return;

            if (!template.CustomStats.Any(s => s.StatIndex == index))
                return;

            membership.Team.UpdateCustomStat(player.CharacterId, index, value);
        }

        /// <summary>
        /// Send the current scoreboard stat snapshot to <see cref="IPlayer"/>.
        /// </summary>
        public void SendScoreboardUpdate(IPlayer player)
        {
            GetCurrentMembership(player)?.Member.Send(BuildStatsUpdate());
        }

        /// <summary>
        /// Start a vote for <see cref="Static.PublicEvent.PublicEventTeam"/> with the supplied voteId and default choice.
        /// </summary>
        /// <remarks>
        /// Default choice will be selected if no response is received within the vote duration.
        /// </remarks>
        public void StartVote(Static.PublicEvent.PublicEventTeam team, uint voteId, uint defaultChoice)
        {
            IPublicEventTeam publicEventTeam = GetTeam(team);
            if (publicEventTeam == null)
                return;

            publicEventTeam.StartVote(voteId, defaultChoice);
        }

        /// <summary>
        /// Respond to vote for the <see cref="IPlayer"/> with the supplied choice.
        /// </summary>
        public void RespondVote(IPlayer player, uint voteId, uint teamId, uint choice)
        {
            if (GetCurrentMembership(player) is not { } membership)
                return;

            // ClientPublicEventVote (0x06EE) carries event, vote, team, and choice.
            // Validate the mapped vote/team envelope here before touching vote state.
            if ((uint)membership.Team.Team != teamId)
                return;

            membership.Team.RespondVote(player, voteId, choice);
        }

        /// <summary>
        /// Finish <see cref="IPublicEvent"/> with the supplied <see cref="Static.PublicEvent.PublicEventTeam"/> as the winner.
        /// </summary>
        public void Finish(Static.PublicEvent.PublicEventTeam? winnerTeam)
        {
            if (!CanChangeProgress)
                return;

            isFinishing = true;
            try
            {
                FinishTeams(winnerTeam);
                entityFactory.RemoveEntities();
                IsFinalised = true;
            }
            finally
            {
                isFinishing = false;
            }

            IPublicEventTeam winner = null;
            if (winnerTeam.HasValue)
                winner = GetTeam(winnerTeam.Value);

            // A content map can finish its match here, which calls Finish on this event again.
            Map.OnPublicEventFinish(this, winner);

            log.LogTrace($"Public event {Guid} has finished.");
        }

        private void FinishTeams(Static.PublicEvent.PublicEventTeam? winnerTeam)
        {
            var teamStats = teams.Values
                .Select(t => t.BuildTeamStats())
                .ToList();

            var participantStats = teams.Values
                .SelectMany(t => t.BuildParticipantStats())
                .ToList();

            // we have to send each individual player a seperate message which contains their stats
            // the client uses this to determine if your stat performance ranked (top DPS, less deaths, etc...)
            var toRemove = new List<ulong>();
            foreach (IPublicEventTeam publicEventTeam in teams.Values)
            {
                foreach (IPublicEventTeamMember publicEventTeamMember in publicEventTeam.GetMembers())
                {
                    var message = new ServerPublicEventEnd
                    {
                        PublicEventId    = Id,
                        Reason           = publicEventTeam.Team == winnerTeam ? PublicEventRemoveReason.Success : PublicEventRemoveReason.Failure,
                        ElapsedTimeMs    = (uint)(elapsedTimer * 1000d),
                        PersonalStats            = publicEventTeamMember.BuildStats(),
                        TeamStats        = teamStats,
                        ParticipantStats = participantStats
                    };

                    publicEventTeamMember.Send(message);
                    toRemove.Add(publicEventTeamMember.CharacterId);
                }
            }

            foreach (ulong characterId in toRemove)
                RemoveCharacter(characterId);
        }

        /// <summary>
        /// Create a new <see cref="IGridEntity"/> that belongs to the <see cref="IPublicEvent"/>.
        /// </summary>
        /// <remarks>
        /// Entity will be automatically removed when the <see cref="IPublicEvent"/> is finished.
        /// </remarks>
        public T CreateEntity<T>() where T : IGridEntity
        {
            return entityFactory.CreateEntity<T>();
        }

        private void Broadcast(IWritable message)
        {
            foreach (IPublicEventTeam publicEventTeam in teams.Values)
                publicEventTeam.Broadcast(message);
        }

        private IPublicEventTeam GetTeam(Static.PublicEvent.PublicEventTeam publicEventTeam)
        {
            return teams.TryGetValue(publicEventTeam, out IPublicEventTeam team) ? team : null;
        }

        /// <summary>
        /// Invoke <see cref="Action{T}"/> against <see cref="IPublicEvent"/> script collection.
        /// </summary>
        public void InvokeScriptCollection<T>(Action<T> action)
        {
            scriptCollection?.Invoke(action);
        }
    }
}
