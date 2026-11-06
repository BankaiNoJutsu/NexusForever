using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Matching;
using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Game.Static.Matching;
using NexusForever.Game.Static.Reputation;
using NexusForever.Shared;

namespace NexusForever.Game.Matching.Queue
{
    public class MatchingQueueGroup : IMatchingQueueGroup
    {
        public Guid Guid { get; private set; }
        public Static.Matching.MatchType MatchType { get; private set; }

        public bool IsFinalised { get; private set; }

        /// <summary>
        /// Determines if the <see cref="IMatchingQueueGroup"/> is paused.
        /// </summary>
        /// <remarks>
        /// When paused, the <see cref="IMatchingQueueGroup"/> will not be matched.
        /// </remarks>
        public bool IsPaused
        {
            get => isPaused;
            set
            {
                isPaused = value;
                log.LogTrace($"Matching queue group {Guid}, Paused: {value}.");
            }
        }

        private bool isPaused;

        /// <summary>
        /// Determines if the <see cref="IMatchingQueueGroup"/> is from a solo queue join request.
        /// </summary>
        /// <remarks>
        /// This is only possible for Expeditions and is a UI option for players joining the queue.
        /// </remarks>
        public bool IsSolo { get; private set; }

        private readonly Dictionary<Guid, IMatchingQueueGroupTeam> teams = [];
        private readonly Dictionary<Faction, IMatchingQueueGroupTeam> factionTeams = [];

        private List<IMatchingMap> maps = [];

        #region Dependency Injection

        private readonly ILogger<MatchingQueueGroup> log;

        private readonly IMatchingDataManager matchingDataManager;
        private readonly IFactory<IMatchingQueueGroupTeam> matchingQueueGroupTeamFactory;

        public MatchingQueueGroup(
            ILogger<MatchingQueueGroup> log,
            IMatchingDataManager matchingDataManager,
            IFactory<IMatchingQueueGroupTeam> matchingQueueGroupTeamFactory)
        {
            this.log                           = log;

            this.matchingDataManager           = matchingDataManager;
            this.matchingQueueGroupTeamFactory = matchingQueueGroupTeamFactory;
        }

        #endregion

        /// <summary>
        /// Initialise <see cref="IMatchingQueueGroup"/> with supplied <see cref="Static.Matching.MatchType"/> and <see cref="Faction"/>.
        /// </summary>
        public void Initialise(IMatchingQueueProposal matchingQueueProposal)
        {
            if (Guid != Guid.Empty)
                throw new InvalidOperationException();

            Guid      = Guid.NewGuid();
            MatchType = matchingQueueProposal.MatchType;

            IsSolo = matchingQueueProposal.MatchingQueueFlags.HasFlag(MatchingQueueFlags.SoloMatch);
            Faction? faction = matchingDataManager.IsSingleFactionEnforced(MatchType) ? matchingQueueProposal.Faction : null;
            IMatchingQueueGroupTeam team = AddTeam(faction);
            if (matchingDataManager.IsPvPMatchType(MatchType))
                AddTeam(faction == null ? null : faction == Faction.Dominion ? Faction.Exile : Faction.Dominion);

            AdmitMatchingQueueProposal(matchingQueueProposal, team);

            log.LogTrace($"Initialised new matching queue group, Guid: {Guid}, MatchType: {MatchType}, Teams: {teams.Count}");
        }

        private IMatchingQueueGroupTeam AddTeam(Faction? faction)
        {
            IMatchingQueueGroupTeam matchingQueueGroupTeam = matchingQueueGroupTeamFactory.Resolve();
            matchingQueueGroupTeam.Initialise(faction);

            if (faction != null)
                factionTeams.Add(faction.Value, matchingQueueGroupTeam);

            teams.Add(matchingQueueGroupTeam.Guid, matchingQueueGroupTeam);
            log.LogTrace($"Added new team to matching queue group {Guid}, Faction: {faction}");

            return matchingQueueGroupTeam;
        }

        private IMatchingQueueGroupTeam GetTeam(Guid guid)
        {
            return teams.TryGetValue(guid, out IMatchingQueueGroupTeam team) ? team : null;
        }

        public IEnumerable<IMatchingQueueGroupTeam> GetTeams()
        {
            return teams.Values;
        }

        /// <summary>
        /// Return common <see cref="IMatchingMap"/>'s between all <see cref="IMatchingQueueGroupTeam"/>'s.
        /// </summary>
        /// <remarks>
        /// When a new <see cref="IMatchingQueueProposal"/> is added or removed, this cache is updated.
        /// </remarks>
        public List<IMatchingMap> GetMatchingMaps()
        {
            return maps;
        }

        /// <summary>
        /// Add <see cref="IMatchingQueueProposal"/> to <see cref="IMatchingQueueGroup"/>.
        /// </summary>
        public void AddMatchingQueueProposal(IMatchingQueueProposal matchingQueueProposal, IMatchingQueueGroupTeam matchingQueueGroupTeam)
        {
            if (IsSolo || IsFinalised)
                throw new InvalidOperationException();

            if (GetTeam(matchingQueueGroupTeam.Guid) == null)
                throw new InvalidOperationException();

            AdmitMatchingQueueProposal(matchingQueueProposal, matchingQueueGroupTeam);
        }

        private void AdmitMatchingQueueProposal(IMatchingQueueProposal proposal, IMatchingQueueGroupTeam team)
        {
            team.AddMatchingQueueProposal(proposal);
            UpdateMatchingMaps();
            proposal.SetMatchingQueueGroup(this);
            log.LogTrace($"Added matching queue proposal {proposal.Guid} to matching queue group {Guid}.");
        }

        /// <summary>
        /// Remove <see cref="IMatchingQueueProposal"/> from <see cref="IMatchingQueueGroup"/>, optionally with a <see cref="MatchingQueueResult"/>.
        /// </summary>
        public void RemoveMatchingQueueProposal(IMatchingQueueProposal matchingQueueProposal, MatchingQueueResult? leaveReason = MatchingQueueResult.Left)
        {
            DetachMatchingQueueProposal(matchingQueueProposal, leaveReason)?.Invoke();
        }

        public Action DetachMatchingQueueProposal(IMatchingQueueProposal matchingQueueProposal, MatchingQueueResult? leaveReason = MatchingQueueResult.Left)
        {
            if (!teams.Values.SelectMany(team => team.GetMembers())
                .Any(member => ReferenceEquals(member.MatchingQueueProposal, matchingQueueProposal)))
                return null;

            foreach (IMatchingQueueGroupTeam matchingQueueGroupTeam in GetTeams())
                matchingQueueGroupTeam.RemoveMatchingQueueProposal(matchingQueueProposal);

            UpdateMatchingMaps();

            if (maps.Count == 0)
            {
                IsFinalised = true;
                log.LogTrace($"Matching queue group {Guid} is empty, finalised.");
            }
            Action publish = matchingQueueProposal.DetachMatchingQueueGroup(leaveReason);
            log.LogTrace($"Removed matching queue proposal {matchingQueueProposal.Guid} from matching queue group {Guid}.");
            return publish;
        }

        private void UpdateMatchingMaps()
        {
            maps = teams.Values.SelectMany(team => team.GetMatchingMaps()).Distinct().ToList();
        }
    }
}
