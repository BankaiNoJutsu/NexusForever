using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map;
using NexusForever.Script;
using NexusForever.Script.Template.Collection;

namespace NexusForever.Game.Entity.Trigger
{
    public class GridTriggerEntity : GridEntity, IGridTriggerEntity
    {
        private uint id;
        private float range;

        #region Dependency Injection

        private readonly IScriptManager scriptManager;

        public GridTriggerEntity(
            IScriptManager scriptManager)
        {
            this.scriptManager = scriptManager;
        }

        #endregion

        /// <summary>
        /// Initialise trigger with supplied id and range.
        /// </summary>
        public void Initialise(uint id, float range)
        {
            if (this.id != 0)
                throw new InvalidOperationException("Grid trigger is already initialised.");

            this.id    = id;
            this.range = range;

            InitialiseScriptCollection();
        }

        private void InitialiseScriptCollection()
        {
            scriptCollection = scriptManager.InitialiseOwnedScripts<IGridTriggerEntity>(this, id);
        }

        /// <summary>
        /// Invoked when <see cref="IGridTriggerEntity"/> is added to <see cref="IBaseMap"/>.
        /// </summary>
        public override void OnAddToMap(IBaseMap map, uint guid, Vector3 vector)
        {
            base.OnAddToMap(map, guid, vector);

            SetInRangeCheck(range);
        }
    }
}
