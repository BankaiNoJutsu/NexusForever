using System.Numerics;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.Tutorial
{
    /// <summary>
    /// Dominion escape pod launch quest — final tutorial quest.
    /// Quest 10530. On completion, teleports player to Northern Wilds (world 426).
    /// Objectives: TalkTo NPC 74778, ActivateTargetGroup 14387 (2 terminals),
    /// ActivateTargetGroupChecklist 14386.
    /// </summary>
    [ScriptFilterOwnerId(10530u)]
    public class Q10530EscapePodQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private const ushort NorthernWildsWorldId = 426;
        private static readonly Vector3 NorthernWildsArrival = new(4086f, -683f, -5217f);

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

            if (!owner.Player.CanTeleport())
            {
                log.LogWarning("Quest {QuestId} completed but player {CharacterId} cannot teleport to Northern Wilds — CanTeleport returned false.",
                    owner.Id, owner.Player.CharacterId);
                return;
            }

            owner.Player.TeleportTo(
                NorthernWildsWorldId,
                NorthernWildsArrival.X,
                NorthernWildsArrival.Y,
                NorthernWildsArrival.Z,
                reason: TeleportReason.Relocate);

            log.LogInformation("Quest {QuestId} completed — teleported player {CharacterId} to Northern Wilds (world {WorldId}, {X:F0},{Y:F0},{Z:F0}).",
                owner.Id, owner.Player.CharacterId, NorthernWildsWorldId,
                NorthernWildsArrival.X, NorthernWildsArrival.Y, NorthernWildsArrival.Z);
        }
    }
}
