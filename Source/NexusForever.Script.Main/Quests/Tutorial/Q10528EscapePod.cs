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
    /// Exile escape pod launch quest - final tutorial quest.
    /// Quest 10528. On completion, teleports player to the starter zone selected by terminal.
    /// Objectives: TalkTo NPC 73604, ActivateTargetGroup 14367 (2 terminals),
    /// ActivateTargetGroupChecklist 14380.
    /// </summary>
    [ScriptFilterOwnerId(10528u)]
    public class Q10528EscapePodQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private readonly record struct DepartureDestination(string Name, ushort WorldId, Vector3 Position);

        private const ushort EverstarGroveWorldId = 990;
        private const ushort NorthernWildsWorldId = 426;
        private static readonly Vector3 EverstarGroveArrival = new(-771.823f, -904.2852f, -2269.56f);
        private static readonly Vector3 NorthernWildsArrival = new(4086f, -683f, -5217f);

        private readonly ILogger<Q10528EscapePodQuestScript> log;
        private IQuest owner;

        public Q10528EscapePodQuestScript(ILogger<Q10528EscapePodQuestScript> log)
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
                ExileEverstarGroveDepartureTerminalCreatureId => new DepartureDestination("Everstar Grove", EverstarGroveWorldId, EverstarGroveArrival),
                ExileNorthernWildsDepartureTerminalCreatureId => new DepartureDestination("Northern Wilds", NorthernWildsWorldId, NorthernWildsArrival),
                _ => new DepartureDestination("Northern Wilds", NorthernWildsWorldId, NorthernWildsArrival)
            };
        }
    }
}
