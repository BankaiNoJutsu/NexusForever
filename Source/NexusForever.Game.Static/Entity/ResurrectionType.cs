namespace NexusForever.Game.Static.Entity
{
    [Flags]
    public enum ResurrectionType
    {
        None                    = 0,
        WakeHere                = 1,
        Holocrypt               = 2,
        SpellCasterLocation     = 4,
        ExitInstance            = 32,
        WakeHereServiceToken    = 64,

        OpenWorld               = WakeHere | WakeHereServiceToken,
        Dungeon                 = ExitInstance,
        ContentPvp              = Holocrypt
    }
}
