using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.StormtalonsLair
{
    [ScriptFilterOwnerId(145)]
    public class StormtalonsLairEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private readonly ICinematicFactory cinematicFactory;

        private IPublicEvent publicEvent;
        private IMapInstance mapInstance;

        public StormtalonsLairEventScript(
            ICinematicFactory cinematicFactory)
        {
            this.cinematicFactory = cinematicFactory;
        }

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            mapInstance = publicEvent.Map as IMapInstance
                ?? throw new InvalidOperationException("Stormtalon's Lair requires a map instance.");

            publicEvent.SetPhase(PublicEventPhase.Enter);
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.Enter:
                    publicEvent.ActivateObjective(PublicEventObjective.SurviveTheThundercallPellZealots);
                    ActivateWipOptionalObjective(PublicEventObjective.GatherTaintedStemSamples);
                    break;
                case PublicEventPhase.DefeatBladeWindTheInvoker:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatBladeWindTheInvoker);
                    ActivateWipOptionalObjective(PublicEventObjective.ObtainDataFromTheThundercallDataAltar);
                    ActivateWipOptionalObjective(PublicEventObjective.HijackPowerFromTheThundercallStormTotems);
                    break;
                case PublicEventPhase.EliminateAethros:
                    publicEvent.ActivateObjective(PublicEventObjective.EliminateAethros);
                    ActivateWipOptionalObjective(PublicEventObjective.FreeTheThundercallSacrificialPrisoners);
                    ActivateWipOptionalObjective(PublicEventObjective.UseYourGrenadesToDisableTheThundercallPell);
                    publicEvent.ActivateObjective(ShouldUseWipArcanistVariant()
                        ? PublicEventObjective.DefeatArcanistBreezeBinderForTheEncryptionKey
                        : PublicEventObjective.KillOverseerDriftCatcher);
                    break;
                case PublicEventPhase.StopTheThundercallHighPriest:
                    publicEvent.ActivateObjective(PublicEventObjective.StopTheThundercallHighPriest, mapInstance.PlayerCount);
                    break;
                case PublicEventPhase.DestroyStormtalon:
                    QueueWipGuessedCinematic<IStormtalonLairStormtalonReborn>();
                    publicEvent.ActivateObjective(PublicEventObjective.DestroyStormtalon);
                    break;
            }
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
                case PublicEventObjective.SurviveTheThundercallPellZealots:
                    publicEvent.SetPhase(PublicEventPhase.DefeatBladeWindTheInvoker);
                    break;
                case PublicEventObjective.DefeatBladeWindTheInvoker:
                    publicEvent.SetPhase(PublicEventPhase.EliminateAethros);
                    break;
                case PublicEventObjective.EliminateAethros:
                    publicEvent.SetPhase(PublicEventPhase.StopTheThundercallHighPriest);
                    break;
                case PublicEventObjective.StopTheThundercallHighPriest:
                    publicEvent.SetPhase(PublicEventPhase.DestroyStormtalon);
                    break;
                case PublicEventObjective.DestroyStormtalon:
                    publicEvent.Finish(PublicEventTeam.PublicTeam);
                    break;
            }
        }

        private void ActivateWipOptionalObjective(PublicEventObjective objective)
        {
            // WIP-guessed from LaughingWS Instances-and-more. The branch randomly
            // includes these optional objectives in the Stormtalon's Lair route; exact
            // retail route weights, trigger placement, and objective availability remain blocked.
            if (ShouldActivateWipOptionalObjective(objective))
                publicEvent.ActivateObjective(objective);
        }

        protected virtual bool ShouldActivateWipOptionalObjective(PublicEventObjective objective)
        {
            return Random.Shared.Next(2) == 1;
        }

        protected virtual bool ShouldUseWipArcanistVariant()
        {
            return Random.Shared.Next(2) == 1;
        }

        private void QueueWipGuessedCinematic<T>() where T : ICinematicBase
        {
            // WIP-guessed from LaughingWS Instances-and-more. The branch queues this
            // Stormtalon reborn cinematic before the final objective, but the real
            // cinematic payload and boss version selection remain blocked.
            foreach (IPlayer player in mapInstance.GetPlayers())
                player.CinematicManager.QueueCinematic(cinematicFactory.CreateCinematic<T>());
        }
    }
}
