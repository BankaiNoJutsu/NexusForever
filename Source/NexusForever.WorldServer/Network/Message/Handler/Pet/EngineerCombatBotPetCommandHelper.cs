using System.Collections.Generic;
using System.Linq;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Pet;

namespace NexusForever.WorldServer.Network.Message.Handler.Pet
{
    internal static class EngineerCombatBotPetCommandHelper
    {
        public const uint PrimaryPetBarAttackSlotIndex = 0u;
        public const uint PrimaryPetBarStopSlotIndex   = 1u;
        public const uint PrimaryPetBarGoToSlotIndex   = 3u;

        private const float DefaultCommandMovementSpeed = 10f;

        private static readonly uint[] EngineerCombatBotCreatureIds =
        [
            42682u,
            59845u,
            42683u,
            59846u,
            42684u,
            59847u,
            42685u,
            59848u
        ];

        public static IReadOnlyCollection<IWorldEntity> GetActiveEngineerCombatBots(IPlayer player)
        {
            IEntitySummonFactory summonFactory = player?.SummonFactory;
            if (summonFactory == null)
                return [];

            var engineerBots = new List<IWorldEntity>();
            foreach (uint creatureId in EngineerCombatBotCreatureIds)
            {
                foreach (IWorldEntity summon in summonFactory.GetSummonCreatures(creatureId))
                {
                    if (IsOwnedEngineerCombatBot(summon, player))
                        engineerBots.Add(summon);
                }
            }

            return engineerBots;
        }

        public static void ClearCombat(IWorldEntity engineerBot)
        {
            if (engineerBot is not IUnitEntity unit)
                return;

            unit.SetTarget((IWorldEntity)null);
            unit.ThreatManager?.ClearThreatList();
            unit.MovementManager?.Finalise();
        }

        public static float GetCommandMovementSpeed(IWorldEntity engineerBot)
        {
            float speed = engineerBot?.GetPropertyValue(Property.MoveSpeedMultiplier) * DefaultCommandMovementSpeed ?? 0f;
            return float.IsFinite(speed) && speed > 0f
                ? speed
                : DefaultCommandMovementSpeed;
        }

        private static bool IsOwnedEngineerCombatBot(IWorldEntity entity, IPlayer owner)
        {
            return entity != null
                && owner != null
                && entity.SummonerGuid == owner.Guid
                && EngineerCombatBotCreatureIds.Contains(entity.CreatureId);
        }
    }
}
