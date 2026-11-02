using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Entity
{
    public class Title : ITitle
    {
        public enum TitleSaveMask
        {
            None          = 0x00,
            Create        = 0x01,
            TimeRemaining = 0x02,
            Revoked       = 0x04
        }

        public ulong CharacterId { get; }
        public CharacterTitleEntry Entry { get; }

        /// <summary>
        /// <see cref="ITitle"/> lifetime in milliseconds.
        /// </summary>
        public double? TimeRemaining
        {
            get { lock (Persistence.Gate) return timeRemaining; }
            set
            {
                lock (Persistence.Gate) { timeRemaining = value; storedTime = (uint)(value ?? 0d); }
            }
        }

        private double? timeRemaining;

        public bool Revoked
        {
            get { lock (Persistence.Gate) return revoked; }
            set
            {
                lock (Persistence.Gate) { revoked = value; storedRevoked = Convert.ToByte(value); }
            }
        }

        private bool revoked;

        private readonly ushort titleId;
        private uint storedTime;
        private byte storedRevoked;
        internal TitlePersistence Persistence { get; }

        /// <summary>
        /// Create a new <see cref="ITitle"/> from an existing database model.
        /// </summary>
        public Title(CharacterTitleModel model, IGameTableManager gameTableManager) : this(model, gameTableManager, null) { }

        internal Title(CharacterTitleModel model, IGameTableManager gameTableManager, TitlePersistence persistence)
        {
            CharacterId = model.Id;
            Entry       = gameTableManager.CharacterTitle?.GetEntry(model.Title);
            revoked     = Convert.ToBoolean(model.Revoked);

            if (Entry != null && Entry.LifeTimeSeconds != 0u)
                timeRemaining = model.TimeRemaining;

            titleId = model.Title; storedTime = model.TimeRemaining; storedRevoked = model.Revoked;
            Persistence = persistence ?? new(CharacterId, new(null, [TitlePersistence.FromModel(model)]), CaptureSnapshot, title: titleId);
        }

        /// <summary>
        /// Create a new <see cref="ITitle"/> from a <see cref="CharacterTitleEntry"/> template.
        /// </summary>
        public Title(ulong owner, CharacterTitleEntry entry) : this(owner, entry, null) { }

        internal Title(ulong owner, CharacterTitleEntry entry, TitlePersistence persistence)
        {
            CharacterId = owner;
            Entry       = entry;

            if (Entry.LifeTimeSeconds != 0u)
                timeRemaining = Entry.LifeTimeSeconds;

            titleId = checked((ushort)entry.Id); storedTime = (uint)(timeRemaining ?? 0d);
            Persistence = persistence ?? new(CharacterId, new(null, []), CaptureSnapshot, title: titleId);
        }

        public void Update(double lastTick)
        {
            if (TimeRemaining != null)
                TimeRemaining -= lastTick;
        }

        public void Save(CharacterContext context) => Persistence.SaveTitle(context, titleId);

        internal TitlePersistence.State CapturePersistence()
        {
            if (Entry != null && Entry.Id != titleId) throw new InvalidOperationException("Title changed its template identity.");
            return new(titleId, storedTime, storedRevoked);
        }
        private TitlePersistence.Snapshot CaptureSnapshot() => new(null, [CapturePersistence()]);
    }
}
