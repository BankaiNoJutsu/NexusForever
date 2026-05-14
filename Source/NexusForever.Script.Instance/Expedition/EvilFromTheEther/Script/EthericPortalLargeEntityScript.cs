using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Creature;
using NexusForever.Game.Abstract.Map;
using NexusForever.Script.Template.Event;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Expedition.EvilFromTheEther.Script
{
    [ScriptFilterCreatureId(71221)]
    public class EthericPortalLargeEntityScript : EthericPortalEntityScript
    {
        #region Dependency Injection

        public EthericPortalLargeEntityScript(
            IScriptEventFactory eventFactory,
            IScriptEventManager eventManager,
            ICreatureInfoManager creatureInfoManager)
            : base(eventFactory, eventManager, creatureInfoManager)
        {
        }

        #endregion

        /// <summary>
        /// Invoked when <see cref="IGridEntity"/> is added to <see cref="IBaseMap"/>.
        /// </summary>
        public override void OnAddToMap(IBaseMap map)
        {
            CreateTetheredOrganism(TimeSpan.FromSeconds(1.5), ToRadians(60f));
            CreateTetheredOrganism(TimeSpan.FromSeconds(3.0), ToRadians(30f));
            CreateTetheredOrganism(TimeSpan.FromSeconds(4.5), 0f);
            CreateTetheredOrganism(TimeSpan.FromSeconds(6.0), ToRadians(-30f));
            CreateTetheredOrganism(TimeSpan.FromSeconds(7.5), ToRadians(-60f));
        }

        private static float ToRadians(float degrees)
        {
            return degrees * MathF.PI / 180f;
        }
    }
}
