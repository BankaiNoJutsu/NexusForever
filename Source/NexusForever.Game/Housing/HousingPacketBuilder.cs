using NexusForever.Game.Abstract.Housing;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Housing
{
    public static class HousingPacketBuilder
    {
        public static ServerHousingRandomCommunityList BuildRandomCommunityList(IGlobalResidenceManager manager, ushort realmId)
        {
            var packet = new ServerHousingRandomCommunityList();
            foreach (IPublicCommunity community in manager.GetRandomVisitableCommunities())
                packet.Communities.Add(new ServerHousingRandomCommunityList.Community
                {
                    RealmId = realmId,
                    NeighborhoodId = community.NeighbourhoodId,
                    Owner = community.Owner,
                    Name = community.Name
                });
            return packet;
        }
    }
}
