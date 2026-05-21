using System.Numerics;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;
using static NexusForever.Game.Static.Tutorial.StarterTutorialDefinition;

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
        private readonly record struct DepartureDestination(string Name, ushort WorldId, Vector3 Position);

        private const ushort CrimsonIsleWorldId = 870;
        private const ushort LevianBayWorldId = 1387;
        private static readonly Vector3 CrimsonIsleArrival = new(-8261.3984f, -995.471f, -242.3648f);
        private static readonly Vector3 LevianBayArrival = new(-3835.341f, -980.2174f, -6050.524f);

        private readonly ILogger<Q10530EscapePodQuestScript> log;
        private IQuest owner;

        public Q10530EscapePodQuestScript(ILogger<Q10530EscapePodQuestScript> log)
        {
            this.log = log;
        }

        public void OnLoad(IQuest owner)
        {
            this.owner = owner;
            log.LogDebug("Loaded quest {QuestId} for character {CharacterId}: faction={Faction}, state={QuestState}.",
                owner.Id, owner.Player.CharacterId, owner.Player.Faction1, owner.State);
        }

        public void OnQuestStateChange(QuestState newState, QuestState oldState)
        {
            log.LogDebug("Quest {QuestId} state changed for character {CharacterId}: {OldState} -> {NewState}.",
                owner.Id, owner.Player.CharacterId, oldState, newState);

            if (newState != QuestState.Completed)
                return;

            DepartureDestination destination = ResolveDepartureDestination(owner.Player.StarterTutorialDepartureTerminalCreatureId);
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

            log.LogInformation("Quest {QuestId} completed - teleported player {CharacterId} to {DestinationName} (world {WorldId}, {X:F0},{Y:F0},{Z:F0}) from terminal {TerminalCreatureId}.",
                owner.Id, owner.Player.CharacterId, destination.Name, destination.WorldId,
                destination.Position.X, destination.Position.Y, destination.Position.Z,
                owner.Player.StarterTutorialDepartureTerminalCreatureId ?? 0u);
        }

        private static DepartureDestination ResolveDepartureDestination(uint? terminalCreatureId)
        {
            return terminalCreatureId switch
            {
                DominionCrimsonIsleDepartureTerminalCreatureId => new DepartureDestination("Crimson Isle", CrimsonIsleWorldId, CrimsonIsleArrival),
                DominionLevianBayDepartureTerminalCreatureId => new DepartureDestination("Levian Bay", LevianBayWorldId, LevianBayArrival),
                _ => new DepartureDestination("Crimson Isle", CrimsonIsleWorldId, CrimsonIsleArrival)
            };
        }
    }
}
