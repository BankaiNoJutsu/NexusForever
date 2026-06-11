using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model.Map;

namespace NexusForever.Game.Map
{
    public class ZoneMap : IZoneMap
    {
        /// <summary>
        /// Returns if all <see cref="MapZoneHexGroupEntry"/> in the <see cref="MapZoneEntry"/> have been discovered.
        /// </summary>
        public bool IsComplete => maxHexGroups != 0u && maxHexGroups == zoneMapHexGroups.Count;

        private readonly MapZoneEntry entry;
        
        private readonly ushort width;
        private readonly ushort height;
        private readonly ushort size;
        private readonly ushort maxHexGroups;
        private readonly uint maxHexCount;

        private readonly NetworkBitArray zoneMapBits;
        private readonly Dictionary<ushort /*ZoneMapHexGroupId*/, bool /*new*/> zoneMapHexGroups = new();

        private readonly IPlayer player;
        private readonly IGameTableManager gameTableManager;

        /// <summary>
        /// Create a new <see cref="IZoneMap"/> from supplied <see cref="MapZoneEntry"/>.
        /// </summary>
        public ZoneMap(MapZoneEntry mapZone, IPlayer owner, IGameTableManager gameTableManager = null)
        {
            player       = owner;
            this.gameTableManager = gameTableManager;

            entry        = mapZone;
            width        = (ushort)(mapZone.HexLimX - mapZone.HexMinX + 1);
            height       = (ushort)(mapZone.HexLimY - mapZone.HexMinY + 1);
            ushort wh    = (ushort)(width * height);
            size         = (ushort)((wh % 8u > 0u ? 8u : 0u) + wh);
            GameTable<MapZoneHexGroupEntry> hexGroupTable = this.gameTableManager?.MapZoneHexGroup;
            GameTable<MapZoneHexGroupEntryEntry> hexGroupEntryTable = this.gameTableManager?.MapZoneHexGroupEntry;

            maxHexGroups = (ushort)(hexGroupTable?.Entries.Count(m => m.MapZoneId == entry.Id) ?? 0);
            zoneMapBits  = new NetworkBitArray(size, NetworkBitArray.BitOrder.LeastSignificantBit);

            uint hexCount = 0;
            if (hexGroupTable != null && hexGroupEntryTable != null)
            {
                foreach (MapZoneHexGroupEntry groupEntry in hexGroupTable.Entries.Where(m => m.MapZoneId == entry.Id))
                    hexCount += (uint)hexGroupEntryTable.Entries.Count(m => m.MapZoneHexGroupId == groupEntry.Id);
            }

            maxHexCount = hexCount;
        }

        public void Save(CharacterContext context)
        {
            foreach ((ushort hexGroupId, bool _) in zoneMapHexGroups.Where(z => z.Value).ToList())
            {
                var model = new CharacterZonemapHexgroupModel
                {
                    Id       = player.CharacterId,
                    ZoneMap  = (ushort)entry.Id,
                    HexGroup = hexGroupId
                };

                context.Add(model);
                zoneMapHexGroups[hexGroupId] = false;
            }
        }

        /// <summary>
        /// Send <see cref="ServerZoneMap"/> to <see cref="IPlayer"/>.
        /// </summary>
        public void Send()
        {
            player.Session.EnqueueMessageEncrypted(new ServerZoneMap
            {
                ZoneMapId   = entry.Id,
                ZoneMapBits = zoneMapBits,
                Count       = size
            });
        }

        /// <summary>
        /// Add a new discovered <see cref="MapZoneHexGroupEntry"/>.
        /// </summary>
        public void AddHexGroup(ushort hexGroupId, bool sendUpdate = true)
        {
            GameTable<MapZoneHexGroupEntryEntry> hexGroupEntryTable = gameTableManager?.MapZoneHexGroupEntry;
            if (hexGroupEntryTable == null)
                return;

            foreach (MapZoneHexGroupEntryEntry mapZoneHexGroupEntry in hexGroupEntryTable.Entries.Where(m => m.MapZoneHexGroupId == hexGroupId))
            {
                var bit = (ushort)
                    (((short)mapZoneHexGroupEntry.HexY - (short)entry.HexMinY) * (short)width +
                    ((short)mapZoneHexGroupEntry.HexX - (short)entry.HexMinX));

                zoneMapBits.SetBit(bit, true);
            }

            zoneMapHexGroups.Add(hexGroupId, sendUpdate);
            if (sendUpdate)
                Send();
        }

        /// <summary>
        /// Returns if the supplied <see cref="MapZoneHexGroupEntry"/> has been discovered.
        /// </summary>
        public bool HasHexGroup(ushort hexGroupId)
        {
            return zoneMapHexGroups.ContainsKey(hexGroupId);
        }

        /// <summary>
        /// Returns the explored percentage of this <see cref="IZoneMap"/> explored.
        /// </summary>
        /// <remarks>
        /// This is here for testing purposes only. Confirmed that client % matched this % through the course of exploring multiple maps.
        /// </remarks>
        public float GetExploredPercent()
        {
            if (maxHexCount == 0u)
                return 0f;

            return (float)Math.Floor(((float)GetCurrentHexCount() / (float)maxHexCount) * 100f);
        }

        private int GetCurrentHexCount()
        {
            int count = 0;
            GameTable<MapZoneHexGroupEntry> hexGroupTable = gameTableManager?.MapZoneHexGroup;
            GameTable<MapZoneHexGroupEntryEntry> hexGroupEntryTable = gameTableManager?.MapZoneHexGroupEntry;
            if (hexGroupTable == null || hexGroupEntryTable == null)
                return count;

            foreach (MapZoneHexGroupEntry groupEntry in hexGroupTable.Entries.Where(m => m.MapZoneId == entry.Id))
            {
                if (!zoneMapHexGroups.ContainsKey((ushort)groupEntry.Id))
                    continue;

                count += hexGroupEntryTable.Entries.Count(m => m.MapZoneHexGroupId == groupEntry.Id);
            }

            return count;
        }
    }
}
