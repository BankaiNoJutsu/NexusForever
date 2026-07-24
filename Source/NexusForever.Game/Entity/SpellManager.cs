using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Abilities;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Static;
using NLog;

namespace NexusForever.Game.Entity
{
    public class SpellManager : ISpellManager
    {
        private const ushort MaxBonusAmpPower = 10;
        private const byte GlobalCooldownType = 0;
        private const byte SpellCooldownType = 1;
        private static readonly UILocation[] AutoSlotLasLocations =
        [
            UILocation.LAS1,
            UILocation.LAS2,
            UILocation.LAS3,
            UILocation.LAS4,
            UILocation.LAS5,
            UILocation.LAS6,
            UILocation.LAS7,
            UILocation.LAS8
        ];

        /// <summary>
        /// Determines which fields need saving for <see cref="ISpellManager"/> when being saved to the database.
        /// </summary>
        [Flags]
        public enum SpellManagerSaveMask
        {
            None                   = 0x0000,
            ActiveActionSet        = 0x0001,
            BonusAbilityTierPoints = 0x0002
        }

        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Index of the active <see cref="IActionSet"/>.
        /// </summary>
        public byte ActiveActionSet
        {
            get => activeActionSet;
            private set
            {
                saveMask |= SpellManagerSaveMask.ActiveActionSet;
                activeActionSet = value;
            }
        }

        private byte activeActionSet;

        private readonly IPlayer player;
        private readonly IPrerequisiteManager prerequisiteManager;
        private readonly IGlobalSpellManager globalSpellManager;
        private readonly IGameTableManager gameTableManager;

        private readonly Dictionary<uint /*spell4BaseId*/, ICharacterSpell> spells = new();
        private readonly Dictionary<CooldownKey, ActiveCooldown> activeCooldowns = new();
        private readonly HashSet<uint> activeFloatingActionBarSpell4Ids = new();
        private readonly HashSet<uint> activeFloatingActionBarOwnerSpellGroupIds = new();
        private readonly Dictionary<uint, uint> activePetActionSpell4Ids = new();
        private readonly HashSet<uint> activePetActionOwnerSpellGroupIds = new();
        private readonly Dictionary<uint, HashSet<uint>> activePetActionOwnerSpellGroupIdsBySpell4Id = new();
        private ushort bonusAmpPower;
        private byte bonusAbilityTierPoints;

        private readonly IActionSet[] actionSets = new ActionSet[ActionSet.MaxActionSets];

        private SpellManagerSaveMask saveMask;

        private readonly record struct CooldownKey(byte Type, uint TypeId);
        private readonly record struct CooldownIdentity(uint TypeId, bool IsCooldownNode);

        private sealed class ActiveCooldown
        {
            public byte Type { get; init; }
            public uint Spell4Id { get; init; }
            public uint TypeId { get; init; }
            public bool TypeIdIsCooldownNode { get; init; }
            public double TimeRemaining { get; set; }
        }

        /// <summary>
        /// Create a new <see cref="ISpellManager"/> from existing <see cref="CharacterModel"/> database model.
        /// </summary>
        public SpellManager(
            IPlayer owner,
            CharacterModel model,
            IPrerequisiteManager prerequisiteManager = null,
            IGlobalSpellManager globalSpellManager = null,
            IGameTableManager gameTableManager = null)
        {
            player = owner;
            this.prerequisiteManager = prerequisiteManager;
            this.globalSpellManager = globalSpellManager;
            this.gameTableManager = gameTableManager;

            foreach (CharacterSpellModel spellModel in model.Spell)
            {
                ISpellBaseInfo spellBaseInfo = GetGlobalSpellManager().GetSpellBaseInfo(spellModel.Spell4BaseId);
                IItem item = player.Inventory.SpellCreate(spellBaseInfo.Entry, ItemUpdateReason.NoReason);
                spells.Add(spellModel.Spell4BaseId, new CharacterSpell(owner, spellModel, spellBaseInfo, item, prerequisiteManager, globalSpellManager, gameTableManager));
            }

            GrantSpells();

            bonusAbilityTierPoints = (byte)Math.Min(model.BonusAbilityTierPoints, ActionSet.MaxBonusTierPoints);

            for (byte i = 0; i < ActionSet.MaxActionSets; i++)
            {
                actionSets[i] = new ActionSet(i, player, gameTableManager, bonusAbilityTierPoints);

                foreach (CharacterActionSetShortcutModel shortcutModel in model.ActionSetShortcut
                    .Where(c => c.SpecIndex == i))
                    actionSets[i].AddShortcut(shortcutModel);

                foreach (CharacterActionSetAmpModel ampModel in model.ActionSetAmp
                    .Where(c => c.SpecIndex == i))
                    actionSets[i].AddAmp(ampModel);
            }

            activeActionSet = model.ActiveSpec;
        }

        public void GrantSpells()
        {
            IGameTableManager gameTables = GetGameTableManager();
            bool actionSetUpdated = false;
            foreach (SpellLevelEntry spellLevel in gameTables.SpellLevel.Entries
                .Where(s => s.ClassId == (byte)player.Class && s.CharacterLevel <= player.Level)
                .OrderBy(s => s.CharacterLevel))
            {
                if (spellLevel.PrerequisiteId > 0 && !GetPrerequisiteManager().Meets(player, spellLevel.PrerequisiteId))
                    continue;

                Spell4Entry spell4Entry = gameTables.Spell4.GetEntry(spellLevel.Spell4Id);
                if (spell4Entry == null)
                    continue;

                if (GetSpell(spell4Entry.Spell4BaseIdBaseSpell) == null)
                {
                    AddSpell(spell4Entry.Spell4BaseIdBaseSpell);
                    actionSetUpdated |= TryAddSpellToFirstEmptyLasSlot(spell4Entry.Spell4BaseIdBaseSpell);
                }
            }

            if (actionSetUpdated)
                SendActiveActionSet();

            ClassEntry classEntry = gameTables.Class.GetEntry((byte)player.Class);
            foreach (uint classSpell in classEntry.Spell4IdInnateAbilityActive
                .Concat(classEntry.Spell4IdInnateAbilityPassive)
                .Concat(classEntry.Spell4IdAttackPrimary)
                .Concat(classEntry.Spell4IdAttackUnarmed))
            {
                Spell4Entry spell4Entry = gameTables.Spell4.GetEntry(classSpell);
                if (spell4Entry == null)
                    continue;

                if (GetSpell(spell4Entry.Spell4BaseIdBaseSpell) == null)
                    AddSpell(spell4Entry.Spell4BaseIdBaseSpell);
            }
        }

        private bool TryAddSpellToFirstEmptyLasSlot(uint spell4BaseId)
        {
            if (player.IsLoading)
                return false;

            if (ActiveActionSet >= ActionSet.MaxActionSets)
                return false;

            IActionSet actionSet = actionSets[ActiveActionSet];
            if (actionSet == null || actionSet.GetShortcut(ShortcutType.SpellbookItem, spell4BaseId) != null)
                return false;

            foreach (UILocation location in AutoSlotLasLocations)
            {
                if (actionSet.GetShortcut(location) != null || !IsLasSlotUnlocked(location))
                    continue;

                actionSet.AddShortcut(location, ShortcutType.SpellbookItem, spell4BaseId, 1);
                return true;
            }

            return false;
        }

        private bool IsLasSlotUnlocked(UILocation location)
        {
            ActionSlotPrereqEntry slotPrerequisite = GetGameTableManager().ActionSlotPrereq?.Entries
                .FirstOrDefault(e => e.SlotIndex == (uint)location);
            if (slotPrerequisite == null || slotPrerequisite.PrerequisiteIdUnlock == 0u)
                return true;

            return GetPrerequisiteManager().Meets(player, slotPrerequisite.PrerequisiteIdUnlock);
        }

        public void Update(double lastTick)
        {
            // update cooldowns
            foreach ((CooldownKey key, ActiveCooldown cooldown) in activeCooldowns.ToArray())
            {
                if (cooldown.TimeRemaining - lastTick <= 0d)
                {
                    activeCooldowns.Remove(key);
                    log.Trace($"Cooldown type {cooldown.Type} id {cooldown.TypeId} has reset.");
                }
                else
                    cooldown.TimeRemaining -= lastTick;
            }

            foreach (CharacterSpell unlockedSpell in spells.Values)
                unlockedSpell.Update(lastTick);
        }

        public void Save(CharacterContext context)
        {
            if (saveMask != SpellManagerSaveMask.None)
            {
                // character is attached in Player::Save, this will only be local lookup
                CharacterModel character = context.Character.Find(player.CharacterId);
                EntityEntry<CharacterModel> entity = context.Entry(character);

                if ((saveMask & SpellManagerSaveMask.ActiveActionSet) != 0)
                {
                    character.ActiveSpec = ActiveActionSet;
                    entity.Property(p => p.ActiveSpec).IsModified = true;
                }

                if ((saveMask & SpellManagerSaveMask.BonusAbilityTierPoints) != 0)
                {
                    character.BonusAbilityTierPoints = bonusAbilityTierPoints;
                    entity.Property(p => p.BonusAbilityTierPoints).IsModified = true;
                }

                saveMask = SpellManagerSaveMask.None;
            }

            foreach (ICharacterSpell spell in spells.Values)
                spell.Save(context);

            foreach (IActionSet actionSet in actionSets)
                actionSet.Save(context);
        }

        /// <summary>
        /// Returns <see cref="ICharacterSpell"/> for an existing spell.
        /// </summary>
        public ICharacterSpell GetSpell(uint spell4BaseId)
        {
            return spells.TryGetValue(spell4BaseId, out ICharacterSpell spell) ? spell : null;
        }

        public ICharacterSpell GetSpellForSpell4Id(uint spell4Id)
        {
            Spell4Entry spell4Entry = GetGameTableManager().Spell4.GetEntry(spell4Id);
            return spell4Entry == null ? null : GetSpell(spell4Entry.Spell4BaseIdBaseSpell);
        }

        public void SetActiveFloatingActionBarShortcutSet(uint actionBarShortcutSetId, uint ownerSpell4Id)
        {
            ClearActiveFloatingActionBarShortcutSet();

            if (actionBarShortcutSetId == 0u)
                return;

            ActionBarShortcutSetEntry entry = GetGameTableManager().ActionBarShortcutSet?.GetEntry(actionBarShortcutSetId);
            if (entry == null)
                return;

            foreach ((uint shortcutType, uint objectId) in EnumerateActionBarShortcutSet(entry))
            {
                if (objectId == 0u || shortcutType != (uint)ShortcutType.Spell)
                    continue;

                activeFloatingActionBarSpell4Ids.Add(objectId);
            }

            TrackOwnerSpellGroups(ownerSpell4Id, activeFloatingActionBarOwnerSpellGroupIds);
        }

        public bool IsActiveFloatingActionBarSpell(uint spell4Id)
        {
            return spell4Id != 0u && activeFloatingActionBarSpell4Ids.Contains(spell4Id);
        }

        public void ClearActiveFloatingActionBarShortcutSet()
        {
            activeFloatingActionBarSpell4Ids.Clear();
            activeFloatingActionBarOwnerSpellGroupIds.Clear();
        }

        public bool ClearActiveFloatingActionBarShortcutSetForSpellGroup(uint spellGroupId)
        {
            if (spellGroupId == 0u || !activeFloatingActionBarOwnerSpellGroupIds.Contains(spellGroupId))
                return false;

            ClearActiveFloatingActionBarShortcutSet();
            return true;
        }

        public void SetActivePetActionSpell(uint petSwitchSpell4Id, uint actionSpell4Id, uint ownerSpell4Id)
        {
            if (actionSpell4Id == 0u)
                return;

            HashSet<uint> ownerSpellGroupIds = GetOwnerSpellGroups(ownerSpell4Id);
            TrackActivePetActionSpell(petSwitchSpell4Id, actionSpell4Id, ownerSpellGroupIds);
            TrackActivePetActionSpell(actionSpell4Id, actionSpell4Id, ownerSpellGroupIds);

            Spell4Entry actionEntry = GetGameTableManager().Spell4?.GetEntry(actionSpell4Id);
            TrackActivePetActionSpell(actionEntry?.Spell4BaseIdBaseSpell ?? 0u, actionSpell4Id, ownerSpellGroupIds);
        }

        public bool TryResolveActivePetActionSpell(uint selectedSpell4Id, out uint actionSpell4Id)
        {
            if (selectedSpell4Id == 0u)
            {
                actionSpell4Id = 0u;
                return false;
            }

            return activePetActionSpell4Ids.TryGetValue(selectedSpell4Id, out actionSpell4Id);
        }

        public bool TryResolveSingleActivePetActionSpell(out uint actionSpell4Id)
        {
            uint[] actionSpell4Ids = activePetActionSpell4Ids.Values
                .Where(id => id != 0u)
                .Distinct()
                .ToArray();

            if (actionSpell4Ids.Length == 1)
            {
                actionSpell4Id = actionSpell4Ids[0];
                return true;
            }

            actionSpell4Id = 0u;
            return false;
        }

        public void ClearActivePetActionSpells()
        {
            activePetActionSpell4Ids.Clear();
            activePetActionOwnerSpellGroupIds.Clear();
            activePetActionOwnerSpellGroupIdsBySpell4Id.Clear();
        }

        public void ClearActivePetActionSpell(uint petSwitchSpell4Id, uint actionSpell4Id)
        {
            ClearActivePetActionSpellKey(petSwitchSpell4Id);
            ClearActivePetActionSpellKey(actionSpell4Id);

            Spell4Entry actionEntry = GetGameTableManager().Spell4?.GetEntry(actionSpell4Id);
            ClearActivePetActionSpellKey(actionEntry?.Spell4BaseIdBaseSpell ?? 0u);

            RebuildActivePetActionOwnerSpellGroups();
        }

        public bool ClearActivePetActionSpellsForSpellGroup(uint spellGroupId)
        {
            if (spellGroupId == 0u || !activePetActionOwnerSpellGroupIds.Contains(spellGroupId))
                return false;

            uint[] spell4Ids = activePetActionOwnerSpellGroupIdsBySpell4Id
                .Where(e => e.Value.Contains(spellGroupId))
                .Select(e => e.Key)
                .ToArray();

            foreach (uint spell4Id in spell4Ids)
                ClearActivePetActionSpellKey(spell4Id);

            RebuildActivePetActionOwnerSpellGroups();
            return true;
        }

        /// <summary>
        /// Add a new <see cref="ICharacterSpell"/> created from supplied spell base id and tier.
        /// </summary>
        public void AddSpell(uint spell4BaseId, byte tier = 1)
        {
            ISpellBaseInfo spellBaseInfo = GetGlobalSpellManager().GetSpellBaseInfo(spell4BaseId);
            if (spellBaseInfo == null)
                throw new ArgumentOutOfRangeException();

            ISpellInfo spellInfo = spellBaseInfo.GetSpellInfo(tier);
            if (spellInfo == null)
                throw new ArgumentOutOfRangeException();

            if (spells.ContainsKey(spell4BaseId))
                throw new InvalidOperationException();

            IItem item = player.Inventory.SpellCreate(spellBaseInfo.Entry, ItemUpdateReason.NoReason);

            var unlockedSpell = new CharacterSpell(player, spellBaseInfo, tier, item, prerequisiteManager, globalSpellManager, gameTableManager);
            if (!player.IsLoading)
            {
                player.Session.EnqueueMessageEncrypted(new ServerSpellUpdate
                {
                    Spell4BaseId = spell4BaseId,
                    TierIndex    = tier,
                    Activated    = tier > 0
                });
            }

            spells.Add(spellBaseInfo.Entry.Id, unlockedSpell);
        }

        private IPrerequisiteManager GetPrerequisiteManager()
        {
            return prerequisiteManager ?? throw new InvalidOperationException($"{nameof(SpellManager)} requires an {nameof(IPrerequisiteManager)}.");
        }

        private IGlobalSpellManager GetGlobalSpellManager()
        {
            return globalSpellManager ?? throw new InvalidOperationException($"{nameof(SpellManager)} requires an {nameof(IGlobalSpellManager)}.");
        }

        private IGameTableManager GetGameTableManager()
        {
            return gameTableManager ?? throw new InvalidOperationException($"{nameof(SpellManager)} requires an {nameof(IGameTableManager)}.");
        }

        private static IEnumerable<(uint ShortcutType, uint ObjectId)> EnumerateActionBarShortcutSet(ActionBarShortcutSetEntry entry)
        {
            yield return (entry.ShortcutType00, entry.ObjectId00);
            yield return (entry.ShortcutType01, entry.ObjectId01);
            yield return (entry.ShortcutType02, entry.ObjectId02);
            yield return (entry.ShortcutType03, entry.ObjectId03);
            yield return (entry.ShortcutType04, entry.ObjectId04);
            yield return (entry.ShortcutType05, entry.ObjectId05);
            yield return (entry.ShortcutType06, entry.ObjectId06);
            yield return (entry.ShortcutType07, entry.ObjectId07);
            yield return (entry.ShortcutType08, entry.ObjectId08);
            yield return (entry.ShortcutType09, entry.ObjectId09);
            yield return (entry.ShortcutType10, entry.ObjectId10);
            yield return (entry.ShortcutType11, entry.ObjectId11);
        }

        private IEnumerable<uint> EnumerateSpell4GroupList(uint spell4GroupListId)
        {
            if (spell4GroupListId == 0u)
                yield break;

            Spell4GroupListEntry entry = GetGameTableManager().Spell4GroupList?.GetEntry(spell4GroupListId);
            if (entry == null)
                yield break;

            uint[] spellGroupIds =
            [
                entry.SpellGroupId00,
                entry.SpellGroupId01,
                entry.SpellGroupId02,
                entry.SpellGroupId03,
                entry.SpellGroupId04,
                entry.SpellGroupId05,
                entry.SpellGroupId06,
                entry.SpellGroupId07,
                entry.SpellGroupId08,
                entry.SpellGroupId09,
                entry.SpellGroupId10,
                entry.SpellGroupId11,
                entry.SpellGroupId12,
                entry.SpellGroupId13,
                entry.SpellGroupId14,
                entry.SpellGroupId15,
                entry.SpellGroupId16,
                entry.SpellGroupId17,
                entry.SpellGroupId18,
                entry.SpellGroupId19,
                entry.SpellGroupId20,
                entry.SpellGroupId21,
                entry.SpellGroupId22,
                entry.SpellGroupId23,
                entry.SpellGroupId24,
                entry.SpellGroupId25,
                entry.SpellGroupId26,
                entry.SpellGroupId27,
                entry.SpellGroupId28,
                entry.SpellGroupId29,
                entry.SpellGroupId30,
                entry.SpellGroupId31
            ];

            foreach (uint spellGroupId in spellGroupIds)
                if (spellGroupId != 0u)
                    yield return spellGroupId;
        }

        private void TrackOwnerSpellGroups(uint ownerSpell4Id, HashSet<uint> ownerSpellGroupIds)
        {
            Spell4Entry ownerSpell = ownerSpell4Id == 0u ? null : GetGameTableManager().Spell4?.GetEntry(ownerSpell4Id);
            if (ownerSpell == null)
                return;

            foreach (uint spellGroupId in EnumerateSpell4GroupList(ownerSpell.Spell4GroupListId))
                ownerSpellGroupIds.Add(spellGroupId);
        }

        private HashSet<uint> GetOwnerSpellGroups(uint ownerSpell4Id)
        {
            var ownerSpellGroupIds = new HashSet<uint>();
            TrackOwnerSpellGroups(ownerSpell4Id, ownerSpellGroupIds);
            return ownerSpellGroupIds;
        }

        private void TrackPetActionOwnerSpellGroups(uint spell4Id, HashSet<uint> ownerSpellGroupIds)
        {
            if (spell4Id == 0u || ownerSpellGroupIds.Count == 0)
                return;

            activePetActionOwnerSpellGroupIdsBySpell4Id[spell4Id] = [.. ownerSpellGroupIds];
            foreach (uint spellGroupId in ownerSpellGroupIds)
                activePetActionOwnerSpellGroupIds.Add(spellGroupId);
        }

        private void TrackActivePetActionSpell(uint selectedSpell4Id, uint actionSpell4Id, HashSet<uint> ownerSpellGroupIds)
        {
            if (selectedSpell4Id == 0u)
                return;

            activePetActionSpell4Ids[selectedSpell4Id] = actionSpell4Id;
            TrackPetActionOwnerSpellGroups(selectedSpell4Id, ownerSpellGroupIds);
        }

        private void ClearActivePetActionSpellKey(uint spell4Id)
        {
            if (spell4Id == 0u)
                return;

            activePetActionSpell4Ids.Remove(spell4Id);
            activePetActionOwnerSpellGroupIdsBySpell4Id.Remove(spell4Id);
        }

        private void RebuildActivePetActionOwnerSpellGroups()
        {
            activePetActionOwnerSpellGroupIds.Clear();
            foreach (HashSet<uint> ownerSpellGroupIds in activePetActionOwnerSpellGroupIdsBySpell4Id.Values)
                foreach (uint spellGroupId in ownerSpellGroupIds)
                    activePetActionOwnerSpellGroupIds.Add(spellGroupId);
        }

        /// <summary>
        /// Update existing <see cref="ICharacterSpell"/> with supplied tier. The base tier will be updated if no action set index is supplied.
        /// </summary>
        public void UpdateSpell(uint spell4BaseId, byte tier, byte? actionSetIndex)
        {
            ISpellBaseInfo spellBaseInfo = GetGlobalSpellManager().GetSpellBaseInfo(spell4BaseId);
            if (spellBaseInfo == null)
                throw new ArgumentOutOfRangeException();

            ISpellInfo spellInfo = spellBaseInfo.GetSpellInfo(tier);
            if (spellInfo == null)
                throw new ArgumentOutOfRangeException();

            ICharacterSpell spell = GetSpell(spell4BaseId);
            if (spell == null)
                throw new ArgumentOutOfRangeException();

            if (actionSetIndex == null)
                spell.Tier = tier;
            else
            {
                IActionSet actionSet = GetActionSet(actionSetIndex.Value);
                actionSet.UpdateSpellShortcut(spell4BaseId, tier);
            }

            if (!player.IsLoading)
            {
                player.Session.EnqueueMessageEncrypted(new ServerSpellUpdate
                {
                    Spell4BaseId = spell4BaseId,
                    TierIndex    = tier,
                    SpecIndex    = actionSetIndex ?? 0,
                    Activated    = tier > 0
                });
            }
        }

        /// <summary>
        /// Return the tier for supplied spell.
        /// This will either be the <see cref="IActionSetShortcut"/> tier if placed in the active <see cref="IActionSet"/> or base tier if not.
        /// </summary>
        public byte GetSpellTier(uint spell4BaseId)
        {
            ICharacterSpell spell = GetSpell(spell4BaseId);
            if (spell == null)
                throw new ArgumentException();

            IActionSet actionSet = GetActionSet(ActiveActionSet);
            IActionSetShortcut shortcut = actionSet.GetShortcut(ShortcutType.SpellbookItem, spell4BaseId);
            return shortcut?.Tier ?? spell.Tier;
        }

        public List<ICharacterSpell> GetPets()
        {
            return spells.Values
                .Where(s => s.BaseInfo.SpellType.Id == 27 ||
                            s.BaseInfo.SpellType.Id == 30 ||
                            s.BaseInfo.SpellType.Id == 104)
                .ToList();
        }

        /// <summary>
        /// Return spell cooldown for supplied spell id in seconds.
        /// </summary>
        public double GetSpellCooldown(uint spellId)
        {
            double cooldown = 0d;
            foreach (CooldownIdentity identity in GetSpellCooldownIdentities(spellId))
                if (activeCooldowns.TryGetValue(new CooldownKey(SpellCooldownType, identity.TypeId), out ActiveCooldown activeCooldown))
                    cooldown = Math.Max(cooldown, activeCooldown.TimeRemaining);

            return cooldown;
        }

        /// <summary>
        /// Set spell cooldown in seconds for supplied spell id.
        /// </summary>
        public void SetSpellCooldown(uint spell4Id, double cooldown)
        {
            if (cooldown < 0d)
                throw new ArgumentOutOfRangeException();

            foreach (CooldownIdentity identity in GetSpellCooldownIdentities(spell4Id).ToList())
            {
                var key = new CooldownKey(SpellCooldownType, identity.TypeId);
                var activeCooldown = new ActiveCooldown
                {
                    Type                 = SpellCooldownType,
                    Spell4Id             = spell4Id,
                    TypeId               = identity.TypeId,
                    TypeIdIsCooldownNode = identity.IsCooldownNode,
                    TimeRemaining        = cooldown
                };

                if (cooldown > 0d)
                    activeCooldowns[key] = activeCooldown;
                else
                    activeCooldowns.Remove(key);

                SendCooldown(activeCooldown);
            }

            log.Trace($"Spell {spell4Id} cooldown set to {cooldown} seconds.");
        }

        public void ResetAllSpellCooldowns()
        {
            foreach ((CooldownKey key, ActiveCooldown cooldown) in activeCooldowns
                .Where(c => c.Value.Type == SpellCooldownType)
                .ToArray())
            {
                cooldown.TimeRemaining = 0d;
                SendCooldown(cooldown);
                activeCooldowns.Remove(key);
            }
        }

        public double GetGlobalSpellCooldown()
        {
            return activeCooldowns.Values
                .Where(c => c.Type == GlobalCooldownType)
                .Select(c => c.TimeRemaining)
                .DefaultIfEmpty(0d)
                .Max();
        }

        public void SetGlobalSpellCooldown(double cooldown)
        {
            SetGlobalSpellCooldown(0u, cooldown);
        }

        public void SetGlobalSpellCooldown(uint cooldownId, double cooldown)
        {
            if (cooldown < 0d)
                throw new ArgumentOutOfRangeException();

            if (cooldown <= 0d)
            {
                ClearGlobalSpellCooldown(cooldownId);
                return;
            }

            CooldownKey? existingKey = null;
            ActiveCooldown existingCooldown = null;
            foreach ((CooldownKey key, ActiveCooldown candidateCooldown) in activeCooldowns)
            {
                if (candidateCooldown.Type != GlobalCooldownType)
                    continue;

                if (existingCooldown == null || candidateCooldown.TimeRemaining > existingCooldown.TimeRemaining)
                {
                    existingKey = key;
                    existingCooldown = candidateCooldown;
                }
            }

            if (existingCooldown != null && existingCooldown.TimeRemaining > cooldown)
                return;

            if (existingKey != null)
                activeCooldowns.Remove(existingKey.Value);

            foreach (CooldownKey key in activeCooldowns
                .Where(c => c.Value.Type == GlobalCooldownType)
                .Select(c => c.Key)
                .ToArray())
                activeCooldowns.Remove(key);

            var globalCooldown = new ActiveCooldown
            {
                Type                 = GlobalCooldownType,
                Spell4Id             = 0u,
                TypeId               = cooldownId,
                TypeIdIsCooldownNode = cooldownId != 0u,
                TimeRemaining        = cooldown
            };
            activeCooldowns[new CooldownKey(GlobalCooldownType, cooldownId)] = globalCooldown;
            SendCooldown(globalCooldown);

            log.Trace($"Global spell cooldown set to {cooldown} seconds.");
        }

        public void AddAmpPower(ushort amount)
        {
            if (amount == 0u)
                return;

            ushort previousBonusAmpPower = bonusAmpPower;
            bonusAmpPower = (ushort)Math.Min(bonusAmpPower + amount, MaxBonusAmpPower);

            ushort addedPower = (ushort)(bonusAmpPower - previousBonusAmpPower);
            if (addedPower == 0u)
                return;

            foreach (IActionSet actionSet in actionSets)
                actionSet.AddAmpPower(addedPower);

            SendServerAmpPowerUpdate();
        }

        public void AddAbilityTierPoints(byte amount)
        {
            if (amount == 0u)
                return;

            byte previousBonusTierPoints = bonusAbilityTierPoints;
            bonusAbilityTierPoints = (byte)Math.Min(bonusAbilityTierPoints + amount, ActionSet.MaxBonusTierPoints);

            byte addedPoints = (byte)(bonusAbilityTierPoints - previousBonusTierPoints);
            if (addedPoints == 0u)
                return;

            foreach (IActionSet actionSet in actionSets)
                actionSet.AddTierPoints(addedPoints);

            saveMask |= SpellManagerSaveMask.BonusAbilityTierPoints;
            player.RequestSave();
            SendServerAbilityPoints();
        }

        /// <summary>
        /// Return <see cref="IActionSet"/> at supplied index.
        /// </summary>
        public IActionSet GetActionSet(byte actionSetIndex)
        {
            if (actionSetIndex >= ActionSet.MaxActionSets)
                throw new ArgumentOutOfRangeException();

            return actionSets[actionSetIndex];
        }

        /// <summary>
        /// Update active <see cref="IActionSet"/> with supplied index, returned <see cref="SpecError"/> is sent to the client.
        /// </summary>
        public SpecError SetActiveActionSet(byte value)
        {
            if (value >= ActionSet.MaxActionSets)
                return SpecError.InvalidIndex;

            if (value == ActiveActionSet)
                return SpecError.NoChange;

            if (!player.IsAlive)
                return SpecError.InvalidPlayer;

            if (player.InCombat)
                return SpecError.InCombat;

            ActiveActionSet = value;
            return SpecError.Ok;
        }

        public void SendInitialPackets()
        {
            SendServerAbilities();
            SendServerSpellList();
            SendServerAbilityPoints();
            SendServerActionSets();
            SendServerAmpLists();
            SendServerAmpPowerUpdate();

            player.Session.EnqueueMessageEncrypted(new ServerCooldownList
            {
                Cooldowns = activeCooldowns.Values
                    .Where(c => c.TimeRemaining > 0d)
                    .Select(BuildCooldown)
                    .ToList()
            });
        }

        private void SendServerAbilities()
        {
            foreach (IItem spell in player.Inventory
                .Where(b => b.Location == InventoryLocation.Ability)
                .SelectMany(s => s))
            {
                player.Session.EnqueueMessageEncrypted(new ServerItemAdd
                {
                    InventoryItem = new InventoryItem
                    {
                        Item   = spell.Build(),
                        Reason = ItemUpdateReason.NoReason
                    }
                });
            }
        }

        public void SendServerSpellList()
        {
            var serverAbilityBook = new ServerAbilityBook();
            foreach ((uint spell4BaseId, ICharacterSpell spell) in spells)
            {
                ISpellBaseInfo spellBaseInfo = GetGlobalSpellManager().GetSpellBaseInfo(spell4BaseId);
                if (spellBaseInfo == null)
                    continue;

                for (byte i = 0; i < ActionSet.MaxActionSets; i++)
                {
                    IActionSetShortcut shortcut = actionSets[i].GetShortcut(ShortcutType.SpellbookItem, spell4BaseId);
                    serverAbilityBook.Spells.Add(new ServerAbilityBook.Spell
                    {
                        Spell4BaseId      = spell4BaseId,
                        TierIndexAchieved = shortcut?.Tier ?? spell.Tier,
                        SpecIndex         = i
                    });

                    // class ability
                    if (spellBaseInfo.SpellType.Id != 5)
                        break;
                }
            }

            player.Session.EnqueueMessageEncrypted(serverAbilityBook);
        }

        public void SendServerAbilityPoints()
        {
            player.Session.EnqueueMessageEncrypted(new ServerAbilityPoints
            {
                AbilityPoints      = actionSets[ActiveActionSet].TierPoints,
                TotalAbilityPoints = (uint)(ActionSet.MaxTierPoints + bonusAbilityTierPoints)
            });
        }

        private void SendServerActionSets()
        {
            for (byte i = 0; i < ActionSet.MaxActionSets; i++)
            {
                IActionSet actionSet = GetActionSet(i);
                player.Session.EnqueueMessageEncrypted(actionSet.BuildServerActionSet());
            }
        }

        private void SendActiveActionSet()
        {
            IActionSet actionSet = GetActionSet(ActiveActionSet);
            player.Session.EnqueueMessageEncrypted(new ServerActionSetClearCache());
            player.Session.EnqueueMessageEncrypted(actionSet.BuildServerActionSet());
        }

        private void SendServerAmpLists()
        {
            for (byte i = 0; i < ActionSet.MaxActionSets; i++)
            {
                IActionSet actionSet = GetActionSet(i);
                player.Session.EnqueueMessageEncrypted(actionSet.BuildServerAmpList());
            }
        }

        public bool SetSpellActivation(uint spell4Id, bool active)
        {
            Spell4Entry spell4Entry = GetGameTableManager().Spell4.GetEntry(spell4Id);
            if (spell4Entry == null)
                return false;

            ICharacterSpell spell = GetSpell(spell4Entry.Spell4BaseIdBaseSpell);
            if (spell == null)
                return false;

            byte tier = active
                ? (byte)Math.Max(1, (int)spell.Tier)
                : (byte)0;

            if (spell.Tier != tier)
                spell.Tier = tier;

            if (!player.IsLoading)
            {
                player.Session.EnqueueMessageEncrypted(new ServerSpellUpdate
                {
                    Spell4BaseId = spell4Entry.Spell4BaseIdBaseSpell,
                    TierIndex    = tier,
                    SpecIndex    = ActiveActionSet,
                    Activated    = active
                });
            }

            return true;
        }

        private void SendServerAmpPowerUpdate()
        {
            player.Session.EnqueueMessageEncrypted(new ServerAmpPowerUpdate
            {
                BonusPower = bonusAmpPower
            });
        }

        /// <summary>
        /// Returns whether any active spell cooldown references the supplied <see cref="SpellCoolDownEntry"/> id.
        /// Proxy for client prerequisite cooldown-node list at entity <c>+0x15d0</c>.
        /// </summary>
        internal bool HasActiveCoolDownNode(uint spellCoolDownId, IGameTableManager gameTableManager)
        {
            return activeCooldowns.Values.Any(c =>
                c.TimeRemaining > 0d
                && c.TypeIdIsCooldownNode
                && c.TypeId == spellCoolDownId);
        }

        /// <summary>
        /// Returns whether the player knows a spell that references the supplied cooldown-node id.
        /// Proxy for client SpellService set membership (type 104).
        /// </summary>
        internal bool KnowsSpellReferencingCoolDownNode(uint spellCoolDownId, IGameTableManager gameTableManager)
        {
            foreach (ICharacterSpell characterSpell in spells.Values)
            {
                Spell4Entry entry = characterSpell.SpellInfo.Entry;
                if (ReferencesCoolDownNode(entry, spellCoolDownId))
                    return true;
            }

            return false;
        }

        private static bool ReferencesCoolDownNode(Spell4Entry entry, uint spellCoolDownId)
        {
            return entry.SpellCoolDownIdGlobal == spellCoolDownId
                || entry.SpellCoolDownId00 == spellCoolDownId
                || entry.SpellCoolDownId01 == spellCoolDownId
                || entry.SpellCoolDownId02 == spellCoolDownId;
        }

        private IEnumerable<CooldownIdentity> GetSpellCooldownIdentities(uint spell4Id)
        {
            Spell4Entry entry = gameTableManager?.Spell4?.GetEntry(spell4Id);
            if (entry == null)
            {
                yield return new CooldownIdentity(spell4Id, false);
                yield break;
            }

            var cooldownNodeIds = new HashSet<uint>();
            foreach (uint cooldownNodeId in EnumerateSpellCooldownNodeIds(entry))
            {
                if (cooldownNodeId == 0u || !cooldownNodeIds.Add(cooldownNodeId))
                    continue;

                yield return new CooldownIdentity(cooldownNodeId, true);
            }

            if (cooldownNodeIds.Count == 0)
                yield return new CooldownIdentity(spell4Id, false);
        }

        private static IEnumerable<uint> EnumerateSpellCooldownNodeIds(Spell4Entry entry)
        {
            yield return entry.SpellCoolDownId00;
            yield return entry.SpellCoolDownId01;
            yield return entry.SpellCoolDownId02;
        }

        private void ClearGlobalSpellCooldown(uint cooldownId)
        {
            KeyValuePair<CooldownKey, ActiveCooldown>[] cooldowns = activeCooldowns
                .Where(c => c.Value.Type == GlobalCooldownType && (cooldownId == 0u || c.Value.TypeId == cooldownId))
                .ToArray();

            if (cooldowns.Length == 0)
            {
                SendCooldown(new ActiveCooldown
                {
                    Type                 = GlobalCooldownType,
                    Spell4Id             = 0u,
                    TypeId               = cooldownId,
                    TypeIdIsCooldownNode = cooldownId != 0u,
                    TimeRemaining        = 0d
                });
                return;
            }

            foreach ((CooldownKey key, ActiveCooldown cooldown) in cooldowns)
            {
                cooldown.TimeRemaining = 0d;
                SendCooldown(cooldown);
                activeCooldowns.Remove(key);
            }
        }

        private void SendCooldown(ActiveCooldown activeCooldown)
        {
            if (player?.IsLoading != false)
                return;

            player.Session.EnqueueMessageEncrypted(new ServerCooldown
            {
                Cooldown = BuildCooldown(activeCooldown)
            });
        }

        private static Cooldown BuildCooldown(ActiveCooldown activeCooldown)
        {
            return new Cooldown
            {
                Type          = activeCooldown.Type,
                SpellId       = activeCooldown.Spell4Id,
                TypeId        = activeCooldown.TypeId,
                TimeRemaining = ToCooldownMilliseconds(activeCooldown.TimeRemaining)
            };
        }

        private static uint ToCooldownMilliseconds(double seconds)
        {
            if (seconds <= 0d)
                return 0u;

            double milliseconds = seconds * 1000d;
            if (!double.IsFinite(milliseconds) || milliseconds >= uint.MaxValue)
                return uint.MaxValue;

            return (uint)milliseconds;
        }
    }
}
