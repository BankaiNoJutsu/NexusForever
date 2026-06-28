using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.PlayerPath;
using NexusForever.Network.World.Message.Model.PlayerPath;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;
using Path = NexusForever.Game.Static.PlayerPath.Path;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    public abstract class NorthernWildsScientistPathMissionEntityScript : IWorldEntityScript, IOwnedScript<ICreatureEntity>
    {
        private ICreatureEntity owner;

        protected abstract uint PathScientistCreatureInfoId { get; }

        public void OnLoad(ICreatureEntity owner)
        {
            this.owner = owner;
        }

        public void OnAddVisibleEntity(IGridEntity entity)
        {
            if (entity is not IPlayer player || player.Path != Path.Scientist)
                return;

            player.Session.EnqueueMessageEncrypted(new ServerPathScientistAddCreatureInfoToCreature
            {
                UnitId = owner.Guid,
                PathScientistCreatureInfoId = PathScientistCreatureInfoId
            });

            player.Session.EnqueueMessageEncrypted(new ServerPathScientistUnitScanParameters
            {
                UnitId = owner.Guid,
                ScanRewardFlags = ScanReward.RewardForScan,
                IsScannable = true
            });
        }

        public void OnActivateSuccess(IPlayer activator)
        {
            if (activator.Path != Path.Scientist)
                return;

            activator.PathManager.ProgressScientistCreatureScanMission(PathScientistCreatureInfoId);
        }
    }

    [ScriptFilterCreatureId(18680u)]
    public class NorthernWildsScientistTurningTheTideEntityScript : NorthernWildsScientistPathMissionEntityScript
    {
        protected override uint PathScientistCreatureInfoId => 34;
    }

    [ScriptFilterCreatureId(15888u)]
    public class NorthernWildsScientistVitaliumCrystalEntityScript : NorthernWildsScientistPathMissionEntityScript
    {
        protected override uint PathScientistCreatureInfoId => 130;
    }

    [ScriptFilterCreatureId(11907u, 11910u, 11912u, 11917u, 36429u, 36884u)]
    public class NorthernWildsScientistSkeechPhysiologyEntityScript : NorthernWildsScientistPathMissionEntityScript
    {
        protected override uint PathScientistCreatureInfoId => 128;
    }
}
