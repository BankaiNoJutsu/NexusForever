using System.Numerics;
using System.Linq;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Entity.Movement.Command.Position;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Entity.Movement;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Game.Static.Entity.Movement.Command;
using NexusForever.Game.Static.Entity.Movement.Command.Mode;
using NexusForever.Game.Static.Entity.Movement.Spline;
using NexusForever.Network.World.Entity;
using NexusForever.Script.Template;
using NexusForever.Shared;
using NexusForever.Shared.Game;

namespace NexusForever.Game.Entity.Movement.Command.Position
{
    public class PositionCommandGroup : IPositionCommandGroup
    {
        /// <summary>
        /// Determines if the group has been modified and needs to be sent to the client.
        /// </summary>
        public bool IsDirty { get; set; }

        /// <summary>
        /// Determines if the group has an entity command that requires resynchronisation after loading.
        /// </summary>
        public bool RequiresSynchronisation => Command.Command
            is EntityCommand.SetPositionKeys
            or EntityCommand.SetPositionPath
            or EntityCommand.SetPositionSpline
            or EntityCommand.SetPositionMultiSpline
            or EntityCommand.SetPositionProjectile;

        /// <summary>
        /// Current position entity command.
        /// </summary>
        public IPositionCommand Command { get; private set; }

        private readonly UpdateTimer relocationTimer = new(TimeSpan.FromSeconds(1));
        private Vector3 lastPosition;

        private const uint TutorialHoverboardFinishWorldLocationId = 51734u;
        private const float TutorialHoverboardFinishRecoveryPadding = 6f;

        private IMovementManager movementManager;

        #region Dependency Injection

        private readonly IFactoryInterface<IPositionCommand> factory;
        private readonly ILogger<PositionCommandGroup> log;

        public PositionCommandGroup(
            IFactoryInterface<IPositionCommand> factory,
            ILogger<PositionCommandGroup> log)
        {
            this.factory = factory;
            this.log     = log;
        }

        #endregion

        /// <summary>
        /// Initialise <see cref="IPositionCommandGroup"/ with default command.
        /// </summary>
        public void Initialise(IMovementManager movementManager)
        {
            this.movementManager = movementManager;

            SetPosition(Vector3.Zero, false);
        }

        /// <summary>
        /// Invoked each world tick with the delta since the previous tick occurred.
        /// </summary>
        public void Update(double lastTick)
        {
            if (Command == null)
                return;

            Command.Update(lastTick);

            relocationTimer.Update(lastTick);
            if (relocationTimer.HasElapsed)
            {
                Relocate();
                relocationTimer.Reset();
            }

            if (Command.IsFinalised)
            {
                movementManager.Owner.InvokeScriptCollection<IWorldEntityScript>(s => s.OnPositionEntityCommandFinalise(Command));
                Finalise();
            }
        }

        private void Relocate()
        {
            Vector3 commandPosition = Command.GetPosition();
            if (!MovementMath.IsFinite(commandPosition))
            {
                log.LogWarning(
                    "Stopping invalid position command for entity {Guid}: command {Command} produced ({X}, {Y}, {Z}).",
                    movementManager.Owner.Guid,
                    Command.Command,
                    commandPosition.X,
                    commandPosition.Y,
                    commandPosition.Z);

                ResetInvalidPositionCommand();
                return;
            }

            Vector3 position = GetRelocationPosition(commandPosition);
            if (!MovementMath.IsFinite(position))
            {
                log.LogWarning(
                    "Stopping invalid relocation for entity {Guid}: command {Command} produced world position ({X}, {Y}, {Z}).",
                    movementManager.Owner.Guid,
                    Command.Command,
                    position.X,
                    position.Y,
                    position.Z);

                ResetInvalidPositionCommand();
                return;
            }

            if (movementManager.Owner.Position == position)
            {
                lastPosition = position;
                return;
            }

            if (lastPosition == position)
                return;

            lastPosition = position;
            movementManager.Owner.Relocate(position);
        }

        private Vector3 GetRelocationPosition()
        {
            return GetRelocationPosition(GetPosition());
        }

        private Vector3 GetRelocationPosition(Vector3 position)
        {
            uint? platformUnitId = movementManager.GetPlatform();
            if (platformUnitId != null)
            {
                IWorldEntity platformEntity = movementManager.Owner.Map.GetEntity<IWorldEntity>(platformUnitId.Value);
                if (platformEntity != null)
                    position += platformEntity.Position;
            }

            return position;
        }

        private void ResetInvalidPositionCommand()
        {
            Vector3 fallback = MovementMath.IsFinite(movementManager.Owner.Position)
                ? movementManager.Owner.Position
                : Vector3.Zero;

            Command = null;
            SetPosition(fallback, true);
        }

        /// <summary>
        /// Return the default <see cref="INetworkEntityCommand"/> for the entity command group.
        /// </summary>
        /// <remarks>
        /// The default command to send if the entity requires synchronisation.
        /// </remarks>
        public INetworkEntityCommand GetDefaultNetworkEntityCommand()
        {
            var command = factory.Resolve<PositionCommand>();
            command.Initialise(GetPosition(), false);
            return command.GetNetworkEntityCommand();
        }

        /// <summary>
        /// Returns the <see cref="INetworkEntityCommand"/> for the entity command group.
        /// </summary>
        public INetworkEntityCommand GetNetworkEntityCommand()
        {
            return Command.GetNetworkEntityCommand();
        }

        /// <summary>
        /// Finalise the current entity command.
        /// </summary>
        public void Finalise()
        {
            if (Command == null)
                return;

            Vector3 position = GetPosition();
            if (!MovementMath.IsFinite(position))
                position = MovementMath.IsFinite(movementManager.Owner.Position)
                    ? movementManager.Owner.Position
                    : Vector3.Zero;

            Command = null;

            SetPosition(position, true);
        }

        /// <summary>
        /// Get the current <see cref="Vector3"/> position value.
        /// </summary>
        public Vector3 GetPosition()
        {
            Vector3 position = Command.GetPosition();
            if (!MovementMath.IsFinite(position))
                return MovementMath.IsFinite(movementManager.Owner.Position)
                    ? movementManager.Owner.Position
                    : Vector3.Zero;

            // add "float" height for modes 1 and 3
            if (movementManager.GetMode() is ModeType.Swim or ModeType.Free)
                position += new Vector3(0f, 1.5707964f, 0f);

            return position;
        }

        /// <summary>
        /// Set the position to the supplied <see cref="Vector3"/> value.
        /// </summary>
        public void SetPosition(Vector3 position, bool blend)
        {
            Finalise();

            if (!MovementMath.IsFinite(position))
            {
                log.LogWarning(
                    "Replacing invalid position for entity {Guid}: ({X}, {Y}, {Z}).",
                    movementManager.Owner.Guid,
                    position.X,
                    position.Y,
                    position.Z);

                position = MovementMath.IsFinite(movementManager.Owner.Position)
                    ? movementManager.Owner.Position
                    : Vector3.Zero;
            }

            var command = factory.Resolve<PositionCommand>();
            command.Initialise(position, blend);
            Command = command;

            if (!movementManager.ServerControl)
                TryUpdateImmediateStarterTutorialObjectives(GetRelocationPosition());

            IsDirty = true;
        }

        private void TryUpdateImmediateStarterTutorialObjectives(Vector3 position)
        {
            if (movementManager.Owner is not IPlayer player || player.Map == null)
                return;

            float targetRadius = player.HitRadius * 0.5f;

            foreach (IQuest quest in player.QuestManager.GetActiveQuests())
            {
                if (quest.Id is not (10513 or 10521 or 10527 or 10532))
                    continue;

                List<IQuestObjective> matchingObjectives = [];
                bool hasRequiredObjective = false;

                foreach (IQuestObjective objective in quest)
                {
                    if (!ShouldUpdateImmediateAreaObjective(objective, position, targetRadius))
                        continue;

                    matchingObjectives.Add(objective);
                    if (!objective.ObjectiveInfo.IsOptional())
                        hasRequiredObjective = true;
                }

                if (matchingObjectives.Count == 0)
                    continue;

                List<IQuestObjective> objectivesToUpdate = (hasRequiredObjective
                    ? matchingObjectives.Where(o => !o.ObjectiveInfo.IsOptional())
                    : matchingObjectives)
                    .OrderByDescending(o => o.Index)
                    .ToList();

                foreach (IQuestObjective objective in objectivesToUpdate)
                {
                    log.LogDebug("Immediate tutorial area update for player {PlayerGuid}: quest {QuestId}, objective {ObjectiveId}, matched world locations {WorldLocationIds}, position ({X}, {Y}, {Z}), hit padding {TargetRadius}.",
                        player.Guid, quest.Id, objective.ObjectiveInfo.Id,
                        string.Join(", ", GetMatchedWorldLocationIds(objective.ObjectiveInfo.Entry, position, targetRadius)),
                        position.X, position.Y, position.Z, targetRadius);
                    quest.ObjectiveUpdate(objective.ObjectiveInfo.Id, 1u);
                }
            }
        }

        private static IEnumerable<uint> GetMatchedWorldLocationIds(QuestObjectiveEntry objectiveEntry, Vector3 position, float targetRadius)
        {
            uint[] worldLocationIds =
            [
                objectiveEntry.WorldLocationsIdIndicator00,
                objectiveEntry.WorldLocationsIdIndicator01,
                objectiveEntry.WorldLocationsIdIndicator02,
                objectiveEntry.WorldLocationsIdIndicator03
            ];

            return worldLocationIds
                .Where(id => id != 0u)
                .Distinct()
                .Where(id => IsInsideWorldLocation(position, targetRadius, id));
        }

        private static bool ShouldUpdateImmediateAreaObjective(IQuestObjective objective, Vector3 position, float targetRadius)
        {
            if (objective.IsComplete() || objective.ObjectiveInfo.Type != QuestObjectiveType.EnterArea)
                return false;

            var objectiveEntry = objective.ObjectiveInfo.Entry;
            return IsInsideWorldLocation(position, targetRadius, objectiveEntry.WorldLocationsIdIndicator00)
                || IsInsideWorldLocation(position, targetRadius, objectiveEntry.WorldLocationsIdIndicator01)
                || IsInsideWorldLocation(position, targetRadius, objectiveEntry.WorldLocationsIdIndicator02)
                || IsInsideWorldLocation(position, targetRadius, objectiveEntry.WorldLocationsIdIndicator03);
        }

        private static bool IsInsideWorldLocation(Vector3 position, float targetRadius, uint worldLocationId)
        {
            if (worldLocationId == 0u)
                return false;

            WorldLocation2Entry worldLocation = GameTableManager.Instance.WorldLocation2.GetEntry(worldLocationId);
            return worldLocation != null && IsInsideWorldLocation(position, targetRadius, worldLocation);
        }

        private static bool IsInsideWorldLocation(Vector3 position, float targetRadius, WorldLocation2Entry worldLocation)
        {
            float horizontalDistanceSquared = Vector2.DistanceSquared(
                new Vector2(position.X, position.Z),
                new Vector2(worldLocation.Position0, worldLocation.Position2));

            float horizontalPadding = worldLocation.Id == TutorialHoverboardFinishWorldLocationId
                ? MathF.Max(targetRadius, TutorialHoverboardFinishRecoveryPadding)
                : targetRadius;
            float horizontalRange = worldLocation.Radius + horizontalPadding;
            if (horizontalDistanceSquared > horizontalRange * horizontalRange)
                return false;

            return worldLocation.MaxVerticalDistance <= 0f
                || MathF.Abs(position.Y - worldLocation.Position1) <= worldLocation.MaxVerticalDistance;
        }

        /// <summary>
        /// Set the position to the interpolated <see cref="Vector3"/> between the supplied times and positions.
        /// </summary> 
        public void SetPositionKeys(List<uint> times, List<Vector3> positions)
        {
            Finalise();

            var command = factory.Resolve<PositionKeysCommand>();
            command.Initialise(movementManager, times, positions);
            Command = command;

            IsDirty = true;
        }

        /// <summary>
        /// Set the position based on the supplied nodes, <see cref="SplineType"/>, <see cref="SplineMode"/> and speed.
        /// </summary>
        public void SetPositionPath(List<Vector3> nodes, SplineType type, SplineMode mode, float speed)
        {
            Finalise();

            var command = factory.Resolve<PositionPathCommand>();
            command.Initialise(nodes, type, mode, speed);
            Command = command;

            IsDirty = true;
        }

        /// <summary>
        /// Set the position based on the supplied spline, <see cref="SplineMode"/> and speed.
        /// </summary>
        public void SetPositionSpline(ushort splineId, SplineMode mode, float speed)
        {
            Finalise();

            var command = factory.Resolve<PositionSplineCommand>();
            command.Initialise(splineId, mode, speed);
            Command = command;

            IsDirty = true;
        }

        /// <summary>
        /// Set the position based on the supplied splines, <see cref="SplineMode"/> and speed.
        /// </summary>
        public void SetPositionMultiSpline(List<ushort> splineIds, SplineMode mode, float speed)
        {
            Finalise();

            var command = factory.Resolve<PositionMultiSplineCommand>();
            command.Initialise(splineIds, mode, speed);
            Command = command;

            IsDirty = true;
        }

        /// <summary>
        /// Set the position using projectile motion to the supplied destination and facing.
        /// </summary>
        public void SetPositionProjectile(Vector3 position, Vector3 rotation, TimeSpan flightTime, float gravity)
        {
            Finalise();

            var command = factory.Resolve<PositionProjectileCommand>();
            command.Initialise(movementManager, position, rotation, flightTime, gravity);
            Command = command;

            IsDirty = true;
        }
    }
}
