namespace NexusForever.Game.Abstract.Entity
{
    // Game-owned publication can validate its original player without depending
    // on the WorldServer's request handler or concrete session implementation.
    public interface IPlayerSession
    {
        IPlayer Player { get; }
    }
}
