using System.Numerics;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Search;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Message.Static;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Main.Creature
{
    [ScriptFilterCreatureId(62476u)]
    public class SpeedBoostEntityScript : IWorldEntityScript, IOwnedScript<ICreatureEntity>
    {
        private const uint SpeedBoostSpellId = 72259u;
        private const float TriggerRange = 4f;
        private const double RangeSweepIntervalSeconds = 0.1d;

        private static readonly TimeSpan boostCooldown = TimeSpan.FromMilliseconds(800d);

        private readonly Dictionary<uint, DateTime> playerBoosts = [];

        private readonly ILogger<SpeedBoostEntityScript> log;
        private readonly IFactory<ISpellParameters> spellParametersFactory;
        private ICreatureEntity owner;
        private double rangeSweepElapsedSeconds;

        public SpeedBoostEntityScript(
            ILogger<SpeedBoostEntityScript> log,
            IFactory<ISpellParameters> spellParametersFactory)
        {
            this.log                    = log;
            this.spellParametersFactory = spellParametersFactory;
        }

        public void OnLoad(ICreatureEntity owner)
        {
            this.owner = owner;
            RegisterRangeCheck();
        }

        public void OnAddToMap(IBaseMap map)
        {
            RegisterRangeCheck();
            log.LogDebug("Speed boost trigger script loaded for entity {EntityGuid}: creature={CreatureId}, position=({X}, {Y}, {Z}), range={Range}.",
                owner.Guid,
                owner.CreatureId,
                owner.Position.X,
                owner.Position.Y,
                owner.Position.Z,
                TriggerRange);
        }

        public void OnEnterRange(IGridEntity entity)
        {
            TryApplyBoost(entity);
        }

        public void Update(double lastTick)
        {
            if (owner == null)
                return;

            rangeSweepElapsedSeconds += Math.Max(lastTick, 0d);
            if (rangeSweepElapsedSeconds < RangeSweepIntervalSeconds)
                return;

            rangeSweepElapsedSeconds = 0d;
            if (owner.Map == null)
                return;

            foreach (IGridEntity entity in owner.Map.Search(owner.Position, TriggerRange, new SpeedBoostSearchCheck(owner, TriggerRange)).ToList())
                TryApplyBoost(entity);
        }

        private void TryApplyBoost(IGridEntity entity)
        {
            if (!TryResolvePlayer(entity, out IPlayer player) || !player.IsAlive)
                return;

            DateTime now = DateTime.UtcNow;
            if (playerBoosts.TryGetValue(player.Guid, out DateTime lastBoost) && now - lastBoost < boostCooldown)
                return;

            ISpellParameters spellParameters = spellParametersFactory.Resolve();
            spellParameters.PrimaryTargetId        = player.Guid;
            spellParameters.UserInitiatedSpellCast = false;
            spellParameters.IgnoreGlobalCooldown   = true;
            spellParameters.CancelActiveTrade      = true;
            spellParameters.ClientRequestSource    = nameof(SpeedBoostEntityScript);

            CastResult castResult = player.TryCastSpell(SpeedBoostSpellId, spellParameters);
            if (castResult != CastResult.Ok)
            {
                log.LogDebug("Speed boost spell failed for player {PlayerGuid}: trigger={TriggerGuid}, spell={SpellId}, result={CastResult}.",
                    player.Guid,
                    owner.Guid,
                    SpeedBoostSpellId,
                    castResult);
                return;
            }

            playerBoosts[player.Guid] = now;
            log.LogDebug("Speed boost applied for player {PlayerGuid}: trigger={TriggerGuid}, spell={SpellId}.",
                player.Guid,
                owner.Guid,
                SpeedBoostSpellId);
        }

        private bool TryResolvePlayer(IGridEntity entity, out IPlayer player)
        {
            player = null;

            if (entity is IPlayer directPlayer)
            {
                player = directPlayer;
                return true;
            }

            if (entity is not IVehicleEntity vehicle)
                return false;

            uint? pilotGuid = vehicle.GetPassenger(VehicleSeatType.Pilot, 0)?.Guid;
            pilotGuid ??= vehicle.ControllerGuid;

            if (!pilotGuid.HasValue)
                return false;

            player = owner.Map?.GetEntity<IPlayer>(pilotGuid.Value);
            return player?.PlatformGuid == vehicle.Guid;
        }

        private void RegisterRangeCheck()
        {
            owner.SetInRangeCheck(TriggerRange);
        }

        private sealed class SpeedBoostSearchCheck : ISearchCheck<IGridEntity>
        {
            private readonly IGridEntity owner;
            private readonly float range;

            public SpeedBoostSearchCheck(IGridEntity owner, float range)
            {
                this.owner = owner;
                this.range = range;
            }

            public bool CheckEntity(IGridEntity entity)
            {
                if (entity == null || entity == owner)
                    return false;

                return Vector3.DistanceSquared(owner.Position, entity.Position) < range * range;
            }
        }
    }
}
