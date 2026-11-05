using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.GameTable;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Adventure.TheHycrestInsurrection
{
    /// <summary>Build16042 vote45 maps the first choice to PE420/421/422. Later missions remain pending.</summary>
    [ScriptFilterOwnerId(419)]
    public class TheHycrestInsurrectionEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private const uint MissionVoteId = 45;
        private static readonly uint[] MissionIds = [420, 421, 422];
        private static readonly uint[] MissionLabels = [432878, 432879, 432880];
        private readonly IGameTableManager tables;
        private IPublicEvent publicEvent;
        private IPublicEvent mission;
        private bool voteStarted;
        private bool voteResolved;
        private bool parentStopped;
        private uint pendingMission;
        private double retryTime;

        public TheHycrestInsurrectionEventScript(IGameTableManager tables = null) => this.tables = tables;

        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            publicEvent.SetPhase(PublicEventPhase.LeadTheHycrestRebelsToVictory);
        }

        public void OnPublicEventPhase(uint phase)
        {
            if ((PublicEventPhase)phase == PublicEventPhase.LeadTheHycrestRebelsToVictory)
                publicEvent.ActivateObjective(PublicEventObjective.LeadTheHycrestRebelsToVictory);
        }

        public void OnPublicEventObjectiveStatus(IPublicEventObjective objective)
        {
            if (objective.Status == PublicEventStatus.Succeeded
                && (PublicEventObjective)objective.Entry.Id == PublicEventObjective.LeadTheHycrestRebelsToVictory)
                publicEvent.Finish(PublicEventTeam.PublicTeam);
        }

        public void OnIntroComplete()
        {
            if (voteStarted || parentStopped || publicEvent.IsFinalised || !HasMappedMissionChoices()) return;
            voteStarted = true;
            publicEvent.StartVote(PublicEventTeam.PublicTeam, MissionVoteId, 0);
        }

        private bool HasMappedMissionChoices()
        {
            if (publicEvent.Id != 419 || publicEvent.Map?.Entry?.Id != 1149) return false;
            var vote = tables?.PublicEventVote?.GetEntry(MissionVoteId);
            if (vote?.LocalizedTextIdOption == null || vote.LocalizedTextIdOption.Length < 3
                || vote.LocalizedTextIdOption.Take(3).Any(id => id == 0)
                || vote.LocalizedTextIdOption.Skip(3).Any(id => id != 0)
                || vote.LocalizedTextIdLabel == null || vote.LocalizedTextIdLabel.Length < 3
                || !vote.LocalizedTextIdLabel.Take(3).SequenceEqual(MissionLabels) || vote.DurationMS == 0) return false;
            return MissionIds.All(id => tables.PublicEvent?.GetEntry(id) is { WorldId: 1149, PublicEventIdParent: 419 });
        }

        public void OnVoteFinished(uint voteId, uint winner)
        {
            if (!voteStarted || voteResolved || parentStopped || publicEvent.IsFinalised
                || voteId != MissionVoteId || winner >= MissionIds.Length) return;
            voteResolved = true;
            pendingMission = MissionIds[winner];
        }

        /// <summary>The map calls this before manager enumeration; vote callbacks cannot create events there.</summary>
        public void Update(double lastTick)
        {
            if (pendingMission == 0 || mission != null
                || publicEvent.Map is not IMapInstance map || !CanRunMission(map)) return;
            retryTime -= lastTick;
            if (retryTime > 0) return;
            retryTime = 1;
            var selected = map.PublicEventManager.GetEvent(pendingMission) ?? map.PublicEventManager.CreateEvent(pendingMission);
            if (selected == null || selected.IsFinalised) return;
            if (!CanRunMission(map))
            {
                selected.Finish(null);
                return;
            }

            mission = selected;
            pendingMission = 0;
            foreach (IPlayer player in map.GetPlayers().ToArray())
                OnAddToMap(player);
        }

        private bool CanRunMission(IMapInstance map) => !parentStopped && !publicEvent.IsFinalised && !map.UnloadStatus.HasValue;

        public void OnAddToMap(IGridEntity entity)
        {
            if (entity is IPlayer player && mission?.IsFinalised == false
                && publicEvent.Map is IMapInstance map && CanRunMission(map) && ReferenceEquals(player.Map, map))
                mission.JoinEvent(player, PublicEventTeam.PublicTeam);
        }

        public void OnParentFinished()
        {
            parentStopped = true;
            pendingMission = 0;
            if (mission != null && !mission.IsFinalised) mission.Finish(null);
        }
    }
}
