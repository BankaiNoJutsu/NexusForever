using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.RuinsOfKelVoreth
{
    [ScriptFilterOwnerId(161)]
    public class RuinsOfKelVorethEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private IPublicEvent publicEvent;

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            _ = publicEvent.Map as IMapInstance
                ?? throw new InvalidOperationException("Ruins of Kel Voreth requires a map instance.");

            publicEvent.SetPhase(PublicEventPhase.FightInBloodPit);
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.FightInBloodPit:
                    publicEvent.ActivateObjective(PublicEventObjective.FightYourWayThroughTheBloodPit, 3u);
                    break;
                case PublicEventPhase.GrondTheCorpsemaker:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatGrondTheCorpsemaker);
                    break;
                case PublicEventPhase.SlaveMasterDrokk:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatSlavemasterDrokk);
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatDarkwitchGurka);
                    publicEvent.ActivateObjective(PublicEventObjective.DodgingTheDefense);
                    ActivateWipOptionalObjective(PublicEventObjective.PutTheKelVorethSlavesOutOfTheirMisery);
                    ActivateWipOptionalObjective(PublicEventObjective.AccessTheHiddenEldanDataStorageDevices);
                    break;
                case PublicEventPhase.ForgeMasterTrogun:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatForgemasterTrogun);
                    ActivateWipOptionalObjective(PublicEventObjective.DestroyKelVorethForges);
                    ActivateWipOptionalObjective(PublicEventObjective.KillBattleswornAndDarkwitchOsun);
                    ActivateWipOptionalObjective(PublicEventObjective.BurnKelVorethWarSupplies);
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
                case PublicEventObjective.FightYourWayThroughTheBloodPit:
                    publicEvent.SetPhase(PublicEventPhase.GrondTheCorpsemaker);
                    break;
                case PublicEventObjective.DefeatGrondTheCorpsemaker:
                    publicEvent.SetPhase(PublicEventPhase.SlaveMasterDrokk);
                    break;
                case PublicEventObjective.DefeatSlavemasterDrokk:
                    publicEvent.SetPhase(PublicEventPhase.ForgeMasterTrogun);
                    break;
                case PublicEventObjective.DefeatForgemasterTrogun:
                    publicEvent.Finish(PublicEventTeam.PublicTeam);
                    break;
            }
        }

        private void ActivateWipOptionalObjective(PublicEventObjective objective)
        {
            // WIP-guessed from LaughingWS Instances-and-more. The branch randomly
            // activates these Ruins optional objectives, but exact route weights,
            // trigger placement, door timing, and objective availability remain blocked.
            if (ShouldActivateWipOptionalObjective(objective))
                publicEvent.ActivateObjective(objective);
        }

        protected virtual bool ShouldActivateWipOptionalObjective(PublicEventObjective objective)
        {
            return Random.Shared.Next(2) == 1;
        }
    }
}
