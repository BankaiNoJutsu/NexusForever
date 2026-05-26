using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Main.Tutorial
{
    [ScriptFilterCreatureId(73416u, 70939u)]
    public class TutorialHoverboardRingEntityScript : IWorldEntityScript, IOwnedScript<ISimpleEntity>
    {
        private const ushort TutorialWorldId = 3460;
        private const ushort ExileHoverboardQuestId = 10527;
        private const ushort DominionHoverboardQuestId = 10532;
        private const ushort ExileCombatQuestId = 10518;
        private const ushort DominionCombatQuestId = 10524;
        private const uint TutorialHoverboardSpellId = 85562u;
        private const uint HoverboardRaceObjectiveFxSpellId = 82460u;
        private const float RingRange = 3.5f;

        private static readonly TimeSpan ringEffectCooldown = TimeSpan.FromMilliseconds(500d);

        private readonly Dictionary<uint, DateTime> playerEffects = [];

        private readonly IFactory<ISpellParameters> spellParametersFactory;
        private readonly ILogger<TutorialHoverboardRingEntityScript> log;
        private ISimpleEntity owner;

        public TutorialHoverboardRingEntityScript(
            ILogger<TutorialHoverboardRingEntityScript> log,
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
            owner.SetInRangeCheck(RingRange);
            log.LogDebug("Starter tutorial hoverboard ring script loaded for entity {EntityGuid}: creature={CreatureId}, position=({X}, {Y}, {Z}), range={Range}.",
                owner.Guid,
                owner.CreatureId,
                owner.Position.X,
                owner.Position.Y,
                owner.Position.Z,
                RingRange);
        }

        public void OnEnterRange(IGridEntity entity)
        {
            IPlayer player = ResolvePlayer(entity);
            if (player == null || !CanTriggerRingEffect(player))
                return;

            DateTime now = DateTime.UtcNow;
            if (playerEffects.TryGetValue(player.Guid, out DateTime lastEffect) && now - lastEffect < ringEffectCooldown)
                return;

            player.CastSpell(HoverboardRaceObjectiveFxSpellId, CreateSpellParameters(player));

            playerEffects[player.Guid] = now;
            log.LogDebug("Starter tutorial hoverboard ring effects applied for player {PlayerGuid}: ring={RingGuid}.",
                player.Guid,
                owner.Guid);
        }

        private ISpellParameters CreateSpellParameters(IPlayer player)
        {
            ISpellParameters spellParameters = spellParametersFactory.Resolve();
            spellParameters.PrimaryTargetId        = player.Guid;
            spellParameters.UserInitiatedSpellCast = false;
            spellParameters.IgnoreGlobalCooldown   = true;
            spellParameters.CancelActiveTrade      = true;
            spellParameters.ClientRequestSource    = nameof(TutorialHoverboardRingEntityScript);
            return spellParameters;
        }

        private IPlayer ResolvePlayer(IGridEntity entity)
        {
            if (entity is IPlayer player)
                return player;

            if (entity is not IVehicleEntity vehicle)
                return null;

            uint? pilotGuid = vehicle.GetPassenger(VehicleSeatType.Pilot, 0)?.Guid;
            pilotGuid ??= vehicle.ControllerGuid;

            return pilotGuid.HasValue
                ? owner.Map?.GetEntity<IPlayer>(pilotGuid.Value)
                : null;
        }

        private static bool CanTriggerRingEffect(IPlayer player)
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
    }
}
