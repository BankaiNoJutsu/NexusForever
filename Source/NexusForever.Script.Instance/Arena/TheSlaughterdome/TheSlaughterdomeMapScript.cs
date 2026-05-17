using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Matching;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Static.Matching;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Arena.TheSlaughterdome
{
    [ScriptFilterOwnerId(1535)]
    public class TheSlaughterdomeMapScript : EventBasePvpContentMapScript
    {
        public override uint PublicEventId => 205u;
        public override uint PublicSubEventId => 209u;

        #region Dependency Injection

        private readonly IMatchingDataManager matchingDataManager;
        private readonly IPlayerManager playerManager;

        public TheSlaughterdomeMapScript(
            IMatchingDataManager matchingDataManager,
            IPlayerManager playerManager)
        {
            this.matchingDataManager = matchingDataManager;
            this.playerManager = playerManager;
        }

        #endregion

        /// <summary>
        /// Invoked when <see cref="IGridEntity"/> is removed from map.
        /// </summary>
        public void OnRemoveFromMap(IGridEntity entity)
        {
            if (entity is not IPlayer player)
                return;

            if (player.ControlGuid != player.Guid)
                player.SetControl(player);
        }

        /// <summary>
        /// Invoked when the <see cref="IPvpMatch"/> for the map finishes.
        /// </summary>
        public override void OnPvpMatchFinish(MatchWinner matchWinner, MatchEndReason matchEndReason)
        {
            base.OnPvpMatchFinish(matchWinner, matchEndReason);

            if (map.Match == null)
                return;

            foreach (IMatchTeam matchTeam in map.Match.GetTeams())
            {
                IMapEntrance entrance = matchingDataManager.GetMapEntrance(map.Entry.Id, (byte)matchTeam.Team);
                if (entrance == null)
                    continue;

                foreach (IMatchTeamMember matchTeamMember in matchTeam.GetMembers())
                {
                    IPlayer player = playerManager.GetPlayer(matchTeamMember.Identity);
                    if (player == null || player.Map != map)
                        continue;

                    player.Rotation = entrance.Rotation;
                    player.TeleportToLocal(entrance.Position, false);
                }
            }
        }
    }
}
