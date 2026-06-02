namespace NexusForever.Game.Static.Entity
{
    public enum DatacubeType
    {
        Datacube              = 0,
        Chronicle             = 1,
        Journal               = 2,
        /// <summary>
        /// NF persistence channel for <see cref="GameTable.Model.PathScientistCreatureInfoEntry"/> scan credit (not sent as client datacube UI).
        /// </summary>
        ScientistCreatureScan = 3,
    }
}
