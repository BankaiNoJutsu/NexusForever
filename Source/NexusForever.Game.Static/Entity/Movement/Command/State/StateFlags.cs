namespace NexusForever.Game.Static.Entity.Movement.Command.State
{
    [Flags]
    public enum StateFlags
    {
        None                = 0x0000,
        Velocity            = 0x0001,
        Move                = 0x0002,
        Unknown04           = 0x0004,
        Fall                = 0x0008,
        Unknown10           = 0x0010,
        Unknown20           = 0x0020,
        Jump                = 0x0040,
        ModeNonWalk         = 0x0080,
        ModeWalk            = 0x0100,
        ModeSwim            = 0x0200,
        ModeSlide           = 0x0400,
        DoubleJump          = 0x0800,
        RollForward         = 0x1000,
        RollBackward        = 0x2000,
        RotationWhileFalling = 0x40000
    }
}
