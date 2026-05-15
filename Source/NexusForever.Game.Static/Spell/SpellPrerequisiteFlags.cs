namespace NexusForever.Game.Static.Spell
{
    [Flags]
    public enum SpellPrerequisiteFlags : uint
    {
        None                   = 0x00000000,
        TargetAvoided          = 0x00000001,
        TargetBlocked          = 0x00000002,
        TargetGlancing         = 0x00000004,
        TargetFierce           = 0x00000008,
        NotUsed                = 0x00000010,
        CasterAvoided          = 0x00000020,
        CasterBlocked          = 0x00000040,
        CasterGlancing         = 0x00000080,
        CasterFierce           = 0x00000100,
        NotUsed1               = 0x00000200,
        CasterSpellSuccess     = 0x00000400,
        LastCasterSpellSuccess = 0x00000800
    }
}
