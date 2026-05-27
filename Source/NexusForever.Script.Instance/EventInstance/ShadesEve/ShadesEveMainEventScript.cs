using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.EventInstances.ShadesEve
{
    [ScriptFilterOwnerId(597)]
    public class ShadesEveMainEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private const uint ChoosePathVoteId = 64u;
        private const uint DefaultVoteChoice = 1u;

        private readonly IGlobalQuestManager globalQuestManager;

        private IPublicEvent publicEvent;
        private IMapInstance mapInstance;

        public ShadesEveMainEventScript(
            IGlobalQuestManager globalQuestManager)
        {
            this.globalQuestManager = globalQuestManager;
        }

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            mapInstance = publicEvent.Map as IMapInstance
                ?? throw new InvalidOperationException("Shades Eve requires a map instance.");

            publicEvent.SetPhase(PublicEventPhase.FindTheFountain);
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.FindTheFountain:
                    publicEvent.ActivateObjective(PublicEventObjective.FindTheFountain, mapInstance.PlayerCount);
                    break;
                case PublicEventPhase.SpeakWithEttyWindsen:
                    BroadcastWipCommunicatorMessage(CommunicatorMessage.TheAngel5);
                    publicEvent.ActivateObjective(PublicEventObjective.SpeakWithEttyWindsen);
                    break;
                case PublicEventPhase.SpeakWithTheMayor:
                    publicEvent.ActivateObjective(PublicEventObjective.SpeakWithTheMayor);
                    break;
                case PublicEventPhase.TalkWithTheLocals:
                    publicEvent.ActivateObjective(PublicEventObjective.TalkWithTheLocals);
                    break;
            }
        }

        /// <summary>
        /// Invoked when the <see cref="IPublicEventObjective"/> status changes.
        /// </summary>
        public void OnPublicEventObjectiveStatus(IPublicEventObjective objective)
        {
            if (objective.Status != PublicEventStatus.Succeeded)
                return;

            switch ((PublicEventObjective)objective.Entry.Id)
            {
                case PublicEventObjective.FindTheFountain:
                    publicEvent.SetPhase(PublicEventPhase.SpeakWithEttyWindsen);
                    break;
                case PublicEventObjective.SpeakWithEttyWindsen:
                    publicEvent.SetPhase(PublicEventPhase.SpeakWithTheMayor);
                    break;
                case PublicEventObjective.SpeakWithTheMayor:
                    publicEvent.SetPhase(PublicEventPhase.TalkWithTheLocals);
                    break;
                case PublicEventObjective.TalkWithTheLocals:
                    publicEvent.StartVote(PublicEventTeam.PublicTeam, ChoosePathVoteId, DefaultVoteChoice);
                    break;
            }
        }

        /// <summary>
        /// Invoked when a cinematic for <see cref="IPlayer"/> has finished.
        /// </summary>
        public void OnCinematicFinish(IPlayer player, uint cinematicId)
        {
            if (publicEvent.Phase != (uint)PublicEventPhase.FindTheFountain)
                return;

            SendWipCommunicatorMessage(player, CommunicatorMessage.TheAngel1);
        }

        private void BroadcastWipCommunicatorMessage(CommunicatorMessage message)
        {
            // WIP-guessed from LaughingWS Instances-and-more. The branch pairs these Shade's Eve
            // communicator ids with phase/cinematic handoffs, but exact cinematic id gating,
            // town-gate timing, and vote follow-up remain blocked pending retail smoke proof.
            ICommunicatorMessage communicatorMessage = globalQuestManager.GetCommunicatorMessage(message);
            foreach (IPlayer player in mapInstance.GetPlayers())
                communicatorMessage?.Send(player.Session);
        }

        private void SendWipCommunicatorMessage(IPlayer player, CommunicatorMessage message)
        {
            // WIP-guessed from LaughingWS Instances-and-more; keep the cinematic follow-up
            // phase-gated until the exact retail cinematic id and replay timing are proven.
            ICommunicatorMessage communicatorMessage = globalQuestManager.GetCommunicatorMessage(message);
            communicatorMessage?.Send(player.Session);
        }
    }
}
