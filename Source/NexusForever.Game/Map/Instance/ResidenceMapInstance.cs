using System.Numerics;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Creature;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Housing;
using NexusForever.Game.Map;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Housing;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.IO.Map;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Abilities;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Static;
using NexusForever.Script;
using NLog;

namespace NexusForever.Game.Map.Instance
{
    public class ResidenceMapInstance : MapInstance, IResidenceMapInstance
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly Vector3 ResidencePlotWorldOrigin = new(1472f, 0f, 1440f);
        private const uint InteriorWallpaperDefaultDecorInfoId = 5u;
        private static readonly uint[] InteriorWallpaperSlotFlags = [0x01u, 0x04u, 0x08u, 0x10u, 0x20u, 0x80u];

        // housing maps have unlimited vision range.
        public override float? VisionRange { get; protected set; } = null;

        private readonly Dictionary<ulong, IResidence> residences = new();
        private readonly Dictionary<ulong, ulong> editModeResidences = new();

        #region Dependency Injection

        private readonly IEntityFactory entityFactory;
        private readonly IMapLockManager mapLockManager;
        private readonly IGlobalResidenceManager globalResidenceManager;
        private readonly IGameTableManager gameTableManager;
        private readonly IRealmContext realmContext;
        private readonly IScriptManager scriptManager;

        public ResidenceMapInstance(
            IEntityFactory entityFactory,
            IPublicEventManager publicEventManager,
            IMapLockManager mapLockManager,
            IGlobalResidenceManager globalResidenceManager,
            IGameTableManager gameTableManager,
            IRealmContext realmContext,
            IScriptManager scriptManager,
            ICreatureInfoManager creatureInfoManager = null)
            : base(entityFactory, publicEventManager, creatureInfoManager)
        {
            this.entityFactory          = entityFactory;
            this.mapLockManager         = mapLockManager;
            this.globalResidenceManager = globalResidenceManager;
            this.gameTableManager       = gameTableManager;
            this.realmContext           = realmContext;
            this.scriptManager          = scriptManager;
        }

        #endregion

        protected override void InitialiseScriptCollection()
        {
            scriptCollection = scriptManager.InitialiseOwnedScripts<IResidenceMapInstance>(this, Entry.Id);
        }

        /// <summary>
        /// Initialise <see cref="IResidenceMapInstance"/> with <see cref="IResidence"/>.
        /// </summary>
        public void Initialise(IResidence residence)
        {
            AddResidence(residence);
            foreach (IResidenceChild childResidence in residence.GetChildren())
                AddResidence(childResidence.Residence);
        }

        private void AddResidence(IResidence residence)
        {
            residences.Add(residence.Id, residence);
            residence.Map = this;

            foreach (IPlot plot in residence.GetPlots()
                .Where(p => p.PlugItemEntry != null))
                AddPlugEntity(plot);
        }

        private void AddPlugEntity(IPlot plot)
        {
            var plug = entityFactory.CreateEntity<IPlugEntity>();
            plug.Initialise(plot.PlotInfoEntry, plot.PlugItemEntry);
            plot.PlugEntity = plug;

            EnqueueAdd(plug, new MapPosition
            {
                Position = Vector3.Zero
            });
        }

        private void RemoveResidence(IResidence residence)
        {
            residences.Remove(residence.Id);
            ClearEditMode(residence);
            residence.Map = null;

            foreach (IPlot plot in residence.GetPlots()
                .Where(p => p.PlugItemEntry != null))
                plot?.PlugEntity.RemoveFromMap();
        }

        protected override IMapPosition GetPlayerReturnLocation(IPlayer player)
        {
            // if the residence is unloaded return player to their own residence
            IResidence returnResidence = globalResidenceManager.GetResidenceByOwner(player.Name);
            returnResidence ??= globalResidenceManager.CreateResidence(player);
            IResidenceEntrance entrance = globalResidenceManager.GetResidenceEntrance(returnResidence.PropertyInfoId);

            IMapLock mapLock = mapLockManager.GetResidenceLock(returnResidence);

            return new MapPosition
            {
                Info = new MapInfo
                {
                    Entry   = entrance.Entry,
                    MapLock = mapLock
                },
                Position = entrance.Position
            };
        }

        protected override void AddEntity(IGridEntity entity, Vector3 vector)
        {
            base.AddEntity(entity, vector);
            if (entity is not IPlayer player)
                return;

            SendResidences(player);
            SendResidencePlots(player);
            SendResidenceDecor(player);

            // this shows the housing toolbar, might need to move this to a more generic place in the future
            player.Session.EnqueueMessageEncrypted(new ServerActionBarSet
            {
                ShortcutSet            = ShortcutSet.FloatingSpellBar,
                ActionBarShortcutSetId = 1553,
                AssociatedUnitId       = player.Guid
            });
        }

        protected override void RemoveEntity(IGridEntity entity)
        {
            if (entity is IPlayer player)
                ClearEditMode(player);

            base.RemoveEntity(entity);
        }

        protected override void OnUnload()
        {
            foreach (IResidence residence in residences.Values.ToList())
                RemoveResidence(residence);
        }

        private void SendResidences(IPlayer player = null)
        {
            var housingProperties = new ServerHousingProperties();
            foreach (IResidence residence in residences.Values)
                housingProperties.Residences.Add(residence.Build());

            if (player != null)
                player.Session.EnqueueMessageEncrypted(housingProperties);
            else
                EnqueueToAll(housingProperties);
        }

        private void SendResidence(IResidence residence, IPlayer player = null)
        {
            var housingProperties = new ServerHousingProperties();
            housingProperties.Residences.Add(residence.Build());

            if (player != null)
                player.Session.EnqueueMessageEncrypted(housingProperties);
            else
                EnqueueToAll(housingProperties);
        }

        private void SendResidenceRemoved(IResidence residence, IPlayer player = null)
        {
            var housingProperties = new ServerHousingProperties();

            ServerHousingProperties.Residence residenceInfo = residence.Build();
            residenceInfo.ResidenceDeleted = true;
            housingProperties.Residences.Add(residenceInfo);

            if (player != null)
                player.Session.EnqueueMessageEncrypted(housingProperties);
            else
                EnqueueToAll(housingProperties);
        }

        private void SendResidencePlots(IPlayer player = null)
        {
            foreach (IResidence residence in residences.Values)
                SendResidencePlots(residence, player);
        }

        private void SendResidencePlots(IResidence residence, IPlayer player = null)
        {
            var housingPlots = new ServerHousingPlots
            {
                RealmId     = realmContext.RealmId,
                ResidenceId = residence.Id
            };

            foreach (IPlot plot in residence.GetPlots())
            {
                housingPlots.Plots.Add(new ServerHousingPlots.Plot
                {
                    PlotPropertyIndex = plot.Index,
                    PlotInfoId        = plot.PlotInfoEntry.Id,
                    PlugFacing        = plot.PlugFacing,
                    PlugItemId        = plot.PlugItemEntry?.Id ?? 0u,
                    BuildState        = plot.BuildState
                });
            }

            if (player != null)
                player.Session.EnqueueMessageEncrypted(housingPlots);
            else
                EnqueueToAll(housingPlots);
        }

        private void SendResidenceDecor(IPlayer player = null)
        {
            // a separate ServerHousingResidenceDecor has to be used for each residence
            // the client uses the residence id from the first decor it receives as the storage for the rest as well
            // no idea why it was implemented like this...
            foreach (IResidence residence in residences.Values)
                SendResidenceDecor(residence, player);
        }

        private void SendResidenceDecor(IResidence residence, IPlayer player = null)
        {
            var residenceDecor = new ServerHousingResidenceDecor
            {
                Operation = 0
            };

            IDecor[] decors = residence.GetDecor().ToArray();
            for (uint i = 0u; i < decors.Length; i++)
            {
                IDecor decor = decors[i];
                residenceDecor.DecorData.Add(decor.Build());

                // client freaks out if too much decor is sent in a single message, limit to 100
                if (i == decors.Length - 1 || i != 0u && i % 100u == 0u)
                {
                    if (player != null)
                        player.Session.EnqueueMessageEncrypted(residenceDecor);
                    else
                        EnqueueToAll(residenceDecor);

                    residenceDecor.DecorData.Clear();
                }
            }
        }

        /// <summary>
        /// Add child <see cref="IResidence"/> to parent <see cref="IResidence"/>.
        /// </summary>
        public void AddChild(IResidence residence, bool temporary)
        {
            IResidence community = residences.Values.SingleOrDefault(r => r.IsCommunityResidence);
            if (community == null)
                throw new InvalidOperationException("Can't add child residence to a map that isn't a community!");

            community.AddChild(residence, temporary);
            AddResidence(residence);

            SendResidence(residence);
            SendResidencePlots(residence);
            SendResidenceDecor(residence);
        }

        /// <summary>
        /// Remove child <see cref="IResidence"/> to parent <see cref="IResidence"/>.
        /// </summary>
        public void RemoveChild(IResidence residence)
        {
            IResidence community = residences.Values.SingleOrDefault(r => r.IsCommunityResidence);
            if (community == null)
                throw new InvalidOperationException("Can't remove child residence from a map that isn't a community!");

            community.RemoveChild(residence);
            RemoveResidence(residence);

            SendResidenceRemoved(residence);
        }

        /// <summary>
        /// Crate all placed <see cref="IDecor"/>.
        /// </summary>
        public void CrateAllDecor(TargetResidence targetResidence, IPlayer player)
        {
            if (!residences.TryGetValue(targetResidence.ResidenceId, out IResidence residence)
                || !residence.CanModifyResidence(player))
                throw new InvalidPacketValueException();

            var housingResidenceDecor = new ServerHousingResidenceDecor();
            foreach (IDecor decor in residence.GetPlacedDecor())
            {
                decor.Crate();
                housingResidenceDecor.DecorData.Add(decor.Build());
            }

            EnqueueToAll(housingResidenceDecor);
        }

        /// <summary>
        /// Handle <see cref="IDecor"/> update (create, move or delete).
        /// </summary>
        public void DecorUpdate(IPlayer player, ClientHousingDecorUpdate housingDecorUpdate)
        {
            foreach (DecorInfo update in housingDecorUpdate.DecorUpdates)
            {
                if (!residences.TryGetValue(update.TargetResidence.ResidenceId, out IResidence residence)
                    || !residence.CanModifyResidence(player))
                    throw new InvalidPacketValueException();

                switch (housingDecorUpdate.Operation)
                {
                    case DecorUpdateOperation.Create:
                        DecorCreate(residence, player, update);
                        break;
                    case DecorUpdateOperation.Move:
                        DecorMove(residence, player, update);
                        break;
                    case DecorUpdateOperation.Delete:
                        DecorDelete(residence, update);
                        break;
                    default:
                        throw new InvalidPacketValueException();
                }
            }
        }

        public void InteriorWallpaperUpdate(IPlayer player, ClientHousingInteriorWallpaperUpdate interiorWallpaperUpdate)
        {
            if (interiorWallpaperUpdate.ExistingDecorFlags.Count != ClientHousingInteriorWallpaperUpdate.SlotCount
                || interiorWallpaperUpdate.DecorUpdates.Count != ClientHousingInteriorWallpaperUpdate.SlotCount)
                throw new InvalidPacketValueException();

            var pendingUpdates = new List<(IResidence Residence, IDecor Decor, DecorInfo Update, HousingWallpaperInfoEntry Entry)>();
            var costs = new Dictionary<CurrencyType, ulong>();
            for (int i = 0; i < ClientHousingInteriorWallpaperUpdate.SlotCount; i++)
            {
                uint existingDecorFlag = interiorWallpaperUpdate.ExistingDecorFlags[i];
                if (existingDecorFlag > 1u)
                    throw new InvalidPacketValueException();

                DecorInfo update = interiorWallpaperUpdate.DecorUpdates[i];
                if (update.DecorInfoId == 0u && update.DecorId == 0ul)
                    continue;

                if (update.TargetResidence.RealmId != realmContext.RealmId
                    || !residences.TryGetValue(update.TargetResidence.ResidenceId, out IResidence residence)
                    || !residence.CanModifyResidence(player))
                    throw new InvalidPacketValueException();

                uint expectedHookIndex = (uint)i + 1u;
                if (update.DecorInfoId == 0u
                    || update.DecorType != DecorType.InteriorWallpaper
                    || update.HookIndex != expectedHookIndex)
                    throw new InvalidPacketValueException();

                HousingWallpaperInfoEntry entry = gameTableManager.HousingWallpaperInfo.GetEntry(update.DecorInfoId);
                if (entry == null)
                    throw new InvalidPacketValueException();
                if (!IsValidInteriorWallpaperSlot(entry, i))
                    throw new InvalidPacketValueException();

                if (HasUnsupportedWallpaperPrerequisites(entry))
                {
                    SendHousingResult(player, residence.Id, HousingResult.Decor_PrereqNotMet);
                    return;
                }

                IDecor decor = null;
                if (update.DecorId != 0ul)
                {
                    if (existingDecorFlag != 1u)
                        throw new InvalidPacketValueException();

                    decor = residence.GetDecor(update.DecorId);
                    if (decor == null)
                        throw new InvalidPacketValueException();
                }
                else if (existingDecorFlag != 0u)
                {
                    throw new InvalidPacketValueException();
                }

                AddInteriorWallpaperCost(costs, entry);
                pendingUpdates.Add((residence, decor, update, entry));
            }

            foreach ((CurrencyType currencyType, ulong cost) in costs)
            {
                if (!player.CurrencyManager.CanAfford(currencyType, cost))
                {
                    SendHousingResult(
                        player,
                        pendingUpdates.FirstOrDefault().Residence?.Id ?? 0ul,
                        HousingResult.Decor_CannotAfford);
                    return;
                }
            }

            foreach ((CurrencyType currencyType, ulong cost) in costs)
                player.CurrencyManager.CurrencySubtractAmount(currencyType, cost);

            var residenceDecor = new ServerHousingResidenceDecor();
            foreach ((IResidence residence, IDecor decorToUpdate, DecorInfo update, HousingWallpaperInfoEntry entry) in pendingUpdates)
            {
                IDecor decor = decorToUpdate;
                if (decor == null)
                    decor = residence.DecorCreateInteriorWallpaper(entry.Id);
                else
                    decor.UpdateDecorInfoId(entry.Id);

                ApplyDecorPacketState(decor, update);
                residenceDecor.DecorData.Add(decor.Build());
            }

            if (residenceDecor.DecorData.Count != 0)
                EnqueueToAll(residenceDecor);
        }

        private static bool IsValidInteriorWallpaperSlot(HousingWallpaperInfoEntry entry, int slotIndex)
        {
            return entry.Id == InteriorWallpaperDefaultDecorInfoId
                || (entry.Flags & InteriorWallpaperSlotFlags[slotIndex]) != 0u;
        }

        private static void AddInteriorWallpaperCost(Dictionary<CurrencyType, ulong> costs, HousingWallpaperInfoEntry entry)
        {
            if (entry.Id == InteriorWallpaperDefaultDecorInfoId)
                return;

            if (entry.CostCurrencyTypeId == 0u || entry.Cost == 0u)
                return;

            if (entry.CostCurrencyTypeId > int.MaxValue
                || !Enum.IsDefined(typeof(CurrencyType), (int)entry.CostCurrencyTypeId))
                throw new InvalidPacketValueException();

            CurrencyType currencyType = (CurrencyType)entry.CostCurrencyTypeId;
            if (costs.ContainsKey(currencyType))
                costs[currencyType] += entry.Cost;
            else
                costs.Add(currencyType, entry.Cost);
        }

        /// <summary>
        /// Handle plug placement, rotation, removal, or repair for a housing plot.
        /// </summary>
        public void PlugUpdate(IPlayer player, ClientHousingPlugUpdate housingPlugUpdate)
        {
            if (housingPlugUpdate.Identity.RealmId != realmContext.RealmId
                || !residences.TryGetValue(housingPlugUpdate.Identity.Id, out IResidence residence)
                || !residence.CanModifyResidence(player))
                throw new InvalidPacketValueException();

            if (housingPlugUpdate.Reserved != 0u)
                throw new InvalidPacketValueException();

            IPlot plot = residence.GetPlot(housingPlugUpdate.HousingPlotInfoId);
            if (plot == null)
                throw new InvalidPacketValueException();

            HousingResult result = housingPlugUpdate.Operation switch
            {
                ClientHousingPlugUpdate.PlugUpdateOperation.PlaceOrRotate => PlugPlaceOrRotate(player, plot, housingPlugUpdate),
                ClientHousingPlugUpdate.PlugUpdateOperation.Remove        => PlugRemove(plot),
                ClientHousingPlugUpdate.PlugUpdateOperation.Repair        => HousingResult.Plug_ModifyFailed,
                _                                                         => throw new InvalidPacketValueException()
            };

            if (result != HousingResult.Success)
            {
                player.Session.EnqueueMessageEncrypted(new ServerHousingResult
                {
                    RealmId     = realmContext.RealmId,
                    ResidenceId = residence.Id,
                    PlayerName  = player.Name,
                    Result      = result
                });
                return;
            }

            SendResidencePlots(residence);
        }

        private HousingResult PlugPlaceOrRotate(IPlayer player, IPlot plot, ClientHousingPlugUpdate housingPlugUpdate)
        {
            HousingPlugItemEntry entry = gameTableManager.HousingPlugItem.GetEntry(housingPlugUpdate.HousingPlugItemId);
            if (entry == null)
                return HousingResult.Plug_InvalidPlug;

            if (entry.Id > ushort.MaxValue)
                return HousingResult.Plug_InvalidPlug;

            if (entry.HousingPlotTypeId != 0u && entry.HousingPlotTypeId != plot.PlotInfoEntry.PlotType)
                return HousingResult.Plug_InvalidPlug;

            if (!Enum.IsDefined(typeof(HousingPlugFacing), housingPlugUpdate.PlugFacing))
                return HousingResult.Plug_CannotRotate;

            bool isRotation = plot.PlugItemEntry?.Id == entry.Id;
            if (!isRotation)
            {
                HousingResult placementResult = ValidateNewPlugPlacement(plot, entry, housingPlugUpdate);
                if (placementResult != HousingResult.Success)
                    return placementResult;
            }

            plot.PlugEntity?.RemoveFromMap();
            if (!isRotation)
                plot.SetPlug((ushort)entry.Id);

            plot.PlugFacing = housingPlugUpdate.PlugFacing;
            AddPlugEntity(plot);
            if (!isRotation)
                player.AchievementManager.CheckAchievements(player, AchievementType.HousingPlugPlace, 0u);

            return HousingResult.Success;
        }

        private static HousingResult ValidateNewPlugPlacement(IPlot plot, HousingPlugItemEntry entry, ClientHousingPlugUpdate housingPlugUpdate)
        {
            if (HasUnsupportedPlugPrerequisites(entry))
                return HousingResult.Plug_PrereqNotMet;

            if (HasUnsupportedPlugContributionCost(entry) || HasContributionPayload(housingPlugUpdate))
                return HousingResult.Plug_CannotAfford;

            if (HasUnsupportedPlugRuntime(entry))
                return HousingResult.Plug_ModifyFailed;

            if (entry.Id != plot.PlotInfoEntry.HousingPlugItemIdDefault)
                return HousingResult.Plug_InvalidPlug;

            return HousingResult.Success;
        }

        private static bool HasUnsupportedPlugPrerequisites(HousingPlugItemEntry entry)
        {
            return entry.HousingResourceIdPrerequisite00 != 0u
                || entry.HousingResourceIdPrerequisite01 != 0u
                || entry.HousingResourceIdPrerequisite02 != 0u
                || entry.PrerequisiteId00 != 0u
                || entry.PrerequisiteId01 != 0u
                || entry.PrerequisiteId02 != 0u
                || entry.PrerequisiteIdUnlock != 0u
                || entry.AccountItemIdUpsell != 0u;
        }

        private static bool HasUnsupportedPlugContributionCost(HousingPlugItemEntry entry)
        {
            return entry.HousingContributionInfoId00 != 0u
                || entry.HousingContributionInfoId01 != 0u
                || entry.HousingContributionInfoId02 != 0u
                || entry.HousingContributionInfoId03 != 0u
                || entry.HousingContributionInfoId04 != 0u
                || entry.HousingContributionInfoIdUpkeepCost00 != 0u
                || entry.HousingContributionInfoIdUpkeepCost01 != 0u
                || entry.HousingContributionInfoIdUpkeepCost02 != 0u
                || entry.HousingContributionInfoIdUpkeepCost03 != 0u
                || entry.HousingContributionInfoIdUpkeepCost04 != 0u;
        }

        private static bool HasUnsupportedPlugRuntime(HousingPlugItemEntry entry)
        {
            return entry.HousingBuildId != 0u
                || entry.HousingUpkeepTypeEnum != 0u
                || entry.UpkeepCharges != 0u
                || entry.UpkeepTime > 0f;
        }

        private static bool HasContributionPayload(ClientHousingPlugUpdate housingPlugUpdate)
        {
            return housingPlugUpdate.Contributions.Any(c => c.HasPayload);
        }

        public bool TryHarvestPlug(IPlayer harvester, IPlugEntity plugEntity)
        {
            if (plugEntity?.PlugEntry == null)
                return false;

            IResidence residence = residences.Values.FirstOrDefault(r =>
                r.GetPlots().Any(p => p.PlugEntity?.Guid == plugEntity.Guid));

            if (residence == null)
                return false;

            IPlot plot = residence.GetPlots().FirstOrDefault(p => p.PlugEntity?.Guid == plugEntity.Guid);
            if (plot == null)
                return false;

            return RetailHousingHarvestGrant.TryHarvestPlug(harvester, residence, plot, plugEntity.PlugEntry, gameTableManager);
        }

        public void SetEditMode(IPlayer player, IResidence residence, bool enabled)
        {
            if (player == null
                || residence == null
                || !residences.TryGetValue(residence.Id, out IResidence loadedResidence)
                || !loadedResidence.CanModifyResidence(player))
                throw new InvalidPacketValueException();

            if (enabled)
                editModeResidences[player.CharacterId] = loadedResidence.Id;
            else
                ClearEditMode(player);
        }

        public bool TryGetEditModeResidence(IPlayer player, out IResidence residence)
        {
            residence = null;
            if (player == null || !editModeResidences.TryGetValue(player.CharacterId, out ulong residenceId))
                return false;

            if (residences.TryGetValue(residenceId, out residence))
                return true;

            editModeResidences.Remove(player.CharacterId);
            return false;
        }

        public void ClearEditMode(IPlayer player)
        {
            if (player == null)
                return;

            editModeResidences.Remove(player.CharacterId);
        }

        private void ClearEditMode(IResidence residence)
        {
            foreach (ulong characterId in editModeResidences
                .Where(p => p.Value == residence.Id)
                .Select(p => p.Key)
                .ToList())
                editModeResidences.Remove(characterId);
        }

        private HousingResult PlugRemove(IPlot plot)
        {
            if (plot.PlugItemEntry == null)
                return HousingResult.Plug_NotActive;

            plot.PlugEntity?.RemoveFromMap();
            plot.PlugEntity    = null;
            plot.PlugItemEntry = null;
            plot.BuildState    = 0;
            return HousingResult.Success;
        }

        /// <summary>
        /// Create and add <see cref="IDecor"/> from supplied <see cref="HousingDecorInfoEntry"/> to your crate.
        /// </summary>
        public void DecorCreate(IResidence residence, HousingDecorInfoEntry entry, uint quantity)
        {
            var residenceDecor = new ServerHousingResidenceDecor();
            for (uint i = 0u; i < quantity; i++)
            {
                IDecor decor = residence.DecorCreate(entry);
                residenceDecor.DecorData.Add(decor.Build());
            }

            EnqueueToAll(residenceDecor);
        }

        private void ApplyDecorPacketState(IDecor decor, DecorInfo update)
        {
            if (update.Scale < 0f)
                throw new InvalidPacketValueException();

            decor.Type             = update.DecorType;
            decor.DecorData        = update.DecorData;
            decor.HookBagIndex     = update.HookBagIndex;
            decor.HookIndex        = update.HookIndex;
            decor.PlotIndex        = update.PlotIndex;
            decor.Position         = update.Position;
            decor.Rotation         = update.Rotation;
            decor.Scale            = update.Scale;
            decor.ActivePropUnitId = update.ActivePropUnitId;
            decor.DecorParentId    = update.ParentDecorId;

            SetDecorColourShift(decor, update.ColourShiftId);
        }

        private void SetDecorColourShift(IDecor decor, ushort colourShiftId)
        {
            if (colourShiftId == decor.ColourShiftId)
                return;

            ValidateDecorColourShift(colourShiftId);
            decor.ColourShiftId = colourShiftId;
        }

        private void ValidateDecorColourShift(ushort colourShiftId)
        {
            if (colourShiftId != 0u)
            {
                ColorShiftEntry colourEntry = gameTableManager.ColorShift.GetEntry(colourShiftId);
                if (colourEntry == null)
                    throw new InvalidPacketValueException();
            }
        }

        private void DecorCreate(IResidence residence, IPlayer player, DecorInfo update)
        {
            HousingDecorInfoEntry entry = gameTableManager.HousingDecorInfo.GetEntry(update.DecorInfoId);
            if (entry == null)
                throw new InvalidPacketValueException();

            if (HasUnsupportedDecorPrerequisites(entry))
            {
                SendHousingResult(player, residence.Id, HousingResult.Decor_PrereqNotMet);
                return;
            }

            CurrencyType? currencyType = null;
            if (entry.CostCurrencyTypeId != 0u && entry.Cost != 0u)
            {
                if (entry.CostCurrencyTypeId > int.MaxValue
                    || !Enum.IsDefined(typeof(CurrencyType), (int)entry.CostCurrencyTypeId))
                    throw new InvalidPacketValueException();

                currencyType = (CurrencyType)entry.CostCurrencyTypeId;
                if (!player.CurrencyManager.CanAfford(currencyType.Value, entry.Cost))
                {
                    SendHousingResult(player, residence.Id, HousingResult.Decor_CannotAfford);
                    return;
                }
            }

            HousingResult result = ValidateDecorCreate(residence, update);
            if (result != HousingResult.Success)
            {
                SendHousingResult(player, residence.Id, result);
                return;
            }

            IDecor decor = residence.DecorCreate(entry);
            if (currencyType.HasValue)
            {
                player.CurrencyManager.CurrencySubtractAmount(currencyType.Value, entry.Cost);
                player.AchievementManager.CheckAchievements(player, AchievementType.HousingDecorPurchase, 0u);
            }

            decor.Type = update.DecorType;
            decor.DecorData = update.DecorData;
            decor.HookBagIndex = update.HookBagIndex;
            decor.HookIndex = update.HookIndex;
            decor.PlotIndex = update.PlotIndex;
            decor.ActivePropUnitId = update.ActivePropUnitId;
            decor.DecorParentId = update.ParentDecorId;

            SetDecorColourShift(decor, update.ColourShiftId);

            if (update.DecorType != DecorType.Crate)
            {
                if (update.Scale < 0f)
                    throw new InvalidPacketValueException();

                // new decor is being placed directly in the world
                decor.Position = update.Position;
                decor.Rotation = update.Rotation;
                decor.Scale    = update.Scale;
            }

            EnqueueToAll(new ServerHousingResidenceDecor
            {
                Operation = 0,
                DecorData = new List<ServerHousingResidenceDecor.Decor>
                {
                     decor.Build()
                }
            });
        }

        private HousingResult ValidateDecorCreate(IResidence residence, DecorInfo update)
        {
            if (update.Scale < 0f)
                throw new InvalidPacketValueException();

            ValidateDecorColourShift(update.ColourShiftId);

            if (update.DecorType != DecorType.Crate
                && !IsValidPlotForPosition(residence, update))
                return HousingResult.Decor_InvalidPosition;

            return HousingResult.Success;
        }

        private void DecorMove(IResidence residence, IPlayer player, DecorInfo update)
        {
            IDecor decor = residence.GetDecor(update.DecorId);
            if (decor == null)
                throw new InvalidPacketValueException();

            HousingResult GetResult()
            {
                if (!IsValidPlotForPosition(residence, update))
                    return HousingResult.Decor_InvalidPosition;

                return HousingResult.Success;
            }

            HousingResult result = GetResult();
            if (result == HousingResult.Success)
            {
                if (update.Scale < 0f)
                    throw new InvalidPacketValueException();

                if (update.PlotIndex != decor.PlotIndex)
                {
                    decor.PlotIndex = update.PlotIndex;
                }

                decor.DecorData        = update.DecorData;
                decor.HookBagIndex     = update.HookBagIndex;
                decor.HookIndex        = update.HookIndex;
                decor.ActivePropUnitId = update.ActivePropUnitId;
                SetDecorColourShift(decor, update.ColourShiftId);

                if (decor.Type == DecorType.Crate)
                {
                    if (decor.Entry.Creature2IdActiveProp != 0u)
                    {
                        log.Debug($"Decor {decor.DecorId} uses active prop creature {decor.Entry.Creature2IdActiveProp}; active prop spawning is deferred until decor entity backing is available.");
                    }

                    // crate->world
                    decor.Move(update.DecorType, update.Position, update.Rotation, update.Scale, update.PlotIndex);
                }
                else
                {
                    if (update.DecorType == DecorType.Crate)
                        decor.Crate();
                    else
                    {
                        // world->world
                        decor.Move(update.DecorType, update.Position, update.Rotation, update.Scale, update.PlotIndex);
                        decor.DecorParentId = update.ParentDecorId;
                    }
                }
            }
            else
            {
                player.Session.EnqueueMessageEncrypted(new ServerHousingResult
                {
                    RealmId     = realmContext.RealmId,
                    ResidenceId = residence.Id,
                    PlayerName  = player.Name,
                    Result      = result
                });
            }

            EnqueueToAll(new ServerHousingResidenceDecor
            {
                Operation = 0,
                DecorData = new List<ServerHousingResidenceDecor.Decor>
                {
                    decor.Build()
                }
            });
        }

        private void DecorDelete(IResidence residence, DecorInfo update)
        {
            IDecor decor = residence.GetDecor(update.DecorId);
            if (decor == null)
                throw new InvalidPacketValueException();

            if (decor.Position != Vector3.Zero)
                throw new InvalidOperationException();

            DecorDelete(residence, decor);
        }

        /// <summary>
        /// Remove an existing <see cref="IDecor"/> from <see cref="IResidence"/>.
        /// </summary>
        public void DecorDelete(IResidence residence, IDecor decor)
        {
            if (decor.PendingCreate)
                residence.DecorRemove(decor);
            else
                decor.EnqueueDelete(true);

            var residenceDecor = new ServerHousingResidenceDecor();
            residenceDecor.DecorData.Add(new ServerHousingResidenceDecor.Decor
            {
                RealmId     = realmContext.RealmId,
                ResidenceId = residence.Id,
                DecorId     = decor.DecorId,
                DecorInfoId = 0
            });

            EnqueueToAll(residenceDecor);
        }

        /// <summary>
        /// Create a new <see cref="IDecor"/> from an existing <see cref="IDecor"/> for <see cref="IResidence"/>.
        /// </summary>
        /// <remarks>
        /// Copies all data from the source <see cref="IDecor"/> with a new id.
        /// </remarks>
        public IDecor DecorCopy(IResidence residence, IDecor decor)
        {
            IDecor newDecor = residence.DecorCopy(decor);

            var residenceDecor = new ServerHousingResidenceDecor();
            residenceDecor.DecorData.Add(newDecor.Build());
            EnqueueToAll(residenceDecor);
            return newDecor;
        }

        /// <summary>
        /// Used to confirm the position and PlotIndex are valid together when placing Decor
        /// </summary>
        private bool IsValidPlotForPosition(IResidence residence, DecorInfo update)
        {
            if (update.DecorType == DecorType.Crate || update.PlotIndex == int.MaxValue)
                return true;

            if (update.PlotIndex > byte.MaxValue)
                return false;

            IPlot plot = residence.GetPlot((byte)update.PlotIndex);
            if (plot?.PlotInfoEntry == null)
                return false;

            WorldSocketEntry worldSocketEntry = gameTableManager.WorldSocket.GetEntry(plot.PlotInfoEntry.WorldSocketId);
            if (worldSocketEntry?.BoundIds == null || worldSocketEntry.BoundIds.All(bound => bound == 0u))
                return true;

            Vector3 worldPosition = ResidencePlotWorldOrigin + update.Position;

            (uint gridX, uint gridZ) = MapGrid.GetGridCoord(worldPosition);
            (uint localCellX, uint localCellZ) = MapCell.GetCellCoord(worldPosition);
            (uint globalCellX, uint globalCellZ) = (gridX * MapDefines.GridCellCount + localCellX, gridZ * MapDefines.GridCellCount + localCellZ);

            uint maxBound = worldSocketEntry.BoundIds.Max() + 1u;
            uint minBound = worldSocketEntry.BoundIds
                .Where(bound => bound != 0u)
                .DefaultIfEmpty(1u)
                .Min() - 1u;

            log.Debug($"IsValidPlotForPosition - PlotIndex: {update.PlotIndex}, Range: {minBound}-{maxBound}, Coords: {globalCellX}, {globalCellZ}");

            return globalCellX >= minBound
                && globalCellX <= maxBound
                && globalCellZ >= minBound
                && globalCellZ <= maxBound;
        }

        private static bool HasUnsupportedDecorPrerequisites(HousingDecorInfoEntry entry)
        {
            return entry.PrerequisiteIdUnlock != 0u;
        }

        private static bool HasUnsupportedWallpaperPrerequisites(HousingWallpaperInfoEntry entry)
        {
            return entry.PrerequisiteIdUnlock != 0u
                || entry.PrerequisiteIdUse != 0u
                || entry.AccountItemIdUpsell != 0u;
        }

        private void SendHousingResult(IPlayer player, ulong residenceId, HousingResult result)
        {
            player.Session.EnqueueMessageEncrypted(new ServerHousingResult
            {
                RealmId     = realmContext.RealmId,
                ResidenceId = residenceId,
                PlayerName  = player.Name,
                Result      = result
            });
        }

        /// <summary>
        /// Rename <see cref="IResidence"/> with supplied name.
        /// </summary>
        public void RenameResidence(IPlayer player, TargetResidence targetResidence, string name)
        {
            if (!residences.TryGetValue(targetResidence.ResidenceId, out IResidence residence)
                || !residence.CanModifyResidence(player))
                throw new InvalidPacketValueException();

            RenameResidence(residence, name);
        }

        /// <summary>
        /// Rename <see cref="IResidence"/> with supplied name.
        /// </summary>
        public void RenameResidence(IResidence residence, string name)
        {
            residence.Name = name;
            SendResidence(residence);
        }

        /// <summary>
        /// Remodel <see cref="IResidence"/>.
        /// </summary>
        public void Remodel(TargetResidence targetResidence, IPlayer player, ClientHousingRemodel housingRemodel)
        {
            if (!residences.TryGetValue(targetResidence.ResidenceId, out IResidence residence)
                || !residence.CanModifyResidence(player))
                throw new InvalidPacketValueException();

            if (housingRemodel.RoofDecorInfoId != 0u)
                residence.Roof = (ushort)housingRemodel.RoofDecorInfoId;
            if (housingRemodel.WallpaperId != 0u)
                residence.Wallpaper = (ushort)housingRemodel.WallpaperId;
            if (housingRemodel.EntrywayDecorInfoId != 0u)
                residence.Entryway = (ushort)housingRemodel.EntrywayDecorInfoId;
            if (housingRemodel.DoorDecorInfoId != 0u)
                residence.Door = (ushort)housingRemodel.DoorDecorInfoId;
            if (housingRemodel.SkyWallpaperId != 0u)
                residence.Sky = (ushort)housingRemodel.SkyWallpaperId;
            if (housingRemodel.MusicId != 0u)
                residence.Music = (ushort)housingRemodel.MusicId;
            if (housingRemodel.GroundWallpaperId != 0u)
                residence.Ground = (ushort)housingRemodel.GroundWallpaperId;

            SendResidences();
        }

        /// <summary>
        /// UpdateResidenceFlags <see cref="IResidence"/>.
        /// </summary>
        public void UpdateResidenceFlags(TargetResidence targetResidence, IPlayer player, ClientHousingFlagsUpdate flagsUpdate)
        {
            if (!residences.TryGetValue(targetResidence.ResidenceId, out IResidence residence)
                || !residence.CanModifyResidence(player))
                throw new InvalidPacketValueException();

            residence.Flags           = flagsUpdate.Flags;
            residence.ResourceSharing = flagsUpdate.ResourceSharing;
            residence.GardenSharing   = flagsUpdate.GardenSharing;

            SendResidences();
        }
    }
}
