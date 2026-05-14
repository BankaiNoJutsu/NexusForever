namespace NexusForever.Game.Static.Spell.Effect
{
    [Flags]
    public enum SpellEffectForcedMoveFlags
    {
        None      = 0x00,
        Target    = 0x01,
        Unknown2  = 0x02,
        Unknown4  = 0x04,
        Facing    = 0x08,
        Unknown10 = 0x10,
        Unknown20 = 0x20
    }
}
