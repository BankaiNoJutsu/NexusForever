using System.Numerics;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Reputation;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Tutorial
{
    [ScriptFilterOwnerId(3460)]
    public class TutorialMapScript : IMapScript, IOwnedScript<IBaseMap>
    {
        private sealed class TutorialMapInfo : IMapInfo
        {
            public required GameTable.Model.WorldEntry Entry { get; init; }
            public IMapLock MapLock { get; init; }
        }

        private sealed class TutorialMapPosition : IMapPosition
        {
            public required IMapInfo Info { get; init; }
            public Vector3 Position { get; set; }
        }

        private const ushort ExileMovementQuestId = 10513;
        private const ushort DominionMovementQuestId = 10521;
        private const ushort ExileHoverboardQuestId = 10527;
        private const ushort DominionHoverboardQuestId = 10532;

        private static readonly ushort[] followUpTutorialQuestIds = [10518, 10519, 10520, 10525, 10528, 10540];
        private static readonly uint[] tutorialWorldLocationIds = [51735u, 51736u, 51737u, 51703u, 51734u];

        private IBaseMap owner;
        private bool tutorialTriggersSpawned;

        #region Dependency Injection

        private readonly ICinematicFactory cinematicFactory;
        private readonly IEntityFactory entityFactory;
        private readonly IGlobalQuestManager globalQuestManager;

        public TutorialMapScript(
            ICinematicFactory cinematicFactory,
            IEntityFactory entityFactory,
            IGlobalQuestManager globalQuestManager)
        {
            this.cinematicFactory   = cinematicFactory;
            this.entityFactory      = entityFactory;
            this.globalQuestManager = globalQuestManager;
        }

        #endregion

        public void OnLoad(IBaseMap owner)
        {
            this.owner = owner;
            EnsureTutorialTriggers();
        }

        public void OnAddToMap(IGridEntity entity)
        {
            if (entity is not IPlayer player)
                return;

            EnsureTutorialQuests(player);
            player.CinematicManager.QueueCinematic(cinematicFactory.CreateCinematic<INoviceTutorialOnEnter>());
        }

        private void EnsureTutorialTriggers()
        {
            if (tutorialTriggersSpawned || owner == null)
                return;

            foreach (uint worldLocationId in tutorialWorldLocationIds)
            {
                IWorldLocationVolumeGridTriggerEntity trigger = entityFactory.CreateEntity<IWorldLocationVolumeGridTriggerEntity>();
                trigger.Initialise(worldLocationId, 0u);

                owner.EnqueueAdd(trigger, new TutorialMapPosition
                {
                    Info = new TutorialMapInfo
                    {
                        Entry = owner.Entry
                    },
                    Position = new Vector3(trigger.Entry.Position0, trigger.Entry.Position1, trigger.Entry.Position2)
                });
            }

            tutorialTriggersSpawned = true;
        }

        private void EnsureTutorialQuests(IPlayer player)
        {
            if (HasAnyQuestState(player, followUpTutorialQuestIds))
                return;

            (ushort movementQuestId, ushort hoverboardQuestId) = player.Faction1 switch
            {
                Faction.Exile    => (ExileMovementQuestId, ExileHoverboardQuestId),
                Faction.Dominion => (DominionMovementQuestId, DominionHoverboardQuestId),
                _                => ((ushort)0, (ushort)0)
            };

            if (movementQuestId == 0)
                return;

            GrantQuestIfMissing(player, movementQuestId);
            GrantQuestIfMissing(player, hoverboardQuestId);
        }

        private void GrantQuestIfMissing(IPlayer player, ushort questId)
        {
            if (player.QuestManager.GetQuestState(questId) != null)
                return;

            IQuestInfo questInfo = globalQuestManager.GetQuestInfo(questId);
            if (questInfo != null)
                player.QuestManager.QuestAdd(questInfo);
        }

        private static bool HasAnyQuestState(IPlayer player, IEnumerable<ushort> questIds)
        {
            return questIds.Any(questId => player.QuestManager.GetQuestState(questId) != null);
        }
    }
}
