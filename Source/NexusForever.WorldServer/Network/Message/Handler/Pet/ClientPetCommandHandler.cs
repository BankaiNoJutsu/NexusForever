using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Spell;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Pet;

namespace NexusForever.WorldServer.Network.Message.Handler.Pet
{
    public class ClientPetCommandHandler : IMessageHandler<IWorldSession, ClientPetCommand>
    {
        private readonly ILogger<ClientPetCommandHandler> log;

        public ClientPetCommandHandler(ILogger<ClientPetCommandHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientPetCommand petCommand)
        {
            if (session.Player?.Map == null)
                return;

            if (petCommand.ShortcutSet != ShortcutSet.PrimaryPetBar)
            {
                log.LogDebug("ClientPetCommand ignored non-primary pet bar command: player={Player} shortcutSet={ShortcutSet} slot={SlotIndex}.",
                    session.Player.Guid, petCommand.ShortcutSet, petCommand.SlotIndex);
                return;
            }

            switch (petCommand.SlotIndex)
            {
                case EngineerCombatBotPetCommandHelper.PrimaryPetBarAttackSlotIndex:
                    HandleAttackCommand(session);
                    break;
                case EngineerCombatBotPetCommandHelper.PrimaryPetBarStopSlotIndex:
                    HandleStopCommand(session);
                    break;
                default:
                    log.LogDebug("ClientPetCommand ignored unsupported primary pet bar slot: player={Player} slot={SlotIndex}.",
                        session.Player.Guid, petCommand.SlotIndex);
                    break;
            }
        }

        private void HandleAttackCommand(IWorldSession session)
        {
            IUnitEntity target = ResolvePlayerTarget(session.Player);
            if (target == null || target.Guid == session.Player.Guid || !target.IsAlive)
            {
                log.LogDebug("ClientPetCommand attack ignored without a valid target: player={Player} target={Target}.",
                    session.Player.Guid, session.Player.TargetGuid ?? 0u);
                return;
            }

            IReadOnlyCollection<IWorldEntity> engineerBots = EngineerCombatBotPetCommandHelper.GetActiveEngineerCombatBots(session.Player);
            uint commandedCount = 0u;
            foreach (IWorldEntity engineerBot in engineerBots)
            {
                if (engineerBot is not IUnitEntity unit || !unit.CanAttack(target))
                    continue;

                unit.SummonCommandFollowRequested = false;
                unit.ThreatManager?.UpdateThreat(target, 1);
                unit.SetTarget(target, 1u);
                commandedCount++;
            }

            log.LogDebug("ClientPetCommand attack: player={Player} target={Target} commandedBots={CommandedCount}.",
                session.Player.Guid, target.Guid, commandedCount);
        }

        private void HandleStopCommand(IWorldSession session)
        {
            IReadOnlyCollection<IWorldEntity> engineerBots = EngineerCombatBotPetCommandHelper.GetActiveEngineerCombatBots(session.Player);
            foreach (IWorldEntity engineerBot in engineerBots)
            {
                EngineerCombatBotPetCommandHelper.ClearCombat(engineerBot);
                engineerBot.SummonCommandFollowRequested = true;
            }

            log.LogDebug("ClientPetCommand stop: player={Player} commandedBots={CommandedCount}.",
                session.Player.Guid, engineerBots.Count);
        }

        private static IUnitEntity ResolvePlayerTarget(IPlayer player)
        {
            uint? targetGuid = player.TargetGuid;
            if (!targetGuid.HasValue)
                return null;

            return player.GetVisible<IUnitEntity>(targetGuid.Value)
                ?? player.Map?.GetEntity<IUnitEntity>(targetGuid.Value);
        }
    }
}
