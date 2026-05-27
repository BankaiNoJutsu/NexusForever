using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.Infestation
{
    [ScriptFilterOwnerId(95)]
    public class InfestationEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private IPublicEvent publicEvent;
        private IMapInstance mapInstance;

        private const uint CargoShipTurnstileTriggerId = 1005u;
        private const uint CargoShipTurnstileObjectiveObjectId = 1005u;
        private const float CargoShipTurnstileRange = 50f;
        private static readonly Vector3 CargoShipTurnstilePosition = new(-0.2391071f, -499.99f, 87.62592f);

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            mapInstance = publicEvent.Map as IMapInstance
                ?? throw new InvalidOperationException("Infestation requires a map instance.");

            publicEvent.SetPhase(PublicEventPhase.ProceedOntoTheCargoShip);
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.ProceedOntoTheCargoShip:
                    publicEvent.ActivateObjective(PublicEventObjective.ProceedOntoTheCargoShip, mapInstance.PlayerCount);
                    SpawnWipGuessedTurnstileTrigger(
                        CargoShipTurnstileTriggerId,
                        CargoShipTurnstileRange,
                        CargoShipTurnstileObjectiveObjectId,
                        CargoShipTurnstilePosition);
                    break;
                case PublicEventPhase.CloseTheShipVents:
                    publicEvent.ActivateObjective(PublicEventObjective.CloseTheShipVents);
                    break;
                case PublicEventPhase.FindTheMedicalBay:
                    publicEvent.ActivateObjective(PublicEventObjective.FindTheMedicalBay, mapInstance.PlayerCount);
                    publicEvent.ActivateObjective(PublicEventObjective.TagValuableCargo);
                    break;
                case PublicEventPhase.SealHullBreaches:
                    publicEvent.ResetObjective(PublicEventObjective.FindTheMedicalBay);
                    publicEvent.ActivateObjective(PublicEventObjective.SealHullBreaches);
                    break;
                case PublicEventPhase.FindTheMedicalBay2:
                    publicEvent.ActivateObjective(PublicEventObjective.FindTheMedicalBay);
                    break;
                case PublicEventPhase.FindMedicalSupplies:
                    publicEvent.ActivateObjective(PublicEventObjective.FindMedicalSupplies);
                    break;
                case PublicEventPhase.DefeatTheAttackOnMedbay:
                    publicEvent.ResetObjective(PublicEventObjective.FindMedicalSupplies);
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheAttackOnMedbay);
                    break;
                case PublicEventPhase.FindMedicalSupplies2:
                    publicEvent.ActivateObjective(PublicEventObjective.FindMedicalSupplies);
                    break;
                case PublicEventPhase.HealContaminatedShiphands:
                    publicEvent.ActivateObjective(PublicEventObjective.HealContaminatedShiphand);
                    break;
            }
        }

        private void SpawnWipGuessedTurnstileTrigger(uint triggerId, float range, uint objectId, Vector3 position)
        {
            // WIP-guessed from LaughingWS Instances-and-more: the branch marks this
            // cargo-ship turnstile range/position as needing proof. Keep it scoped
            // to the initial participant-gather objective until retail trigger rows
            // or manual expedition smoke confirm the exact placement.
            ITurnstileGridTriggerEntity triggerEntity = publicEvent.CreateEntity<ITurnstileGridTriggerEntity>();
            triggerEntity.Initialise(triggerId, range, objectId);
            AddToMap(triggerEntity, position);
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

        /// <summary>
        /// Invoked when the <see cref="IPublicEventObjective"/> status changes.
        /// </summary>
        public void OnPublicEventObjectiveStatus(IPublicEventObjective objective)
        {
            if (objective.Status != PublicEventStatus.Succeeded)
                return;

            switch ((PublicEventObjective)objective.Entry.Id)
            {
                case PublicEventObjective.ProceedOntoTheCargoShip:
                    publicEvent.SetPhase(PublicEventPhase.CloseTheShipVents);
                    break;
                case PublicEventObjective.CloseTheShipVents:
                    publicEvent.SetPhase(PublicEventPhase.FindTheMedicalBay);
                    break;
                case PublicEventObjective.SealHullBreaches:
                    publicEvent.SetPhase(PublicEventPhase.FindTheMedicalBay2);
                    break;
                case PublicEventObjective.DefeatTheAttackOnMedbay:
                    publicEvent.SetPhase(PublicEventPhase.FindMedicalSupplies2);
                    break;
                case PublicEventObjective.FindMedicalSupplies:
                    publicEvent.SetPhase(PublicEventPhase.HealContaminatedShiphands);
                    break;
                case PublicEventObjective.KillCyclopeanParasite:
                case PublicEventObjective.KillLumberingParasites:
                    publicEvent.Finish(PublicEventTeam.PublicTeam);
                    break;
            }
        }
    }
}
