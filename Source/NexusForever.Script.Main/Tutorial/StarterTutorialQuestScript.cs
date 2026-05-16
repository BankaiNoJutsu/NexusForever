using NexusForever.Game.Abstract.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Tutorial
{
    [ScriptFilterOwnerId(10513u, 10521u, 10527u, 10532u)]
    public class StarterTutorialQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private IQuest owner;
        private bool syncingHiddenOptionalObjectives;

        public void OnLoad(IQuest owner)
        {
            this.owner = owner;
            SyncHiddenOptionalObjectives();
        }

        void IQuestScript.OnObjectiveUpdate(IQuestObjective objective)
        {
            SyncHiddenOptionalObjectives();
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