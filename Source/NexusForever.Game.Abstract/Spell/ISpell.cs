using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared;

namespace NexusForever.Game.Abstract.Spell
{
    public interface ISpell : IDisposable, IUpdate
    {
        ISpellParameters Parameters { get; }
        uint CastingId { get; }
        bool IsCasting { get; }
        bool IsFinished { get; }

        IUnitEntity Caster { get; }

        /// <summary>
        /// Begin cast, checking prerequisites before initiating.
        /// </summary>
        CastResult Cast();

        /// <summary>
        /// Cancel cast with supplied <see cref="CastResult"/>.
        /// </summary>
        void CancelCast(CastResult result);

        /// <summary>
        /// Attempt to cancel a client-cancelable active effect owned by this spell for the supplied requester.
        /// </summary>
        bool TryCancelEffect(IUnitEntity requester);

        bool IsMovingInterrupted();
    }
}