using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Tutorial;
using NexusForever.Script.Main.Tutorial;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.Tutorial
{
    /// <summary>
    /// Dominion escape pod launch quest - final tutorial quest.
    /// Quest 10530. On completion, teleports player to the starter zone selected by terminal.
    /// Objectives: TalkTo NPC 74778, ActivateTargetGroup 14387 (2 terminals),
    /// ActivateTargetGroupChecklist 14386.
    /// </summary>
    [ScriptFilterOwnerId(10530u)]
    public class Q10530EscapePodQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private readonly ILogger<Q10530EscapePodQuestScript> log;
        private readonly IGlobalQuestManager globalQuestManager;
        private IQuest owner;
        private TutorialCommunicatorSequence communicatorSequence;

        public Q10530EscapePodQuestScript(
            ILogger<Q10530EscapePodQuestScript> log,
            IGlobalQuestManager globalQuestManager)
        {
            this.log                = log;
            this.globalQuestManager = globalQuestManager;
        }

        public void OnLoad(IQuest owner)
        {
            this.owner = owner;
            communicatorSequence = new TutorialCommunicatorSequence(owner, globalQuestManager);
            log.LogDebug("Loaded quest {QuestId} for character {CharacterId}: faction={Faction}, state={QuestState}.",
                owner.Id, owner.Player.CharacterId, owner.Player.Faction1, owner.State);
        }

        public void OnObjectiveUpdate(IQuestObjective objective)
        {
            communicatorSequence.TrySendWhenComplete(objective, 21347u, 8064u);
        }

        public void OnQuestStateChange(QuestState newState, QuestState oldState)
        {
            log.LogDebug("Quest {QuestId} state changed for character {CharacterId}: {OldState} -> {NewState}.",
                owner.Id, owner.Player.CharacterId, oldState, newState);

            if (newState != QuestState.Completed)
                return;

            StarterTutorialDepartureDestination destination = StarterTutorialDepartureRouting.ResolveDominionDestination(owner.Player.StarterTutorialDepartureTerminalCreatureId);
            if (!owner.Player.CanTeleport())
            {
                log.LogWarning("Quest {QuestId} completed but player {CharacterId} cannot teleport to {DestinationName} - CanTeleport returned false.",
                    owner.Id, owner.Player.CharacterId, destination.Name);
                return;
            }

            owner.Player.TeleportTo(
                destination.WorldId,
                destination.Position.X,
                destination.Position.Y,
                destination.Position.Z,
                reason: TeleportReason.Relocate);

            GrantWelcomeQuest(destination);

            log.LogInformation("Quest {QuestId} completed - teleported player {CharacterId} to {DestinationName} (world {WorldId}, {X:F0},{Y:F0},{Z:F0}) from terminal {TerminalCreatureId}.",
                owner.Id, owner.Player.CharacterId, destination.Name, destination.WorldId,
                destination.Position.X, destination.Position.Y, destination.Position.Z,
                owner.Player.StarterTutorialDepartureTerminalCreatureId ?? 0u);
        }

        private void GrantWelcomeQuest(StarterTutorialDepartureDestination destination)
        {
            if (owner.Player.QuestManager.GetQuestState(destination.WelcomeQuestId) != null)
                return;

            IQuestInfo questInfo = globalQuestManager.GetQuestInfo(destination.WelcomeQuestId);
            if (questInfo == null)
            {
                log.LogWarning("Welcome quest {WelcomeQuestId} info missing for character {CharacterId} after Rider's Reef departure to {DestinationName}.",
                    destination.WelcomeQuestId, owner.Player.CharacterId, destination.Name);
                return;
            }

            owner.Player.QuestManager.QuestAdd(questInfo);
            log.LogDebug("Granted welcome quest {WelcomeQuestId} to character {CharacterId} after Rider's Reef departure to {DestinationName}.",
                destination.WelcomeQuestId, owner.Player.CharacterId, destination.Name);
        }
    }
}
