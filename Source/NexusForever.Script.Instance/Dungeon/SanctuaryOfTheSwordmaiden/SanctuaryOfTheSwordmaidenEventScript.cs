using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.SanctuaryOfTheSwordmaiden
{
    [ScriptFilterOwnerId(166)]
    public class SanctuaryOfTheSwordmaidenEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private readonly IGlobalQuestManager globalQuestManager;

        private IPublicEvent publicEvent;
        private IMapInstance mapInstance;
        private PublicEventPhase? wipRandomPathPhase;

        public SanctuaryOfTheSwordmaidenEventScript(
            IGlobalQuestManager globalQuestManager)
        {
            this.globalQuestManager = globalQuestManager;
        }

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            mapInstance = publicEvent.Map as IMapInstance
                ?? throw new InvalidOperationException("Sanctuary of the Swordmaiden requires a map instance.");

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
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatDeadringerShallaos);
                    break;
                case PublicEventPhase.RandomPath:
                    publicEvent.ActivateObjective(PublicEventObjective.CollectTorineSpiritRelics);
                    BroadcastWipCommunicatorMessage(CommunicatorMessage.SpiritMotherSelene14);
                    // WIP-guessed from LaughingWS Instances-and-more: the branch
                    // randomly chooses Temple of the Life Speaker or Moldwood
                    // Corruption after the relic handoff. Only phase routing is
                    // ported here; branch door opening and trigger creation use
                    // guessed placements and remain blocked pending smoke proof.
                    wipRandomPathPhase = ShouldUseWipTempleRoute()
                        ? PublicEventPhase.TempleOfTheLifeSpeaker
                        : PublicEventPhase.MoldwoodCorruption;
                    publicEvent.SetPhase(wipRandomPathPhase.Value);
                    break;
                case PublicEventPhase.TempleOfTheLifeSpeaker:
                    publicEvent.ActivateObjective(PublicEventObjective.EnterTheTempleOfTheLifeSpeaker);
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatHammerfistMoldjaw);
                    break;
                case PublicEventPhase.MoldwoodCorruption:
                    publicEvent.ActivateObjective(PublicEventObjective.ReachTheMoldwoodCorruption);
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatCorruptedEdgesmithTorian);
                    ActivateWipOptionalObjective(PublicEventObjective.FreeTheSpiritsOfTheCorruptedTorineSisters);
                    break;
                case PublicEventPhase.RaynaDarkspeaker:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatRaynaDarkspeaker);
                    publicEvent.ActivateObjective(PublicEventObjective.DestroyTheFlameCrazedDemon);
                    publicEvent.ActivateObjective(PublicEventObjective.DodgeTorineTotemOfFlame);
                    publicEvent.ActivateObjective(PublicEventObjective.DestroyTorineTotemsOfFlame);
                    break;
                case PublicEventPhase.MoldwoodOverlordSkash:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatMoldwoodOverlordSkash);
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatSkashOrHeWillCorruptThePrisoner);
                    publicEvent.ActivateObjective(PublicEventObjective.DestroyTheElderMoldwoodRavager);
                    ActivateWipOptionalObjective(PublicEventObjective.DestroyTheMoldwoodCorruptors);
                    ActivateWipOptionalObjective(PublicEventObjective.DestroyMoldwoodSkurgeAndCrawlers);
                    ActivateWipOptionalObjective(PublicEventObjective.UseTheSoulSporeOnMoldwoodGorgers);
                    break;
                case PublicEventPhase.GoToLifeWeaverTerracePath1:
                    publicEvent.ActivateObjective(PublicEventObjective.EnterLifeweaverTerrace);
                    break;
                case PublicEventPhase.GoToLifeWeaverTerracePath2:
                    publicEvent.ActivateObjective(PublicEventObjective.EnterLifeweaverTerrace);
                    publicEvent.ActivateObjective(PublicEventObjective.KillCorruptedLifecallerKhalee);
                    break;
                case PublicEventPhase.OnduLifeWeaver:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatOnduLifeweaver);
                    publicEvent.ActivateObjective(PublicEventObjective.KillTheLifeweaverGuardian);
                    ActivateWipOptionalObjective(PublicEventObjective.KillTheCorruptedTerrorantulas);
                    ActivateWipOptionalObjective(PublicEventObjective.DefeatCorruptedLifeweaverPell);
                    break;
                case PublicEventPhase.PlaceSpiritRelics:
                    publicEvent.ActivateObjective(PublicEventObjective.PlaceTheSpiritRelics);
                    publicEvent.ActivateObjective(PublicEventObjective.PlaceSpiritRelicsButDoNotfall);
                    break;
                case PublicEventPhase.SpiritmotherSelene:
                    publicEvent.ActivateObjective(PublicEventObjective.EscortSpiritmotherSelene);
                    break;
                case PublicEventPhase.SpiritmotherSeleneTheCorrupted:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatSpiritmotherSeleneTheCorrupted);
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
                case PublicEventObjective.DefeatDeadringerShallaos:
                    publicEvent.SetPhase(PublicEventPhase.RandomPath);
                    break;
                case PublicEventObjective.CollectTorineSpiritRelics:
                    // WIP-guessed branch routing chooses the next path when the
                    // RandomPath phase starts. If this objective completes after
                    // that, do not override a Moldwood route with the historical
                    // conservative Temple fallback. Keep the fallback only for
                    // defensive compatibility if a status arrives first.
                    if (wipRandomPathPhase == null)
                        publicEvent.SetPhase(PublicEventPhase.TempleOfTheLifeSpeaker);
                    break;
                case PublicEventObjective.EnterTheTempleOfTheLifeSpeaker:
                    publicEvent.SetPhase(PublicEventPhase.RaynaDarkspeaker);
                    break;
                case PublicEventObjective.ReachTheMoldwoodCorruption:
                    publicEvent.SetPhase(PublicEventPhase.MoldwoodOverlordSkash);
                    break;
                case PublicEventObjective.DefeatRaynaDarkspeaker:
                    publicEvent.SetPhase(PublicEventPhase.GoToLifeWeaverTerracePath1);
                    break;
                case PublicEventObjective.DefeatMoldwoodOverlordSkash:
                    publicEvent.SetPhase(PublicEventPhase.GoToLifeWeaverTerracePath2);
                    break;
                case PublicEventObjective.EnterLifeweaverTerrace:
                    publicEvent.SetPhase(PublicEventPhase.OnduLifeWeaver);
                    break;
                case PublicEventObjective.DefeatOnduLifeweaver:
                    publicEvent.SetPhase(PublicEventPhase.PlaceSpiritRelics);
                    break;
                case PublicEventObjective.PlaceTheSpiritRelics:
                    publicEvent.SetPhase(PublicEventPhase.SpiritmotherSelene);
                    break;
                case PublicEventObjective.EscortSpiritmotherSelene:
                    publicEvent.SetPhase(PublicEventPhase.SpiritmotherSeleneTheCorrupted);
                    break;
                case PublicEventObjective.DefeatSpiritmotherSeleneTheCorrupted:
                    publicEvent.Finish(PublicEventTeam.PublicTeam);
                    break;
            }
        }

        private void BroadcastWipCommunicatorMessage(CommunicatorMessage message)
        {
            // WIP-guessed from LaughingWS Instances-and-more. The branch pairs this Selene
            // callout with Sanctuary's random-path handoff, but exact random path selection,
            // route timing, and door choreography remain blocked pending retail smoke proof.
            ICommunicatorMessage communicatorMessage = globalQuestManager.GetCommunicatorMessage(message);
            foreach (IPlayer player in mapInstance.GetPlayers())
                communicatorMessage?.Send(player.Session);
        }

        private void ActivateWipOptionalObjective(PublicEventObjective objective)
        {
            // WIP-guessed from LaughingWS Instances-and-more. The branch randomly
            // activates these Sanctuary optional objectives, but exact route weights,
            // path availability, trigger placement, and door timing remain blocked.
            if (ShouldActivateWipOptionalObjective(objective))
                publicEvent.ActivateObjective(objective);
        }

        protected virtual bool ShouldActivateWipOptionalObjective(PublicEventObjective objective)
        {
            return Random.Shared.Next(2) == 1;
        }

        protected virtual bool ShouldUseWipTempleRoute()
        {
            return Random.Shared.Next(2) == 1;
        }
    }
}
