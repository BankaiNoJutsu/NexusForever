using System.Numerics;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.SanctuaryOfTheSwordmaiden
{
    [ScriptFilterOwnerId(166)]
    public class SanctuaryOfTheSwordmaidenEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private const uint FlameCrazedDemonEntityId = 1100300051u;
        private const uint FlameCrazedDemonCreatureId = 29254u;
        private const ushort FlameCrazedDemonWorldId = 1271;
        private const ushort FlameCrazedDemonAreaId = 1556;
        private const uint FlameCrazedDemonPublicEventId = 166u;
        private const uint FlameCrazedDemonPublicEventPhase = 4u;
        private const uint FlameCrazedDemonDisplayInfo = 24811u;
        private const ushort FlameCrazedDemonFactionId = 978;
        private const string FlameCrazedDemonScriptName = "FlameCrazedDemonEntityScript";

        private static readonly Vector3 FlameCrazedDemonPosition = new(4877.322f, -797.6906f, -3318.368f);

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
                    publicEvent.ActivateObjective(PublicEventObjective.EliminateZealousTorine);
                    ActivateWipOptionalObjective(PublicEventObjective.SabotageLifeweaverTechClusters);
                    ActivateWipOptionalObjective(PublicEventObjective.KillCorruptedDeathbringerDareia);
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
                    SpawnFlameCrazedDemon();
                    publicEvent.ActivateObjective(PublicEventObjective.DodgeTorineTotemOfFlame);
                    publicEvent.ActivateObjective(PublicEventObjective.DestroyTorineTotemsOfFlame);
                    break;
                case PublicEventPhase.MoldwoodOverlordSkash:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatMoldwoodOverlordSkash);
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatSkashOrHeWillCorruptThePrisoner);
                    publicEvent.ActivateObjective(PublicEventObjective.DestroyTheElderMoldwoodRavager);
                    ActivateWipOptionalObjective(PublicEventObjective.DestroyTheMoldwoodCorruptors);
                    ActivateWipOptionalObjective(PublicEventObjective.DestroyMoldwoodSkurgeAndCrawlers);
                    ActivateWipOptionalObjective(PublicEventObjective.KillDistractedMoldwoodMaulers);
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
                    ActivateWipOptionalObjective(PublicEventObjective.DestroyDeathstingSwarms);
                    ActivateWipOptionalObjective(PublicEventObjective.KillCorruptedVeteranSwordmaidens);
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

        private void SpawnFlameCrazedDemon()
        {
            // Build 16042 reviewed instance entity 1100300051 places the
            // Flame-Crazed Demon in event 166 phase 4 with its objective-credit script.
            INonPlayerEntity flameCrazedDemon = publicEvent.CreateEntity<INonPlayerEntity>();
            flameCrazedDemon.Initialise(CreateFlameCrazedDemonEntityModel());
            AddToMap(flameCrazedDemon, FlameCrazedDemonPosition);
        }

        private static EntityModel CreateFlameCrazedDemonEntityModel()
        {
            return new EntityModel
            {
                Id          = FlameCrazedDemonEntityId,
                Type        = EntityType.NonPlayer,
                Creature    = FlameCrazedDemonCreatureId,
                World       = FlameCrazedDemonWorldId,
                Area        = FlameCrazedDemonAreaId,
                X           = FlameCrazedDemonPosition.X,
                Y           = FlameCrazedDemonPosition.Y,
                Z           = FlameCrazedDemonPosition.Z,
                DisplayInfo = FlameCrazedDemonDisplayInfo,
                Faction1    = FlameCrazedDemonFactionId,
                Faction2    = FlameCrazedDemonFactionId,
                EntityEvent = new EntityEventModel
                {
                    EventId = FlameCrazedDemonPublicEventId,
                    Phase   = FlameCrazedDemonPublicEventPhase
                },
                EntityScript =
                {
                    new EntityScriptModel
                    {
                        ScriptName = FlameCrazedDemonScriptName
                    }
                },
                EntityStat =
                {
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Level,
                        Value = 40f
                    }
                }
            };
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
