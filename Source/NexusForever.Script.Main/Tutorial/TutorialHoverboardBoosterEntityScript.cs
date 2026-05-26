using System.Numerics;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Entity.Movement.Command.State;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Main.Tutorial
{
    [ScriptFilterCreatureId(73461u)]
    public class TutorialHoverboardBoosterEntityScript : IWorldEntityScript, IOwnedScript<ISimpleEntity>
    {
        private const ushort TutorialWorldId = 3460;
        private const ushort ExileHoverboardQuestId = 10527;
        private const ushort DominionHoverboardQuestId = 10532;
        private const ushort ExileCombatQuestId = 10518;
        private const ushort DominionCombatQuestId = 10524;
        private const uint TutorialHoverboardSpellId = 85562u;
        private const uint ExtraGasSpellId = 85424u;
        private const uint HoverboardSprintVisualSpellId = 82298u;
        private const float BoosterRange = 4.5f;
        private const float BoosterSpeed = 57f;

        private static readonly TimeSpan boostCooldown = TimeSpan.FromMilliseconds(800d);

        private readonly Dictionary<uint, DateTime> playerBoosts = [];

        private readonly IFactory<ISpellParameters> spellParametersFactory;
        private readonly ILogger<TutorialHoverboardBoosterEntityScript> log;
        private ISimpleEntity owner;

        public TutorialHoverboardBoosterEntityScript(
            ILogger<TutorialHoverboardBoosterEntityScript> log,
            IFactory<ISpellParameters> spellParametersFactory)
        {
            this.log                    = log;
            this.spellParametersFactory = spellParametersFactory;
        }

        public void OnLoad(ISimpleEntity owner)
        {
            this.owner = owner;
        }

        public void OnAddToMap(IBaseMap map)
        {
            owner.SetInRangeCheck(BoosterRange);
            log.LogDebug("Starter tutorial hoverboard booster script loaded for entity {EntityGuid}: creature={CreatureId}, position=({X}, {Y}, {Z}), range={Range}.",
                owner.Guid,
                owner.CreatureId,
                owner.Position.X,
                owner.Position.Y,
                owner.Position.Z,
                BoosterRange);
        }

        public void OnEnterRange(IGridEntity entity)
        {
            if (!TryResolveBoostContext(entity, out IPlayer player, out IWorldEntity mover) || !CanBoost(player))
                return;

            DateTime now = DateTime.UtcNow;
            if (playerBoosts.TryGetValue(player.Guid, out DateTime lastBoost) && now - lastBoost < boostCooldown)
                return;

            Vector3 direction = ResolveBoostDirection(player, mover);
            ApplyBoost(mover, direction);
            CastBoosterEffects(player);

            playerBoosts[player.Guid] = now;
            log.LogDebug("Starter tutorial hoverboard booster applied for player {PlayerGuid}: booster={BoosterGuid}, mover={MoverGuid}, speed={Speed}, direction=({X}, {Y}, {Z}).",
                player.Guid,
                owner.Guid,
                mover.Guid,
                BoosterSpeed,
                direction.X,
                direction.Y,
                direction.Z);
        }

        private bool TryResolveBoostContext(IGridEntity entity, out IPlayer player, out IWorldEntity mover)
        {
            player = null;
            mover = null;

            if (entity is IPlayer directPlayer)
            {
                player = directPlayer;
                mover = ResolveMover(directPlayer);
                return true;
            }

            if (entity is not IVehicleEntity vehicle)
                return false;

            player = ResolvePilot(vehicle);
            if (player == null || player.PlatformGuid != vehicle.Guid)
                return false;

            mover = vehicle;
            return true;
        }

        private IPlayer ResolvePilot(IVehicleEntity vehicle)
        {
            uint? pilotGuid = vehicle.GetPassenger(VehicleSeatType.Pilot, 0)?.Guid;
            pilotGuid ??= vehicle.ControllerGuid;

            return pilotGuid.HasValue
                ? owner.Map?.GetEntity<IPlayer>(pilotGuid.Value)
                : null;
        }

        private void CastBoosterEffects(IPlayer player)
        {
            player.CastSpell(ExtraGasSpellId, CreateSpellParameters(player));
            player.CastSpell(HoverboardSprintVisualSpellId, CreateSpellParameters(player));
        }

        private ISpellParameters CreateSpellParameters(IPlayer player)
        {
            ISpellParameters spellParameters = spellParametersFactory.Resolve();
            spellParameters.PrimaryTargetId        = player.Guid;
            spellParameters.UserInitiatedSpellCast = false;
            spellParameters.IgnoreGlobalCooldown   = true;
            spellParameters.CancelActiveTrade      = true;
            spellParameters.ClientRequestSource    = nameof(TutorialHoverboardBoosterEntityScript);
            return spellParameters;
        }

        private static bool CanBoost(IPlayer player)
        {
            if (player.Map?.Entry?.Id != TutorialWorldId)
                return false;

            if (player.PlatformGuid == null && !player.HasTrackedSpellState(TutorialHoverboardSpellId))
                return false;

            return HasQuestState(player, ExileHoverboardQuestId)
                || HasQuestState(player, DominionHoverboardQuestId)
                || HasQuestState(player, ExileCombatQuestId)
                || HasQuestState(player, DominionCombatQuestId);
        }

        private static bool HasQuestState(IPlayer player, ushort questId)
        {
            return player.QuestManager.GetQuestState(questId) is QuestState.Accepted or QuestState.Achieved or QuestState.Completed;
        }

        private static IWorldEntity ResolveMover(IPlayer player)
        {
            if (player.PlatformGuid != null)
            {
                IWorldEntity platform = player.Map?.GetEntity<IWorldEntity>(player.PlatformGuid.Value);
                if (platform != null)
                    return platform;
            }

            return player;
        }

        private static Vector3 ResolveBoostDirection(IPlayer player, IWorldEntity mover)
        {
            Vector3 direction = mover.MovementManager.GetVelocity();
            if (TryNormaliseHorizontal(direction, out Vector3 normalised))
                return normalised;

            direction = player.MovementManager.GetVelocity();
            if (TryNormaliseHorizontal(direction, out normalised))
                return normalised;

            direction = player.MovementManager.GetMove();
            if (TryNormaliseHorizontal(direction, out normalised))
                return normalised;

            float angle = -player.Rotation.X + MathF.PI / 2f;
            return new Vector3(MathF.Cos(angle), 0f, MathF.Sin(angle));
        }

        private static bool TryNormaliseHorizontal(Vector3 direction, out Vector3 normalised)
        {
            direction.Y = 0f;
            if (direction.LengthSquared() <= 0.0001f)
            {
                normalised = Vector3.Zero;
                return false;
            }

            normalised = Vector3.Normalize(direction);
            return true;
        }

        private static void ApplyBoost(IWorldEntity mover, Vector3 direction)
        {
            mover.MovementManager.SetState(mover.MovementManager.GetState() | StateFlags.Velocity);
            mover.MovementManager.SetVelocity(direction * BoosterSpeed, false);
            mover.MovementManager.BroadcastNetworkEntityCommands();
        }
    }
}
