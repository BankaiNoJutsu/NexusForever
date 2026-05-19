using System.Numerics;
using NexusForever.Game.Abstract.Entity.Movement.Command.Position;
using NexusForever.Game.Abstract.Entity.Movement.Spline;
using NexusForever.Game.Abstract.Entity.Movement.Spline.Template;
using NexusForever.Game.Entity.Movement.Spline.Template;
using NexusForever.Game.Static.Entity.Movement.Command;
using NexusForever.Game.Static.Entity.Movement.Spline;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Command;

namespace NexusForever.Game.Entity.Movement.Command.Position
{
    public class PositionMultiSplineCommand : IPositionCommand
    {
        public EntityCommand Command => EntityCommand.SetPositionMultiSpline;

        /// <summary>
        /// Returns if the command has been finalised.
        /// </summary>
        public bool IsFinalised => spline?.IsFinialised ?? false;

        private readonly List<uint> splineIds = [];

        #region Dependency Injection

        private readonly IGameTableManager gameTableManager;
        private readonly ISpline spline;

        public PositionMultiSplineCommand(
            IGameTableManager gameTableManager,
            ISpline spline)
        {
            this.gameTableManager = gameTableManager;
            this.spline           = spline;
        }

        #endregion

        /// <summary>
        /// Initialise command with the supplied splines, <see cref="SplineMode"/> and speed.
        /// </summary>
        public void Initialise(List<ushort> splineIds, SplineMode mode, float speed)
        {
            ArgumentNullException.ThrowIfNull(splineIds);
            if (splineIds.Count == 0)
                throw new ArgumentException("At least one spline id is required.", nameof(splineIds));

            this.splineIds.Clear();
            this.splineIds.AddRange(splineIds.Select(id => (uint)id));

            spline.Initialise(BuildTemplate(splineIds), mode, speed);
        }

        /// <summary>
        /// Invoked each world tick with the delta since the previous tick occurred.
        /// </summary>
        public void Update(double lastTick)
        {
            spline.Update(lastTick);
        }

        /// <summary>
        /// Returns the <see cref="INetworkEntityCommand"/> for the entity command.
        /// </summary>
        public INetworkEntityCommand GetNetworkEntityCommand()
        {
            return new NetworkEntityCommand
            {
                Command = Command,
                Model   = new SetPositionMultiSplineCommand
                {
                    SplineIds = splineIds,
                    Speed     = spline.Speed,
                    Position  = spline.Offset,
                    Mode      = spline.Mode.Mode,
                    Blend     = false,
                    IsContinuing = false
                }
            };
        }

        /// <summary>
        /// Returns the current <see cref="Vector3"/> position for the entity command.
        /// </summary>
        public Vector3 GetPosition()
        {
            return spline.GetPosition();
        }

        /// <summary>
        /// Returns the current <see cref="Vector3"/> rotation for the entity command.
        /// </summary>
        public Vector3 GetRotation()
        {
            return spline.GetRotation();
        }

        private ISplineTemplate BuildTemplate(IEnumerable<ushort> splineIds)
        {
            var template = new MultiSplineTemplate();
            bool hasType = false;

            foreach (ushort splineId in splineIds)
            {
                Spline2Entry splineEntry = gameTableManager.Spline2.GetEntry(splineId);
                if (splineEntry == null)
                    throw new ArgumentOutOfRangeException(nameof(splineIds), splineId, "Unknown spline id.");

                if (!hasType)
                {
                    template.Type = splineEntry.SplineType;
                    hasType = true;
                }
                else if (template.Type != splineEntry.SplineType)
                    throw new NotSupportedException("Multi-spline commands require all spline segments to share the same spline type.");

                foreach (Spline2NodeEntry nodeEntry in gameTableManager.Spline2Node.Entries
                    .Where(s => s.SplineId == splineId)
                    .OrderBy(s => s.Ordinal))
                {
                    var point = new SplineTemplatePoint
                    {
                        Position  = new Vector3(nodeEntry.Position0, nodeEntry.Position1, nodeEntry.Position2),
                        Rotation  = new Quaternion(nodeEntry.Facing0, nodeEntry.Facing1, nodeEntry.Facing2, nodeEntry.Facing3),
                        Delay     = nodeEntry.Delay > 0 ? nodeEntry.Delay : null,
                        FrameTime = nodeEntry.FrameTime
                    };

                    if (template.Points.Count > 0
                        && Vector3.DistanceSquared(template.Points[^1].Position, point.Position) < 0.0001f)
                        continue;

                    template.Points.Add(point);
                }
            }

            if (!hasType || template.Points.Count < 2)
                throw new ArgumentException("Multi-spline commands require at least two spline points.", nameof(splineIds));

            return template;
        }

        private sealed class MultiSplineTemplate : ISplineTemplate
        {
            public SplineType Type { get; set; }
            public List<ISplineTemplatePoint> Points { get; } = [];
        }
    }
}
