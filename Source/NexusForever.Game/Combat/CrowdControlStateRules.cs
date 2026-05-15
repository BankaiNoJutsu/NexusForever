using NexusForever.Game.Static.Combat.CrowdControl;
using NexusForever.Game.Static.Entity.Movement.Command.State;
using NexusForever.Game.Static.Spell;

namespace NexusForever.Game.Combat.CrowdControl
{
    public static class CrowdControlStateRules
    {
        public const uint ClientMovementBlockMask =
            (1u << (int)CCState.Stun)
            | (1u << (int)CCState.Sleep)
            | (1u << (int)CCState.Root)
            | (1u << (int)CCState.Hold)
            | (1u << (int)CCState.Knockdown)
            | (1u << (int)CCState.Polymorph)
            | (1u << (int)CCState.Disable);

        public const uint ClientJumpBlockMask = 1u << (int)CCState.Grounded;

        public const StateFlags ClientMovementBlockedStateFlags = StateFlags.Move
            | StateFlags.Velocity
            | StateFlags.Jump
            | StateFlags.DoubleJump
            | StateFlags.RollForward
            | StateFlags.RollBackward;

        public const StateFlags ClientJumpBlockedStateFlags = StateFlags.Jump | StateFlags.DoubleJump;

        public static bool BlocksCasting(CCState state, SpellSchool school)
        {
            return state switch
            {
                CCState.Silence => school == SpellSchool.Spell,
                CCState.Disarm => school is SpellSchool.Melee or SpellSchool.Ranged or SpellSchool.Unarmed,
                CCState.Stun
                    or CCState.Sleep
                    or CCState.Fear
                    or CCState.Hold
                    or CCState.Knockdown
                    or CCState.Polymorph
                    or CCState.Disable
                    or CCState.Daze
                    or CCState.Subdue
                    or CCState.DisableCinematic
                    or CCState.AbilityRestriction => true,
                _ => false
            };
        }

        public static bool HasClientMovementBlock(uint activeMask)
        {
            return (activeMask & ClientMovementBlockMask) != 0u;
        }

        public static bool HasClientJumpBlock(uint activeMask)
        {
            return (activeMask & ClientJumpBlockMask) != 0u;
        }

        public static StateFlags FilterClientStateFlags(StateFlags state, uint activeMask)
        {
            if (HasClientMovementBlock(activeMask))
                return state & ~ClientMovementBlockedStateFlags;

            if (HasClientJumpBlock(activeMask))
                return state & ~ClientJumpBlockedStateFlags;

            return state;
        }
    }
}