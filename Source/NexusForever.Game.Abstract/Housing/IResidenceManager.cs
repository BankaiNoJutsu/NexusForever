using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Housing;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Abstract.Housing
{
    public interface IResidenceManager
    {
        IResidence Residence { get; }

        /// <summary>
        /// Create new <see cref="IDecor"/> from supplied <see cref="HousingDecorInfoEntry"/> to residence your crate.
        /// </summary>
        /// <remarks>
        /// Decor will be created for your residence regardless of the current residence you are on.
        /// </remarks>
        void DecorCreate(HousingDecorInfoEntry entry, uint quantity = 1);

        /// <summary>
        /// Set the privacy level of your residence with the supplied <see cref="ResidencePrivacyLevel"/>.
        /// </summary>
        void SetResidencePrivacy(ResidencePrivacyLevel privacy);

        /// <summary>
        /// Returns the player's residence, creating it when required.
        /// </summary>
        IResidence GetOrCreateResidence();

        /// <summary>
        /// Returns the current pending neighbor invite, or null when none is active.
        /// </summary>
        ResidenceNeighborInviteInfo GetPendingNeighborInvite();

        /// <summary>
        /// Stores a pending neighbor invite if one is not already active.
        /// </summary>
        bool TryQueueNeighborInvite(ResidenceNeighborInviteInfo invite);

        /// <summary>
        /// Clears the current pending neighbor invite.
        /// </summary>
        void ClearPendingNeighborInvite();

        /// <summary>
        /// Send <see cref="ServerHousingBasics"/> message to <see cref="IPlayer"/>.
        /// </summary>
        void SendHousingBasics();

        /// <summary>
        /// Send <see cref="ServerHousingNeighbors"/> for the player's residence.
        /// </summary>
        void SendHousingNeighbors();
    }
}