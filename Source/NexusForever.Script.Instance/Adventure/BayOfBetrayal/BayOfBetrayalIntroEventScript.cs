using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Adventure.BayOfBetrayal
{
    /// <summary>
    /// The Truth Will Set You Free intro for Bay of Betrayal. Build 16042
    /// objective rows map the opening route as talk to Herald Anku'mar, listen
    /// to his speech, then protect him.
    /// </summary>
    [ScriptFilterOwnerId(672)]
    public class BayOfBetrayalIntroEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private IPublicEvent publicEvent;
        private bool speechPhaseStarted;
        private bool protectPhaseStarted;

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            publicEvent.SetPhase(PublicEventPhase.TalkToHeraldAnkumar);
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.TalkToHeraldAnkumar:
                    publicEvent.ActivateObjective(PublicEventObjective.TalkToHeraldAnkumar);
                    break;
                case PublicEventPhase.ListenToHeraldAnkumarSpeech:
                    publicEvent.ActivateObjective(PublicEventObjective.ListenToHeraldAnkumarSpeech);
                    publicEvent.UpdateObjective(PublicEventObjective.ListenToHeraldAnkumarSpeech, 1);
                    break;
                case PublicEventPhase.ProtectHeraldAnkumar:
                    publicEvent.ActivateObjective(PublicEventObjective.ProtectHeraldAnkumar);
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
                case PublicEventObjective.TalkToHeraldAnkumar:
                    StartSpeechPhase();
                    break;
                case PublicEventObjective.ListenToHeraldAnkumarSpeech:
                    StartProtectPhase();
                    break;
                case PublicEventObjective.ProtectHeraldAnkumar:
                    publicEvent.Finish(PublicEventTeam.PublicTeam);
                    break;
            }
        }

        private void StartSpeechPhase()
        {
            if (speechPhaseStarted)
                return;

            speechPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.ListenToHeraldAnkumarSpeech);
        }

        private void StartProtectPhase()
        {
            if (protectPhaseStarted)
                return;

            protectPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.ProtectHeraldAnkumar);
        }
    }
}
