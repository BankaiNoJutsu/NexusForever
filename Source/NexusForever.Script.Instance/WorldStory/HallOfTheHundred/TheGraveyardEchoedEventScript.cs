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
    /// Restores Hall of the Hundred side public event 696 ("The Graveyard Echoed")
    /// from build 16042 PublicEventObjective/TargetGroup/WorldLocation2 rows.
    /// </summary>
    [ScriptFilterOwnerId(696)]
    public class TheGraveyardEchoedEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private const uint MainPublicEventId = 666u;
        private const uint InvestigateWorldLocationId = 48675u;
        private const uint InvestigateObjectId = 7895u;

        private static readonly Vector3 InvestigatePosition = new(-714.623f, -684.77f, -828.793f);

        private IPublicEvent publicEvent;
        private IMapInstance mapInstance;
        private uint investigateTriggerGuid;
        private bool incensePhaseStarted;
        private bool altarPhaseStarted;
        private bool primalEchoesPhaseStarted;
        private bool primalWraithPhaseStarted;

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            mapInstance = publicEvent.Map as IMapInstance
                ?? throw new InvalidOperationException("The Graveyard Echoed requires a map instance.");

            publicEvent.SetPhase(PublicEventPhase.InvestigatePellGraveyard);
        }

        /// <summary>
        /// Invoked when a <see cref="IGridEntity"/> is added to the map the public event is on.
        /// </summary>
        public void OnAddToMap(IGridEntity entity)
        {
            if (entity is IWorldLocationVolumeGridTriggerEntity worldLocationEntity
                && worldLocationEntity.Entry.Id == InvestigateWorldLocationId)
                investigateTriggerGuid = worldLocationEntity.Guid;
        }

        /// <summary>
        /// Invoked when a <see cref="IGridEntity"/> is removed from the map the public event is on.
        /// </summary>
        public void OnRemoveFromMap(IGridEntity entity)
        {
            if (entity is IWorldLocationVolumeGridTriggerEntity worldLocationEntity
                && worldLocationEntity.Entry.Id == InvestigateWorldLocationId)
                investigateTriggerGuid = 0;
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.InvestigatePellGraveyard:
                    OnPhaseInvestigatePellGraveyard();
                    break;
                case PublicEventPhase.LightIncenseInPellGraveyard:
                    OnPhaseLightIncenseInPellGraveyard();
                    break;
                case PublicEventPhase.DeliverRestlessSpiritsToAltar:
                    OnPhaseDeliverRestlessSpiritsToAltar();
                    break;
                case PublicEventPhase.FightOffPrimalEchoes:
                    OnPhaseFightOffPrimalEchoes();
                    break;
                case PublicEventPhase.DefeatPrimalWraith:
                    OnPhaseDefeatPrimalWraith();
                    break;
            }
        }

        private void OnPhaseInvestigatePellGraveyard()
        {
            // Objective 4451 is ParticipantsInTriggerVolume object 7895 at
            // WorldLocation2 48675. Objective 5295 is the main-event Script
            // parent marker at nearby WorldLocation2 48673.
            mapInstance.PublicEventManager?.GetEvent(MainPublicEventId)
                ?.ActivateObjective(PublicEventObjective.InvestigatePellGraveyardParent);
            publicEvent.ActivateObjective(PublicEventObjective.InvestigatePellGraveyard);

            IWorldLocationVolumeGridTriggerEntity triggerEntity = publicEvent.CreateEntity<IWorldLocationVolumeGridTriggerEntity>();
            triggerEntity.Initialise(InvestigateWorldLocationId, InvestigateObjectId);
            mapInstance.EnqueueAdd(triggerEntity, new ScriptMapPosition
            {
                Info = new ScriptMapInfo
                {
                    Entry   = mapInstance.Entry,
                    MapLock = mapInstance.MapLock
                },
                Position = InvestigatePosition
            });
        }

        private void OnPhaseLightIncenseInPellGraveyard()
        {
            // Objective 4452 is ActivateTargetGroupChecklist 12275, whose
            // member is Creature2 67962 (Unlit Incense - w3009 - Side Mission 4).
            publicEvent.ActivateObjective(PublicEventObjective.LightIncenseInPellGraveyard);
        }

        private void OnPhaseDeliverRestlessSpiritsToAltar()
        {
            // Objective 4453 is ActivateTargetGroupChecklist 12302, whose
            // member is Creature2 68012 (Sacred Bas-Relief - w3009 - Side Mission 4).
            publicEvent.ActivateObjective(PublicEventObjective.DeliverRestlessSpiritsToAltar);
        }

        private void OnPhaseFightOffPrimalEchoes()
        {
            // Objective 4462 is a count-one Script row at WorldLocation2 48690.
            // Exact Primal Echo wave composition remains blocked; credit the
            // script gate so the proved Primal Wraith boss step can complete.
            publicEvent.ActivateObjective(PublicEventObjective.FightOffPrimalEchoes);
            publicEvent.UpdateObjective(PublicEventObjective.FightOffPrimalEchoes, 1);
        }

        private void OnPhaseDefeatPrimalWraith()
        {
            // Objective 4463 points at Creature2 68013 (Primal Wraith - Boss).
            publicEvent.ActivateObjective(PublicEventObjective.DefeatPrimalWraith);
        }

        public void OnPublicEventObjectiveStatus(IPublicEventObjective objective)
        {
            if (objective.Status != PublicEventStatus.Succeeded)
                return;

            switch ((PublicEventObjective)objective.Entry.Id)
            {
                case PublicEventObjective.InvestigatePellGraveyard:
                    RemoveTrigger(investigateTriggerGuid);
                    StartIncensePhase();
                    break;
                case PublicEventObjective.LightIncenseInPellGraveyard:
                    StartAltarPhase();
                    break;
                case PublicEventObjective.DeliverRestlessSpiritsToAltar:
                    StartPrimalEchoesPhase();
                    break;
                case PublicEventObjective.FightOffPrimalEchoes:
                    StartPrimalWraithPhase();
                    break;
                case PublicEventObjective.DefeatPrimalWraith:
                    publicEvent.Finish(PublicEventTeam.PublicTeam);
                    break;
            }
        }

        private void StartIncensePhase()
        {
            if (incensePhaseStarted)
                return;

            incensePhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.LightIncenseInPellGraveyard);
        }

        private void StartAltarPhase()
        {
            if (altarPhaseStarted)
                return;

            altarPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.DeliverRestlessSpiritsToAltar);
        }

        private void StartPrimalEchoesPhase()
        {
            if (primalEchoesPhaseStarted)
                return;

            primalEchoesPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.FightOffPrimalEchoes);
        }

        private void StartPrimalWraithPhase()
        {
            if (primalWraithPhaseStarted)
                return;

            primalWraithPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.DefeatPrimalWraith);
        }

        private void RemoveTrigger(uint guid)
        {
            IWorldLocationVolumeGridTriggerEntity triggerEntity = mapInstance.GetEntity<IWorldLocationVolumeGridTriggerEntity>(guid);
            triggerEntity?.RemoveFromMap();
        }
    }
}
