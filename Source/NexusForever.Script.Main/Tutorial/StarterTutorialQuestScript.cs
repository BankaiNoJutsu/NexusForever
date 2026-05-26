using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Main.Tutorial
{
    [ScriptFilterOwnerId(10513u, 10521u, 10527u, 10532u)]
    public class StarterTutorialQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private const uint HoverboardSprintVisualSpellId = 82298u;

        private static readonly uint[] hoverboardProjectorObjectiveIds = [21324u, 21354u];

        private readonly IGlobalQuestManager globalQuestManager;
        private readonly IFactory<ISpellParameters> spellParametersFactory;
        private IQuest owner;
        private TutorialCommunicatorSequence communicatorSequence;
        private bool syncingHiddenOptionalObjectives;

        public StarterTutorialQuestScript(IGlobalQuestManager globalQuestManager, IFactory<ISpellParameters> spellParametersFactory)
        {
            this.globalQuestManager     = globalQuestManager;
            this.spellParametersFactory = spellParametersFactory;
        }

        public void OnLoad(IQuest owner)
        {
            this.owner = owner;
            communicatorSequence = new TutorialCommunicatorSequence(owner, globalQuestManager);
            SyncHiddenOptionalObjectives();
        }

        void IQuestScript.OnObjectiveUpdate(IQuestObjective objective)
        {
            SyncHiddenOptionalObjectives();
            CastHoverboardTrailEffect(objective);
            SendHoverboardCheckpointCommunicator(objective);
        }

        private void CastHoverboardTrailEffect(IQuestObjective objective)
        {
            if (!ShouldCastPlayerSpell(objective, hoverboardProjectorObjectiveIds))
                return;

            CastPlayerSpell(HoverboardSprintVisualSpellId);
        }

        private bool ShouldCastPlayerSpell(IQuestObjective objective, IReadOnlyCollection<uint> objectiveIds)
        {
            return owner?.Player != null
                && objectiveIds.Contains(objective.ObjectiveInfo.Id)
                && objective.IsComplete();
        }

        private void CastPlayerSpell(uint spellId)
        {
            ISpellParameters spellParameters = spellParametersFactory.Resolve();
            spellParameters.PrimaryTargetId        = owner.Player.Guid;
            spellParameters.UserInitiatedSpellCast = false;
            spellParameters.IgnoreGlobalCooldown   = true;
            spellParameters.CancelActiveTrade      = true;
            spellParameters.ClientRequestSource    = nameof(StarterTutorialQuestScript);

            owner.Player.CastSpell(spellId, spellParameters);
        }

        private void SendHoverboardCheckpointCommunicator(IQuestObjective objective)
        {
            switch (owner.Id)
            {
                case 10527:
                    communicatorSequence.TrySendWhenComplete(objective, 21324u, 7981u);
                    communicatorSequence.TrySendWhenComplete(objective, 21323u, 8025u);
                    break;
                case 10532:
                    communicatorSequence.TrySendWhenComplete(objective, 21354u, 7997u);
                    communicatorSequence.TrySendWhenComplete(objective, 21355u, 8000u);
                    break;
            }
        }

        private void SyncHiddenOptionalObjectives()
        {
            if (owner == null || syncingHiddenOptionalObjectives)
                return;

            syncingHiddenOptionalObjectives = true;
            try
            {
                bool updatedObjective;
                do
                {
                    updatedObjective = false;

                    foreach (IQuestObjective objective in owner
                        .Where(o => o.ObjectiveInfo.IsOptional() && o.ObjectiveInfo.IsHidden() && !o.IsComplete())
                        .ToList())
                    {
                        uint previousProgress = objective.Progress;
                        owner.ObjectiveUpdate(objective.ObjectiveInfo.Id, Math.Max(1u, objective.ObjectiveInfo.Entry.Count));

                        if (objective.Progress != previousProgress)
                        {
                            updatedObjective = true;
                            break;
                        }
                    }
                }
                while (updatedObjective);
            }
            finally
            {
                syncingHiddenOptionalObjectives = false;
            }
        }
    }
}
