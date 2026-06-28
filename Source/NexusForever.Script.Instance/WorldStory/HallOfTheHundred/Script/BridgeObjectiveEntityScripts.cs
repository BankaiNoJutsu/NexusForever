using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.WorldStory.HallOfTheHundred.Script
{
    public abstract class HallDirectObjectiveEntityScriptBase : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private readonly PublicEventObjective objective;
        private readonly int count;
        private readonly bool removeOnActivation;

        private IWorldEntity entity;
        private bool activated;

        protected HallDirectObjectiveEntityScriptBase(
            PublicEventObjective objective,
            int count = 1,
            bool removeOnActivation = false)
        {
            this.objective           = objective;
            this.count               = count;
            this.removeOnActivation  = removeOnActivation;
        }

        public void OnLoad(IWorldEntity owner)
        {
            entity = owner;
        }

        public void OnActivateSuccess(IPlayer _)
        {
            if (activated)
                return;

            activated = true;
            entity.Map.PublicEventManager.UpdateObjective(objective, count);

            if (removeOnActivation)
                entity.RemoveFromMap();
        }
    }

    public abstract class HallTargetGroupObjectiveEntityScriptBase : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private readonly PublicEventObjectiveType objectiveType;
        private readonly uint targetGroupId;
        private readonly bool removeOnActivation;

        private IWorldEntity entity;
        private bool activated;

        protected HallTargetGroupObjectiveEntityScriptBase(
            PublicEventObjectiveType objectiveType,
            uint targetGroupId,
            bool removeOnActivation)
        {
            this.objectiveType       = objectiveType;
            this.targetGroupId       = targetGroupId;
            this.removeOnActivation  = removeOnActivation;
        }

        public void OnLoad(IWorldEntity owner)
        {
            entity = owner;
        }

        public void OnActivateSuccess(IPlayer _)
        {
            if (activated)
                return;

            activated = true;
            entity.Map.PublicEventManager.UpdateObjective(objectiveType, targetGroupId, GetObjectiveUpdateCount());

            if (removeOnActivation)
                entity.RemoveFromMap();
        }

        private int GetObjectiveUpdateCount()
        {
            if (objectiveType is PublicEventObjectiveType.ActivateTargetGroupChecklist
                or PublicEventObjectiveType.TalkToChecklist)
                return entity.QuestChecklistIdx;

            return 1;
        }
    }

    public abstract class HallTalkObjectiveEntityScriptBase : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private readonly uint targetGroupId;
        private readonly HashSet<ulong> creditedCharacterIds = [];

        private IWorldEntity entity;

        protected HallTalkObjectiveEntityScriptBase(uint targetGroupId)
        {
            this.targetGroupId = targetGroupId;
        }

        public void OnLoad(IWorldEntity owner)
        {
            entity = owner;
        }

        public void OnActivateSuccess(IPlayer player)
        {
            if (!creditedCharacterIds.Add(player.CharacterId))
                return;

            entity.Map.PublicEventManager.UpdateObjective(
                player,
                PublicEventObjectiveType.TalkTo,
                targetGroupId,
                1);
        }
    }

    /// <summary>
    /// Build 16042 maps objective 4301 (Revive Dorian) through TargetGroup
    /// 12158, which includes Creature2 67423 (w3009 - Dorian Walker).
    /// </summary>
    [ScriptFilterCreatureId(67423u)]
    public class DorianWalkerReviveEntityScript : HallDirectObjectiveEntityScriptBase
    {
        public DorianWalkerReviveEntityScript()
            : base(PublicEventObjective.ReviveDorian)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 4302 (Revive Artemis) through TargetGroup
    /// 12158, which includes Creature2 67425 (w3009 - Artemis Zin).
    /// </summary>
    [ScriptFilterCreatureId(67425u)]
    public class ArtemisZinReviveEntityScript : HallDirectObjectiveEntityScriptBase
    {
        public ArtemisZinReviveEntityScript()
            : base(PublicEventObjective.ReviveArtemis)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 5100 to TalkTo TargetGroup 14194, whose
    /// members include Creature2 67423 (w3009 - Dorian Walker).
    /// </summary>
    [ScriptFilterCreatureId(67423u)]
    public class DorianWalkerHolocubeTalkEntityScript : HallTalkObjectiveEntityScriptBase
    {
        public DorianWalkerHolocubeTalkEntityScript()
            : base(14194u)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 5100 to TalkTo TargetGroup 14194, whose
    /// members include Creature2 67425 (w3009 - Artemis Zin).
    /// </summary>
    [ScriptFilterCreatureId(67425u)]
    public class ArtemisZinHolocubeTalkEntityScript : HallTalkObjectiveEntityScriptBase
    {
        public ArtemisZinHolocubeTalkEntityScript()
            : base(14194u)
        {
        }
    }

    /// <summary>
    /// Build 16042 objective 5133 asks the player to use Creature2 72199,
    /// the second-floor elevator control, to return to the bottom floor.
    /// </summary>
    [ScriptFilterCreatureId(72199u)]
    public class WatchtowerReturnElevatorEntityScript : HallDirectObjectiveEntityScriptBase
    {
        public WatchtowerReturnElevatorEntityScript()
            : base(PublicEventObjective.UseElevatorToBottomFloor, 0)
        {
        }
    }

    /// <summary>
    /// Build 16042 objective 4327 is a count-six Script row whose text points
    /// players at Creature2 72903, the Forcefield Power Link.
    /// </summary>
    [ScriptFilterCreatureId(72903u)]
    public class ForcefieldPowerLinkEntityScript : HallDirectObjectiveEntityScriptBase
    {
        public ForcefieldPowerLinkEntityScript()
            : base(PublicEventObjective.DestroyVaultConstructs, removeOnActivation: true)
        {
        }
    }

    /// <summary>
    /// Build 16042 objective 4314 uses ActivateTargetGroup 14104, whose first
    /// member is Creature2 71617 (w3009 - WS2 - Kel Havik Staff).
    /// </summary>
    [ScriptFilterCreatureId(71617u)]
    public class KelHavikStaffKeyEntityScript : HallTargetGroupObjectiveEntityScriptBase
    {
        public KelHavikStaffKeyEntityScript()
            : base(PublicEventObjectiveType.ActivateTargetGroup, 14104u, true)
        {
        }
    }

    /// <summary>
    /// Build 16042 objective 4314 uses ActivateTargetGroup 14104, whose second
    /// member is Creature2 71618 (w3009 - WS2 - Kel Havik Hammer).
    /// </summary>
    [ScriptFilterCreatureId(71618u)]
    public class KelHavikHammerKeyEntityScript : HallTargetGroupObjectiveEntityScriptBase
    {
        public KelHavikHammerKeyEntityScript()
            : base(PublicEventObjectiveType.ActivateTargetGroup, 14104u, true)
        {
        }
    }

    /// <summary>
    /// Build 16042 objective 4314 uses ActivateTargetGroup 14104, whose third
    /// member is Creature2 71619 (w3009 - WS2 - Kel Havik Polearm).
    /// </summary>
    [ScriptFilterCreatureId(71619u)]
    public class KelHavikPolearmKeyEntityScript : HallTargetGroupObjectiveEntityScriptBase
    {
        public KelHavikPolearmKeyEntityScript()
            : base(PublicEventObjectiveType.ActivateTargetGroup, 14104u, true)
        {
        }
    }

    /// <summary>
    /// Build 16042 objective 5155 uses ActivateTargetGroup 14276, whose member
    /// is Creature2 72958 (w3009 - Key Fragment - Vault Secret Room).
    /// </summary>
    [ScriptFilterCreatureId(72958u)]
    public class VaultSecretRoomKeyFragmentEntityScript : HallTargetGroupObjectiveEntityScriptBase
    {
        public VaultSecretRoomKeyFragmentEntityScript()
            : base(PublicEventObjectiveType.ActivateTargetGroup, 14276u, true)
        {
        }
    }

    /// <summary>
    /// Build 16042 objective 4322 uses ActivateTargetGroupChecklist 14212,
    /// whose member is Creature2 72367 (w3009 - Kel Havik Door Lock - Open Fortress).
    /// </summary>
    [ScriptFilterCreatureId(72367u)]
    public class KelHavikDoorLockEntityScript : HallTargetGroupObjectiveEntityScriptBase
    {
        public KelHavikDoorLockEntityScript()
            : base(PublicEventObjectiveType.ActivateTargetGroupChecklist, 14212u, true)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 4304 to ActivateTargetGroupChecklist
    /// TargetGroup 14163, whose Creature2 member is weak point 72108.
    /// </summary>
    [ScriptFilterCreatureId(72108u)]
    public class BridgeWeakPointEntityScript : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private const uint BridgeWeakPointTargetGroupId = 14163u;

        private IWorldEntity entity;
        private bool activated;

        public void OnLoad(IWorldEntity owner)
        {
            entity = owner;
        }

        public void OnActivateSuccess(IPlayer _)
        {
            if (activated)
                return;

            activated = true;
            entity.Map.PublicEventManager.UpdateObjective(
                PublicEventObjectiveType.ActivateTargetGroupChecklist,
                BridgeWeakPointTargetGroupId,
                entity.QuestChecklistIdx);
        }
    }

    /// <summary>
    /// Build 16042 side-event objective 4452 uses ActivateTargetGroupChecklist
    /// 12275, whose member is Creature2 67962 (Unlit Incense - w3009 - Side Mission 4).
    /// </summary>
    [ScriptFilterCreatureId(67962u)]
    public class UnlitIncenseEntityScript : HallTargetGroupObjectiveEntityScriptBase
    {
        public UnlitIncenseEntityScript()
            : base(PublicEventObjectiveType.ActivateTargetGroupChecklist, 12275u, true)
        {
        }
    }

    /// <summary>
    /// Build 16042 side-event objective 4453 uses ActivateTargetGroupChecklist
    /// 12302, whose member is Creature2 68012 (Sacred Bas-Relief - w3009 - Side Mission 4).
    /// </summary>
    [ScriptFilterCreatureId(68012u)]
    public class SacredBasReliefEntityScript : HallTargetGroupObjectiveEntityScriptBase
    {
        public SacredBasReliefEntityScript()
            : base(PublicEventObjectiveType.ActivateTargetGroupChecklist, 12302u, true)
        {
        }
    }

    /// <summary>
    /// Build 16042 side-event objective 4378 uses
    /// ActivateTargetGroupChecklist 12213, whose member is Creature2 67659
    /// (Frozen Carcass - w3009 - Side Mission 1).
    /// </summary>
    [ScriptFilterCreatureId(67659u)]
    public class FrozenCarcassEntityScript : HallTargetGroupObjectiveEntityScriptBase
    {
        public FrozenCarcassEntityScript()
            : base(PublicEventObjectiveType.ActivateTargetGroupChecklist, 12213u, true)
        {
        }
    }

    /// <summary>
    /// Build 16042 side-event objectives 4392 and 4398 both describe reviving
    /// the body represented by TargetGroup 12240 / Creature2 67778.
    /// </summary>
    [ScriptFilterCreatureId(67778u)]
    public class SoulrottedBodyEntityScript : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private const uint SoulrottedBodyTargetGroupId = 12240u;

        private IWorldEntity entity;
        private bool activated;

        public void OnLoad(IWorldEntity owner)
        {
            entity = owner;
        }

        public void OnActivateSuccess(IPlayer _)
        {
            if (activated)
                return;

            activated = true;
            entity.Map.PublicEventManager.UpdateObjective(
                PublicEventObjectiveType.ActivateTargetGroup,
                SoulrottedBodyTargetGroupId,
                1);
            entity.Map.PublicEventManager.UpdateObjective(PublicEventObjective.ReviveSoulrottedBodyScriptGate, 1);
            entity.RemoveFromMap();
        }
    }

    /// <summary>
    /// Build 16042 objective 4393 is a count-ten Script row whose text and
    /// reward pane map Soulrot collection actors through TargetGroup 12248.
    /// </summary>
    [ScriptFilterCreatureId(67841u, 67842u)]
    public class SoulrotSampleEntityScript : HallDirectObjectiveEntityScriptBase
    {
        public SoulrotSampleEntityScript()
            : base(PublicEventObjective.CollectSoulrot, removeOnActivation: true)
        {
        }
    }

    /// <summary>
    /// Build 16042 objective 4394 uses ActivateTargetGroup 14232, whose member
    /// is Creature2 67797.
    /// </summary>
    [ScriptFilterCreatureId(67797u)]
    public class SoulrotWasteDisposalEntityScript : HallTargetGroupObjectiveEntityScriptBase
    {
        public SoulrotWasteDisposalEntityScript()
            : base(PublicEventObjectiveType.ActivateTargetGroup, 14232u, true)
        {
        }
    }
}
