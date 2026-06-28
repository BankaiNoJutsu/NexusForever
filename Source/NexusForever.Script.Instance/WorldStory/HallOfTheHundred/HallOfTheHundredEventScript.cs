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
    [ScriptFilterOwnerId(666)]
    public class HallOfTheHundredEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private IPublicEvent publicEvent;
        private IMapInstance mapInstance;
        private uint openingGatherTriggerGuid;
        private uint openingConversationTriggerGuid;
        private uint lockedGateInvestigationTriggerGuid;
        private uint bridgeMeetTriggerGuid;
        private uint kelHavikRegroupTriggerGuid;
        private uint watchtowerFirstFloorTriggerGuid;
        private uint watchtowerUpperFloorsTriggerGuid;
        private uint watchtowerTopFloorTriggerGuid;
        private uint courtyardRegroupTriggerGuid;
        private uint courtyardStatueTriggerGuid;
        private uint vaultDoorTriggerGuid;
        private uint vaultEntranceTriggerGuid;
        private uint vaultExitTriggerGuid;
        private uint accessTerminalTriggerGuid;
        private bool openingConversationPhaseStarted;
        private bool rejoinPhaseStarted;
        private bool listenToPlanPhaseStarted;
        private bool lockedGateInvestigationPhaseStarted;
        private bool varegorPhaseStarted;
        private bool reviveCompanionsPhaseStarted;
        private bool reviveDorianSucceeded;
        private bool reviveArtemisSucceeded;
        private bool meetAtBridgePhaseStarted;
        private bool destroyBridgeIcePhaseStarted;
        private bool followIntoKelHavikFortressPhaseStarted;
        private bool regroupOutsideKelHavikFortressPhaseStarted;
        private bool studyMysteriousTabletsPhaseStarted;
        private bool enterWatchtowerPhaseStarted;
        private bool gatherAtFirstFloorPhaseStarted;
        private bool exploreUpperFloorsPhaseStarted;
        private bool exploreTopFloorPhaseStarted;
        private bool useElevatorToBottomFloorPhaseStarted;
        private bool followBackToCourtyardPhaseStarted;
        private bool regroupInKelHavikCourtyardPhaseStarted;
        private bool meetAtKelHavikCourtyardPhaseStarted;
        private bool findCourtyardKeysPhaseStarted;
        private bool findCourtyardKeysSucceeded;
        private bool collectKeyFragmentsSucceeded;
        private bool placeCourtyardKeysPhaseStarted;
        private bool gatherAtVaultDoorPhaseStarted;
        private bool waitVaultDoorOpeningPhaseStarted;
        private bool followInsideHallPhaseStarted;
        private bool followToVaultEntrancePhaseStarted;
        private bool meetAtVaultEntrancePhaseStarted;
        private bool followIntoVaultPhaseStarted;
        private bool meetAtAccessTerminalPhaseStarted;
        private bool deactivateVaultForceFieldPhaseStarted;
        private bool watchHolocubeExplainVaultPhaseStarted;
        private bool speakAboutHolocubePhaseStarted;
        private bool destroyVaultConstructsPhaseStarted;
        private bool defeatHarizogPhaseStarted;
        private bool defeatOsunBlockingTheWayPhaseStarted;
        private bool meetAtVaultExitPhaseStarted;

        private const uint OpeningGatherWorldLocationId = 48684u;
        private const uint OpeningGatherObjectId = 8396u;
        private const uint OpeningConversationWorldLocationId = 50547u;
        private const uint OpeningConversationObjectId = 8278u;
        private const uint LockedGateInvestigationWorldLocationId = 48658u;
        private const uint LockedGateInvestigationObjectId = 7834u;
        private const uint BridgeMeetWorldLocationId = 50596u;
        private const uint BridgeMeetObjectId = 7836u;
        private const uint KelHavikRegroupWorldLocationId = 50597u;
        private const uint KelHavikRegroupObjectId = 7857u;
        private const uint WatchtowerTurnstileTriggerId = 48499u;
        private const uint WatchtowerTurnstileObjectId = 7861u;
        private const float WatchtowerTurnstileRange = 9.73506f;
        private const uint WatchtowerFirstFloorWorldLocationId = 48479u;
        private const uint WatchtowerFirstFloorObjectId = 8527u;
        private const uint WatchtowerUpperFloorsWorldLocationId = 50579u;
        private const uint WatchtowerUpperFloorsObjectId = 8345u;
        private const uint WatchtowerTopFloorWorldLocationId = 50367u;
        private const uint WatchtowerTopFloorObjectId = 8349u;
        private const uint CourtyardRegroupWorldLocationId = 51027u;
        private const uint CourtyardRegroupObjectId = 8304u;
        private const uint CourtyardStatueWorldLocationId = 50605u;
        private const uint CourtyardStatueObjectId = 7894u;
        private const uint VaultDoorWorldLocationId = 48426u;
        private const uint VaultDoorObjectId = 8314u;
        private const uint VaultEntranceWorldLocationId = 50645u;
        private const uint VaultEntranceObjectId = 8298u;
        private const uint VaultExitWorldLocationId = 50702u;
        private const uint VaultExitObjectId = 8339u;
        private const uint AccessTerminalWorldLocationId = 51020u;
        private const uint AccessTerminalObjectId = 8295u;

        private static readonly Vector3 OpeningGatherPosition = new(-952.698f, -710.584f, -156.552f);
        private static readonly Vector3 OpeningConversationPosition = new(-892.391f, -710.492f, -197.729f);
        private static readonly Vector3 LockedGateInvestigationPosition = new(368.129f, -620.752f, -1459.28f);
        private static readonly Vector3 BridgeMeetPosition = new(370.969f, -654.254f, -1260.47f);
        private static readonly Vector3 KelHavikRegroupPosition = new(618.068f, -654.879f, -987.026f);
        private static readonly Vector3 WatchtowerTurnstilePosition = new(751.826f, -657.249f, -992.613f);
        private static readonly Vector3 WatchtowerFirstFloorPosition = new(1399.28f, -865.55f, -1175.43f);
        private static readonly Vector3 WatchtowerUpperFloorsPosition = new(1402.81f, -745.789f, -1176.99f);
        private static readonly Vector3 WatchtowerTopFloorPosition = new(1407.9f, -626.027f, -1174.63f);
        private static readonly Vector3 CourtyardRegroupPosition = new(799.909f, -664.708f, -800.904f);
        private static readonly Vector3 CourtyardStatuePosition = new(751.019f, -647.451f, -852.278f);
        private static readonly Vector3 VaultDoorPosition = new(1210.14f, -649.188f, -503.206f);
        private static readonly Vector3 VaultEntrancePosition = new(1361.12f, -626.637f, -414.843f);
        private static readonly Vector3 VaultExitPosition = new(1372.81f, -628.495f, -429.714f);
        private static readonly Vector3 AccessTerminalPosition = new(1451.66f, -673.04f, -498.369f);

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            mapInstance = publicEvent.Map as IMapInstance
                ?? throw new InvalidOperationException("Hall of the Hundred requires a map instance.");

            publicEvent.SetPhase(PublicEventPhase.GatherInFrontOfTheCrashedShip);
        }

        /// <summary>
        /// Invoked when a <see cref="IGridEntity"/> is added to the map the public event is on.
        /// </summary>
        public void OnAddToMap(IGridEntity entity)
        {
            if (entity is IWorldLocationVolumeGridTriggerEntity worldLocationEntity
                && worldLocationEntity.Entry.Id == OpeningGatherWorldLocationId)
                openingGatherTriggerGuid = worldLocationEntity.Guid;

            if (entity is IWorldLocationVolumeGridTriggerEntity conversationEntity
                && conversationEntity.Entry.Id == OpeningConversationWorldLocationId)
                openingConversationTriggerGuid = conversationEntity.Guid;

            if (entity is IWorldLocationVolumeGridTriggerEntity lockedGateInvestigationEntity
                && lockedGateInvestigationEntity.Entry.Id == LockedGateInvestigationWorldLocationId)
                lockedGateInvestigationTriggerGuid = lockedGateInvestigationEntity.Guid;

            if (entity is IWorldLocationVolumeGridTriggerEntity bridgeMeetEntity
                && bridgeMeetEntity.Entry.Id == BridgeMeetWorldLocationId)
                bridgeMeetTriggerGuid = bridgeMeetEntity.Guid;

            if (entity is IWorldLocationVolumeGridTriggerEntity kelHavikRegroupEntity
                && kelHavikRegroupEntity.Entry.Id == KelHavikRegroupWorldLocationId)
                kelHavikRegroupTriggerGuid = kelHavikRegroupEntity.Guid;

            if (entity is IWorldLocationVolumeGridTriggerEntity firstFloorEntity
                && firstFloorEntity.Entry.Id == WatchtowerFirstFloorWorldLocationId)
                watchtowerFirstFloorTriggerGuid = firstFloorEntity.Guid;

            if (entity is IWorldLocationVolumeGridTriggerEntity upperFloorsEntity
                && upperFloorsEntity.Entry.Id == WatchtowerUpperFloorsWorldLocationId)
                watchtowerUpperFloorsTriggerGuid = upperFloorsEntity.Guid;

            if (entity is IWorldLocationVolumeGridTriggerEntity topFloorEntity
                && topFloorEntity.Entry.Id == WatchtowerTopFloorWorldLocationId)
                watchtowerTopFloorTriggerGuid = topFloorEntity.Guid;

            if (entity is IWorldLocationVolumeGridTriggerEntity courtyardRegroupEntity
                && courtyardRegroupEntity.Entry.Id == CourtyardRegroupWorldLocationId)
                courtyardRegroupTriggerGuid = courtyardRegroupEntity.Guid;

            if (entity is IWorldLocationVolumeGridTriggerEntity courtyardStatueEntity
                && courtyardStatueEntity.Entry.Id == CourtyardStatueWorldLocationId)
                courtyardStatueTriggerGuid = courtyardStatueEntity.Guid;

            if (entity is IWorldLocationVolumeGridTriggerEntity vaultDoorEntity
                && vaultDoorEntity.Entry.Id == VaultDoorWorldLocationId)
                vaultDoorTriggerGuid = vaultDoorEntity.Guid;

            if (entity is IWorldLocationVolumeGridTriggerEntity vaultEntranceEntity
                && vaultEntranceEntity.Entry.Id == VaultEntranceWorldLocationId)
                vaultEntranceTriggerGuid = vaultEntranceEntity.Guid;

            if (entity is IWorldLocationVolumeGridTriggerEntity vaultExitEntity
                && vaultExitEntity.Entry.Id == VaultExitWorldLocationId)
                vaultExitTriggerGuid = vaultExitEntity.Guid;

            if (entity is IWorldLocationVolumeGridTriggerEntity accessTerminalEntity
                && accessTerminalEntity.Entry.Id == AccessTerminalWorldLocationId)
                accessTerminalTriggerGuid = accessTerminalEntity.Guid;
        }

        /// <summary>
        /// Invoked when a <see cref="IGridEntity"/> is removed from the map the public event is on.
        /// </summary>
        public void OnRemoveFromMap(IGridEntity entity)
        {
            if (entity is IWorldLocationVolumeGridTriggerEntity worldLocationEntity
                && worldLocationEntity.Entry.Id == OpeningGatherWorldLocationId)
                openingGatherTriggerGuid = 0;

            if (entity is IWorldLocationVolumeGridTriggerEntity conversationEntity
                && conversationEntity.Entry.Id == OpeningConversationWorldLocationId)
                openingConversationTriggerGuid = 0;

            if (entity is IWorldLocationVolumeGridTriggerEntity lockedGateInvestigationEntity
                && lockedGateInvestigationEntity.Entry.Id == LockedGateInvestigationWorldLocationId)
                lockedGateInvestigationTriggerGuid = 0;

            if (entity is IWorldLocationVolumeGridTriggerEntity bridgeMeetEntity
                && bridgeMeetEntity.Entry.Id == BridgeMeetWorldLocationId)
                bridgeMeetTriggerGuid = 0;

            if (entity is IWorldLocationVolumeGridTriggerEntity kelHavikRegroupEntity
                && kelHavikRegroupEntity.Entry.Id == KelHavikRegroupWorldLocationId)
                kelHavikRegroupTriggerGuid = 0;

            if (entity is IWorldLocationVolumeGridTriggerEntity firstFloorEntity
                && firstFloorEntity.Entry.Id == WatchtowerFirstFloorWorldLocationId)
                watchtowerFirstFloorTriggerGuid = 0;

            if (entity is IWorldLocationVolumeGridTriggerEntity upperFloorsEntity
                && upperFloorsEntity.Entry.Id == WatchtowerUpperFloorsWorldLocationId)
                watchtowerUpperFloorsTriggerGuid = 0;

            if (entity is IWorldLocationVolumeGridTriggerEntity topFloorEntity
                && topFloorEntity.Entry.Id == WatchtowerTopFloorWorldLocationId)
                watchtowerTopFloorTriggerGuid = 0;

            if (entity is IWorldLocationVolumeGridTriggerEntity courtyardRegroupEntity
                && courtyardRegroupEntity.Entry.Id == CourtyardRegroupWorldLocationId)
                courtyardRegroupTriggerGuid = 0;

            if (entity is IWorldLocationVolumeGridTriggerEntity courtyardStatueEntity
                && courtyardStatueEntity.Entry.Id == CourtyardStatueWorldLocationId)
                courtyardStatueTriggerGuid = 0;

            if (entity is IWorldLocationVolumeGridTriggerEntity vaultDoorEntity
                && vaultDoorEntity.Entry.Id == VaultDoorWorldLocationId)
                vaultDoorTriggerGuid = 0;

            if (entity is IWorldLocationVolumeGridTriggerEntity vaultEntranceEntity
                && vaultEntranceEntity.Entry.Id == VaultEntranceWorldLocationId)
                vaultEntranceTriggerGuid = 0;

            if (entity is IWorldLocationVolumeGridTriggerEntity vaultExitEntity
                && vaultExitEntity.Entry.Id == VaultExitWorldLocationId)
                vaultExitTriggerGuid = 0;

            if (entity is IWorldLocationVolumeGridTriggerEntity accessTerminalEntity
                && accessTerminalEntity.Entry.Id == AccessTerminalWorldLocationId)
                accessTerminalTriggerGuid = 0;
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.GatherInFrontOfTheCrashedShip:
                    OnPhaseGatherInFrontOfTheCrashedShip();
                    break;
                case PublicEventPhase.SpeakWithDorianAndArtemis:
                    OnPhaseSpeakWithDorianAndArtemis();
                    break;
                case PublicEventPhase.RejoinGroupAtMysteriousTree:
                    OnPhaseRejoinGroupAtMysteriousTree();
                    break;
                case PublicEventPhase.ListenToPlan:
                    OnPhaseListenToPlan();
                    break;
                case PublicEventPhase.InvestigateLockedGate:
                    OnPhaseInvestigateLockedGate();
                    break;
                case PublicEventPhase.DefeatVaregor:
                    OnPhaseDefeatVaregor();
                    break;
                case PublicEventPhase.ReviveDorianAndArtemis:
                    OnPhaseReviveDorianAndArtemis();
                    break;
                case PublicEventPhase.MeetAtTheBridge:
                    OnPhaseMeetAtTheBridge();
                    break;
                case PublicEventPhase.DestroyBridgeIce:
                    OnPhaseDestroyBridgeIce();
                    break;
                case PublicEventPhase.FollowIntoKelHavikFortress:
                    OnPhaseFollowIntoKelHavikFortress();
                    break;
                case PublicEventPhase.RegroupOutsideKelHavikFortress:
                    OnPhaseRegroupOutsideKelHavikFortress();
                    break;
                case PublicEventPhase.StudyMysteriousTablets:
                    OnPhaseStudyMysteriousTablets();
                    break;
                case PublicEventPhase.EnterWatchtower:
                    OnPhaseEnterWatchtower();
                    break;
                case PublicEventPhase.GatherAtFirstFloor:
                    OnPhaseGatherAtFirstFloor();
                    break;
                case PublicEventPhase.ExploreUpperFloors:
                    OnPhaseExploreUpperFloors();
                    break;
                case PublicEventPhase.ExploreTopFloor:
                    OnPhaseExploreTopFloor();
                    break;
                case PublicEventPhase.UseElevatorToBottomFloor:
                    OnPhaseUseElevatorToBottomFloor();
                    break;
                case PublicEventPhase.FollowBackToCourtyard:
                    OnPhaseFollowBackToCourtyard();
                    break;
                case PublicEventPhase.RegroupInKelHavikCourtyard:
                    OnPhaseRegroupInKelHavikCourtyard();
                    break;
                case PublicEventPhase.MeetAtKelHavikCourtyard:
                    OnPhaseMeetAtKelHavikCourtyard();
                    break;
                case PublicEventPhase.FindCourtyardKeys:
                    OnPhaseFindCourtyardKeys();
                    break;
                case PublicEventPhase.PlaceCourtyardKeys:
                    OnPhasePlaceCourtyardKeys();
                    break;
                case PublicEventPhase.GatherAtVaultDoor:
                    OnPhaseGatherAtVaultDoor();
                    break;
                case PublicEventPhase.WaitVaultDoorOpening:
                    OnPhaseWaitVaultDoorOpening();
                    break;
                case PublicEventPhase.FollowInsideHall:
                    OnPhaseFollowInsideHall();
                    break;
                case PublicEventPhase.FollowToVaultEntrance:
                    OnPhaseFollowToVaultEntrance();
                    break;
                case PublicEventPhase.MeetAtVaultEntrance:
                    OnPhaseMeetAtVaultEntrance();
                    break;
                case PublicEventPhase.FollowIntoVault:
                    OnPhaseFollowIntoVault();
                    break;
                case PublicEventPhase.MeetAtAccessTerminal:
                    OnPhaseMeetAtAccessTerminal();
                    break;
                case PublicEventPhase.DeactivateVaultForceField:
                    OnPhaseDeactivateVaultForceField();
                    break;
                case PublicEventPhase.WatchHolocubeExplainVault:
                    OnPhaseWatchHolocubeExplainVault();
                    break;
                case PublicEventPhase.SpeakAboutHolocube:
                    OnPhaseSpeakAboutHolocube();
                    break;
                case PublicEventPhase.DestroyVaultConstructs:
                    OnPhaseDestroyVaultConstructs();
                    break;
                case PublicEventPhase.DefeatHarizog:
                    OnPhaseDefeatHarizog();
                    break;
                case PublicEventPhase.DefeatOsunBlockingTheWay:
                    OnPhaseDefeatOsunBlockingTheWay();
                    break;
                case PublicEventPhase.MeetAtVaultExit:
                    OnPhaseMeetAtVaultExit();
                    break;
            }
        }

        private void OnPhaseGatherInFrontOfTheCrashedShip()
        {
            // Build 16042 exposes objectives 4456/4457 as ParticipantsInTriggerVolume
            // rows targeting object 8396 at WorldLocation2 48684.
            publicEvent.ActivateObjective(PublicEventObjective.GatherInFrontOfTheCrashedShip);
            publicEvent.ActivateObjective(PublicEventObjective.GatherInFrontOfTheCrashedShipAlt);

            IWorldLocationVolumeGridTriggerEntity triggerEntity = publicEvent.CreateEntity<IWorldLocationVolumeGridTriggerEntity>();
            triggerEntity.Initialise(OpeningGatherWorldLocationId, OpeningGatherObjectId);
            AddToMap(triggerEntity, OpeningGatherPosition);
        }

        private void OnPhaseSpeakWithDorianAndArtemis()
        {
            // Build 16042 exposes objectives 4458/4459 as ParticipantsInTriggerVolume
            // rows targeting object 8278 at WorldLocation2 50547.
            publicEvent.ActivateObjective(PublicEventObjective.SpeakWithDorian);
            publicEvent.ActivateObjective(PublicEventObjective.SpeakWithArtemis);

            CreateOpeningConversationTrigger();
        }

        private void OnPhaseRejoinGroupAtMysteriousTree()
        {
            // Build 16042 objective 4949 reuses object 8278 at the same
            // WorldLocation2 50547 as the Dorian/Artemis conversation.
            // Objective 5149 is the matching zero-count Script route row for
            // following them to the mysterious tree.
            publicEvent.ActivateObjective(PublicEventObjective.RejoinGroupAtMysteriousTree);
            publicEvent.ActivateObjective(PublicEventObjective.FollowToMysteriousTree);
            publicEvent.UpdateObjective(PublicEventObjective.FollowToMysteriousTree, 0);
            CreateOpeningConversationTrigger();
        }

        private void OnPhaseListenToPlan()
        {
            // Build 16042 objective 4950 is a Script objective for the opening
            // plan sequence. Credit it directly after activation while the
            // detailed NPC conversation choreography remains blocked.
            // Objective 4980 is the paired count-one Bloodhearth tree dialog
            // row at WorldLocation2 51105, next to the same conversation area.
            publicEvent.ActivateObjective(PublicEventObjective.LearnAboutBloodhearthTrees);
            publicEvent.ActivateObjective(PublicEventObjective.ListenToPlan);
            publicEvent.UpdateObjective(PublicEventObjective.LearnAboutBloodhearthTrees, 1);
            publicEvent.UpdateObjective(PublicEventObjective.ListenToPlan, 1);
        }

        private void OnPhaseInvestigateLockedGate()
        {
            // Build 16042 objective 4299 is a Script row targeting object 7834
            // at WorldLocation2 48658, the locked-gate investigation area.
            // Objective 4384 is the matching ParticipantsInTriggerVolume
            // route row for object 7879 at the same WorldLocation2.
            publicEvent.ActivateObjective(PublicEventObjective.InvestigateLockedGate);
            publicEvent.ActivateObjective(PublicEventObjective.NavigateVaregorPass);

            IWorldLocationVolumeGridTriggerEntity triggerEntity = publicEvent.CreateEntity<IWorldLocationVolumeGridTriggerEntity>();
            triggerEntity.Initialise(LockedGateInvestigationWorldLocationId, LockedGateInvestigationObjectId);
            AddToMap(triggerEntity, LockedGateInvestigationPosition);
        }

        private void OnPhaseDefeatVaregor()
        {
            // Build 16042 objective 4300 is KillTargetGroup 12157, whose only
            // member is Creature2 67457 (Varegor the Abominable).
            publicEvent.ActivateObjective(PublicEventObjective.DefeatVaregor);
        }

        private void OnPhaseReviveDorianAndArtemis()
        {
            // Build 16042 maps objectives 4301/4302 to ActivateTargetGroup
            // TargetGroup 12158, whose members are Creature2 67423/67425.
            publicEvent.ActivateObjective(PublicEventObjective.ReviveDorian);
            publicEvent.ActivateObjective(PublicEventObjective.ReviveArtemis);
        }

        private void OnPhaseMeetAtTheBridge()
        {
            // Build 16042 objective 4303 is a ParticipantsInTriggerVolume row
            // targeting object 7836 at WorldLocation2 50596.
            publicEvent.ActivateObjective(PublicEventObjective.MeetAtTheBridge);

            IWorldLocationVolumeGridTriggerEntity triggerEntity = publicEvent.CreateEntity<IWorldLocationVolumeGridTriggerEntity>();
            triggerEntity.Initialise(BridgeMeetWorldLocationId, BridgeMeetObjectId);
            AddToMap(triggerEntity, BridgeMeetPosition);
        }

        private void OnPhaseDestroyBridgeIce()
        {
            // Build 16042 objective 4304 is ActivateTargetGroupChecklist
            // TargetGroup 14163, whose Creature2 member is weak point 72108.
            publicEvent.ActivateObjective(PublicEventObjective.PlaceExplosivesOnBridgeIce);
        }

        private void OnPhaseFollowIntoKelHavikFortress()
        {
            // Build 16042 objective 4305 is a zero-count Script row for object
            // 8343. Credit it directly while the exact NPC walk choreography is blocked.
            publicEvent.ActivateObjective(PublicEventObjective.FollowIntoKelHavikFortress);
            publicEvent.UpdateObjective(PublicEventObjective.FollowIntoKelHavikFortress, 0);
        }

        private void OnPhaseRegroupOutsideKelHavikFortress()
        {
            // Build 16042 objective 4306 targets object 7857 at WorldLocation2
            // 50597 outside Kel Havik fortress.
            publicEvent.ActivateObjective(PublicEventObjective.RegroupOutsideKelHavikFortress);

            IWorldLocationVolumeGridTriggerEntity triggerEntity = publicEvent.CreateEntity<IWorldLocationVolumeGridTriggerEntity>();
            triggerEntity.Initialise(KelHavikRegroupWorldLocationId, KelHavikRegroupObjectId);
            AddToMap(triggerEntity, KelHavikRegroupPosition);
        }

        private void OnPhaseStudyMysteriousTablets()
        {
            // Build 16042 objective 4308 is a Script row for object 7860, count
            // 1. Credit it after activation until tablet-study choreography is proven.
            publicEvent.ActivateObjective(PublicEventObjective.StudyMysteriousTablets);
            publicEvent.UpdateObjective(PublicEventObjective.StudyMysteriousTablets, 1);
        }

        private void OnPhaseEnterWatchtower()
        {
            // Build 16042 objective 4310 is a Turnstile row for object 7861 at
            // WorldLocation2 48499 with radius 9.73506.
            publicEvent.ActivateObjective(PublicEventObjective.EnterWatchtower);

            ITurnstileGridTriggerEntity triggerEntity = publicEvent.CreateEntity<ITurnstileGridTriggerEntity>();
            triggerEntity.Initialise(WatchtowerTurnstileTriggerId, WatchtowerTurnstileRange, WatchtowerTurnstileObjectId);
            AddToMap(triggerEntity, WatchtowerTurnstilePosition);
        }

        private void OnPhaseGatherAtFirstFloor()
        {
            // Build 16042 objective 4311 is a trigger-volume row for object
            // 8527. Its QuestDirection 2442 starts at WorldLocation2 48479.
            publicEvent.ActivateObjective(PublicEventObjective.GatherAtFirstFloor);
            CreateWorldLocationTrigger(
                WatchtowerFirstFloorWorldLocationId,
                WatchtowerFirstFloorObjectId,
                WatchtowerFirstFloorPosition);
        }

        private void OnPhaseExploreUpperFloors()
        {
            // Build 16042 objective 5002 is a trigger-volume row for object
            // 8345. QuestDirection 2483 starts at WorldLocation2 50579.
            publicEvent.ActivateObjective(PublicEventObjective.ExploreUpperFloors);
            // Optional objective 4318 is KillEventUnit object 14128, whose
            // TargetGroup member is Creature2 71414 (Unbound Flame Elemental).
            publicEvent.ActivateObjective(PublicEventObjective.DefeatUnboundFlameElemental);
            // Optional objective 4972 is KillTargetGroup object 14275, whose
            // TargetGroup member is Creature2 71577 (Icebound Overlord).
            publicEvent.ActivateObjective(PublicEventObjective.DefeatIceboundOverlord);
            CreateWorldLocationTrigger(
                WatchtowerUpperFloorsWorldLocationId,
                WatchtowerUpperFloorsObjectId,
                WatchtowerUpperFloorsPosition);
        }

        private void OnPhaseExploreTopFloor()
        {
            // Build 16042 objective 5054 is a trigger-volume row for object
            // 8349. QuestDirection 2484 starts at WorldLocation2 50367.
            publicEvent.ActivateObjective(PublicEventObjective.ExploreTopFloor);
            // Optional objective 5001 is KillEventUnit object 14048, whose
            // TargetGroup member is Creature2 71173 (Darkwitch Yotul).
            publicEvent.ActivateObjective(PublicEventObjective.DefeatDarkwitchYotul);
            CreateWorldLocationTrigger(
                WatchtowerTopFloorWorldLocationId,
                WatchtowerTopFloorObjectId,
                WatchtowerTopFloorPosition);
        }

        private void OnPhaseUseElevatorToBottomFloor()
        {
            // Build 16042 objective 5133 is a zero-count Script row whose text
            // points at Creature2 72199, the second-floor elevator control.
            publicEvent.ActivateObjective(PublicEventObjective.UseElevatorToBottomFloor);
        }

        private void OnPhaseFollowBackToCourtyard()
        {
            // Build 16042 objective 5136 is a zero-count Script row for object
            // 7839. Credit it directly until exact NPC return pathing is proven.
            publicEvent.ActivateObjective(PublicEventObjective.FollowBackToCourtyard);
            publicEvent.UpdateObjective(PublicEventObjective.FollowBackToCourtyard, 0);
        }

        private void OnPhaseRegroupInKelHavikCourtyard()
        {
            // Build 16042 objective 4340 is a trigger-volume row for object
            // 8304. QuestDirection 2450 ends at WorldLocation2 51027.
            publicEvent.ActivateObjective(PublicEventObjective.RegroupInKelHavikCourtyard);
            CreateWorldLocationTrigger(
                CourtyardRegroupWorldLocationId,
                CourtyardRegroupObjectId,
                CourtyardRegroupPosition);
        }

        private void OnPhaseMeetAtKelHavikCourtyard()
        {
            // Build 16042 objective 4320 is a trigger-volume row for object
            // 7894. QuestDirection 2443 resolves to WorldLocation2 50605.
            publicEvent.ActivateObjective(PublicEventObjective.MeetAtKelHavikCourtyard);
            CreateWorldLocationTrigger(
                CourtyardStatueWorldLocationId,
                CourtyardStatueObjectId,
                CourtyardStatuePosition);
        }

        private void OnPhaseFindCourtyardKeys()
        {
            // Build 16042 objective 4314 is ActivateTargetGroup 14104, whose
            // members are the Kel Havik staff/hammer/polearm key objects
            // 71617/71618/71619. Reward-pane TargetGroup 14306 also references
            // the matching frozen relic creatures 73275/73276/73277.
            // Objective 5155 is the same route segment's count-five
            // ActivateTargetGroup row for TargetGroup 14276, whose member is
            // Creature2 72958 (w3009 - Key Fragment - Vault Secret Room).
            publicEvent.ActivateObjective(PublicEventObjective.FindCourtyardKeys);
            publicEvent.ActivateObjective(PublicEventObjective.CollectKeyFragments);
        }

        private void OnPhasePlaceCourtyardKeys()
        {
            // Build 16042 objective 4322 is ActivateTargetGroupChecklist 14212
            // with count 3. The TargetGroup member is Creature2 72367, the Kel
            // Havik door-lock placement object; exact socket placement remains blocked.
            publicEvent.ActivateObjective(PublicEventObjective.PlaceCourtyardKeys);
        }

        private void OnPhaseGatherAtVaultDoor()
        {
            // Build 16042 objective 4986 is a ParticipantsInTriggerVolume row
            // for object 8314. QuestDirection 2444 starts at QuestDirectionEntry
            // 3986, which resolves to WorldLocation2 48426 in world 3009.
            publicEvent.ActivateObjective(PublicEventObjective.GatherAtVaultDoor);
            CreateWorldLocationTrigger(
                VaultDoorWorldLocationId,
                VaultDoorObjectId,
                VaultDoorPosition);
        }

        private void OnPhaseWaitVaultDoorOpening()
        {
            // Build 16042 objective 4987 is a zero-count Script row on the same
            // QuestDirection 2444 route. Credit it directly while exact
            // Dorian/Artemis door-opening choreography remains blocked.
            publicEvent.ActivateObjective(PublicEventObjective.WaitVaultDoorOpening);
            publicEvent.UpdateObjective(PublicEventObjective.WaitVaultDoorOpening, 0);
        }

        private void OnPhaseFollowInsideHall()
        {
            // Build 16042 objective 4323 is a count-one Script row for the
            // route into the Hall of the Hundred. Credit it directly while
            // exact Dorian/Artemis path choreography remains blocked.
            publicEvent.ActivateObjective(PublicEventObjective.FollowInsideHall);
            publicEvent.UpdateObjective(PublicEventObjective.FollowInsideHall, 1);
        }

        private void OnPhaseFollowToVaultEntrance()
        {
            // Build 16042 objective 4324 is a zero-count Script row for the
            // route to the Vault of the Archon entrance.
            publicEvent.ActivateObjective(PublicEventObjective.FollowToVaultEntrance);
            publicEvent.UpdateObjective(PublicEventObjective.FollowToVaultEntrance, 0);
        }

        private void OnPhaseMeetAtVaultEntrance()
        {
            // Build 16042 objective 5260 is a ParticipantsInTriggerVolume row
            // for object 8298. QuestDirection 2445 starts at QuestDirectionEntry
            // 3989, which resolves to WorldLocation2 50645 in world 3009.
            publicEvent.ActivateObjective(PublicEventObjective.MeetAtVaultEntrance);
            CreateWorldLocationTrigger(
                VaultEntranceWorldLocationId,
                VaultEntranceObjectId,
                VaultEntrancePosition);
        }

        private void OnPhaseFollowIntoVault()
        {
            // Build 16042 objective 5249 is a count-one ScriptWithoutCount row
            // for the QuestDirection 2446 route into the Vault of the Archon.
            publicEvent.ActivateObjective(PublicEventObjective.FollowIntoVault);
            publicEvent.UpdateObjective(PublicEventObjective.FollowIntoVault, 1);
        }

        private void OnPhaseMeetAtAccessTerminal()
        {
            // Build 16042 objective 4325 is a ParticipantsInTriggerVolume row
            // for object 8295. QuestDirection 2446 starts at QuestDirectionEntry
            // 3990, which resolves to WorldLocation2 51020 in world 3009.
            publicEvent.ActivateObjective(PublicEventObjective.MeetAtAccessTerminal);
            CreateWorldLocationTrigger(
                AccessTerminalWorldLocationId,
                AccessTerminalObjectId,
                AccessTerminalPosition);
        }

        private void OnPhaseDeactivateVaultForceField()
        {
            // Build 16042 objective 4326 is a count-one Script row using the
            // same QuestDirection 2446 as the access terminal. Credit the wait
            // step directly while Artemis/Dorian force-field choreography remains blocked.
            publicEvent.ActivateObjective(PublicEventObjective.DeactivateVaultForceField);
            publicEvent.UpdateObjective(PublicEventObjective.DeactivateVaultForceField, 1);
        }

        private void OnPhaseWatchHolocubeExplainVault()
        {
            // Build 16042 objective 4971 is a count-one Script row using
            // QuestDirection 2448 for the Holocube explanation after force-field
            // deactivation. Credit it directly until exact presentation timing is proven.
            publicEvent.ActivateObjective(PublicEventObjective.WatchHolocubeExplainVault);
            publicEvent.UpdateObjective(PublicEventObjective.WatchHolocubeExplainVault, 1);
        }

        private void OnPhaseSpeakAboutHolocube()
        {
            // Build 16042 objective 5100 is TalkTo TargetGroup 14194, whose
            // members are Creature2 67423/67425 (Dorian Walker and Artemis Zin).
            publicEvent.ActivateObjective(PublicEventObjective.SpeakAboutHolocube);
        }

        private void OnPhaseDestroyVaultConstructs()
        {
            // Build 16042 objective 4327 is a count-six Script row tied to
            // QuestDirection 2447 and Forcefield Power Link Creature2 72903.
            // Power-link activation is credited by ForcefieldPowerLinkEntityScript;
            // exact construct kills, spell 83784 gating, and portal mechanics
            // remain blocked.
            publicEvent.ActivateObjective(PublicEventObjective.DestroyVaultConstructs);
        }

        private void OnPhaseDefeatHarizog()
        {
            // Build 16042 objective 4328 targets TargetGroup 12162, whose
            // only member is Creature2 67444 (Harizog Coldblood). The same
            // phase exposes objective 4973, a zero-count Script row for
            // witnessing Harizog break free of his ice prison, plus objective
            // rows 4952/4953/4954 for Harizog's
            // summoned add set, backed by Creature2 67851/67855/67853 and
            // TargetGroup 12211; their timing and spawn cadence remain blocked.
            publicEvent.ActivateObjective(PublicEventObjective.WitnessHarizogBreakFree);
            publicEvent.ActivateObjective(PublicEventObjective.DefeatHarizog);
            publicEvent.ActivateObjective(PublicEventObjective.DefeatHavikShiverhound);
            publicEvent.ActivateObjective(PublicEventObjective.DefeatDarkwitchUhrga);
            publicEvent.ActivateObjective(PublicEventObjective.DefeatHavikHonorguard);
            publicEvent.UpdateObjective(PublicEventObjective.WitnessHarizogBreakFree, 0);
        }

        private void OnPhaseDefeatOsunBlockingTheWay()
        {
            // Build 16042 objective 4329 is an Exterminate row for object
            // 7839. Creature2 67429/67430 now supply tested kill credit;
            // exact Osun wave/spawn choreography remains blocked.
            publicEvent.ActivateObjective(PublicEventObjective.DefeatOsunBlockingTheWay);
        }

        private void OnPhaseMeetAtVaultExit()
        {
            // Build 16042 objective 4994 is a ParticipantsInTriggerVolume row
            // for object 8339. QuestDirection 2449 starts at QuestDirectionEntry
            // 3993, which resolves to WorldLocation2 50702 in world 3009.
            publicEvent.ActivateObjective(PublicEventObjective.MeetAtVaultExit);
            CreateWorldLocationTrigger(
                VaultExitWorldLocationId,
                VaultExitObjectId,
                VaultExitPosition);
        }

        public void OnPublicEventObjectiveStatus(IPublicEventObjective objective)
        {
            if (objective.Status != PublicEventStatus.Succeeded)
                return;

            switch ((PublicEventObjective)objective.Entry.Id)
            {
                case PublicEventObjective.GatherInFrontOfTheCrashedShip:
                case PublicEventObjective.GatherInFrontOfTheCrashedShipAlt:
                    RemoveTrigger(openingGatherTriggerGuid);
                    StartOpeningConversationPhase();
                    break;
                case PublicEventObjective.SpeakWithDorian:
                case PublicEventObjective.SpeakWithArtemis:
                    RemoveTrigger(openingConversationTriggerGuid);
                    StartRejoinPhase();
                    break;
                case PublicEventObjective.RejoinGroupAtMysteriousTree:
                    RemoveTrigger(openingConversationTriggerGuid);
                    StartListenToPlanPhase();
                    break;
                case PublicEventObjective.ListenToPlan:
                    StartLockedGateInvestigationPhase();
                    break;
                case PublicEventObjective.InvestigateLockedGate:
                    RemoveTrigger(lockedGateInvestigationTriggerGuid);
                    StartVaregorPhase();
                    break;
                case PublicEventObjective.DefeatVaregor:
                    StartReviveCompanionsPhase();
                    break;
                case PublicEventObjective.ReviveDorian:
                    reviveDorianSucceeded = true;
                    StartMeetAtBridgePhaseWhenReady();
                    break;
                case PublicEventObjective.ReviveArtemis:
                    reviveArtemisSucceeded = true;
                    StartMeetAtBridgePhaseWhenReady();
                    break;
                case PublicEventObjective.MeetAtTheBridge:
                    RemoveTrigger(bridgeMeetTriggerGuid);
                    StartDestroyBridgeIcePhase();
                    break;
                case PublicEventObjective.PlaceExplosivesOnBridgeIce:
                    StartFollowIntoKelHavikFortressPhase();
                    break;
                case PublicEventObjective.FollowIntoKelHavikFortress:
                    StartRegroupOutsideKelHavikFortressPhase();
                    break;
                case PublicEventObjective.RegroupOutsideKelHavikFortress:
                    RemoveTrigger(kelHavikRegroupTriggerGuid);
                    StartStudyMysteriousTabletsPhase();
                    break;
                case PublicEventObjective.StudyMysteriousTablets:
                    StartEnterWatchtowerPhase();
                    break;
                case PublicEventObjective.EnterWatchtower:
                    StartGatherAtFirstFloorPhase();
                    break;
                case PublicEventObjective.GatherAtFirstFloor:
                    RemoveTrigger(watchtowerFirstFloorTriggerGuid);
                    StartExploreUpperFloorsPhase();
                    break;
                case PublicEventObjective.ExploreUpperFloors:
                    RemoveTrigger(watchtowerUpperFloorsTriggerGuid);
                    StartExploreTopFloorPhase();
                    break;
                case PublicEventObjective.ExploreTopFloor:
                    RemoveTrigger(watchtowerTopFloorTriggerGuid);
                    StartUseElevatorToBottomFloorPhase();
                    break;
                case PublicEventObjective.UseElevatorToBottomFloor:
                    StartFollowBackToCourtyardPhase();
                    break;
                case PublicEventObjective.FollowBackToCourtyard:
                    StartRegroupInKelHavikCourtyardPhase();
                    break;
                case PublicEventObjective.RegroupInKelHavikCourtyard:
                    RemoveTrigger(courtyardRegroupTriggerGuid);
                    StartMeetAtKelHavikCourtyardPhase();
                    break;
                case PublicEventObjective.MeetAtKelHavikCourtyard:
                    RemoveTrigger(courtyardStatueTriggerGuid);
                    StartFindCourtyardKeysPhase();
                    break;
                case PublicEventObjective.FindCourtyardKeys:
                    findCourtyardKeysSucceeded = true;
                    StartPlaceCourtyardKeysPhaseWhenReady();
                    break;
                case PublicEventObjective.CollectKeyFragments:
                    collectKeyFragmentsSucceeded = true;
                    StartPlaceCourtyardKeysPhaseWhenReady();
                    break;
                case PublicEventObjective.PlaceCourtyardKeys:
                    StartGatherAtVaultDoorPhase();
                    break;
                case PublicEventObjective.GatherAtVaultDoor:
                    RemoveTrigger(vaultDoorTriggerGuid);
                    StartWaitVaultDoorOpeningPhase();
                    break;
                case PublicEventObjective.WaitVaultDoorOpening:
                    StartFollowInsideHallPhase();
                    break;
                case PublicEventObjective.FollowInsideHall:
                    StartFollowToVaultEntrancePhase();
                    break;
                case PublicEventObjective.FollowToVaultEntrance:
                    StartMeetAtVaultEntrancePhase();
                    break;
                case PublicEventObjective.MeetAtVaultEntrance:
                    RemoveTrigger(vaultEntranceTriggerGuid);
                    StartFollowIntoVaultPhase();
                    break;
                case PublicEventObjective.FollowIntoVault:
                    StartMeetAtAccessTerminalPhase();
                    break;
                case PublicEventObjective.MeetAtAccessTerminal:
                    RemoveTrigger(accessTerminalTriggerGuid);
                    StartDeactivateVaultForceFieldPhase();
                    break;
                case PublicEventObjective.DeactivateVaultForceField:
                    StartWatchHolocubeExplainVaultPhase();
                    break;
                case PublicEventObjective.WatchHolocubeExplainVault:
                    StartSpeakAboutHolocubePhase();
                    break;
                case PublicEventObjective.SpeakAboutHolocube:
                    StartDestroyVaultConstructsPhase();
                    break;
                case PublicEventObjective.DestroyVaultConstructs:
                    StartDefeatHarizogPhase();
                    break;
                case PublicEventObjective.DefeatHarizog:
                    StartDefeatOsunBlockingTheWayPhase();
                    break;
                case PublicEventObjective.DefeatOsunBlockingTheWay:
                    StartMeetAtVaultExitPhase();
                    break;
                case PublicEventObjective.MeetAtVaultExit:
                    RemoveTrigger(vaultExitTriggerGuid);
                    publicEvent.Finish(PublicEventTeam.PublicTeam);
                    break;
            }
        }

        private void StartOpeningConversationPhase()
        {
            if (openingConversationPhaseStarted)
                return;

            openingConversationPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.SpeakWithDorianAndArtemis);
        }

        private void StartRejoinPhase()
        {
            if (rejoinPhaseStarted)
                return;

            rejoinPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.RejoinGroupAtMysteriousTree);
        }

        private void StartListenToPlanPhase()
        {
            if (listenToPlanPhaseStarted)
                return;

            listenToPlanPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.ListenToPlan);
        }

        private void StartLockedGateInvestigationPhase()
        {
            if (lockedGateInvestigationPhaseStarted)
                return;

            lockedGateInvestigationPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.InvestigateLockedGate);
        }

        private void StartVaregorPhase()
        {
            if (varegorPhaseStarted)
                return;

            varegorPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.DefeatVaregor);
        }

        private void StartReviveCompanionsPhase()
        {
            if (reviveCompanionsPhaseStarted)
                return;

            reviveCompanionsPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.ReviveDorianAndArtemis);
        }

        private void StartMeetAtBridgePhaseWhenReady()
        {
            if (!reviveDorianSucceeded || !reviveArtemisSucceeded)
                return;

            if (meetAtBridgePhaseStarted)
                return;

            meetAtBridgePhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.MeetAtTheBridge);
        }

        private void StartDestroyBridgeIcePhase()
        {
            if (destroyBridgeIcePhaseStarted)
                return;

            destroyBridgeIcePhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.DestroyBridgeIce);
        }

        private void StartFollowIntoKelHavikFortressPhase()
        {
            if (followIntoKelHavikFortressPhaseStarted)
                return;

            followIntoKelHavikFortressPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.FollowIntoKelHavikFortress);
        }

        private void StartRegroupOutsideKelHavikFortressPhase()
        {
            if (regroupOutsideKelHavikFortressPhaseStarted)
                return;

            regroupOutsideKelHavikFortressPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.RegroupOutsideKelHavikFortress);
        }

        private void StartStudyMysteriousTabletsPhase()
        {
            if (studyMysteriousTabletsPhaseStarted)
                return;

            studyMysteriousTabletsPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.StudyMysteriousTablets);
        }

        private void StartEnterWatchtowerPhase()
        {
            if (enterWatchtowerPhaseStarted)
                return;

            enterWatchtowerPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.EnterWatchtower);
        }

        private void StartGatherAtFirstFloorPhase()
        {
            if (gatherAtFirstFloorPhaseStarted)
                return;

            gatherAtFirstFloorPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.GatherAtFirstFloor);
        }

        private void StartExploreUpperFloorsPhase()
        {
            if (exploreUpperFloorsPhaseStarted)
                return;

            exploreUpperFloorsPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.ExploreUpperFloors);
        }

        private void StartExploreTopFloorPhase()
        {
            if (exploreTopFloorPhaseStarted)
                return;

            exploreTopFloorPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.ExploreTopFloor);
        }

        private void StartUseElevatorToBottomFloorPhase()
        {
            if (useElevatorToBottomFloorPhaseStarted)
                return;

            useElevatorToBottomFloorPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.UseElevatorToBottomFloor);
        }

        private void StartFollowBackToCourtyardPhase()
        {
            if (followBackToCourtyardPhaseStarted)
                return;

            followBackToCourtyardPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.FollowBackToCourtyard);
        }

        private void StartRegroupInKelHavikCourtyardPhase()
        {
            if (regroupInKelHavikCourtyardPhaseStarted)
                return;

            regroupInKelHavikCourtyardPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.RegroupInKelHavikCourtyard);
        }

        private void StartMeetAtKelHavikCourtyardPhase()
        {
            if (meetAtKelHavikCourtyardPhaseStarted)
                return;

            meetAtKelHavikCourtyardPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.MeetAtKelHavikCourtyard);
        }

        private void StartFindCourtyardKeysPhase()
        {
            if (findCourtyardKeysPhaseStarted)
                return;

            findCourtyardKeysPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.FindCourtyardKeys);
        }

        private void StartPlaceCourtyardKeysPhaseWhenReady()
        {
            if (!findCourtyardKeysSucceeded || !collectKeyFragmentsSucceeded)
                return;

            StartPlaceCourtyardKeysPhase();
        }

        private void StartPlaceCourtyardKeysPhase()
        {
            if (placeCourtyardKeysPhaseStarted)
                return;

            placeCourtyardKeysPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.PlaceCourtyardKeys);
        }

        private void StartGatherAtVaultDoorPhase()
        {
            if (gatherAtVaultDoorPhaseStarted)
                return;

            gatherAtVaultDoorPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.GatherAtVaultDoor);
        }

        private void StartWaitVaultDoorOpeningPhase()
        {
            if (waitVaultDoorOpeningPhaseStarted)
                return;

            waitVaultDoorOpeningPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.WaitVaultDoorOpening);
        }

        private void StartFollowInsideHallPhase()
        {
            if (followInsideHallPhaseStarted)
                return;

            followInsideHallPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.FollowInsideHall);
        }

        private void StartFollowToVaultEntrancePhase()
        {
            if (followToVaultEntrancePhaseStarted)
                return;

            followToVaultEntrancePhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.FollowToVaultEntrance);
        }

        private void StartMeetAtVaultEntrancePhase()
        {
            if (meetAtVaultEntrancePhaseStarted)
                return;

            meetAtVaultEntrancePhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.MeetAtVaultEntrance);
        }

        private void StartFollowIntoVaultPhase()
        {
            if (followIntoVaultPhaseStarted)
                return;

            followIntoVaultPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.FollowIntoVault);
        }

        private void StartMeetAtAccessTerminalPhase()
        {
            if (meetAtAccessTerminalPhaseStarted)
                return;

            meetAtAccessTerminalPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.MeetAtAccessTerminal);
        }

        private void StartDeactivateVaultForceFieldPhase()
        {
            if (deactivateVaultForceFieldPhaseStarted)
                return;

            deactivateVaultForceFieldPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.DeactivateVaultForceField);
        }

        private void StartWatchHolocubeExplainVaultPhase()
        {
            if (watchHolocubeExplainVaultPhaseStarted)
                return;

            watchHolocubeExplainVaultPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.WatchHolocubeExplainVault);
        }

        private void StartSpeakAboutHolocubePhase()
        {
            if (speakAboutHolocubePhaseStarted)
                return;

            speakAboutHolocubePhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.SpeakAboutHolocube);
        }

        private void StartDestroyVaultConstructsPhase()
        {
            if (destroyVaultConstructsPhaseStarted)
                return;

            destroyVaultConstructsPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.DestroyVaultConstructs);
        }

        private void StartDefeatHarizogPhase()
        {
            if (defeatHarizogPhaseStarted)
                return;

            defeatHarizogPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.DefeatHarizog);
        }

        private void StartDefeatOsunBlockingTheWayPhase()
        {
            if (defeatOsunBlockingTheWayPhaseStarted)
                return;

            defeatOsunBlockingTheWayPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.DefeatOsunBlockingTheWay);
        }

        private void StartMeetAtVaultExitPhase()
        {
            if (meetAtVaultExitPhaseStarted)
                return;

            meetAtVaultExitPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.MeetAtVaultExit);
        }

        private void CreateOpeningConversationTrigger()
        {
            CreateWorldLocationTrigger(
                OpeningConversationWorldLocationId,
                OpeningConversationObjectId,
                OpeningConversationPosition);
        }

        private void CreateWorldLocationTrigger(uint worldLocationId, uint objectId, Vector3 position)
        {
            IWorldLocationVolumeGridTriggerEntity triggerEntity = publicEvent.CreateEntity<IWorldLocationVolumeGridTriggerEntity>();
            triggerEntity.Initialise(worldLocationId, objectId);
            AddToMap(triggerEntity, position);
        }

        private void RemoveTrigger(uint guid)
        {
            IWorldLocationVolumeGridTriggerEntity triggerEntity = mapInstance.GetEntity<IWorldLocationVolumeGridTriggerEntity>(guid);
            triggerEntity?.RemoveFromMap();
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
