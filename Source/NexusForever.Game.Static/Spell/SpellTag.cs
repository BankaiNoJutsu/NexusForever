using System.Diagnostics.CodeAnalysis;

namespace NexusForever.Game.Static.Spell
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum SpellTag
    {
        Assault   = 0x0001,
        Support   = 0x0002,
        Path      = 0x0003,
        Misc      = 0x0004,
        Mount     = 0x0005,
        UNUSED006 = 0x0006,
        Utility   = 0x0007,
        UNUSED008 = 0x0008,
        UNUSED009 = 0x0009,
        UNUSED010 = 0x000A,
        UNUSED011 = 0x000B
    }
}
