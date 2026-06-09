using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Challenges;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Reputation;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Map
{
    internal static class ZoneCompletionProgressTracker
    {
        private const uint DatacubeTypeEnumDatacube = 0u;
        private const uint DatacubeTypeEnumTale = 1u;

        public static ZoneCompletionProgress GetProgress(IPlayer player, uint mapZoneId)
        {
            HashSet<uint> worldZoneIds = GetWorldZoneIdsForMapZone(mapZoneId);
            if (worldZoneIds.Count == 0)
                return default;

            HashSet<uint> episodeQuestIds = GetEpisodeQuestIds();

            uint episodeQuests = 0u;
            uint taskQuests = 0u;
            GameTable<Quest2Entry> questTable = GameTableManager.Instance.Quest2;
            if (questTable != null)
            {
                foreach (Quest2Entry quest in questTable.Entries)
                {
                    if (!worldZoneIds.Contains(quest.WorldZoneId))
                        continue;
                    if (!QuestMatchesPlayerFaction(quest, player.Faction1))
                        continue;

                    QuestState? state = player.QuestManager.GetQuestState((ushort)quest.Id);
                    if (state != QuestState.Completed)
                        continue;

                    if (episodeQuestIds.Contains(quest.Id))
                        episodeQuests++;
                    else
                        taskQuests++;
                }
            }

            return CountDatacubesChallengesAndJournals(player, worldZoneIds, episodeQuests, taskQuests);
        }

        private static ZoneCompletionProgress CountDatacubesChallengesAndJournals(
            IPlayer player,
            HashSet<uint> worldZoneIds,
            uint episodeQuests,
            uint taskQuests)
        {
            uint datacubes = 0u;
            uint tales = 0u;
            GameTable<DatacubeEntry> datacubeTable = GameTableManager.Instance.Datacube;
            if (datacubeTable != null)
            {
                foreach (DatacubeEntry entry in datacubeTable.Entries)
                {
                    if (!worldZoneIds.Contains(entry.WorldZoneId))
                        continue;
                    if (!DatacubeMatchesPlayerFaction(entry, player.Faction1))
                        continue;

                    if (entry.DatacubeTypeEnum == DatacubeTypeEnumDatacube)
                    {
                        if (player.DatacubeManager.GetDatacube((ushort)entry.Id, DatacubeType.Datacube) != null)
                            datacubes++;
                    }
                    else if (entry.DatacubeTypeEnum == DatacubeTypeEnumTale)
                    {
                        if (player.DatacubeManager.GetDatacube((ushort)entry.Id, DatacubeType.Chronicle) != null)
                            tales++;
                    }
                }
            }

            uint journals = 0u;
            GameTable<DatacubeVolumeEntry> volumeTable = GameTableManager.Instance.DatacubeVolume;
            if (volumeTable != null && datacubeTable != null)
            {
                foreach (DatacubeVolumeEntry volume in volumeTable.Entries)
                {
                    if (player.DatacubeManager.GetDatacube((ushort)volume.Id, DatacubeType.Journal) == null)
                        continue;

                    if (VolumeTouchesWorldZones(volume, worldZoneIds, datacubeTable))
                        journals++;
                }
            }

            uint challenges = 0u;
            if (player.ChallengeManager is ChallengeManager challengeManager)
                challenges = challengeManager.GetCompletedCountForWorldZones(worldZoneIds);

            return new ZoneCompletionProgress
            {
                EpisodeQuestCount = episodeQuests,
                TaskQuestCount    = taskQuests,
                ChallengeCount    = challenges,
                DatacubeCount     = datacubes,
                TaleCount         = tales,
                JournalCount      = journals,
            };
        }

        private static bool QuestMatchesPlayerFaction(Quest2Entry quest, Faction playerFaction)
        {
            return quest.QuestPlayerFactionEnum switch
            {
                0u => playerFaction == Faction.Exile,
                1u => playerFaction == Faction.Dominion,
                _  => true,
            };
        }

        private static bool DatacubeMatchesPlayerFaction(DatacubeEntry entry, Faction playerFaction)
        {
            return entry.DatacubeFactionEnum switch
            {
                0u => playerFaction == Faction.Exile,
                1u => playerFaction == Faction.Dominion,
                _  => true,
            };
        }

        private static HashSet<uint> GetEpisodeQuestIds()
        {
            return GameTableManager.Instance.EpisodeQuest?.Entries
                .Select(e => e.QuestId)
                .ToHashSet() ?? new HashSet<uint>();
        }

        private static HashSet<uint> GetWorldZoneIdsForMapZone(uint mapZoneId)
        {
            GameTable<MapZoneEntry> mapZoneTable = GameTableManager.Instance.MapZone;
            MapZoneEntry mapZone = mapZoneTable?.Entries?
                .FirstOrDefault(m => m.Id == mapZoneId);
            if (mapZone == null)
                return new HashSet<uint>();

            var worldZoneIds = new HashSet<uint>();
            if (mapZone.WorldZoneId != 0u)
            {
                GameTable<WorldZoneEntry> worldZoneTable = GameTableManager.Instance.WorldZone;
                if (worldZoneTable != null)
                {
                    foreach (WorldZoneEntry zone in worldZoneTable.Entries)
                    {
                        if (IsWorldZoneInTree(worldZoneTable, zone.Id, mapZone.WorldZoneId))
                            worldZoneIds.Add(zone.Id);
                    }
                }
            }

            return worldZoneIds;
        }

        private static bool IsWorldZoneInTree(GameTable<WorldZoneEntry> worldZoneTable, uint zoneId, uint rootZoneId)
        {
            var visited = new HashSet<uint>();
            WorldZoneEntry zone = worldZoneTable.GetEntry(zoneId);
            while (zone != null && visited.Add(zone.Id))
            {
                if (zone.Id == rootZoneId)
                    return true;
                if (zone.ParentZoneId == 0u)
                    return false;

                zone = worldZoneTable.GetEntry(zone.ParentZoneId);
            }

            return false;
        }

        private static bool VolumeTouchesWorldZones(
            DatacubeVolumeEntry volume,
            HashSet<uint> worldZoneIds,
            GameTable<DatacubeEntry> datacubeTable)
        {
            foreach (uint datacubeId in GetVolumeDatacubeIds(volume))
            {
                if (datacubeId == 0u)
                    continue;

                DatacubeEntry datacube = datacubeTable.GetEntry(datacubeId);
                if (datacube != null && worldZoneIds.Contains(datacube.WorldZoneId))
                    return true;
            }

            return false;
        }

        private static IEnumerable<uint> GetVolumeDatacubeIds(DatacubeVolumeEntry volume)
        {
            yield return volume.DatacubeId00;
            yield return volume.DatacubeId01;
            yield return volume.DatacubeId02;
            yield return volume.DatacubeId03;
            yield return volume.DatacubeId04;
            yield return volume.DatacubeId05;
            yield return volume.DatacubeId06;
            yield return volume.DatacubeId07;
            yield return volume.DatacubeId08;
            yield return volume.DatacubeId09;
            yield return volume.DatacubeId10;
            yield return volume.DatacubeId11;
            yield return volume.DatacubeId12;
            yield return volume.DatacubeId13;
            yield return volume.DatacubeId14;
            yield return volume.DatacubeId15;
        }
    }
}
