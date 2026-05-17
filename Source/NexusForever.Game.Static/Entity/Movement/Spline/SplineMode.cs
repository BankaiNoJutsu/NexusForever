namespace NexusForever.Game.Static.Entity.Movement.Spline
{
    public enum SplineMode
    {
        OneShot,
        BackAndForth,
        Cyclic,
        OneShotReverse,
        BackAndForthReverse,
        CyclicReverse,

        // Modes 6-10 are valid four-bit client values but are not selected by the current server spline factory.
        SplineMode6,
        SplineMode7,
        SplineMode8,
        SplineMode9,
        SplineMode10,
    }
}
