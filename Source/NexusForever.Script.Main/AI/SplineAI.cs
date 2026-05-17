using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Static.Entity.Movement.Command.Mode;
using NexusForever.Game.Static.Entity.Movement.Spline;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;
using NexusForever.Script.Template.Filter.Dynamic;

namespace NexusForever.Script.Main.AI
{
    [ScriptFilterDynamic<IScriptFilterDynamicEntitySpline>]
    //[ScriptFilterIgnore]
    public class SplineAI : IWorldEntityScript, IOwnedScript<ICreatureEntity>
    {
        private ICreatureEntity owner;

        public void OnLoad(ICreatureEntity owner)
        {
            this.owner = owner;
        }

        /// <summary>
        /// Invoked when <see cref="IGridEntity"/> is added to <see cref="IBaseMap"/>.
        /// </summary>
        public void OnAddToMap(IBaseMap map)
        {
            if (owner.Spline == null)
                return;

            if (!TryResolveSpline(owner.Spline.Mode, owner.Spline.Speed, out SplineMode mode, out float speed))
                return;

            owner.MovementManager.SetMode(ModeType.Walk);
            owner.MovementManager.LaunchSpline(owner.Spline.SplineId, mode, speed, false);
        }

        private static bool TryResolveSpline(SplineMode mode, float speed, out SplineMode resolvedMode, out float resolvedSpeed)
        {
            resolvedMode  = mode;
            resolvedSpeed = MathF.Abs(speed);

            if (resolvedSpeed == 0f || mode > SplineMode.CyclicReverse)
                return false;

            if (speed >= 0f)
                return true;

            resolvedMode = mode switch
            {
                SplineMode.OneShot             => SplineMode.OneShotReverse,
                SplineMode.OneShotReverse      => SplineMode.OneShot,
                SplineMode.BackAndForth        => SplineMode.BackAndForthReverse,
                SplineMode.BackAndForthReverse => SplineMode.BackAndForth,
                SplineMode.Cyclic              => SplineMode.CyclicReverse,
                SplineMode.CyclicReverse       => SplineMode.Cyclic,
                _                              => mode
            };

            return true;
        }
    }
}
