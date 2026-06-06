using NexusForever.Game.Abstract.Entity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
        private readonly ILogger<SplineAI> log;

        private ICreatureEntity owner;

        public SplineAI(
            ILogger<SplineAI> log = null)
        {
            this.log = log ?? NullLogger<SplineAI>.Instance;
        }

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
            {
                log.LogTrace("SplineAI skipped entity {EntityId} creature {CreatureId} guid {Guid}: no entity_spline row.",
                    owner.EntityId, owner.CreatureId, owner.Guid);
                return;
            }

            if (!TryResolveSpline(owner.Spline.Mode, owner.Spline.Speed, out SplineMode mode, out float speed))
            {
                log.LogWarning("SplineAI skipped entity {EntityId} creature {CreatureId} guid {Guid}: unsupported spline {SplineId}, mode={Mode}, speed={Speed}.",
                    owner.EntityId, owner.CreatureId, owner.Guid, owner.Spline.SplineId, owner.Spline.Mode, owner.Spline.Speed);
                return;
            }

            owner.MovementManager.SetMode(ModeType.Walk);
            owner.MovementManager.LaunchSpline(owner.Spline.SplineId, mode, speed, false);

            log.LogDebug("SplineAI launched entity {EntityId} creature {CreatureId} guid {Guid}: spline={SplineId}, mode={Mode}->{ResolvedMode}, speed={Speed}->{ResolvedSpeed}.",
                owner.EntityId, owner.CreatureId, owner.Guid, owner.Spline.SplineId, owner.Spline.Mode, mode, owner.Spline.Speed, speed);
        }

        internal static bool TryResolveSpline(SplineMode mode, float speed, out SplineMode resolvedMode, out float resolvedSpeed)
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
