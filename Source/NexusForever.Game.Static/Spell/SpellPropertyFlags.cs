namespace NexusForever.Game.Static.Spell
{
    [Flags]
    public enum SpellPropertyFlags : uint
    {
        None                  = 0x00000000,
        HideCooldownInTooltip = 0x00000200,
        IsBeneficial          = 0x04000000,
        HasServiceTokenCost   = 0x20000000
    }
}
