using System;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Character;
using NexusForever.Game.Static.Housing;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Shared;

namespace NexusForever.Game.Housing
{
    public class ResidenceManager : IResidenceManager
    {
        private const double DefaultNeighborInviteExpirySeconds = 300d;

        public IResidence Residence { get; private set; }

        private readonly IPlayer owner;
        private ResidenceNeighborInviteInfo pendingNeighborInvite;

        /// <summary>
        /// Create a new <see cref="IResidenceManager"/>.
        /// </summary>
        public ResidenceManager(IPlayer player)
        {
            owner     = player;
            Residence = GlobalResidenceManager.Instance.GetResidenceByOwner(owner.CharacterId);
        }

        /// <summary>
        /// Create new <see cref="IDecor"/> from supplied <see cref="HousingDecorInfoEntry"/> to residence your crate.
        /// </summary>
        /// <remarks>
        /// Decor will be created for your residence regardless of the current residence you are on.
        /// </remarks>
        public void DecorCreate(HousingDecorInfoEntry entry, uint quantity = 1u)
        {
            GetOrCreateResidence();

            if (Residence.Map != null)
                Residence.Map.DecorCreate(Residence, entry, quantity);
            else
            {
                for (uint i = 0u; i < quantity; i++)
                    Residence.DecorCreate(entry);
            }
        }

        /// <summary>
        /// Set the privacy level of your residence with the supplied <see cref="ResidencePrivacyLevel"/>.
        /// </summary>
        public void SetResidencePrivacy(ResidencePrivacyLevel privacy)
        {
            if (Residence == null)
                throw new InvalidOperationException();

            Residence.PrivacyLevel = privacy;
            SendHousingBasics();
        }

        public IResidence GetOrCreateResidence()
        {
            Residence ??= GlobalResidenceManager.Instance.CreateResidence(owner);
            return Residence;
        }

        public ResidenceNeighborInviteInfo GetPendingNeighborInvite()
        {
            ClearExpiredNeighborInvite();
            return pendingNeighborInvite;
        }

        public bool TryQueueNeighborInvite(ResidenceNeighborInviteInfo invite)
        {
            if (invite == null)
                throw new ArgumentNullException(nameof(invite));

            ClearExpiredNeighborInvite();
            if (pendingNeighborInvite != null)
                return false;

            pendingNeighborInvite = new ResidenceNeighborInviteInfo
            {
                InviterCharacterId = invite.InviterCharacterId,
                InviterResidenceId = invite.InviterResidenceId,
                InviterName        = invite.InviterName ?? string.Empty,
                ExpiresAt          = invite.ExpiresAt == default
                    ? DateTime.UtcNow.AddSeconds(DefaultNeighborInviteExpirySeconds)
                    : invite.ExpiresAt
            };

            return true;
        }

        public void ClearPendingNeighborInvite()
        {
            pendingNeighborInvite = null;
        }

        private void ClearExpiredNeighborInvite()
        {
            if (pendingNeighborInvite == null || pendingNeighborInvite.ExpiresAt > DateTime.UtcNow)
                return;

            pendingNeighborInvite = null;
        }

        /// <summary>
        /// Send <see cref="ServerHousingBasics"/> message to <see cref="IPlayer"/>.
        /// </summary>
        public void SendHousingBasics()
        {
            // not really sure why the client converts the privacy level to and from flags
            // see Residence.GetResidencePrivacyLevel LUA function for more context
            var flags = (Residence?.PrivacyLevel ?? ResidencePrivacyLevel.Public) switch
            {
                ResidencePrivacyLevel.Public        => ServerHousingBasics.ResidencePrivacyLevelFlags.Public,
                ResidencePrivacyLevel.Private       => ServerHousingBasics.ResidencePrivacyLevelFlags.Private,
                ResidencePrivacyLevel.NeighborsOnly => ServerHousingBasics.ResidencePrivacyLevelFlags.NeighborsOnly,
                ResidencePrivacyLevel.RoommatesOnly => ServerHousingBasics.ResidencePrivacyLevelFlags.RoommatesOnly,
                _                                   => throw new InvalidOperationException($"Unsupported residence privacy level {Residence?.PrivacyLevel}.")
            };

            owner.Session.EnqueueMessageEncrypted(new ServerHousingBasics
            {
                ResidenceId     = Residence?.Id ?? 0ul,
                /*NeighbourhoodId = GuildManager.GetGuild<Community>(GuildType.Community)?.Id ?? 0ul,*/
                PrivacyLevel    = flags
            });
        }

        /// <summary>
        /// Send <see cref="ServerHousingNeighbors"/> for the owner's residence neighbor list.
        /// </summary>
        public void SendHousingNeighbors()
        {
            var packet = new ServerHousingNeighbors();
            if (Residence == null)
            {
                owner.Session.EnqueueMessageEncrypted(packet);
                return;
            }

            ushort realmId = RealmContext.Instance.RealmId;
            foreach ((ulong characterId, byte permissionLevel) in Residence.GetNeighbors())
            {
                ICharacter character = CharacterManager.Instance.GetCharacter(characterId);
                IResidence neighborResidence = GlobalResidenceManager.Instance.GetResidenceByOwner(characterId);
                if (character == null || neighborResidence == null)
                    continue;

                packet.Neighbors.Add(new ServerHousingNeighbors.Neighbor
                {
                    CharacterId = characterId,
                    Permission = permissionLevel
                });

                packet.Neighbors[^1].TargetResidence.RealmId     = realmId;
                packet.Neighbors[^1].TargetResidence.ResidenceId = neighborResidence.Id;
            }

            owner.Session.EnqueueMessageEncrypted(packet);
        }
    }
}
