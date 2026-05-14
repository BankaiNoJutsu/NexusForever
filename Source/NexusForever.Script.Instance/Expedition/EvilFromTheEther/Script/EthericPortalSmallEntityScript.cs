using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Creature;
using NexusForever.Game.Abstract.Map;
using NexusForever.Script.Template.Event;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Expedition.EvilFromTheEther.Script
{
    [ScriptFilterCreatureId(71132)]
    public class EthericPortalSmallEntityScript : EthericPortalEntityScript
    {
        #region Dependency Injection

        public EthericPortalSmallEntityScript(
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
            CreateTetheredOrganism(TimeSpan.FromSeconds(2), ToRadians(45f));
            CreateTetheredOrganism(TimeSpan.FromSeconds(4), 0f);
            CreateTetheredOrganism(TimeSpan.FromSeconds(6), ToRadians(-45f));
        }

        private static float ToRadians(float degrees)
        {
            return degrees * MathF.PI / 180f;
        }
    }
}
