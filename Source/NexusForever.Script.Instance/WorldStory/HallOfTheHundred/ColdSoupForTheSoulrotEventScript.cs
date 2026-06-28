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
    /// 678 ("Cold Soup for the Soulrot"). Final Soulless ownership remains
    /// blocked pending Creature2/object evidence.
    /// </summary>
    [ScriptFilterOwnerId(678)]
    public class ColdSoupForTheSoulrotEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private const uint MainPublicEventId = 666u;
        private const uint CanisterWorldLocationId = 48534u;
        private const uint CanisterObjectId = 7864u;

        private static readonly Vector3 CanisterPosition = new(27.9102f, -652.776f, -311.237f);

        private IPublicEvent publicEvent;
        private IMapInstance mapInstance;
        private uint canisterTriggerGuid;
        private bool revivePhaseStarted;
        private bool collectPhaseStarted;
        private bool disposePhaseStarted;
        private bool killSoullessPhaseStarted;

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            mapInstance = publicEvent.Map as IMapInstance
                ?? throw new InvalidOperationException("Cold Soup for the Soulrot requires a map instance.");

            publicEvent.SetPhase(PublicEventPhase.InvestigateSoulrotCave);
        }

        /// <summary>
        /// Invoked when a <see cref="IGridEntity"/> is added to the map the public event is on.
        /// </summary>
        public void OnAddToMap(IGridEntity entity)
        {
            if (entity is IWorldLocationVolumeGridTriggerEntity worldLocationEntity
                && worldLocationEntity.Entry.Id == CanisterWorldLocationId)
                canisterTriggerGuid = worldLocationEntity.Guid;
        }

        /// <summary>
        /// Invoked when a <see cref="IGridEntity"/> is removed from the map the public event is on.
        /// </summary>
        public void OnRemoveFromMap(IGridEntity entity)
        {
            if (entity is IWorldLocationVolumeGridTriggerEntity worldLocationEntity
                && worldLocationEntity.Entry.Id == CanisterWorldLocationId)
                canisterTriggerGuid = 0;
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.InvestigateSoulrotCave:
                    OnPhaseInvestigateSoulrotCave();
                    break;
                case PublicEventPhase.ReviveSoulrottedBody:
                    OnPhaseReviveSoulrottedBody();
                    break;
                case PublicEventPhase.CollectSoulrot:
                    OnPhaseCollectSoulrot();
                    break;
                case PublicEventPhase.DisposeSoulrotWaste:
                    OnPhaseDisposeSoulrotWaste();
                    break;
                case PublicEventPhase.KillSoulless:
                    OnPhaseKillSoulless();
                    break;
            }
        }

        private void OnPhaseInvestigateSoulrotCave()
        {
            // Objective 4391 is ParticipantsInTriggerVolume object 7864 at
            // WorldLocation2 48534. Main-event objective 5293 is the nearby
            // Soulrot Cave Script parent marker.
            mapInstance.PublicEventManager?.GetEvent(MainPublicEventId)
                ?.ActivateObjective(PublicEventObjective.InvestigateSoulrotCaveParent);
            publicEvent.ActivateObjective(PublicEventObjective.InvestigateLeakingSoulrotCanister);

            IWorldLocationVolumeGridTriggerEntity triggerEntity = publicEvent.CreateEntity<IWorldLocationVolumeGridTriggerEntity>();
            triggerEntity.Initialise(CanisterWorldLocationId, CanisterObjectId);
            mapInstance.EnqueueAdd(triggerEntity, new ScriptMapPosition
            {
                Info = new ScriptMapInfo
                {
                    Entry   = mapInstance.Entry,
                    MapLock = mapInstance.MapLock
                },
                Position = CanisterPosition
            });
        }

        private void OnPhaseReviveSoulrottedBody()
        {
            // Objectives 4392 and 4398 share text and TargetGroup 12240
            // (Creature2 67778). The entity script credits both mapped rows.
            publicEvent.ActivateObjective(PublicEventObjective.ReviveSoulrottedBody);
            publicEvent.ActivateObjective(PublicEventObjective.ReviveSoulrottedBodyScriptGate);
        }

        private void OnPhaseCollectSoulrot()
        {
            // Objective 4393 is a count-ten Script row whose text/reward pane
            // map the collection actors to TargetGroup 12248.
            publicEvent.ActivateObjective(PublicEventObjective.CollectSoulrot);
        }

        private void OnPhaseDisposeSoulrotWaste()
        {
            // Objective 4394 uses ActivateTargetGroup 14232, whose member is
            // Creature2 67797.
            publicEvent.ActivateObjective(PublicEventObjective.DisposeSoulrotWaste);
        }

        private void OnPhaseKillSoulless()
        {
            // Objective 4395 has no mapped object id, TargetGroup, or reviewed
            // Creature2 owner yet. Activate it, but keep credit blocked.
            publicEvent.ActivateObjective(PublicEventObjective.KillTheSoulless);
        }

        public void OnPublicEventObjectiveStatus(IPublicEventObjective objective)
        {
            if (objective.Status != PublicEventStatus.Succeeded)
                return;

            switch ((PublicEventObjective)objective.Entry.Id)
            {
                case PublicEventObjective.InvestigateLeakingSoulrotCanister:
                    RemoveTrigger(canisterTriggerGuid);
                    StartRevivePhase();
                    break;
                case PublicEventObjective.ReviveSoulrottedBody:
                case PublicEventObjective.ReviveSoulrottedBodyScriptGate:
                    StartCollectPhase();
                    break;
                case PublicEventObjective.CollectSoulrot:
                    StartDisposePhase();
                    break;
                case PublicEventObjective.DisposeSoulrotWaste:
                    StartKillSoullessPhase();
                    break;
                case PublicEventObjective.KillTheSoulless:
                    publicEvent.Finish(PublicEventTeam.PublicTeam);
                    break;
            }
        }

        private void StartRevivePhase()
        {
            if (revivePhaseStarted)
                return;

            revivePhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.ReviveSoulrottedBody);
        }

        private void StartCollectPhase()
        {
            if (collectPhaseStarted)
                return;

            collectPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.CollectSoulrot);
        }

        private void StartDisposePhase()
        {
            if (disposePhaseStarted)
                return;

            disposePhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.DisposeSoulrotWaste);
        }

        private void StartKillSoullessPhase()
        {
            if (killSoullessPhaseStarted)
                return;

            killSoullessPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.KillSoulless);
        }

        private void RemoveTrigger(uint guid)
        {
            IWorldLocationVolumeGridTriggerEntity triggerEntity = mapInstance.GetEntity<IWorldLocationVolumeGridTriggerEntity>(guid);
            triggerEntity?.RemoveFromMap();
        }
    }
}
