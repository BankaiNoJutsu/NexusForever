using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Housing
{
    /// <summary>
    /// Emits housing harvest UI rows via <see cref="ServerHousingHarvestItemsSentToOwner"/>.
    /// </summary>
    public static class RetailHousingHarvestNotify
    {
        public static void NotifyGrants(IPlayer player, IEnumerable<(uint Item2Id, uint Count)> grants)
        {
            if (player?.Session == null)
                return;

            var packet = new ServerHousingHarvestItemsSentToOwner();
            foreach ((uint item2Id, uint count) in grants)
            {
                if (item2Id == 0u || count == 0u)
                    continue;

                packet.Items.Add(new ServerHousingHarvestItemsSentToOwner.HarvestItemRow
                {
                    Item2Id = item2Id,
                    Count   = count
                });
            }

            if (packet.Items.Count == 0)
                return;

            player.Session.EnqueueMessageEncrypted(packet);
        }
    }
}
