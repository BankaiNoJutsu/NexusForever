using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.ColdbloodCitadel
{
    [ScriptFilterOwnerId(907)]
    public class ColdbloodCitadelEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private IPublicEvent publicEvent;
        private IMapInstance mapInstance;

        private uint gatherRingGuid;
        private uint gatherRingTriggerGuid;

        #region Dependency Injection

        private readonly IGlobalQuestManager globalQuestManager;

        public ColdbloodCitadelEventScript(
            IGlobalQuestManager globalQuestManager)
        {
            this.globalQuestManager = globalQuestManager;
        }

        #endregion

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            mapInstance = publicEvent.Map as IMapInstance
                ?? throw new InvalidOperationException("Coldblood Citadel requires a map instance.");

            publicEvent.SetPhase(PublicEventPhase.Enter);
        }

        /// <summary>
        /// Invoked when a <see cref="IGridEntity"/> is added to the map the public event is on.
        /// </summary>
        public void OnAddToMap(IGridEntity entity)
        {
            switch (entity)
            {
                case IWorldLocationVolumeGridTriggerEntity worldLocationEntity:
                    OnAddToMapWorldLocationEntity(worldLocationEntity);
                    break;
                case IWorldEntity worldEntity:
                    OnAddToMapWorldEntity(worldEntity);
                    break;
            }
        }

        private void OnAddToMapWorldEntity(IWorldEntity worldEntity)
        {
            if (worldEntity.CreatureId == (uint)PublicEventCreature.GatherRing)
                gatherRingGuid = worldEntity.Guid;
        }

        private void OnAddToMapWorldLocationEntity(IWorldLocationVolumeGridTriggerEntity worldLocationEntity)
        {
            if (worldLocationEntity.Entry.Id == (uint)PublicEventCreature.GatherRing)
                gatherRingTriggerGuid = worldLocationEntity.Guid;
        }

        /// <summary>
        /// Invoked when a <see cref="IGridEntity"/> is removed from the map the public event is on.
        /// </summary>
        public void OnRemoveFromMap(IGridEntity entity)
        {
            switch (entity)
            {
                case IWorldLocationVolumeGridTriggerEntity worldLocationEntity:
                    if (worldLocationEntity.Entry.Id == (uint)PublicEventCreature.GatherRing)
                        gatherRingTriggerGuid = 0;
                    break;
                case IWorldEntity worldEntity:
                    if (worldEntity.CreatureId == (uint)PublicEventCreature.GatherRing)
                        gatherRingGuid = 0;
                    break;
            }
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.Enter:
                    OnPhaseEnter();
                    break;
                case PublicEventPhase.HailStoneGatecrasher:
                    OnPhaseHailStoneGatecrasher();
                    break;
                case PublicEventPhase.IceBloodCoven:
                    OnPhaseIceBloodCoven();
                    break;
                case PublicEventPhase.RisenHarizog:
                    OnPhaseRisenHarizog();
                    break;
            }
        }

        private void OnPhaseEnter()
        {
            IWorldLocationVolumeGridTriggerEntity triggerEntity = publicEvent.CreateEntity<IWorldLocationVolumeGridTriggerEntity>();
            triggerEntity.Initialise(53206, 8656);
            AddToMap(triggerEntity, new Vector3(604.33f, -475.452f, -322.957f));
        }

        private void OnPhaseHailStoneGatecrasher()
        {
            publicEvent.ActivateObjective(PublicEventObjective.DefeatHailStoneGatecrasher);

            IWorldLocationVolumeGridTriggerEntity triggerEntity = mapInstance.GetEntity<IWorldLocationVolumeGridTriggerEntity>(gatherRingTriggerGuid);
            triggerEntity?.RemoveFromMap();

            IWorldEntity gatherRing = mapInstance.GetEntity<IWorldEntity>(gatherRingGuid);
            gatherRing?.RemoveFromMap();

            publicEvent.ActivateObjective(PublicEventObjective.SavePellFightingTheOsun);

            ActivateWipOptionalObjective(PublicEventObjective.StealSampleOfLiquidSoulfrost);
            ActivateWipOptionalObjective(PublicEventObjective.GatherSoulfrostShards);
            ActivateWipOptionalObjective(PublicEventObjective.RallyTheWinterfuryPell);

            BroadcastCommunicatorMessage(CommunicatorMessage.TowerEngineerRenhakul1);
        }

        private void OnPhaseIceBloodCoven()
        {
            publicEvent.ActivateObjective(PublicEventObjective.DefeatTheIcebloodCoven);
            publicEvent.ActivateObjective(PublicEventObjective.RescueThePellArchitect);
            publicEvent.ActivateObjective(PublicEventObjective.KillKrovakSummonersAndTheirFrostguards);
            publicEvent.ActivateObjective(PublicEventObjective.ConcurrentCovenCollapse);

            BroadcastCommunicatorMessage(CommunicatorMessage.TowerEngineerRenhakul11);

            ActivateWipOptionalObjective(PublicEventObjective.RescueWinterfuryPrisoners);
            ActivateWipOptionalObjective(PublicEventObjective.DestroySoulrotCanisters);
            ActivateWipOptionalObjective(PublicEventObjective.DisableSoulfrostTraps);
        }

        private void OnPhaseRisenHarizog()
        {
            publicEvent.ActivateObjective(PublicEventObjective.DefeatTheRisenHarizog);
            publicEvent.ActivateObjective(PublicEventObjective.InfusionInterdiction);

            BroadcastCommunicatorMessage(CommunicatorMessage.TowerEngineerRenhakul4);
        }

        private void BroadcastCommunicatorMessage(CommunicatorMessage message)
        {
            // WIP-guessed from LaughingWS Instances-and-more. The branch ties these Renhakul
            // callouts to Coldblood phase handoffs, but optional objective selection and exact
            // dungeon route timing remain blocked pending manual smoke proof.
            ICommunicatorMessage communicatorMessage = globalQuestManager.GetCommunicatorMessage(message);
            foreach (IPlayer player in mapInstance.GetPlayers())
                communicatorMessage?.Send(player.Session);
        }

        private void ActivateWipOptionalObjective(PublicEventObjective objective)
        {
            // WIP-guessed from LaughingWS Instances-and-more. The branch activates these
            // optional objectives by random 0/1 rolls; exact retail route selection, weights,
            // and trigger placement remain blocked pending manual dungeon smoke proof.
            if (ShouldActivateWipOptionalObjective(objective))
                publicEvent.ActivateObjective(objective);
        }

        protected virtual bool ShouldActivateWipOptionalObjective(PublicEventObjective objective)
        {
            return Random.Shared.Next(2) == 1;
        }

        public void OnPublicEventObjectiveStatus(IPublicEventObjective objective)
        {
            if (objective.Status != PublicEventStatus.Succeeded)
                return;

            switch ((PublicEventObjective)objective.Entry.Id)
            {
                case PublicEventObjective.FindThePellAttackingColdbloodCitadel:
                    publicEvent.SetPhase(PublicEventPhase.HailStoneGatecrasher);
                    break;
                case PublicEventObjective.DefeatHailStoneGatecrasher:
                    publicEvent.SetPhase(PublicEventPhase.IceBloodCoven);
                    break;
                case PublicEventObjective.DefeatTheIcebloodCoven:
                    publicEvent.SetPhase(PublicEventPhase.RisenHarizog);
                    break;
                case PublicEventObjective.DefeatTheRisenHarizog:
                    publicEvent.Finish(PublicEventTeam.PublicTeam);
                    break;
            }
        }

        private void AddToMap(IGridEntity entity, Vector3 position)
        {
            mapInstance.EnqueueAdd(entity, new ScriptMapPosition
            {
                Info = new ScriptMapInfo
                {
                    Entry   = mapInstance.Entry,
                    MapLock = mapInstance.MapLock
                },
                Position = position
            });
        }
    }
}
