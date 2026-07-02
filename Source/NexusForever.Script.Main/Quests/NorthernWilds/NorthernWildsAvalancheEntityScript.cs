using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement.Command.Position;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Search;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Combat.CrowdControl;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Entity.Movement.Command.Mode;
using NexusForever.Game.Static.Entity.Movement.Command.State;
using NexusForever.Game.Static.Entity.Movement.Spline;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Spell;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Entity;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;
using NetworkPosition = NexusForever.Network.World.Entity.Position;
using SharedInitialPosition = NexusForever.Network.World.Message.Model.Shared.InitialPosition;
using SharedTargetInfo = NexusForever.Network.World.Message.Model.Shared.TargetInfo;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    [ScriptFilterCreatureId(AvalancheVisualCreatureId, AvalancheCasterCreatureId)]
    public class NorthernWildsAvalancheEntityScript : IUnitScript, IOwnedScript<ICreatureEntity>, IOwnedScript<ISimpleEntity>
    {
        private sealed record AvalancheRoute(Vector3[] Nodes)
        {
            public Vector3 Source => Nodes[0];
        }

        private sealed class AvalancheHitSearchCheck : ISearchCheck<IPlayer>
        {
            private readonly AvalancheHitShape hitShape;

            public AvalancheHitSearchCheck(AvalancheHitShape hitShape)
            {
                this.hitShape = hitShape;
            }

            public bool CheckEntity(IPlayer entity)
            {
                return entity != null
                    && entity.IsAlive
                    && IsPlayerWithinHitRectangle(entity, hitShape);
            }
        }

        private readonly record struct AvalancheHitShape(Vector3 Position, Vector2 Forward);

        private const ushort NorthernWildsWorldId = 426;
        internal const uint AvalancheVisualCreatureId = 15615u;
        internal const uint AvalancheCasterCreatureId = 67662u;
        private const ushort EmpoweredTowerQuest = 3486;
        private const ushort SnowSmashAchievement = 3504;
        private const float AvalancheSearchRange = 12f;
        private const float AvalancheContactHalfWidth = 10f;
        private const float AvalancheContactHalfDepth = 2.5f;
        private const float AvalancheSpawnHitSuppressDistance = 6f;
        private const float AvalancheVerticalContactRange = 8f;
        private const float AvalancheMoveSpeed = 9f;
        private const float AvalancheSourceSearchRange = 8f;
        private const float AvalancheTerrainVisualOffset = 2.5f;
        private const float AvalancheRouteVisualLift = 5f;
        private const float AvalancheTerrainSampleSpacing = 3f;
        private const uint AvalancheDamageAmount = 350u;
        private const double RepeatHitCooldownSeconds = 8d;
        private const double KnockdownDurationSeconds = 2.5d;
        private const uint AvalancheKnockdownSpell4EffectId = 42793u;
        private const uint AvalancheDamageSpell4EffectId = 42794u;
        private const uint AvalancheAchievementSpell4EffectId = 126234u;
        private const uint AvalancheKnockdownSpellId = 26298u;

        // Creature2 15615 is the visible Northern Wilds avalanche. Imported
        // 67662 rows and Snow Smash video evidence place the hazard on this
        // chute, so the managed fallbacks loop downhill through that sampled
        // path instead of broad static runout rows.
        private static readonly AvalancheRoute[] AvalancheRoutes =
        [
            new(
            [
                new(3951f, -663f, -5673f),
                new(3955f, -670f, -5661f),
                new(3960f, -678f, -5650f),
                new(3968f, -686f, -5639f),
                new(3978f, -694f, -5630f),
                new(3988f, -702f, -5621f),
                new(3999f, -709f, -5614f),
                new(4012f, -714f, -5606f)
            ]),
            new(
            [
                new(3941f, -693f, -5607f),
                new(3970f, -709f, -5587f),
                new(3984f, -716f, -5572f),
                new(3994f, -720f, -5562f),
                new(4006f, -723f, -5553f),
                new(4019f, -724f, -5546f)
            ]),
            new(
            [
                new(3962f, -710f, -5540f),
                new(3974f, -716f, -5534f),
                new(3986f, -721f, -5529f),
                new(4006f, -727f, -5529f),
                new(4017f, -728f, -5518f)
            ])
        ];

        internal static IReadOnlyList<Vector3> FallbackSourcePositions { get; } = AvalancheRoutes
            .Select(route => route.Source)
            .ToArray();

        private readonly Dictionary<ulong, double> lastHitTimes = [];
        private readonly Dictionary<ulong, KnockdownRecord> activeKnockdowns = [];

        private readonly IGlobalSpellManager globalSpellManager;
        private IUnitEntity owner;
        private double elapsedSeconds;
        private AvalancheRoute activeRoute;
        private List<Vector3> activePath;
        private float activePathLength;
        private double movementStartedAtSeconds;
        private double movementRunDurationSeconds;
        private bool onNorthernWildsMap;
        private bool managedAvalanche;
        private bool movementActive;

        public NorthernWildsAvalancheEntityScript(IGlobalSpellManager globalSpellManager)
        {
            this.globalSpellManager = globalSpellManager;
        }

        public void OnLoad(ICreatureEntity owner)
        {
            this.owner = owner;
        }

        public void OnLoad(ISimpleEntity owner)
        {
            this.owner = owner;
        }

        public void OnAddToMap(IBaseMap map)
        {
            if (map?.Entry?.Id != NorthernWildsWorldId)
                return;

            onNorthernWildsMap = true;
            if (!IsManagedAvalancheFallback(owner))
                return;

            owner.SetInRangeCheck(AvalancheSearchRange);
            activeRoute = FindManagedRoute(owner.Position);
            if (activeRoute == null)
            {
                owner.RemoveFromMap();
                return;
            }

            managedAvalanche = true;
            StartMovement();
        }

        public void OnRemoveFromMap(IBaseMap map)
        {
            ClearActiveKnockdowns();

            onNorthernWildsMap         = false;
            managedAvalanche           = false;
            movementActive             = false;
            activePath                 = null;
            activeRoute                = null;
            activePathLength           = 0f;
            movementStartedAtSeconds   = 0d;
            movementRunDurationSeconds = 0d;
            lastHitTimes.Clear();
        }

        public void Update(double lastTick)
        {
            if (lastTick > 0d && double.IsFinite(lastTick))
                elapsedSeconds += lastTick;

            ExpireKnockdowns();
            RestartMovementRunIfNeeded();
            SweepNearbyPlayers();

            foreach (IPlayer player in owner.GetInRange<IPlayer>(0u).ToList())
                TryHit(player);
        }

        public void OnPositionEntityCommandFinalise(IPositionCommand command)
        {
        }

        public void OnAddVisibleEntity(IGridEntity entity)
        {
        }

        public void OnRemoveVisibleEntity(IGridEntity entity)
        {
        }

        public void OnEnterRange(IGridEntity entity)
        {
            if (entity is IPlayer player)
                TryHit(player);
        }

        private void TryHit(IPlayer player)
        {
            if (!managedAvalanche)
                return;

            if (player == null || !player.IsAlive)
                return;

            if (owner?.Map?.Entry?.Id != NorthernWildsWorldId)
                return;

            if (HasCompletedArrivalEpisode(player))
                return;

            if (!TryGetCurrentHitShape(out AvalancheHitShape hitShape))
                return;

            if (!IsPlayerWithinHitRectangle(player, hitShape))
                return;

            double now = elapsedSeconds;
            if (lastHitTimes.TryGetValue(player.CharacterId, out double lastHit)
                && now - lastHit < RepeatHitCooldownSeconds)
                return;

            lastHitTimes[player.CharacterId] = now;

            uint castingId = globalSpellManager.NextCastingId;
            uint damageEffectId = globalSpellManager.NextEffectId;
            uint knockdownEffectId = globalSpellManager.NextEffectId;
            uint achievementEffectId = ShouldGrantSnowSmash(player) ? globalSpellManager.NextEffectId : 0u;
            AvalancheDamageResult damageResult = ApplyDamage(player, damageEffectId);
            bool appliedKnockdown = ApplyKnockdown(player, now, knockdownEffectId, castingId);

            SendAvalancheHitSpellStart(player, castingId, hitShape.Position);
            SendAvalancheHitSpellGo(player, castingId, hitShape.Position, damageResult, appliedKnockdown ? knockdownEffectId : 0u, achievementEffectId);
            if (appliedKnockdown)
                SendKnockdownSet(player, knockdownEffectId);

            owner.EnqueueToVisible(new ServerSpellFinish
            {
                ServerUniqueId = castingId
            }, true);

            if (achievementEffectId != 0u)
                GrantSnowSmash(player);
        }

        private void SweepNearbyPlayers()
        {
            if (!managedAvalanche || owner?.Map == null)
                return;

            if (!TryGetCurrentHitShape(out AvalancheHitShape hitShape))
                return;

            var hitPlayerGuids = new HashSet<uint>();
            foreach (IPlayer player in owner.Map.Search(hitShape.Position, AvalancheSearchRange, new AvalancheHitSearchCheck(hitShape)).ToList())
            {
                if (!hitPlayerGuids.Add(player.Guid))
                    continue;

                TryHit(player);
            }
        }

        private bool TryGetCurrentHitShape(out AvalancheHitShape hitShape)
        {
            hitShape = default;
            if (!managedAvalanche || !movementActive || activePath == null || activePath.Count < 2)
                return false;

            if (!TryGetCurrentDistanceAlongPath(out float distanceAlongPath)
                || distanceAlongPath < AvalancheSpawnHitSuppressDistance)
                return false;

            if (!TryGetPathSampleAtDistance(distanceAlongPath, out Vector3 position, out Vector2 forward))
                return false;

            hitShape = new AvalancheHitShape(position, forward);
            return true;
        }

        private static bool HasCompletedArrivalEpisode(IPlayer player)
        {
            return player?.QuestManager?.GetQuestState(EmpoweredTowerQuest) is QuestState.Achieved or QuestState.Completed;
        }

        internal static bool IsManagedAvalancheFallback(IWorldEntity entity)
        {
            return entity != null
                && entity.CreatureId == AvalancheVisualCreatureId
                && entity.EntityId == 0u
                && (entity.CreateFlags & EntityCreateFlag.Immediate) != 0
                && FindManagedRoute(entity.Position) != null;
        }

        private void StartMovement()
        {
            if (!onNorthernWildsMap || !managedAvalanche || movementActive || activeRoute == null || owner?.MovementManager == null)
                return;

            List<Vector3> path = BuildDownhillMovementPath(activeRoute, owner.Map);
            if (path.Count < 2)
                return;

            activePath               = path;
            activePathLength         = CalculatePathLength(path);
            if (activePathLength <= float.Epsilon)
                return;

            movementRunDurationSeconds = activePathLength / AvalancheMoveSpeed;
            movementActive             = true;
            LaunchMovementRun();
        }

        private void RestartMovementRunIfNeeded()
        {
            if (!movementActive || activePath == null || activePath.Count < 2 || movementRunDurationSeconds <= 0d)
                return;

            double elapsedRunSeconds = elapsedSeconds - movementStartedAtSeconds;
            if (!double.IsFinite(elapsedRunSeconds) || elapsedRunSeconds < movementRunDurationSeconds)
                return;

            LaunchMovementRun();
        }

        private void LaunchMovementRun()
        {
            if (owner?.MovementManager == null || activePath == null || activePath.Count < 2)
                return;

            movementStartedAtSeconds = elapsedSeconds;
            owner.MovementManager.SetPosition(activePath[0], false);
            owner.MovementManager.SetMode(ModeType.Walk);
            owner.MovementManager.LaunchSpline(activePath, SplineType.Linear, SplineMode.OneShot, AvalancheMoveSpeed);
            owner.MovementManager.BroadcastNetworkEntityCommands();
        }

        private static List<Vector3> BuildDownhillMovementPath(AvalancheRoute route, IBaseMap map)
        {
            var path = new List<Vector3>();
            AddPathNode(path, ApplyTerrainHeight(route.Nodes[0], map, null));

            for (int i = 1; i < route.Nodes.Length; i++)
                AddTerrainSampledSegment(path, route.Nodes[i - 1], route.Nodes[i], map);

            return path;
        }

        private static void AddTerrainSampledSegment(List<Vector3> path, Vector3 from, Vector3 to, IBaseMap map)
        {
            float segmentLength = Vector2.Distance(new Vector2(from.X, from.Z), new Vector2(to.X, to.Z));
            int sampleCount = Math.Max(1, (int)MathF.Ceiling(segmentLength / AvalancheTerrainSampleSpacing));
            for (int i = 1; i <= sampleCount; i++)
            {
                float t = i / (float)sampleCount;
                float? previousY = path.Count == 0 ? null : path[^1].Y;
                AddPathNode(path, ApplyTerrainHeight(Vector3.Lerp(from, to, t), map, previousY));
            }
        }

        private static Vector3 ApplyTerrainHeight(Vector3 node, IBaseMap map, float? previousY)
        {
            var liftedNode = new Vector3(node.X, node.Y + AvalancheRouteVisualLift, node.Z);
            float? terrainHeight = map?.GetTerrainHeight(node.X, node.Z);
            if (!terrainHeight.HasValue || !float.IsFinite(terrainHeight.Value))
                return ClampDownhill(liftedNode, previousY);

            float minimumVisibleY = terrainHeight.Value + AvalancheTerrainVisualOffset;
            if (liftedNode.Y >= minimumVisibleY)
                return ClampDownhill(liftedNode, previousY);

            return ClampDownhill(new Vector3(node.X, minimumVisibleY, node.Z), previousY);
        }

        private static Vector3 ClampDownhill(Vector3 node, float? previousY)
        {
            if (!previousY.HasValue || node.Y <= previousY.Value)
                return node;

            return new Vector3(node.X, previousY.Value, node.Z);
        }

        private static AvalancheRoute FindManagedRoute(Vector3 position)
        {
            if (!IsFinite(position))
                return null;

            AvalancheRoute closestRoute = null;
            float closestDistance = float.MaxValue;
            foreach (AvalancheRoute route in AvalancheRoutes)
            {
                float distance = Vector3.DistanceSquared(position, route.Source);
                if (distance > AvalancheSourceSearchRange * AvalancheSourceSearchRange || distance >= closestDistance)
                    continue;

                closestRoute    = route;
                closestDistance = distance;
            }

            return closestRoute;
        }

        private static void AddPathNode(List<Vector3> path, Vector3 node)
        {
            if (path.Count != 0 && Vector3.DistanceSquared(path[^1], node) < 0.25f)
                return;

            path.Add(node);
        }

        private bool TryGetCurrentDistanceAlongPath(out float distanceAlongPath)
        {
            distanceAlongPath = 0f;
            if (activePathLength <= float.Epsilon || !double.IsFinite(elapsedSeconds) || !double.IsFinite(movementStartedAtSeconds))
                return false;

            double elapsedMovementSeconds = Math.Max(0d, elapsedSeconds - movementStartedAtSeconds);
            if (movementRunDurationSeconds <= 0d || elapsedMovementSeconds >= movementRunDurationSeconds)
                return false;

            double distance = elapsedMovementSeconds * AvalancheMoveSpeed;
            if (!double.IsFinite(distance))
                return false;

            distanceAlongPath = (float)distance;
            return true;
        }

        private bool TryGetPathSampleAtDistance(float distanceAlongPath, out Vector3 position, out Vector2 forward)
        {
            position = default;
            forward = default;
            if (activePath == null || activePath.Count < 2)
                return false;

            float remainingDistance = distanceAlongPath;

            for (int i = 0; i < activePath.Count - 1; i++)
            {
                Vector3 from = activePath[i];
                Vector3 to = activePath[i + 1];
                Vector3 segment = to - from;
                float segmentLength = segment.Length();
                if (segmentLength < 0.0001f)
                    continue;

                if (remainingDistance > segmentLength)
                {
                    remainingDistance -= segmentLength;
                    continue;
                }

                float t = Math.Clamp(remainingDistance / segmentLength, 0f, 1f);
                Vector3 sampledPosition = Vector3.Lerp(from, to, t);
                if (!TryGetForward2D(segment, out Vector2 segmentForward))
                    return false;

                position = sampledPosition;
                forward = segmentForward;
                return IsFinite(position);
            }

            Vector3 finalSegment = activePath[^1] - activePath[^2];
            if (!TryGetForward2D(finalSegment, out forward))
                return false;

            position = activePath[^1];
            return IsFinite(position);
        }

        private static float CalculatePathLength(IReadOnlyList<Vector3> path)
        {
            float length = 0f;
            for (int i = 0; i < path.Count - 1; i++)
                length += Vector3.Distance(path[i], path[i + 1]);

            return length;
        }

        private static bool TryGetForward2D(Vector3 segment, out Vector2 forward)
        {
            forward = default;
            var direction = new Vector2(segment.X, segment.Z);
            if (direction.LengthSquared() < 0.0001f)
                return false;

            forward = Vector2.Normalize(direction);
            return IsFinite(forward);
        }

        private static bool IsFinite(Vector3 position)
        {
            return float.IsFinite(position.X)
                && float.IsFinite(position.Y)
                && float.IsFinite(position.Z);
        }

        private static bool IsFinite(Vector2 position)
        {
            return float.IsFinite(position.X)
                && float.IsFinite(position.Y);
        }

        private static bool IsPlayerWithinHitRectangle(IPlayer player, AvalancheHitShape hitShape)
        {
            if (player == null || !IsFinite(hitShape.Position) || !IsFinite(hitShape.Forward) || !IsFinite(player.Position))
                return false;

            float verticalDistance = MathF.Abs(player.Position.Y - hitShape.Position.Y);
            if (verticalDistance > AvalancheVerticalContactRange)
                return false;

            var avalanchePosition = new Vector2(hitShape.Position.X, hitShape.Position.Z);
            var playerPosition = new Vector2(player.Position.X, player.Position.Z);
            Vector2 offset = playerPosition - avalanchePosition;
            float along = Vector2.Dot(offset, hitShape.Forward);
            var sideAxis = new Vector2(-hitShape.Forward.Y, hitShape.Forward.X);
            float side = Vector2.Dot(offset, sideAxis);
            return MathF.Abs(along) <= AvalancheContactHalfDepth
                && MathF.Abs(side) <= AvalancheContactHalfWidth;
        }

        private AvalancheDamageResult ApplyDamage(IPlayer player, uint effectId)
        {
            uint shieldAbsorbAmount = Math.Min(player.Shield, AvalancheDamageAmount);
            if (shieldAbsorbAmount != 0u)
                player.Shield -= shieldAbsorbAmount;

            uint healthDamage = AvalancheDamageAmount - shieldAbsorbAmount;
            if (healthDamage == 0u || player.Health <= 1u)
                return new AvalancheDamageResult(effectId, AvalancheDamageAmount, shieldAbsorbAmount, 0u, 0u, false);

            uint nonLethalHealthDamage = Math.Min(healthDamage, player.Health - 1u);
            if (nonLethalHealthDamage != 0u)
                player.ModifyHealth(nonLethalHealthDamage, DamageType.Magic, owner);

            return new AvalancheDamageResult(effectId, AvalancheDamageAmount, shieldAbsorbAmount, nonLethalHealthDamage, 0u, false);
        }

        private bool ApplyKnockdown(IPlayer player, double now, uint effectId, uint castingId)
        {
            player.AddCCState(CCState.Knockdown, effectId, AvalancheKnockdownSpellId, castingId);
            StopPlayerMovement(player);
            activeKnockdowns[player.CharacterId] = new KnockdownRecord(player, now + KnockdownDurationSeconds, effectId, castingId);
            return true;
        }

        private void SendAvalancheHitSpellStart(IPlayer player, uint castingId, Vector3 position)
        {
            if (!IsFinite(position))
                position = owner.Position;

            var spellStart = new ServerSpellStart
            {
                CastingId       = castingId,
                Spell4Id        = AvalancheKnockdownSpellId,
                RootSpell4Id    = AvalancheKnockdownSpellId,
                ParentSpell4Id  = AvalancheKnockdownSpellId,
                CasterId        = owner.Guid,
                PrimaryTargetId = player.Guid,
                FieldPosition   = new NetworkPosition(position),
                Yaw             = owner.Rotation.X
            };

            spellStart.InitialPositionData.Add(new ServerSpellStart.InitialPosition
            {
                UnitId      = owner.Guid,
                TargetFlags = 3,
                Position    = new NetworkPosition(position),
                Yaw         = owner.Rotation.X
            });

            owner.EnqueueToVisible(spellStart, true);
        }

        private void SendAvalancheHitSpellGo(IPlayer player, uint castingId, Vector3 position, AvalancheDamageResult damageResult, uint knockdownEffectId, uint achievementEffectId)
        {
            if (!IsFinite(position))
                position = owner.Position;

            var spellGo = new ServerSpellGo
            {
                ServerUniqueId     = castingId,
                BIgnoreCooldown    = true,
                PrimaryDestination = new NetworkPosition(position),
                Phase              = -1
            };

            spellGo.InitialPositionData.Add(new SharedInitialPosition
            {
                UnitId      = owner.Guid,
                TargetFlags = 3,
                Position    = new NetworkPosition(position),
                Yaw         = owner.Rotation.X
            });

            var targetInfo = new SharedTargetInfo
            {
                UnitId        = player.Guid,
                TargetFlags   = 1,
                InstanceCount = 1,
                CombatResult  = CombatResult.Hit
            };

            if (knockdownEffectId != 0u)
            {
                targetInfo.EffectInfoData.Add(new SharedTargetInfo.EffectInfo
                {
                    Spell4EffectId = AvalancheKnockdownSpell4EffectId,
                    EffectUniqueId = knockdownEffectId,
                    TimeRemaining  = -1
                });
            }

            targetInfo.EffectInfoData.Add(new SharedTargetInfo.EffectInfo
            {
                Spell4EffectId = AvalancheDamageSpell4EffectId,
                EffectUniqueId = damageResult.EffectId,
                TimeRemaining  = -1,
                InfoType       = 1,
                DamageDescriptionData = new SharedTargetInfo.EffectInfo.DamageDescription
                {
                    RawDamage          = damageResult.RawDamage,
                    RawScaledDamage    = damageResult.RawDamage,
                    ShieldAbsorbAmount = damageResult.ShieldAbsorbAmount,
                    AdjustedDamage     = damageResult.HealthDamage,
                    OverkillAmount     = damageResult.OverkillAmount,
                    KilledTarget       = damageResult.KilledTarget,
                    CombatResult       = CombatResult.Hit,
                    DamageType         = DamageType.Magic
                }
            });

            if (achievementEffectId != 0u)
            {
                targetInfo.EffectInfoData.Add(new SharedTargetInfo.EffectInfo
                {
                    Spell4EffectId = AvalancheAchievementSpell4EffectId,
                    EffectUniqueId = achievementEffectId,
                    TimeRemaining  = -1
                });
            }

            spellGo.TargetInfoData.Add(targetInfo);
            owner.EnqueueToVisible(spellGo, true);
        }

        private void SendKnockdownSet(IPlayer player, uint effectId)
        {
            owner.EnqueueToVisible(new ServerEntityCCStateSet
            {
                UnitId              = player.Guid,
                CCType              = CCState.Knockdown,
                SpellEffectUniqueId = effectId
            }, true);
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

                RemoveKnockdown(entry.Value);
                activeKnockdowns.Remove(entry.Key);
            }
        }

        private void ClearActiveKnockdowns()
        {
            foreach (KnockdownRecord record in activeKnockdowns.Values.ToArray())
                RemoveKnockdown(record);

            activeKnockdowns.Clear();
        }

        private static void RemoveKnockdown(KnockdownRecord record)
        {
            IPlayer player = record.Player;
            if (!player.RemoveCCState(CCState.Knockdown, record.EffectId))
                return;

            player.EnqueueToVisible(new ServerEntityCCStateRemove
            {
                UnitId              = player.Guid,
                CCType              = CCState.Knockdown,
                SpellCastUniqueId   = record.CastingId,
                SpellEffectUniqueId = record.EffectId,
                Removed             = true
            }, true);
        }

        private static bool ShouldGrantSnowSmash(IPlayer player)
        {
            if (player.AchievementManager == null)
                return false;

            if (player.AchievementManager.HasCompletedAchievement(SnowSmashAchievement))
                return false;

            return true;
        }

        private static void GrantSnowSmash(IPlayer player)
        {
            if (!ShouldGrantSnowSmash(player))
                return;

            player.AchievementManager.GrantAchievement(SnowSmashAchievement);
        }

        private readonly record struct AvalancheDamageResult(uint EffectId, uint RawDamage, uint ShieldAbsorbAmount, uint HealthDamage, uint OverkillAmount, bool KilledTarget);
        private readonly record struct KnockdownRecord(IPlayer Player, double ExpiresAt, uint EffectId, uint CastingId);
    }
}
