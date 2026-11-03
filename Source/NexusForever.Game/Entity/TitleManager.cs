using System.Collections;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Achievement;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Entity
{
    public partial class TitleManager : ITitleManager
    {
        public ushort ActiveTitleId
        {
            get { lock (persistence.Gate) return activeTitleId; }
            set
            {
                if (value != 0 && (!titles.TryGetValue(value, out ITitle title) || title.Revoked || activeTitleId == value))
                    return;

                lock (persistence.Gate)
                {
                    activeTitleId = value;
                    if (player is Player) { RetainNotice(new SelectTitle(value)); return; }
                }

                player.EnqueueToVisible(new ServerTitleSet
                {
                    Guid  = player.Guid,
                    Title = ActiveTitleId
                }, true);
            }
        }

        private ushort activeTitleId;
        private readonly ulong characterId;
        private readonly TitlePersistence persistence;
        private readonly List<TitlePersistence.State> unavailableTitles = [];

        private readonly IPlayer player;
        private readonly IGameTableManager gameTableManager;
        private readonly Dictionary<ushort, ITitle> titles = new();

        /// <summary>
        /// Create a new <see cref="ITitleManager"/> from existing <see cref="CharacterModel"/> database model.
        /// </summary>
        public TitleManager(IPlayer owner, CharacterModel model, IGameTableManager gameTableManager)
        {
            player = owner;
            this.gameTableManager = gameTableManager;
            activeTitleId = model.Title;
            characterId = model.Id;
            var origin = CharacterLoadOrigin.Get(model);
            if (characterId != owner.CharacterId || origin != null && origin.AccountId != owner.Account?.Id)
                throw new InvalidOperationException("Titles changed their loaded Character/account owner.");
            persistence = new(characterId, TitlePersistence.FromModel(model), CapturePersistence, origin?.Database,
                publicationVersion: player is Player ? () => publicationVersion : null,
                acknowledge: player is Player ? AcknowledgePublication : null,
                publicationReady: player is Player ? SchedulePublication : null);

            foreach (CharacterTitleModel titleModel in model.CharacterTitle)
            {
                var title = new Title(titleModel, gameTableManager, persistence);
                if (title.Entry == null)
                { unavailableTitles.Add(TitlePersistence.FromModel(titleModel)); continue; }

                titles.Add(titleModel.Title, title);
            }

            InitialisePublication(model);
            EnsureActiveTitleIsOwned();
        }

        public void Update(double lastTick)
        {
            foreach (ITitle title in titles.Values)
            {
                title.Update(lastTick);
                if (!title.Revoked && title.TimeRemaining != null && title.TimeRemaining <= 0d)
                {
                    // title has expired
                    RevokeTitle((ushort)title.Entry.Id);
                }
            }
            SchedulePublication();
        }

        public void Save(CharacterContext context)
        {
            persistence.Save(context); SchedulePublication();
        }

        internal Action<CharacterContext> CaptureNormalSave(object token, string database) => persistence.CaptureNormal(token, database);
        private TitlePersistence.Snapshot CapturePersistence()
        {
            if (player.CharacterId != characterId) throw new InvalidOperationException("Titles changed their Character owner.");
            return new(activeTitleId, unavailableTitles.Concat(titles.Values.Cast<Title>().Select(title => title.CapturePersistence())).OrderBy(row => row.Id).ToArray());
        }

        /// <summary>
        /// Add new <see cref="ITitle"/> with supplied title id.
        /// </summary>
        /// <remarks>
        /// If suppress if true, update won't be sent to client.
        /// </remarks>
        public void AddTitle(ushort titleId, bool suppress = false)
        {
            CharacterTitleEntry entry = gameTableManager.CharacterTitle?.GetEntry(titleId);
            if (entry == null)
                throw new InvalidPacketValueException();

            bool alreadyOwned;
            lock (persistence.Gate)
            {
                alreadyOwned = titles.TryGetValue(titleId, out ITitle title) && !title.Revoked;
                if (title != null) title.Revoked = false;
                else titles.Add(titleId, new Title(player.CharacterId, entry, persistence));
                if (player is Player && !suppress) RetainNotice(new UpdateTitle(titleId, alreadyOwned, false));
            }
            if (alreadyOwned)
            {
                if (player is not Player)
                    player.Session.EnqueueMessageEncrypted(new ServerTitleUpdate { TitleId = titleId, Alreadyowned = true });
                return;
            }

            player.AchievementManager.CheckAchievements(player, AchievementType.TitleEarned, 0u);

            if (!suppress && player is not Player)
            {
                player.Session.EnqueueMessageEncrypted(new ServerTitleUpdate
                {
                    TitleId = titleId
                });
            }
        }

        /// <summary>
        /// Revoke <see cref="ITitle"/> with supplied title id.
        /// </summary>
        /// <remarks>
        /// If suppress if true, update won't be sent to client.
        /// </remarks>
        public void RevokeTitle(ushort titleId, bool suppress = false)
        {
            if (gameTableManager.CharacterTitle?.GetEntry(titleId) == null)
                throw new InvalidPacketValueException();

            if (!titles.TryGetValue(titleId, out ITitle title))
                throw new InvalidPacketValueException();

            lock (persistence.Gate)
            {
                title.Revoked = true;
                if (player is Player && !suppress) RetainNotice(new UpdateTitle(titleId, false, true));
                if (player is Player) EnsureActiveTitleIsOwned();
            }

            if (!suppress && player is not Player)
            {
                player.Session.EnqueueMessageEncrypted(new ServerTitleUpdate
                {
                    TitleId = (ushort)title.Entry.Id,
                    Revoked = true
                });
            }

            if (player is not Player) EnsureActiveTitleIsOwned();
        }

        /// <summary>
        /// Send all owned titles to client.
        /// </summary>
        public void SendTitles()
        {
            if (player is Player) { SendPublishedTitles(); return; }
            player.Session.EnqueueMessageEncrypted(new ServerTitles
            {
                Titles = titles.Values.Select(t => new ServerTitles.Title
                {
                    TitleId       = (ushort)t.Entry.Id,
                    Revoked       = t.Revoked,
                    TimeRemaining = (uint)(t.TimeRemaining ?? 0d)
                }).ToList()
            });
        }

        /// <summary>
        /// Add all available titles.
        /// </summary>
        /// <remarks>
        /// This is only used debug/command purposes.
        /// </remarks>
        public void AddAllTitles()
        {
            CharacterTitleEntry[] entries = gameTableManager.CharacterTitle?.Entries;
            if (entries == null)
            {
                SendTitles();
                return;
            }

            ushort[] titleIds = entries
                .Select(entry => (ushort)entry.Id)
                .ToArray();

            foreach (ushort titleId in titleIds)
                AddTitle(titleId, true);

            // client crashes if too many single title updates are sent, send as bulk
            if (player is Player) { lock (persistence.Gate) RetainNotice(new ListTitles(CapturePersistence())); }
            else SendTitles();
        }

        /// <summary>
        /// Remove all available titles.
        /// </summary>
        /// <remarks>
        /// This is only used debug/command purposes.
        /// </remarks>
        public void RevokeAllTitles()
        {
            ushort[] titleIds = titles.Values
                .Select(entry => (ushort)entry.Entry.Id)
                .ToArray();

            foreach (ushort titleId in titleIds)
                RevokeTitle(titleId, true);

            // client crashes if too many single title updates are sent, send as bulk
            if (player is Player) { lock (persistence.Gate) RetainNotice(new ListTitles(CapturePersistence())); }
            else SendTitles();
        }

        private void EnsureActiveTitleIsOwned()
        {
            if (activeTitleId != 0 && (!titles.TryGetValue(activeTitleId, out ITitle title) || title.Revoked))
                ActiveTitleId = 0;
        }

        /// <summary>
        /// Returns if title with supplied id is owned.
        /// </summary>
        public bool HasTitle(ushort id)
        {
            return titles.TryGetValue(id, out ITitle title) && !title.Revoked;
        }

        public IEnumerator<ITitle> GetEnumerator()
        {
            return titles.Values
                .Where(t => !t.Revoked)
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
