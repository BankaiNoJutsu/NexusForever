using System.Collections.Generic;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Housing;
using NexusForever.Game.Static.Guild;
using NexusForever.Game.Static.Housing;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Housing
{
    public class ClientHousingCommunityDonateHandler : IMessageHandler<IWorldSession, ClientHousingCommunityDonate>
    {
        private const uint HousingDecorInfoCannotDonateFlag = 0x8u;

        public void HandleMessage(IWorldSession session, ClientHousingCommunityDonate housingCommunityDonate)
        {
            // can only donate to a community from a residence map
            if (session.Player.Map is not IResidenceMapInstance)
                throw new InvalidPacketValueException();

            IResidence residence = session.Player.ResidenceManager.Residence;
            if (residence == null)
                throw new InvalidPacketValueException();

            ICommunity community = session.Player.GuildManager.GetGuild<ICommunity>(GuildType.Community);
            if (community?.Residence == null)
                throw new InvalidPacketValueException();

            var decorToDonate = new List<ulong>();
            var seen = new HashSet<ulong>();

            foreach (DecorInfo decorInfo in housingCommunityDonate.Decor)
            {
                IDecor decor = residence.GetDecor(decorInfo.DecorId);
                if (decor == null || decor.PendingDelete || !seen.Add(decorInfo.DecorId))
                    throw new InvalidPacketValueException();

                if (decor.Type != DecorType.Crate)
                    throw new InvalidPacketValueException();

                if (HasCannotDonateFlag(decor))
                {
                    SendHousingResult(session, residence, HousingResult.Decor_CannotDonate);
                    return;
                }
                decorToDonate.Add(decorInfo.DecorId);
            }

            if (decorToDonate.Count == 0) return;
            if (residence is not Residence donor) throw new InvalidPacketValueException();
            donor.Donate(session.Player, community, decorToDonate);
        }

        private static bool HasCannotDonateFlag(IDecor decor)
        {
            return decor.Entry == null || (decor.Entry.Flags & HousingDecorInfoCannotDonateFlag) != 0u;
        }

        private static void SendHousingResult(IWorldSession session, IResidence residence, HousingResult result)
        {
            session.EnqueueMessageEncrypted(new ServerHousingResult
            {
                RealmId     = session.Player.Identity.RealmId,
                ResidenceId = residence.Id,
                PlayerName  = session.Player.Name ?? string.Empty,
                Result      = result
            });
        }
    }
}
