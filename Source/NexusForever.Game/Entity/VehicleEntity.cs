using System.Collections;
using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Map;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.PlayerPath;
using NetworkVehiclePassenger = NexusForever.Network.World.Message.Model.Shared.VehiclePassenger;

namespace NexusForever.Game.Entity
{
    public class VehicleEntity : WorldEntity, IVehicleEntity
    {
        public override EntityType Type => EntityType.Vehicle;

        public UnitVehicleEntry VehicleEntry { get; private set; }
        public Spell4Entry SpellEntry { get; private set; }

        private sealed record PassengerAdmission(IVehiclePassenger Seat, IPlayer Player, IGameSession Session, ulong? Admission);
        private readonly List<PassengerAdmission> passengers = new();
        private readonly Queue<PassengerAdmission> pendingAdd = new();
        protected IEnumerable<IVehiclePassenger> Passengers => passengers.Select(passenger => passenger.Seat);
        protected bool HasPassengers => passengers.Count != 0;

        #region Dependency Injection

        public VehicleEntity(IMovementManager movementManager)
            : base(movementManager)
        {
        }

        #endregion

        public void Initialise(uint creatureId, uint vehicleId, uint spell4Id)
        {
            Initialise(creatureId);

            VehicleEntry = GetGameTableManager().UnitVehicle.GetEntry(vehicleId != 0u ? vehicleId : CreatureEntry.UnitVehicleId);
            SpellEntry   = GetGameTableManager().Spell4.GetEntry(spell4Id);

            // temp
            SetBaseProperty(Property.BaseHealth, 800.0f);

            SetStat(Stat.Health, 800u);
            SetStat(Stat.Level, 3u);
            SetStat(Stat.Sheathed, 800u);
        }

        protected override IEntityModel BuildEntityModel()
        {
            return new VehicleEntityModel
            {
                CreatureId    = CreatureEntry.Id,
                UnitVehicleId = (ushort)VehicleEntry.Id,
                Passengers    = Passengers
                    .Select(p => new NetworkVehiclePassenger
                    {
                        SeatType     = p.SeatType,
                        SeatPosition = p.SeatPosition,
                        UnitId       = p.Guid
                    })
                    .ToList()
            };
        }

        public override void OnRemoveFromMap()
        {
            while (pendingAdd.TryDequeue(out var pending))
                OnPassengerAdmissionRejected(pending.Player, pending.Seat.SeatType, removingVehicle: true);

            foreach (var passenger in passengers.ToArray())
                RemovePassenger(passenger);

            base.OnRemoveFromMap();
        }

        public override void OnRelocate(Vector3 vector)
        {
            foreach (var admission in passengers)
            {
                var player = admission.Player;
                if (!OwnsAttachedRider(admission) || !PlayerMapAdmissionGuard.IsCurrent(player, admission.Admission))
                    continue;

                if (Map is BaseMap map)
                    map.EnqueuePassengerRelocate(player, vector, admission.Admission,
                        () => passengers.Contains(admission) && OwnsAttachedRider(admission));
                else
                    Map.EnqueueRelocate(player, vector);
            }

            base.OnRelocate(vector);
        }

        public override void Update(double lastTick)
        {
            base.Update(lastTick);

            // passengers are delay added to make sure the vehicle exists at the client
            while (pendingAdd.TryDequeue(out var passenger))
                PassengerAdd(passenger);
        }

        /// <summary>
        /// Return <see cref="IVehiclePassenger"/> with supplied guid.
        /// </summary>
        public IVehiclePassenger GetPassenger(uint guid)
        {
            return Passengers.SingleOrDefault(p => p.Guid == guid);
        }

        /// <summary>
        /// Return <see cref="IVehiclePassenger"/> with supplied <see cref="VehicleSeatType"/> and seat position.
        /// </summary>
        public IVehiclePassenger GetPassenger(VehicleSeatType seatType, byte seatPosition)
        {
            return Passengers.SingleOrDefault(p => p.SeatType == seatType && p.SeatPosition == seatPosition);
        }

        /// <summary>
        /// Enqueue <see cref="IPlayer"/> to be added as a passenger with supplied <see cref="VehicleSeatType"/> and seat position.
        /// </summary>
        public void EnqueuePassengerAdd(IPlayer player, VehicleSeatType seatType, byte seatPosition)
        {
            if (seatType >= VehicleSeatType.Invalid)
                throw new ArgumentOutOfRangeException();

            byte passengerCount = (byte)passengers.Count(p => p.Seat.SeatType == seatType);
            switch (seatType)
            {
                case VehicleSeatType.Pilot:
                    if (passengerCount >= VehicleEntry.NumberPilots)
                        throw new ArgumentException();
                    break;
                case VehicleSeatType.Passenger:
                    if (passengerCount >= VehicleEntry.NumberPassengers)
                        throw new ArgumentException();
                    break;
                case VehicleSeatType.Gunner:
                    if (passengerCount >= VehicleEntry.NumberGunners)
                        throw new ArgumentException();
                    break;
            }

            if (GetPassenger(seatType, seatPosition) != null)
                throw new InvalidOperationException();

            if (pendingAdd.Any(p => p.Seat.Guid == player.Guid))
                throw new InvalidOperationException();

            pendingAdd.Enqueue(new(new VehiclePassenger(seatType, seatPosition, player.Guid),
                player, player.Session, PlayerMapAdmissionGuard.Capture(player)));
        }

        private void PassengerAdd(PassengerAdmission pending)
        {
            var player = pending.Player;
            var seat = pending.Seat;
            bool seated = false;
            PlayerMapAdmissionGuard.Run(player, pending.Admission, () =>
            {
                if (Map == null || !ReferenceEquals(player.Map, Map) || player.Guid != seat.Guid || player.PlatformGuid != null
                    || !ReferenceEquals(GetVisible<IPlayer>(seat.Guid), player) || pending.Session == null
                    || !ReferenceEquals(player.Session, pending.Session)
                    || (pending.Session is IPlayerSession bound && !ReferenceEquals(bound.Player, player))
                    || !CanSeatPassenger(player, seat.SeatType))
                    return;

                SeatPassenger(pending);
                seated = true;
            });
            if (!seated) OnPassengerAdmissionRejected(player, seat.SeatType, removingVehicle: false);
        }

        protected virtual bool CanSeatPassenger(IPlayer player, VehicleSeatType seatType) => true;

        protected virtual void OnPassengerAdmissionRejected(IPlayer player, VehicleSeatType seatType, bool removingVehicle) { }

        private void SeatPassenger(PassengerAdmission admission)
        {
            var player = admission.Player;
            var passenger = admission.Seat;
            player.Session.EnqueueMessageEncrypted(new ServerPathScientistUnitScanParameters
            {
                UnitId = Guid,
                ScanRewardFlags  = 0,
                IsScannable  = true
            });

            player.Session.EnqueueMessageEncrypted(new ServerUiWindowState
            {
            });

            // sets vehicle guid, seat type and seat position to local self entity at client
            // might not be correct as ServerVehiclePassengerAdd does this too, used for changing seats instead?
            player.Session.EnqueueMessageEncrypted(new ServerVehiclePassengerSelf
            {
                Self         = passenger.Guid,
                Vehicle      = Guid,
                SeatType     = passenger.SeatType,
                SeatPosition = passenger.SeatPosition
            });

            EnqueueToVisible(new ServerVehiclePassengerAdd
            {
                Self         = Guid,
                SeatType     = passenger.SeatType,
                SeatPosition = passenger.SeatPosition,
                UnitId       = passenger.Guid
            }, true);

            if (passenger.SeatType == VehicleSeatType.Pilot)
            {
                player.SetControl(this);

                EnqueueToVisible(new ServerEntityFaction
                {
                    UnitId  = Guid,
                    Faction = player.Faction1
                }, true);

                Faction1 = player.Faction1;
                Faction2 = player.Faction2;
            }

            player.SetPlatform(this);
            player.TryRecoverStarterTutorialQuestProgression();
            player.SyncStarterTutorialEntityVisibility();

            passengers.Add(admission);
            OnPassengerAdd(player, passenger.SeatType, passenger.SeatPosition);
        }

        /// <summary>
        /// Invoked when <see cref="IPlayer"/> is added as a passenger to <see cref="VehicleSeatType"/> and seat position.
        /// </summary>
        protected virtual void OnPassengerAdd(IPlayer player, VehicleSeatType seatType, byte seatPosition)
        {
            // deliberately empty
        }

        /// <summary>
        /// Remove <see cref="IPlayer"/> as a passenger.
        /// </summary>
        public void PassengerRemove(IPlayer player)
        {
            if (!TryRemovePassenger(player))
                throw new ArgumentException();
        }

        internal bool TryRemovePassenger(IPlayer player)
        {
            var passenger = passengers.SingleOrDefault(row => row.Seat.Guid == player.Guid && ReferenceEquals(row.Player, player));
            if (passenger == null)
                return false;

            bool detached = RemovePassenger(passenger);
            if (!HasPassengers && Map != null)
                RemoveFromMap();
            return detached;
        }

        private bool RemovePassenger(PassengerAdmission admission)
        {
            if (!passengers.Remove(admission))
                return false;

            var player = admission.Player;
            var passenger = admission.Seat;
            // The generic platform cleanup also uses unit IDs. Retire this seat's
            // platform entry before base teardown can resolve it to another actor.
            RemovePlatformPassenger(passenger.Guid);
            if (!OwnsAttachedRider(admission))
                return false;

            player.SetPlatform(null);

            if (passenger.SeatType == VehicleSeatType.Pilot && (player.ControlGuid == Guid || player.ControlGuid == null))
                player.SetControl(player);

            OnPassengerRemove(player, passenger.SeatType, passenger.SeatPosition);

            EnqueueToVisible(new ServerVehiclePassengerRemove
            {
                Self      = Guid,
                Passenger = passenger.Guid
            }, true);

            return true;
        }

        private bool OwnsAttachedRider(PassengerAdmission admission)
        {
            var player = admission.Player;
            return Map != null && ReferenceEquals(player.Map, Map) && player.Guid == admission.Seat.Guid && player.PlatformGuid == Guid
                && ReferenceEquals(player.Session, admission.Session)
                && (admission.Session is not IPlayerSession bound || ReferenceEquals(bound.Player, player));
        }

        /// <summary>
        /// Invoked when <see cref="IPlayer"/> is removed as a passenger from <see cref="VehicleSeatType"/> and seat position.
        /// </summary>
        protected virtual void OnPassengerRemove(IPlayer player, VehicleSeatType seatType, byte seatPosition)
        {
            // deliberately empty
        }

        public IEnumerator<IVehiclePassenger> GetEnumerator()
        {
            return Passengers.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
