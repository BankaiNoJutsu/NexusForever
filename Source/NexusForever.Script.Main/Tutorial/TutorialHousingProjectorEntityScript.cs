using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Tutorial
{
    [ScriptFilterCreatureId(73741u)]
    public class TutorialHousingProjectorEntityScript : IWorldEntityScript, IOwnedScript<ISimpleCollidableEntity>
    {
        private const ushort TutorialWorldId = 3460;
        private const ushort ExileClaimQuestId = 10525;
        private const ushort DominionClaimQuestId = 10526;
        private const uint NexusVirtualityZoneId = 4965u;
        private const uint SkyplotEntryWorldLocationId = 51711u;

        private ISimpleCollidableEntity owner;

        private readonly IGameTableManager gameTableManager;
        private readonly ILogger<TutorialHousingProjectorEntityScript> log;

        public TutorialHousingProjectorEntityScript(
            ILogger<TutorialHousingProjectorEntityScript> log,
            IGameTableManager gameTableManager)
        {
            this.log              = log;
            this.gameTableManager = gameTableManager;
        }

        public void OnLoad(ISimpleCollidableEntity owner)
        {
            this.owner = owner;
            log.LogDebug("Starter tutorial housing projector script loaded for entity {EntityGuid}: creature={CreatureId}.",
                owner.Guid,
                owner.CreatureId);
        }

        public void OnActivateSuccess(IPlayer activator)
        {
            if (activator == null || owner?.Map?.Entry?.Id != TutorialWorldId || activator.Map?.Entry?.Id != TutorialWorldId)
                return;

            activator.TryRecoverStarterTutorialQuestProgression();

            QuestState? exileQuestState = activator.QuestManager.GetQuestState(ExileClaimQuestId);
            QuestState? dominionQuestState = activator.QuestManager.GetQuestState(DominionClaimQuestId);
            if (!CanUseProjector(exileQuestState) && !CanUseProjector(dominionQuestState))
            {
                log.LogDebug("Starter tutorial housing projector skipped activation for player {PlayerGuid}: exileQuestState={ExileQuestState}, dominionQuestState={DominionQuestState}.",
                    activator.Guid,
                    exileQuestState?.ToString() ?? "None",
                    dominionQuestState?.ToString() ?? "None");
                return;
            }

            WorldLocation2Entry destination = gameTableManager.WorldLocation2.GetEntry(SkyplotEntryWorldLocationId);
            if (destination == null)
            {
                log.LogWarning("Starter tutorial housing projector missing destination world location {WorldLocationId} for player {PlayerGuid}.",
                    SkyplotEntryWorldLocationId,
                    activator.Guid);
                return;
            }

            if (!activator.CanTeleport())
            {
                log.LogDebug("Starter tutorial housing projector skipped transport for player {PlayerGuid}: teleport currently unavailable.",
                    activator.Guid);
                return;
            }

            activator.QuestManager.ObjectiveUpdate(QuestObjectiveType.EnterZone, NexusVirtualityZoneId, 1u);
            activator.TeleportTo((ushort)destination.WorldId, destination.Position0, destination.Position1, destination.Position2);

            log.LogDebug("Starter tutorial housing projector transported player {PlayerGuid} to skyplot world location {WorldLocationId}: ({X}, {Y}, {Z}), exileQuestState={ExileQuestState}, dominionQuestState={DominionQuestState}.",
                activator.Guid,
                SkyplotEntryWorldLocationId,
                destination.Position0,
                destination.Position1,
                destination.Position2,
                exileQuestState?.ToString() ?? "None",
                dominionQuestState?.ToString() ?? "None");
        }

        private static bool CanUseProjector(QuestState? questState)
        {
            return questState is QuestState.Accepted or QuestState.Achieved or QuestState.Completed;
        }
    }
}
