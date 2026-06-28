using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.WorldStory.HallOfTheHundred
{
    /// <summary>
    /// Restores the proved Hall of the Hundred side-event gates for public event
    /// 677 ("Cold and Hungry"). Exact carcass placement and chase timing remain
    /// blocked pending client smoke.
    /// </summary>
    [ScriptFilterOwnerId(677)]
    public class ColdAndHungryEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private const uint MainPublicEventId = 666u;
        private const uint LeaveCaveWorldLocationId = 48428u;
        private const uint LeaveCaveObjectId = 7855u;

        private static readonly Vector3 LeaveCavePosition = new(-261.565f, -666.126f, -869.018f);

        private IPublicEvent publicEvent;
        private IMapInstance mapInstance;
        private uint leaveCaveTriggerGuid;
        private bool escapePhaseStarted;
        private bool defeatYetiPhaseStarted;

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            mapInstance = publicEvent.Map as IMapInstance
                ?? throw new InvalidOperationException("Cold and Hungry requires a map instance.");

            publicEvent.SetPhase(PublicEventPhase.ExploreYetiCave);
        }

        /// <summary>
        /// Invoked when a <see cref="IGridEntity"/> is added to the map the public event is on.
        /// </summary>
        public void OnAddToMap(IGridEntity entity)
        {
            if (entity is IWorldLocationVolumeGridTriggerEntity worldLocationEntity
                && worldLocationEntity.Entry.Id == LeaveCaveWorldLocationId)
                leaveCaveTriggerGuid = worldLocationEntity.Guid;
        }

        /// <summary>
        /// Invoked when a <see cref="IGridEntity"/> is removed from the map the public event is on.
        /// </summary>
        public void OnRemoveFromMap(IGridEntity entity)
        {
            if (entity is IWorldLocationVolumeGridTriggerEntity worldLocationEntity
                && worldLocationEntity.Entry.Id == LeaveCaveWorldLocationId)
                leaveCaveTriggerGuid = 0;
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.ExploreYetiCave:
                    OnPhaseExploreYetiCave();
                    break;
                case PublicEventPhase.EscapeYeti:
                    OnPhaseEscapeYeti();
                    break;
                case PublicEventPhase.DefeatYeti:
                    OnPhaseDefeatYeti();
                    break;
            }
        }

        private void OnPhaseExploreYetiCave()
        {
            // Objective 4378 points at TargetGroup 12213 / Creature2 67659 in
            // the same cave volume, but the exact carcass placements are not
            // proved yet. Activate the mapped row and leave credit blocked.
            mapInstance.PublicEventManager?.GetEvent(MainPublicEventId)
                ?.ActivateObjective(PublicEventObjective.ExploreYetiCaveParent);
            publicEvent.ActivateObjective(PublicEventObjective.InvestigateFrozenCarcasses);
            publicEvent.ActivateObjective(PublicEventObjective.LeaveYetiCave);

            IWorldLocationVolumeGridTriggerEntity triggerEntity = publicEvent.CreateEntity<IWorldLocationVolumeGridTriggerEntity>();
            triggerEntity.Initialise(LeaveCaveWorldLocationId, LeaveCaveObjectId);
            mapInstance.EnqueueAdd(triggerEntity, new ScriptMapPosition
            {
                Info = new ScriptMapInfo
                {
                    Entry   = mapInstance.Entry,
                    MapLock = mapInstance.MapLock
                },
                Position = LeaveCavePosition
            });
        }

        private void OnPhaseEscapeYeti()
        {
            // Objective 4380 is a Script row at WorldLocation2 48427. The exact
            // chase timer/avoidance mechanics are not mapped, so the proved gate
            // is credited to allow the death-credit objective to run.
            publicEvent.ActivateObjective(PublicEventObjective.EscapeYeti);
            publicEvent.UpdateObjective(PublicEventObjective.EscapeYeti, 1);
        }

        private void OnPhaseDefeatYeti()
        {
            // Objective 4381 names Creature2 67657 in the same escape volume.
            // ColdAndHungryYetiEntityScript supplies the direct death credit.
            publicEvent.ActivateObjective(PublicEventObjective.DefeatYeti);
        }

        public void OnPublicEventObjectiveStatus(IPublicEventObjective objective)
        {
            if (objective.Status != PublicEventStatus.Succeeded)
                return;

            switch ((PublicEventObjective)objective.Entry.Id)
            {
                case PublicEventObjective.LeaveYetiCave:
                    RemoveTrigger(leaveCaveTriggerGuid);
                    StartEscapePhase();
                    break;
                case PublicEventObjective.EscapeYeti:
                    StartDefeatYetiPhase();
                    break;
                case PublicEventObjective.DefeatYeti:
                    publicEvent.Finish(PublicEventTeam.PublicTeam);
                    break;
            }
        }

        private void StartEscapePhase()
        {
            if (escapePhaseStarted)
                return;

            escapePhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.EscapeYeti);
        }

        private void StartDefeatYetiPhase()
        {
            if (defeatYetiPhaseStarted)
                return;

            defeatYetiPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.DefeatYeti);
        }

        private void RemoveTrigger(uint guid)
        {
            IWorldLocationVolumeGridTriggerEntity triggerEntity = mapInstance.GetEntity<IWorldLocationVolumeGridTriggerEntity>(guid);
            triggerEntity?.RemoveFromMap();
        }
    }
}
