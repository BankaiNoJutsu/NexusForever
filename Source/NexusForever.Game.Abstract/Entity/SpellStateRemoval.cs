using NexusForever.Game.Static.Combat.CrowdControl;

namespace NexusForever.Game.Abstract.Entity
{
    public enum SpellStateRemovalKind
    {
        PropertyModifier,
        CrowdControl,
        Stealth,
        AggroImmune,
        UnitState,
        Busy,
        Absorption,
        SpellEffectImmunity,
        SpellImmunity,
        DelayDeath,
        Proc,
        VitalClamp,
        ShieldOverload,
        Scale,
        Faction,
        DisguiseOutfit,
        MimicDisguise,
        HealingAbsorption
    }

    public sealed record SpellStateRemoval(
        SpellStateRemovalKind Kind,
        uint Spell4Id,
        uint CastingId,
        uint EffectId = 0u,
        CCState? CCState = null);
}
