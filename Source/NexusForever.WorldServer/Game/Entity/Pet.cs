using System.Linq;
using System.Numerics;
using NexusForever.Shared;
using NexusForever.Shared.Game;
using NexusForever.Shared.GameTable;
using NexusForever.Shared.GameTable.Model;
using NexusForever.WorldServer.Game.Entity.Network;
using NexusForever.WorldServer.Game.Entity.Network.Model;
using NexusForever.WorldServer.Game.Entity.Static;
using NexusForever.WorldServer.Game.Map;
using NexusForever.WorldServer.Game.Spell;
using NexusForever.WorldServer.Network.Message.Model;
using NLog;
using NetworkPet = NexusForever.WorldServer.Network.Message.Model.Shared.Pet;

namespace NexusForever.WorldServer.Game.Entity
{
    public class Pet : WorldEntity
    {
        private const float FollowDistance = 3f;
        private const float FollowMinRecalculateDistance = 5f;

        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        public uint OwnerGuid { get; private set; }
        public Creature2Entry Creature { get; }
        public Creature2DisplayGroupEntryEntry Creature2DisplayGroup { get; }
        public uint CastingId { get; private set; }

        private uint spell4BaseId;

        private readonly UpdateTimer followTimer = new UpdateTimer(1d);

        public Pet(Player owner, uint creature, uint castingId, uint spell4BaseId)
            : base(EntityType.Pet)
        {
            OwnerGuid               = owner.Guid;
            CastingId               = castingId;
            Creature                = GameTableManager.Instance.Creature2.GetEntry(creature);
            Creature2DisplayGroup   = GameTableManager.Instance.Creature2DisplayGroupEntry.Entries.SingleOrDefault(x => x.Creature2DisplayGroupId == Creature.Creature2DisplayGroupId);
            DisplayInfo             = Creature2DisplayGroup?.Creature2DisplayInfoId ?? 0u;
            this.spell4BaseId       = spell4BaseId;

            SetProperty(Property.BaseHealth, 800.0f);

            SetStat(Stat.Health, 800u);
            SetStat(Stat.Level, 3u);
            SetStat(Stat.Sheathed, 0u);
        }

        protected override IEntityModel BuildEntityModel()
        {
            return new PetEntityModel
            {
                CreatureId  = Creature.Id,
                OwnerId     = OwnerGuid,
                Name        = ""
            };
        }

        public override void OnAddToMap(BaseMap map, uint guid, Vector3 vector)
        {
            base.OnAddToMap(map, guid, vector);

            Player owner = GetVisible<Player>(OwnerGuid);
            if (owner == null)
            {
                // this shouldn't happen, log it anyway
                log.Error($"Pet {Guid} has lost it's owner {OwnerGuid}!");
                RemoveFromMap();
                return;
            }

            owner.SpellManager.GetSpell(spell4BaseId).SetPetUnitId(Guid);
            //owner.VanityPetGuid = Guid;

            owner.EnqueueToVisible(new Server08B3
            {
                MountGuid = Guid,
                Unknown0 = 0,
                Unknown1 = true
            }, true);

            // TODO: Move ActionBars to Actionbar Manager
            owner.Session.EnqueueMessageEncrypted(new ServerShowActionBar
            {
                ActionBarShortcutSetId = 299,
                ShortcutSet = Spell.Static.ShortcutSet.PrimaryPetBar,
                Guid = Guid
            });

            // TODO: Move ActionBars to Actionbar Manager
            owner.Session.EnqueueMessageEncrypted(new ServerShowActionBar
            {
                ActionBarShortcutSetId = 499,
                ShortcutSet = Spell.Static.ShortcutSet.PetMiniBar0,
                Guid = Guid
            });

            owner.Session.EnqueueMessageEncrypted(new ServerPlayerPet
            {
                Pet = new NetworkPet
                {
                    Guid = Guid,
                    Stance = 1,
                    ValidStances = 31,
                    SummoningSpell = CastingId
                }
            });

            owner.Session.EnqueueMessageEncrypted(new ServerChangePetStance
            {
                PetUnitId = Guid,
                Stance = 1
            });

            //owner.Session.EnqueueMessageEncrypted(new ServerCombatLog
            //{
            //    LogType = 26,
            //    PetData = new ServerCombatLog.PetLog
            //    {
            //        CasterId = owner.Guid,
            //        TargetId = Guid,
            //        SpellId = CastingId,
            //        CombatResult = 8
            //    }
            //});
        }

        public override void OnEnqueueRemoveFromMap()
        {
            followTimer.Reset(false);
            OwnerGuid = 0u;
        }

        public override void Update(double lastTick)
        {
            base.Update(lastTick);
            Follow(lastTick);
        }

        private void Follow(double lastTick)
        {
            followTimer.Update(lastTick);
            if (!followTimer.HasElapsed)
                return;

            Player owner = GetVisible<Player>(OwnerGuid);
            if (owner == null)
            {
                // this shouldn't happen, log it anyway
                log.Error($"VanityPet {Guid} has lost it's owner {OwnerGuid}!");
                RemoveFromMap();
                return;
            }

            // only recalculate the path to owner if distance is significant
            float distance = owner.Position.GetDistance(Position);
            if (distance < FollowMinRecalculateDistance)
                return;

            MovementManager.Follow(owner, FollowDistance);

            followTimer.Reset();
        }
    }
}
