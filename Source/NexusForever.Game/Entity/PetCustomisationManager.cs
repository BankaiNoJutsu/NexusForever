using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.GameTable.Text.Filter;
using NexusForever.GameTable.Text.Static;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Pet;

namespace NexusForever.Game.Entity
{
    public partial class PetCustomisationManager : IPetCustomisationManager
    {
        public const byte MaxCustomisationFlairs = 4;

        private static ulong PetCustomisationHash(PetType type, uint objectId)
        {
            return (ulong)objectId << 32 | (byte)type;
        }

        private readonly IPlayer player;
        private readonly IGameTableManager gameTableManager;
        private readonly ITextFilterManager textFilterManager;
        private readonly IPrerequisiteManager prerequisiteManager;
        private readonly Dictionary<uint/*petFlairId*/, IPetFlair> petFlairs = new();
        private readonly Dictionary<ulong/*hash*/, IPetCustomisation> petCustomisations = new();
        internal ExperiencePersistence.Pet[] LoadedExperiencePets { get; }
        internal ExperiencePersistence.Flair[] LoadedExperienceFlairs { get; }
        internal ExperiencePersistence.Flair[] CaptureExperienceFlairs()
        {
            if (player.CharacterId != PetCharacter || LoadedExperienceFlairs.Any(row => row.Owner != PetCharacter)
                || petFlairs.Any(row => row.Value.Owner != PetCharacter || row.Key != row.Value.Entry.Id))
                throw new InvalidOperationException("Pet flairs changed their Character owner or identity.");
            return LoadedExperienceFlairs.Concat(petFlairs.Keys.Select(id => new ExperiencePersistence.Flair(PetCharacter, id))).Distinct().OrderBy(row => row.Id).ToArray();
        }
        internal ulong PetCharacter { get; }
        internal string PetDatabase { get; }
        internal ExperiencePersistence.Pet[] CaptureExperiencePets()
        {
            if (player.CharacterId != PetCharacter || petCustomisations.Values.Any(row => row.Owner != PetCharacter)) throw new InvalidOperationException("Pets changed their Character owner.");
            return petCustomisations.Values.Select(row => ((PetCustomisation)row).CaptureExperiencePet()).OrderBy(row => row.Type).ThenBy(row => row.Id).ToArray();
        }
        /// <summary>
        /// Create a new <see cref="IPetCustomisationManager"/> from existing <see cref="CharacterModel"/> database model.
        /// </summary>
        public PetCustomisationManager(
            IPlayer owner,
            CharacterModel model,
            IGameTableManager gameTableManager = null,
            ITextFilterManager textFilterManager = null,
            IPrerequisiteManager prerequisiteManager = null)
        {
            player = owner;
            this.gameTableManager = gameTableManager;
            this.textFilterManager = textFilterManager;
            this.prerequisiteManager = prerequisiteManager;
            PetCharacter = model.Id; PetDatabase = CharacterLoadOrigin.Get(model)?.Database;
            LoadedExperiencePets = model.PetCustomisation.Select(ExperiencePersistence.FromModel).OrderBy(row => row.Type).ThenBy(row => row.Id).ToArray();
            LoadedExperienceFlairs = model.PetFlair.Select(ExperiencePersistence.FromModel).OrderBy(row => row.Id).ToArray();

            foreach (CharacterPetFlairModel flairModel in model.PetFlair)
            {
                var petFlair = new PetFlair(flairModel, GetGameTableManager().PetFlair?.GetEntry(flairModel.PetFlairId));
                if (petFlair.Entry == null)
                    continue;

                petFlairs.Add(petFlair.Entry.Id, petFlair);
            }

            foreach (CharacterPetCustomisationModel customisationModel in model.PetCustomisation)
            {
                var customisation = new PetCustomisation(customisationModel, GetGameTableManager().PetFlair) { CharacterSaveOwner = TrySaveExperience };
                petCustomisations.Add(PetCustomisationHash(customisation.Type, customisation.ObjectId), customisation);
            }
            InitialiseFlairPublication();
        }

        public void Save(CharacterContext context)
        {
            if (TrySaveExperience(context)) return;
            foreach (IPetFlair flair in petFlairs.Values)
                flair.Save(context);

            foreach (IPetCustomisation customisation in petCustomisations.Values)
                customisation.Save(context);

            PetCustomisation.UpsertTrackedCreates(context);
        }
        private bool TrySaveExperience(CharacterContext context)
        {
            if (player is not Player owner) return false;
            if (!ReferenceEquals(owner.PetCustomisationManager, this)) { var error = new InvalidOperationException("Pets lost their Character owner."); context.RejectSave(error); throw error; }
            owner.SaveExperience(context); return true;
        }

        /// <summary>
        /// Unlock pet flair with supplied id.
        /// </summary>
        public void UnlockFlair(ushort id)
        {
            PetFlairEntry entry = GetGameTableManager().PetFlair?.GetEntry(id);
            if (entry == null || id > MaxWireFlairId || entry.UnlockBitIndex == null || entry.UnlockBitIndex.Any(bit => bit >= FlairBitCount))
                throw new ArgumentOutOfRangeException();

            if (petFlairs.ContainsKey(id))
                throw new ArgumentException();

            if (entry.PrerequisiteId > 0 && !GetPrerequisiteManager().Meets(player, entry.PrerequisiteId))
                return;

            petFlairs.Add(id, new PetFlair(player.CharacterId, entry));

            if (player is Player)
            {
                QueueFlairPublication(id, !player.IsLoading); player.RequestSave(); return;
            }

            if (!player.IsLoading)
            {
                player.Session.EnqueueMessageEncrypted(new ServerUnlockPetFlair
                {
                    PetFlairId = id
                });
            }
        }

        public bool HasFlair(ushort id)
        {
            return petFlairs.ContainsKey(id);
        }

        /// <summary>
        /// Renames the pet name for <see cref="PetType"/> and object id.
        /// </summary>
        public void RenamePet(PetType type, uint objectId, String name)
        {
            if (!GetTextFilterManager().IsTextValid(name, UserText.ScientistScanbotName))
                throw new InvalidPacketValueException();

            ulong hash = PetCustomisationHash(type, objectId);
            if (!petCustomisations.TryGetValue(hash, out IPetCustomisation customisation))
            {
                customisation = new PetCustomisation(player.CharacterId, type, objectId) { CharacterSaveOwner = TrySaveExperience };
                petCustomisations.Add(hash, customisation);
            }

            customisation.Name = name;

            if (!player.IsLoading)
            {
                player.Session.EnqueueMessageEncrypted(new ServerPetCustomisation
                {
                    PetCustomisation = customisation.Build()
                });
            }

            if (type == PetType.ScanBot)
                player.PathManager?.RefreshScientistScanbotName(objectId);
        }

        /// <summary>
        /// Add or update equipped pet flair at supplied index for <see cref="PetType"/> and object id.
        /// </summary>
        public void AddCustomisation(PetType type, uint objectId, ushort index, ushort flairId)
        {
            if (index >= MaxCustomisationFlairs)
                throw new ArgumentOutOfRangeException();

            PetFlairEntry entry = null;
            if (flairId != 0)
            {
                if (!petFlairs.ContainsKey(flairId))
                    throw new ArgumentException();

                entry = GetGameTableManager().PetFlair?.GetEntry(flairId);
                if (entry == null)
                    throw new ArgumentException();
            }

            ulong hash = PetCustomisationHash(type, objectId);
            if (!petCustomisations.TryGetValue(hash, out IPetCustomisation customisation))
            {
                customisation = new PetCustomisation(player.CharacterId, type, objectId) { CharacterSaveOwner = TrySaveExperience };
                petCustomisations.Add(hash, customisation);
            }

            customisation.AddFlair(index, entry);

            if (!player.IsLoading)
            {
                player.Session.EnqueueMessageEncrypted(new ServerPetCustomisation
                {
                    PetCustomisation = customisation.Build()
                });
            }
        }

        /// <summary>
        /// Return <see cref="IPetCustomisation"/> for supplied <see cref="PetType"/> and object id.
        /// </summary>
        public IPetCustomisation GetCustomisation(PetType type, uint objectId)
        {
            ulong hash = PetCustomisationHash(type, objectId);
            petCustomisations.TryGetValue(hash, out IPetCustomisation customisation);
            return customisation;
        }

        /// <summary>
        /// Unlock scientist scanbot customisation with supplied profile id.
        /// </summary>
        public void UnlockScanBotProfile(uint id)
        {
            PathScientistScanBotProfileEntry entry = GetGameTableManager().PathScientistScanBotProfile?.GetEntry(id);
            if (entry == null)
                throw new ArgumentOutOfRangeException();

            UnlockCustomisation(PetType.ScanBot, id);
        }

        private void UnlockCustomisation(PetType type, uint objectId)
        {
            ulong hash = PetCustomisationHash(type, objectId);
            if (petCustomisations.ContainsKey(hash))
                return;

            var customisation = new PetCustomisation(player.CharacterId, type, objectId) { CharacterSaveOwner = TrySaveExperience };
            petCustomisations.Add(hash, customisation);

            if (!player.IsLoading)
            {
                player.Session.EnqueueMessageEncrypted(new ServerPetCustomisation
                {
                    PetCustomisation = customisation.Build()
                });
            }
        }

        public void SendInitialPackets()
        {
            if (player is Player) { QueueInitialFlairPublication(); return; }
            var petCustomisationList = new ServerPetCustomisationList
            {
                PetCustomisations = petCustomisations.Values
                    .Select(c => c.Build())
                    .ToList()
            };

            foreach (uint flairBitIndex in petFlairs
                .SelectMany(f => f.Value.Entry.UnlockBitIndex))
                petCustomisationList.UnlockedFlair.SetBit(flairBitIndex, true);

            player.Session.EnqueueMessageEncrypted(petCustomisationList);
        }

        private IGameTableManager GetGameTableManager()
        {
            return gameTableManager ?? throw new InvalidOperationException("Game table manager dependency was not supplied.");
        }

        private ITextFilterManager GetTextFilterManager()
        {
            return textFilterManager ?? throw new InvalidOperationException("Text filter manager dependency was not supplied.");
        }

        private IPrerequisiteManager GetPrerequisiteManager()
        {
            return prerequisiteManager ?? throw new InvalidOperationException("Prerequisite manager dependency was not supplied.");
        }
    }
}
