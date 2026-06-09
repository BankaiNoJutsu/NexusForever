using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared;

namespace NexusForever.Game.Entity
{
    public class DatacubeManager : IDatacubeManager
    {
        private static uint DatacubeHash(ushort id, DatacubeType type)
        {
            return (uint)id << 16 | (uint)type;
        }

        private readonly IPlayer player;
        private readonly IGameTableManager gameTableManager;
        private readonly Dictionary<uint, IDatacube> datacubes = new();

        /// <summary>
        /// Create a new <see cref="IDatacubeManager"/> from an existing database model.
        /// </summary>
        public DatacubeManager(IPlayer owner, CharacterModel characterModel)
            : this(owner, characterModel, null)
        {
        }

        /// <summary>
        /// Create a new <see cref="IDatacubeManager"/> from an existing database model.
        /// </summary>
        public DatacubeManager(IPlayer owner, CharacterModel characterModel, IGameTableManager gameTableManager)
        {
            player                = owner;
            this.gameTableManager = gameTableManager;

            foreach (CharacterDatacubeModel model in characterModel.Datacube)
            {
                var datacube = new Datacube(player, model);
                uint hash = DatacubeHash(datacube.Id, datacube.Type);
                if (datacubes.TryGetValue(hash, out IDatacube existingDatacube))
                {
                    existingDatacube.Progress |= datacube.Progress;
                    continue;
                }

                datacubes.Add(hash, datacube);
            }
        }

        public void Save(CharacterContext context)
        {
            foreach (IDatacube datacube in datacubes.Values)
                datacube.Save(context);
        }

        /// <summary>
        /// Return <see cref="IDatacube"/> with supplied id and <see cref="DatacubeType"/>.
        /// </summary>
        public IDatacube GetDatacube(ushort id, DatacubeType type)
        {
            uint hash = DatacubeHash(id, type);
            return datacubes.TryGetValue(hash, out IDatacube datacube) ? datacube : null;
        }

        /// <summary>
        /// Create a new <see cref="IDatacube"/> of type <see cref="DatacubeType.Datacube"/> with supplied id and progress.
        /// </summary>
        public void AddDatacube(ushort id, uint progress)
        {
            if (ResolveDatacubeGameTableManager()?.Datacube?.GetEntry(id) == null)
                throw new ArgumentException();

            uint hash = DatacubeHash(id, DatacubeType.Datacube);
            if (datacubes.TryGetValue(hash, out IDatacube existingDatacube))
            {
                existingDatacube.Progress |= progress;
                SendDatacube(existingDatacube);
                return;
            }

            var datacube = new Datacube(player, id, DatacubeType.Datacube, progress);
            datacubes.Add(hash, datacube);

            SendDatacube(datacube);
        }

        /// <summary>
        /// Create a new <see cref="IDatacube"/> of type <see cref="DatacubeType.Journal"/> with supplied id and progress.
        /// </summary>
        public void AddDatacubeVolume(ushort id, uint progress)
        {
            if (ResolveDatacubeGameTableManager()?.DatacubeVolume?.GetEntry(id) == null)
                throw new ArgumentException();

            uint hash = DatacubeHash(id, DatacubeType.Journal);
            if (datacubes.TryGetValue(hash, out IDatacube existingDatacube))
            {
                existingDatacube.Progress |= progress;
                SendDatacubeVolume(existingDatacube);
                return;
            }

            var datacube = new Datacube(player, id, DatacubeType.Journal, progress);
            datacubes.Add(hash, datacube);

            SendDatacubeVolume(datacube);
        }

        public void AddScientistCreatureScan(ushort pathScientistCreatureInfoId)
        {
            if (pathScientistCreatureInfoId == 0u)
                return;

            uint completionMask = 1u;
            IGameTableManager resolvedGameTableManager = ResolveScientistScanGameTableManager();
            if (resolvedGameTableManager?.PathScientistCreatureInfo != null)
            {
                PathScientistCreatureInfoEntry entry = resolvedGameTableManager.PathScientistCreatureInfo.GetEntry(pathScientistCreatureInfoId);
                completionMask = PathScientistScanHelper.GetCompletionMask(entry);
            }

            uint hash = DatacubeHash(pathScientistCreatureInfoId, DatacubeType.ScientistCreatureScan);
            if (datacubes.TryGetValue(hash, out IDatacube existing))
            {
                existing.Progress |= completionMask;
                return;
            }

            datacubes.Add(hash, new Datacube(player, pathScientistCreatureInfoId, DatacubeType.ScientistCreatureScan, completionMask));
        }

        public bool HasScientistCreatureScan(ushort pathScientistCreatureInfoId)
        {
            if (pathScientistCreatureInfoId == 0u)
                return false;

            uint progress = GetScientistCreatureScanProgress(pathScientistCreatureInfoId);
            if (progress == 0u)
                return false;

            IGameTableManager resolvedGameTableManager = ResolveScientistScanGameTableManager();
            if (resolvedGameTableManager?.PathScientistCreatureInfo == null)
                return true;

            PathScientistCreatureInfoEntry entry = resolvedGameTableManager.PathScientistCreatureInfo.GetEntry(pathScientistCreatureInfoId);
            return PathScientistScanHelper.IsFullyScanned(progress, entry);
        }

        public uint GetScientistCreatureScanProgress(ushort pathScientistCreatureInfoId)
        {
            if (pathScientistCreatureInfoId == 0u)
                return 0u;

            uint hash = DatacubeHash(pathScientistCreatureInfoId, DatacubeType.ScientistCreatureScan);
            return datacubes.TryGetValue(hash, out IDatacube datacube) ? datacube.Progress : 0u;
        }

        public void SendInitialPackets()
        {
            var datacubeUpdateList = new ServerDatacubeUpdateList();
            foreach (IDatacube datacube in datacubes.Values)
            {
                if (datacube.Type == DatacubeType.ScientistCreatureScan)
                    continue;

                if (datacube.Type == DatacubeType.Datacube)
                    datacubeUpdateList.DatacubeData.Add(datacube.Build());
                else if (datacube.Type is DatacubeType.Chronicle or DatacubeType.Journal)
                    datacubeUpdateList.DatacubeVolumeData.Add(datacube.Build());
            }

            player.Session.EnqueueMessageEncrypted(datacubeUpdateList);
        }

        public void SendDatacube(IDatacube datacube)
        {
            player.Session.EnqueueMessageEncrypted(new ServerDatacubeUpdate
            {
                DatacubeData = datacube.Build()
            });
        }

        public void SendDatacubeVolume(IDatacube datacube)
        {
            player.Session.EnqueueMessageEncrypted(new ServerDatacubeVolumeUpdate
            {
                DatacubeVolumeData = datacube.Build()
            });
        }

        private IGameTableManager ResolveDatacubeGameTableManager()
        {
            return gameTableManager ?? GameTableManager.Instance;
        }

        private IGameTableManager ResolveScientistScanGameTableManager()
        {
            return gameTableManager ?? LegacyServiceProvider.Provider?.GetService<IGameTableManager>();
        }
    }
}
