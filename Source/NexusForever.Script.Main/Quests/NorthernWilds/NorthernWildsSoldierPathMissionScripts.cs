using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.PlayerPath;
using NexusForever.Network.World.Message.Model.PlayerPath;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;
using Path = NexusForever.Game.Static.PlayerPath.Path;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    public abstract class NorthernWildsSoldierHoldoutEntityScript : IWorldEntityScript, IOwnedScript<ICreatureEntity>
    {
        protected abstract ushort PathSoldierEventId { get; }

        public void OnLoad(ICreatureEntity owner)
        {
        }

        public void OnActivateSuccess(IPlayer activator)
        {
            if (activator.Path != Path.Soldier)
                return;

            if (!activator.PathManager.CompleteMissionByObjectId(PathSoldierEventId))
                return;

            activator.Session.EnqueueMessageEncrypted(new ServerPathSoldierHoldoutEnd
            {
                PathSoldierEventId = PathSoldierEventId,
                Reason = PlayerPathSoldierResult.Success
            });
        }
    }

    [ScriptFilterCreatureId(11139u)]
    public class NorthernWildsColdburrowSkeechHoldoutEntityScript : NorthernWildsSoldierHoldoutEntityScript
    {
        protected override ushort PathSoldierEventId => 12;
    }

    [ScriptFilterCreatureId(11141u)]
    public class NorthernWildsDominionHoldoutEntityScript : NorthernWildsSoldierHoldoutEntityScript
    {
        protected override ushort PathSoldierEventId => 13;
    }

    [ScriptFilterCreatureId(12508u)]
    public class NorthernWildsYetiHoldoutEntityScript : NorthernWildsSoldierHoldoutEntityScript
    {
        protected override ushort PathSoldierEventId => 2;
    }
}
