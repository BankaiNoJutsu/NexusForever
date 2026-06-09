using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Housing
{
    public class ClientHousingVendorListHandler : IMessageHandler<IWorldSession, ClientHousingVendorList>
    {
        #region Dependency Injection

        private readonly IGameTableManager gameTableManager;

        public ClientHousingVendorListHandler(
            IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientHousingVendorList _)
        {
            var serverHousingVendorList = new ServerHousingVendorList
            {
                ListType = 0
            };

            foreach (HousingPlugItemEntry entry in gameTableManager.HousingPlugItem?.Entries ?? [])
            {
                serverHousingVendorList.PlugItems.Add(new ServerHousingVendorList.PlugItem
                {
                    PlugItemId     = entry.Id,
                    Cost           = GetContributionCost(entry),
                    PlugItemFlags  = entry.Flags
                });
            }

            session.EnqueueMessageEncrypted(serverHousingVendorList);
        }

        private uint GetContributionCost(HousingPlugItemEntry entry)
        {
            uint[] contributionIds =
            [
                entry.HousingContributionInfoId00,
                entry.HousingContributionInfoId01,
                entry.HousingContributionInfoId02,
                entry.HousingContributionInfoId03,
                entry.HousingContributionInfoId04
            ];

            foreach (uint contributionId in contributionIds)
            {
                HousingContributionInfoEntry contribution = gameTableManager.HousingContributionInfo?.GetEntry(contributionId);
                if (contribution?.ContributionPointRequirement > 0u)
                    return contribution.ContributionPointRequirement;
            }

            return 0u;
        }
    }
}
