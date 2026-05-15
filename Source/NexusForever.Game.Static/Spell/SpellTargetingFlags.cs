namespace NexusForever.Game.Static.Spell
{
    [Flags]
    public enum SpellTargetingFlags : uint
    {
        None            = 0x00000000,
        InterruptOnMove = 0x00000040,
        FreeformTarget  = 0x00400000
    }
}