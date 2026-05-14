namespace NexusForever.Game.Abstract.Entity.Creature
{
    public interface ICreatureInfoManager
    {
        void Initialise();

        /// <summary>
        /// Get the <see cref="ICreatureInfo"/> by creature id.
        /// </summary>
        ICreatureInfo GetCreatureInfo<T>(T creatureId) where T : Enum;

        /// <summary>
        /// Get the <see cref="ICreatureInfo"/> by creature id.
        /// </summary>
        ICreatureInfo GetCreatureInfo(uint creatureId);
    }
}
