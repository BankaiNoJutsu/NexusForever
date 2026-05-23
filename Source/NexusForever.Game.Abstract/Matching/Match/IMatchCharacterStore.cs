using NexusForever.Shared;

namespace NexusForever.Game.Abstract.Matching.Match
{
    public interface IMatchCharacterStore
    {
        /// <summary>
        /// Return <see cref="IMatchCharacter"/> for supplied character id.
        /// </summary>
        /// <remarks>
        /// Will return a new <see cref="IMatchCharacter"/> if one does not exist.
        /// </remarks>
        IMatchCharacter GetMatchCharacter(Identity identity);
    }
}