using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using NexusForever.Database.World.Model;
using NexusForever.Shared.Network.Message;
using NexusForever.WorldServer.Game.Entity.Movement;
using NexusForever.WorldServer.Game.Entity.Network;
using NexusForever.WorldServer.Game.Entity.Static;
using NexusForever.WorldServer.Game.Map;
using NexusForever.WorldServer.Game.Reputation;
using NexusForever.WorldServer.Game.Reputation.Static;
using NexusForever.WorldServer.Network.Message.Model;
using NexusForever.WorldServer.Network.Message.Model.Shared;

namespace NexusForever.WorldServer.Game.Entity
{
    public abstract class WorldEntity : GridEntity
    {
        public EntityType Type { get; }
        public EntityCreateFlag CreateFlags { get; set; }
        public Vector3 Rotation { get; set; } = Vector3.Zero;
        public Dictionary<Property, PropertyValue> Properties { get; } = new();

        public uint EntityId { get; protected set; }
        public uint CreatureId { get; protected set; }
        public uint DisplayInfo { get; protected set; }
        public ushort OutfitInfo { get; protected set; }
        public Faction Faction1 { get; set; }
        public Faction Faction2 { get; set; }

        public ulong ActivePropId { get; private set; }
        public ushort WorldSocketId { get; private set; }

        public Vector3 LeashPosition { get; protected set; }
        public float LeashRange { get; protected set; } = 15f;
        public MovementManager MovementManager { get; private set; }

        public uint Health
        {
            get => GetStatInteger(Stat.Health) ?? 0u;
            private set => SetStat(Stat.Health, Math.Clamp(value, 0u, MaxHealth));
        }

        public uint MaxHealth
        {
            get => (uint)GetPropertyValue(Property.BaseHealth);
            set => SetProperty(Property.BaseHealth, value);
        }

        public uint Shield
        {
            get => GetStatInteger(Stat.Shield) ?? 0u;
            set => SetStat(Stat.Shield, Math.Clamp(value, 0u, MaxShieldCapacity)); // TODO: Handle overshield
        }

        public uint MaxShieldCapacity
        {
            get => (uint)GetPropertyValue(Property.ShieldCapacityMax);
            set => SetProperty(Property.ShieldCapacityMax, value);
        }

        public bool IsAlive => Health > 0u && DeathState is not DeathState.JustDied or DeathState.Corpse or DeathState.Dead;

        public uint Level
        {
            get => GetStatInteger(Stat.Level) ?? 1u;
            set => SetStat(Stat.Level, value);
        }

        public bool Sheathed
        {
            get => Convert.ToBoolean(GetStatInteger(Stat.Sheathed) ?? 0u);
            set => SetStat(Stat.Sheathed, Convert.ToUInt32(value));
        }

        /// <summary>
        /// Guid of the <see cref="WorldEntity"/> currently targeted.
        /// </summary>
        public uint TargetGuid { get; set; }

        /// <summary>
        /// Guid of the <see cref="Player"/> currently controlling this <see cref="WorldEntity"/>.
        /// </summary>
        public uint ControllerGuid { get; set; }

        protected readonly Dictionary<Stat, StatValue> stats = new();

        private readonly Dictionary<ItemSlot, ItemVisual> itemVisuals = new();

        protected DeathState DeathState 
        {
            get => deathState;
            private set
            {
                deathState = value;
                OnDeathStateChange(value);
            }
        }
        private DeathState deathState = DeathState.JustSpawned;

        /// <summary>
        /// Create a new <see cref="WorldEntity"/> with supplied <see cref="EntityType"/>.
        /// </summary>
        protected WorldEntity(EntityType type)
        {
            Type = type;
        }

        /// <summary>
        /// Initialise <see cref="WorldEntity"/> from an existing database model.
        /// </summary>
        public virtual void Initialise(EntityModel model)
        {
            EntityId     = model.Id;
            CreatureId   = model.Creature;
            Rotation     = new Vector3(model.Rx, model.Ry, model.Rz);
            DisplayInfo  = model.DisplayInfo;
            OutfitInfo   = model.OutfitInfo;
            Faction1     = (Faction)model.Faction1;
            Faction2     = (Faction)model.Faction2;
            ActivePropId = model.ActivePropId;
            WorldSocketId = model.WorldSocketId;

            foreach (EntityStatModel statModel in model.EntityStat)
                stats.Add((Stat)statModel.Stat, new StatValue(statModel));

            if (Health > MaxHealth)
                MaxHealth = Health;
        }

        public override void OnAddToMap(BaseMap map, uint guid, Vector3 vector)
        {
            LeashPosition   = vector;
            MovementManager = new MovementManager(this, vector, Rotation);
            base.OnAddToMap(map, guid, vector);
        }

        public override void OnRemoveFromMap()
        {
            base.OnRemoveFromMap();
            MovementManager = null;
        }

        /// <summary>
        /// Invoked each world tick with the delta since the previous tick occurred.
        /// </summary>
        public override void Update(double lastTick)
        {
            MovementManager.Update(lastTick);
        }

        protected abstract IEntityModel BuildEntityModel();

        public virtual ServerEntityCreate BuildCreatePacket()
        {
            var entityCreatePacket = new ServerEntityCreate
            {
                Guid         = Guid,
                Type         = Type,
                EntityModel  = BuildEntityModel(),
                CreateFlags  = (byte)CreateFlags,
                Stats        = stats.Values.ToList(),
                Commands     = MovementManager.ToList(),
                VisibleItems = itemVisuals.Values.ToList(),
                Properties   = Properties.Values.ToList(),
                Faction1     = Faction1,
                Faction2     = Faction2,
                DisplayInfo  = DisplayInfo,
                OutfitInfo   = OutfitInfo
            };

            // Plugs should not have this portion of the packet set by this Class. The Plug Class should set it itself.
            // This is in large part due to the way Plugs are tied either to a DecorId OR Guid. Other entities do not have the same issue.
            if (!(this is Plug))
            {
                if (ActivePropId > 0 || WorldSocketId > 0)
                {
                    entityCreatePacket.WorldPlacementData = new ServerEntityCreate.WorldPlacement
                    {
                        Type = 1,
                        ActivePropId = ActivePropId,
                        SocketId = WorldSocketId
                    };
                }
            }

            return entityCreatePacket;
        }

        // TODO: research the difference between a standard activation and cast activation

        /// <summary>
        /// Invoked when <see cref="WorldEntity"/> is activated.
        /// </summary>
        public virtual void OnActivate(Player activator)
        {
            // deliberately empty
        }

        /// <summary>
        /// Invoked when <see cref="WorldEntity"/> is cast activated.
        /// </summary>
        public virtual void OnActivateCast(Player activator)
        {
            // deliberately empty
        }

        protected void SetProperty(Property property, float value, float baseValue = 0.0f)
        {
            if (Properties.ContainsKey(property))
                Properties[property].Value = value;
            else
                Properties.Add(property, new PropertyValue(property, baseValue, value));
        }

        protected float? GetPropertyValue(Property property)
        {
            return Properties.ContainsKey(property) ? Properties[property].Value : default;
        }

        /// <summary>
        /// Return the <see cref="float"/> value of the supplied <see cref="Stat"/>.
        /// </summary>
        protected float? GetStatFloat(Stat stat)
        {
            StatAttribute attribute = EntityManager.Instance.GetStatAttribute(stat);
            if (attribute?.Type != StatType.Float)
                throw new ArgumentException();

            if (!stats.TryGetValue(stat, out StatValue statValue))
                return null;

            return statValue.Value;
        }

        /// <summary>
        /// Return the <see cref="uint"/> value of the supplied <see cref="Stat"/>.
        /// </summary>
        protected uint? GetStatInteger(Stat stat)
        {
            StatAttribute attribute = EntityManager.Instance.GetStatAttribute(stat);
            if (attribute?.Type != StatType.Integer)
                throw new ArgumentException();

            if (!stats.TryGetValue(stat, out StatValue statValue))
                return null;

            return (uint)statValue.Value;
        }

        /// <summary>
        /// Return the <see cref="uint"/> value of the supplied <see cref="Stat"/> as an <see cref="Enum"/>.
        /// </summary>
        public T? GetStatEnum<T>(Stat stat) where T : struct, Enum
        {
            uint? value = GetStatInteger(stat);
            if (value == null)
                return null;

            return (T)Enum.ToObject(typeof(T), value.Value);
        }

        /// <summary>
        /// Set <see cref="Stat"/> to the supplied <see cref="float"/> value.
        /// </summary>
        protected void SetStat(Stat stat, float value)
        {
            StatAttribute attribute = EntityManager.Instance.GetStatAttribute(stat);
            if (attribute?.Type != StatType.Float)
                throw new ArgumentException();

            if (stats.TryGetValue(stat, out StatValue statValue))
                statValue.Value = value;
            else
            {
                statValue = new StatValue(stat, value);
                stats.Add(stat, statValue);
            }

            if (attribute.SendUpdate)
            {
                EnqueueToVisible(new ServerEntityStatUpdateFloat
                {
                    UnitId = Guid,
                    Stat   = statValue
                }, true);
            }
        }

        /// <summary>
        /// Set <see cref="Stat"/> to the supplied <see cref="uint"/> value.
        /// </summary>
        protected void SetStat(Stat stat, uint value)
        {
            StatAttribute attribute = EntityManager.Instance.GetStatAttribute(stat);
            if (attribute?.Type != StatType.Integer)
                throw new ArgumentException();

            if (stats.TryGetValue(stat, out StatValue statValue))
                statValue.Value = value;
            else
            {
                statValue = new StatValue(stat, value);
                stats.Add(stat, statValue);
            }

            if (attribute.SendUpdate)
            {
                EnqueueToVisible(new ServerEntityStatUpdateInteger
                {
                    UnitId = Guid,
                    Stat   = statValue
                }, true);
            }
        }

        /// <summary>
        /// Set <see cref="Stat"/> to the supplied <see cref="Enum"/> value.
        /// </summary>
        protected void SetStat<T>(Stat stat, T value) where T : Enum, IConvertible
        {
            SetStat(stat, value.ToUInt32(null));
        }

        /// <summary>
        /// Update <see cref="ItemVisual"/> for multiple supplied <see cref="ItemSlot"/>.
        /// </summary>
        public void SetAppearance(IEnumerable<ItemVisual> visuals)
        {
            foreach (ItemVisual visual in visuals)
                SetAppearance(visual);
        }

        /// <summary>
        /// Update <see cref="ItemVisual"/> for supplied <see cref="ItemVisual"/>.
        /// </summary>
        public void SetAppearance(ItemVisual visual)
        {
            if (visual.DisplayId != 0)
            {
                if (!itemVisuals.ContainsKey(visual.Slot))
                    itemVisuals.Add(visual.Slot, visual);
                else
                    itemVisuals[visual.Slot] = visual;
            }
            else
                itemVisuals.Remove(visual.Slot);
        }

        public IEnumerable<ItemVisual> GetAppearance()
        {
            return itemVisuals.Values;
        }

        /// <summary>
        /// Update the display info for the <see cref="WorldEntity"/>, this overrides any other appearance changes.
        /// </summary>
        public void SetDisplayInfo(uint displayInfo)
        {
            DisplayInfo = displayInfo;

            EnqueueToVisible(new ServerEntityVisualUpdate
            {
                UnitId      = Guid,
                DisplayInfo = DisplayInfo
            }, true);
        }

        /// <summary>
        /// Enqueue broadcast of <see cref="IWritable"/> to all visible <see cref="Player"/>'s in range.
        /// </summary>
        public void EnqueueToVisible(IWritable message, bool includeSelf = false)
        {
            // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
            foreach (WorldEntity entity in visibleEntities.Values)
            {
                if (!(entity is Player player))
                    continue;

                if (!includeSelf && (Guid == entity.Guid || ControllerGuid == entity.Guid))
                    continue;

                player.Session.EnqueueMessageEncrypted(message);
            }
        }

        /// <summary>
        /// Return <see cref="Disposition"/> between <see cref="WorldEntity"/> and <see cref="Faction"/>.
        /// </summary>
        public virtual Disposition GetDispositionTo(Faction factionId, bool primary = true)
        {
            FactionNode targetFaction = FactionManager.Instance.GetFaction(factionId);
            if (targetFaction == null)
                throw new ArgumentException($"Invalid faction {factionId}!");

            // find disposition based on faction friendships
            Disposition? dispositionFromFactionTarget = GetDispositionFromFactionFriendship(targetFaction, primary ? Faction1 : Faction2);
            if (dispositionFromFactionTarget.HasValue)
                return dispositionFromFactionTarget.Value;

            FactionNode invokeFaction = FactionManager.Instance.GetFaction(primary ? Faction1 : Faction2);
            Disposition? dispositionFromFactionInvoker = GetDispositionFromFactionFriendship(invokeFaction, factionId);
            if (dispositionFromFactionInvoker.HasValue)
                return dispositionFromFactionInvoker.Value;

            // TODO: client does a few more checks, might not be 100% accurate

            // default to neutral if we have no disposition from other sources
            return Disposition.Neutral;
        }

        private Disposition? GetDispositionFromFactionFriendship(FactionNode node, Faction factionId)
        {
            if (node == null)
                return null;

            // check if current node has required friendship
            FactionLevel? level = node.GetFriendshipFactionLevel(factionId);
            if (level.HasValue)
                return FactionNode.GetDisposition(level.Value);

            // check if parent node has required friendship
            return GetDispositionFromFactionFriendship(node.Parent, factionId);
        }

        /// <summary>
        /// Modify this Entity's Health by the given value (Negative for Damage, Positive for Healing).
        /// </summary>
        public void ModifyHealth(long health)
        {
            int currentHealth = (int)Health;
            int newHealth = (int)(Health + health);

            if (newHealth <= 0)
                Health = 0;
            else if (newHealth > MaxHealth)
                Health = MaxHealth;
            else
                Health = (uint)newHealth;

            UpdateHealthMask mask = (UpdateHealthMask)4;
            if (Health <= 0 && health < 0)
                mask = (UpdateHealthMask)128;

            EnqueueToVisible(new ServerEntityHealthUpdate
            {
                UnitId = Guid,
                Health = Health
            });

            if (this is Player)
                EnqueueToVisible(new ServerPlayerHealthUpdate
                {
                    UnitId = Guid,
                    Health = Health,
                    Mask = mask
                }, true);

            if (currentHealth == 0u && newHealth > 0) // Resurrecting
            {
                SendDeathPacket(false, 2);
                SetDeathState(DeathState.JustSpawned);
            }
            else if (currentHealth > 0u && newHealth <= 0u) // Dying
            {
                SendDeathPacket(true, 7);
                SetDeathState(DeathState.JustDied);
            }
        }

        private void SendDeathPacket(bool isDead, byte deathReason)
        {
            EnqueueToVisible(new ServerEntityDeath
            {
                UnitId = Guid,
                Dead = isDead,
                Reason = deathReason,
                RezHealth = isDead ? 0u : Health
            }, true);
        }

        protected void SetDeathState(DeathState newState)
        {
            DeathState = newState;

            switch (newState)
            {
                case DeathState.JustSpawned:
                    // Do stuff on spawn
                    SetDeathState(DeathState.Alive);
                    break;
                case DeathState.Alive:
                    // Do stuff on alive
                    break;
                case DeathState.JustDied:
                    SetDeathState(DeathState.Corpse);
                    break;
                case DeathState.Corpse:
                    // Do stuff on corpse
                    break;
                case DeathState.Dead:
                    // Do stuff when entering dead state
                    if (this is Player)
                        throw new InvalidOperationException("Invalid Death State for a Player!");
                    break;
                default:
                    break;
            }
        }

        protected virtual void OnDeathStateChange(DeathState newState)
        {
            // Deliberately empty
        }
        
        protected virtual void OnDeath(UnitEntity killer)
        {
            // Deliberately empty
        }
    }
}
