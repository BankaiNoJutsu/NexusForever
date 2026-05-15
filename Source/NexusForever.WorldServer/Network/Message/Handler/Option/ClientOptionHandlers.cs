using Microsoft.Extensions.Logging;
using NexusForever.Game.Static.Option;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Option;

namespace NexusForever.WorldServer.Network.Message.Handler.Option
{
    public class ClientCombatOptionsHandler : IMessageHandler<IWorldSession, ClientCombatOptions>
    {
        private readonly ILogger<ClientCombatOptionsHandler> log;

        public ClientCombatOptionsHandler(ILogger<ClientCombatOptionsHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientCombatOptions combatOptions)
        {
            if (!ClientOptionValidation.HasOnlyKnownCastingOptions(combatOptions.CastingOptions) ||
                !ClientOptionValidation.HasOnlyKnownCombatLogOptions(combatOptions.CombatLogDisableFlags))
            {
                throw new InvalidPacketValueException($"Invalid combat options received: casting={combatOptions.CastingOptions}, combatLog={combatOptions.CombatLogDisableFlags}");
            }

            log.LogDebug("Ignoring unsupported combat option sync from player {PlayerGuid}: casting {CastingOptions}, disable other player logs {DisableOtherPlayers}, combat log disables {CombatLogDisables}.",
                session.Player?.Guid, combatOptions.CastingOptions, combatOptions.DisableOtherPlayersLogging, combatOptions.CombatLogDisableFlags);
        }
    }

    public class ClientOptionsHandler : IMessageHandler<IWorldSession, ClientOptions>
    {
        private readonly ILogger<ClientOptionsHandler> log;

        public ClientOptionsHandler(ILogger<ClientOptionsHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientOptions options)
        {
            switch (options.Type)
            {
                case OptionType.Casting:
                    if (!ClientOptionValidation.HasOnlyKnownCastingOptions((CastingOptionFlags)options.NewValue))
                        throw new InvalidPacketValueException($"Invalid casting options received: {options.NewValue}");
                    break;
                case OptionType.SharedChallenge:
                    if (options.NewValue > 1u)
                        throw new InvalidPacketValueException($"Invalid shared challenge option received: {options.NewValue}");
                    break;
                default:
                    throw new InvalidPacketValueException($"Invalid option type received: {options.Type}");
            }

            log.LogDebug("Ignoring unsupported option update from player {PlayerGuid}: type {OptionType}, value {OptionValue}.",
                session.Player?.Guid, options.Type, options.NewValue);
        }
    }

    public class ClientCombatLogDisableOthersHandler : IMessageHandler<IWorldSession, ClientCombatLogDisableOthers>
    {
        private readonly ILogger<ClientCombatLogDisableOthersHandler> log;

        public ClientCombatLogDisableOthersHandler(ILogger<ClientCombatLogDisableOthersHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientCombatLogDisableOthers combatLogDisableOthers)
        {
            log.LogDebug("Ignoring unsupported combat log disable-others update from player {PlayerGuid}: disable {DisableOtherPlayers}.",
                session.Player?.Guid, combatLogDisableOthers.DisableOtherPlayers);
        }
    }

    public class ClientCombatLogDisablesHandler : IMessageHandler<IWorldSession, ClientCombatLogDisables>
    {
        private readonly ILogger<ClientCombatLogDisablesHandler> log;

        public ClientCombatLogDisablesHandler(ILogger<ClientCombatLogDisablesHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientCombatLogDisables combatLogDisables)
        {
            if (!ClientOptionValidation.HasOnlyKnownCombatLogOptions(combatLogDisables.DisableFlags))
                throw new InvalidPacketValueException($"Invalid combat log disable flags received: {combatLogDisables.DisableFlags}");

            log.LogDebug("Ignoring unsupported combat log disable-flags update from player {PlayerGuid}: disables {CombatLogDisables}.",
                session.Player?.Guid, combatLogDisables.DisableFlags);
        }
    }

    internal static class ClientOptionValidation
    {
        private const uint CastingOptionMask =
            (uint)(CastingOptionFlags.Unknown |
            CastingOptionFlags.ButtonDownForAbilities |
            CastingOptionFlags.AutoTargetting |
            CastingOptionFlags.HoldToContinueCasting);

        private const uint CombatLogOptionMask =
            (uint)(CombatLogOptions.DisableAbsorption |
            CombatLogOptions.DisableCCState |
            CombatLogOptions.DisableDamage |
            CombatLogOptions.DisableDeflect |
            CombatLogOptions.DisableDelayDeath |
            CombatLogOptions.DisableDispel |
            CombatLogOptions.DisableFallingDamage |
            CombatLogOptions.DisableHeal |
            CombatLogOptions.DisableImmunity |
            CombatLogOptions.DisableInterrupted |
            CombatLogOptions.DisableInterruptArmor |
            CombatLogOptions.DisableTransference |
            CombatLogOptions.DisableVitalModifier |
            CombatLogOptions.DisableDeath);

        public static bool HasOnlyKnownCastingOptions(CastingOptionFlags options)
        {
            return ((uint)options & ~CastingOptionMask) == 0u;
        }

        public static bool HasOnlyKnownCombatLogOptions(CombatLogOptions options)
        {
            return ((uint)options & ~CombatLogOptionMask) == 0u;
        }
    }
}
