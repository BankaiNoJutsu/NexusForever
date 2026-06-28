using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement.Command.Position;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Search;
using NexusForever.Game.Static.Combat.CrowdControl;
using NexusForever.Game.Static.Entity.Movement.Command.Mode;
using NexusForever.Game.Static.Entity.Movement.Command.State;
using NexusForever.Game.Static.Entity.Movement.Spline;
using NexusForever.Game.Static.Spell;
using NexusForever.Network.World.Message.Model.Entity;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    [ScriptFilterCreatureId(AvalancheCreatureId)]
    public class NorthernWildsAvalancheEntityScript : IUnitScript, IOwnedScript<ICreatureEntity>
    {
        private sealed class EarlierAvalancheSourceSearchCheck : ISearchCheck<IWorldEntity>
        {
            private readonly uint ownerGuid;

            public EarlierAvalancheSourceSearchCheck(uint ownerGuid)
            {
                this.ownerGuid = ownerGuid;
            }

            public bool CheckEntity(IWorldEntity entity)
            {
                return entity != null
                    && entity.Guid < ownerGuid
                    && entity.CreatureId == AvalancheCreatureId
                    && IsNearAvalancheSource(entity.Position);
            }
        }

        private const ushort NorthernWildsWorldId = 426;
        private const uint AvalancheCreatureId = 67662u;
        private const ushort SnowSmashAchievement = 3504;
        private const float AvalancheHitRange = 6f;
        private const float AvalancheMoveSpeed = 9f;
        private const float AvalancheSourceSearchRange = 35f;
        private const uint AvalancheDamageAmount = 250u;
        private const double RepeatHitCooldownSeconds = 8d;
        private const double KnockdownDurationSeconds = 2d;
        private const uint AvalancheKnockdownEffectId = AvalancheCreatureId;
        private const uint AvalancheKnockdownSpellId = AvalancheCreatureId;
        private const uint AvalancheKnockdownCastingId = AvalancheCreatureId;

        // Reviewed from Tools/DataMapping/output/creature_spawn_map.csv for Creature2 67662.
        private static readonly Vector3[] AvalancheChutePath =
        [
            new(3952f, -664f, -5672f),
            new(3959f, -678f, -5650f),
            new(3973f, -716f, -5534f),
            new(3985f, -721f, -5529f),
            new(3993f, -719f, -5563f),
            new(4018f, -724f, -5546f),
            new(4013f, -727f, -5519f),
            new(4020f, -728f, -5518f)
        ];

        private readonly Dictionary<ulong, double> lastHitTimes = [];
        private readonly Dictionary<ulong, KnockdownRecord> activeKnockdowns = [];

        private ICreatureEntity owner;
        private double elapsedSeconds;
        private Vector3? startPosition;
        private bool pendingMovementRestart;
        private bool onNorthernWildsMap;
        private bool rollingSourceAvalanche;
        private readonly HashSet<uint> visiblePlayerGuids = [];

        public void OnLoad(ICreatureEntity owner)
        {
            this.owner = owner;
        }

        public void OnAddToMap(IBaseMap map)
        {
            if (map?.Entry?.Id != NorthernWildsWorldId)
                return;

            onNorthernWildsMap = true;
            owner.SetInRangeCheck(AvalancheHitRange);
            startPosition = AvalancheChutePath[0];
            if (!IsRollingSourceAvalanche())
            {
                owner.RemoveFromMap();
                return;
            }

            rollingSourceAvalanche = true;
            StartMovementIfVisible(resetToStart: true);
        }

        public void OnRemoveFromMap(IBaseMap map)
        {
            onNorthernWildsMap    = false;
            rollingSourceAvalanche = false;
            pendingMovementRestart = false;
            visiblePlayerGuids.Clear();
        }

        public void Update(double lastTick)
        {
            if (lastTick > 0d && double.IsFinite(lastTick))
                elapsedSeconds += lastTick;

            StartPendingMovementRestart();
            ExpireKnockdowns();

            foreach (IPlayer player in owner.GetInRange<IPlayer>(0u).ToList())
                TryHit(player);
        }

        public void OnPositionEntityCommandFinalise(IPositionCommand command)
        {
            if (!onNorthernWildsMap || !rollingSourceAvalanche)
                return;

            pendingMovementRestart = true;
        }

        public void OnAddVisibleEntity(IGridEntity entity)
        {
            if (entity is not IPlayer player)
                return;

            bool wasEmpty = visiblePlayerGuids.Count == 0;
            if (!visiblePlayerGuids.Add(player.Guid))
                return;

            if (wasEmpty)
                StartMovementIfVisible(resetToStart: true);
        }

        public void OnRemoveVisibleEntity(IGridEntity entity)
        {
            if (entity is IPlayer player)
                visiblePlayerGuids.Remove(player.Guid);
        }

        public void OnEnterRange(IGridEntity entity)
        {
            if (entity is IPlayer player)
                TryHit(player);
        }

        private void TryHit(IPlayer player)
        {
            if (player == null || !player.IsAlive)
                return;

            if (owner?.Map?.Entry?.Id != NorthernWildsWorldId)
                return;

            double now = elapsedSeconds;
            if (lastHitTimes.TryGetValue(player.CharacterId, out double lastHit)
                && now - lastHit < RepeatHitCooldownSeconds)
                return;

            lastHitTimes[player.CharacterId] = now;

            ApplyDamage(player);
            ApplyKnockdown(player, now);
            GrantSnowSmash(player);
        }

        private void StartMovementIfVisible(bool resetToStart)
        {
            if (!onNorthernWildsMap || !rollingSourceAvalanche || visiblePlayerGuids.Count == 0)
                return;

            StartMovement(resetToStart);
        }

        private void StartMovement(bool resetToStart)
        {
            if (owner?.MovementManager == null)
                return;

            Vector3 start = startPosition ?? (IsFinite(owner.Position) ? owner.Position : AvalancheChutePath[0]);
            List<Vector3> path = BuildDownhillMovementPath(start);
            if (path.Count < 2)
                return;

            if (resetToStart)
                owner.MovementManager.SetPosition(path[0], false);

            owner.MovementManager.SetMode(ModeType.Walk);
            owner.MovementManager.LaunchSpline(path, SplineType.Linear, SplineMode.OneShot, AvalancheMoveSpeed);
            owner.MovementManager.BroadcastNetworkEntityCommands();
        }

        private bool IsRollingSourceAvalanche()
        {
            if (!IsNearAvalancheSource(owner.Position))
                return false;

            if (owner.Map == null || owner.Guid == 0u)
                return true;

            return !owner.Map.Search(AvalancheChutePath[0], AvalancheSourceSearchRange, new EarlierAvalancheSourceSearchCheck(owner.Guid)).Any();
        }

        private void StartPendingMovementRestart()
        {
            if (!pendingMovementRestart)
                return;

            pendingMovementRestart = false;
            StartMovementIfVisible(resetToStart: true);
        }

        private static List<Vector3> BuildDownhillMovementPath(Vector3 position)
        {
            if (!IsFinite(position))
                position = AvalancheChutePath[0];

            int closestIndex = 0;
            float closestDistance = float.MaxValue;
            for (int i = 0; i < AvalancheChutePath.Length; i++)
            {
                float distance = Vector3.DistanceSquared(position, AvalancheChutePath[i]);
                if (distance >= closestDistance)
                    continue;

                closestDistance = distance;
                closestIndex = i;
            }

            var path = new List<Vector3>();
            AddPathNode(path, position);
            AddPathNode(path, AvalancheChutePath[closestIndex]);

            for (int i = closestIndex + 1; i < AvalancheChutePath.Length; i++)
                AddPathNode(path, AvalancheChutePath[i]);

            return path;
        }

        private static void AddPathNode(List<Vector3> path, Vector3 node)
        {
            if (path.Count != 0 && Vector3.DistanceSquared(path[^1], node) < 0.25f)
                return;

            path.Add(node);
        }

        private static bool IsFinite(Vector3 position)
        {
            return float.IsFinite(position.X)
                && float.IsFinite(position.Y)
                && float.IsFinite(position.Z);
        }

        private static bool IsNearAvalancheSource(Vector3 position)
        {
            return IsFinite(position)
                && Vector3.DistanceSquared(position, AvalancheChutePath[0]) <= AvalancheSourceSearchRange * AvalancheSourceSearchRange;
        }

        private void ApplyDamage(IPlayer player)
        {
            uint shieldAbsorbAmount = Math.Min(player.Shield, AvalancheDamageAmount);
            if (shieldAbsorbAmount != 0u)
                player.Shield -= shieldAbsorbAmount;

            uint healthDamage = AvalancheDamageAmount - shieldAbsorbAmount;
            if (healthDamage == 0u || player.Health <= 1u)
                return;

            uint nonLethalHealthDamage = Math.Min(healthDamage, player.Health - 1u);
            if (nonLethalHealthDamage != 0u)
                player.ModifyHealth(nonLethalHealthDamage, DamageType.Physical, owner);
        }

        private void ApplyKnockdown(IPlayer player, double now)
        {
            if ((player.ActiveCCStateMask & (1u << (int)CCState.Knockdown)) != 0u)
                return;

            player.AddCCState(CCState.Knockdown, AvalancheKnockdownEffectId, AvalancheKnockdownSpellId, AvalancheKnockdownCastingId);
            StopPlayerMovement(player);
            player.EnqueueToVisible(new ServerEntityCCStateSet
            {
                UnitId              = player.Guid,
                CCType              = CCState.Knockdown,
                SpellEffectUniqueId = AvalancheKnockdownEffectId
            }, true);

            activeKnockdowns[player.CharacterId] = new KnockdownRecord(player, now + KnockdownDurationSeconds);
        }

        private static void StopPlayerMovement(IPlayer player)
        {
            if (player.MovementManager == null)
                return;

            player.MovementManager.SetMove(Vector3.Zero, false);
            player.MovementManager.SetVelocity(Vector3.Zero, false);
            player.MovementManager.SetState(player.MovementManager.GetState() & ~(
                StateFlags.Move
                | StateFlags.Velocity
                | StateFlags.Jump
                | StateFlags.DoubleJump
                | StateFlags.RollForward
                | StateFlags.RollBackward));
        }

        private void ExpireKnockdowns()
        {
            foreach (KeyValuePair<ulong, KnockdownRecord> entry in activeKnockdowns.ToArray())
            {
                if (entry.Value.ExpiresAt > elapsedSeconds)
                    continue;

                RemoveKnockdown(entry.Value.Player);
                activeKnockdowns.Remove(entry.Key);
            }
        }

        private static void RemoveKnockdown(IPlayer player)
        {
            if (!player.RemoveCCState(CCState.Knockdown, AvalancheKnockdownEffectId))
                return;

            player.EnqueueToVisible(new ServerEntityCCStateRemove
            {
                UnitId              = player.Guid,
                CCType              = CCState.Knockdown,
                SpellCastUniqueId   = AvalancheKnockdownCastingId,
                SpellEffectUniqueId = AvalancheKnockdownEffectId,
                Removed             = true
            }, true);
        }

        private static void GrantSnowSmash(IPlayer player)
        {
            if (player.AchievementManager == null)
                return;

            if (player.AchievementManager.HasCompletedAchievement(SnowSmashAchievement))
                return;

            player.AchievementManager.GrantAchievement(SnowSmashAchievement);
        }

        private readonly record struct KnockdownRecord(IPlayer Player, double ExpiresAt);
    }
}
