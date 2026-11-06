using System;
using System.Numerics;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    /// <summary>
    /// Quest 3963 (More Important Than Revenge) - end of Shellshock! chain.
    /// Objectives: ActivateEntity targetGroup=7573 wl=45401/45402.
    /// No chain grant (end of chain).
    /// </summary>
    [ScriptFilterOwnerId(3963u)]
    public class Q3963MoreImportantThanRevengeQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private const ushort NorthernWildsWorldId = 426;
        private const uint AlgorocDepartureWorldLocationId = 9801u;
        private static readonly TimeSpan AlgorocDepartureTeleportDelay = TimeSpan.FromMilliseconds(26500d);

        private readonly ILogger<Q3963MoreImportantThanRevengeQuestScript> log;
        private readonly ICinematicFactory cinematicFactory;
        private readonly IGameTableManager gameTableManager;
        private sealed class Departure
        {
            public Func<bool> Current { get; init; }
            public bool Executed { get; set; }
        }

        private Departure pendingDeparture;
        private IQuest owner;

        public Q3963MoreImportantThanRevengeQuestScript(
            ILogger<Q3963MoreImportantThanRevengeQuestScript> log,
            ICinematicFactory cinematicFactory,
            IGameTableManager gameTableManager)
        {
            this.log              = log;
            this.cinematicFactory = cinematicFactory;
            this.gameTableManager = gameTableManager;
        }

        public void OnLoad(IQuest owner)
        {
            this.owner = owner;
            pendingDeparture = null;
            log.LogDebug("Loaded quest {QuestId} for character {CharacterId}.", owner.Id, owner.Player.CharacterId);
        }

        public void OnUnload()
        {
            pendingDeparture = null;
            owner = null;
        }

        public void OnQuestStateChange(QuestState newState, QuestState oldState)
        {
            if (owner == null)
                return;
            log.LogDebug("Quest {QuestId} state: {OldState} -> {NewState}.", owner.Id, oldState, newState);

            if (newState is not (QuestState.Achieved or QuestState.Completed))
                pendingDeparture = null;

            if (newState != QuestState.Achieved || oldState is QuestState.Achieved or QuestState.Completed)
                return;

            TryDepartToAlgoroc();
        }

        private void TryDepartToAlgoroc()
        {
            if (pendingDeparture?.Current() == true)
                return;

            pendingDeparture = null;
            IQuest quest = owner;
            IPlayer player = quest.Player;
            var session = player.Session;
            var events = session?.Events;
            var map = player.Map;
            var account = player.Account;
            var epoch = account?.WorldSessionEpoch;
            var character = player.CharacterId;
            var realm = player.Identity?.RealmId;
            var cinematics = player.CinematicManager;
            var admission = (player as IPlayerMapAdmission)?.MapAdmissionGeneration;
            bool OwnsDeparture() => ReferenceEquals(owner, quest) && !quest.PendingDelete
                && ReferenceEquals(quest.Player, player) && player.CharacterId == character && player.Identity?.RealmId == realm
                && ReferenceEquals(player.Session, session) && ReferenceEquals(session?.Events, events)
                && (session is not IPlayerSession bound || ReferenceEquals(bound.Player, player))
                && session is not NetworkSession { IsDisconnecting: true }
                && ReferenceEquals(player.Account, account) && account?.WorldSessionEpoch == epoch
                && (account == null || ReferenceEquals(account.Session, session))
                && ReferenceEquals(player.Map, map) && map?.Entry?.Id == NorthernWildsWorldId
                && ReferenceEquals(player.CinematicManager, cinematics)
                && (admission == null || ((IPlayerMapAdmission)player).IsMapAdmissionCurrent(admission.Value));

            if (events == null || !OwnsDeparture())
                return;

            WorldLocation2Entry destination = gameTableManager.WorldLocation2.GetEntry(AlgorocDepartureWorldLocationId);
            if (destination == null)
            {
                log.LogWarning("Quest {QuestId} achieved but Algoroc departure world location {WorldLocationId} is missing.",
                    quest.Id,
                    AlgorocDepartureWorldLocationId);
                return;
            }

            if (!player.CanTeleport() || !OwnsDeparture())
            {
                log.LogWarning("Quest {QuestId} achieved but player {CharacterId} cannot depart to Algoroc - CanTeleport returned false.",
                    quest.Id,
                    player.CharacterId);
                return;
            }

            var departure = new Departure { Current = OwnsDeparture };
            pendingDeparture = departure;
            bool Current() => ReferenceEquals(pendingDeparture, departure) && OwnsDeparture();
            void Retire()
            {
                if (ReferenceEquals(pendingDeparture, departure))
                    pendingDeparture = null;
            }

            try
            {
                var cinematic = cinematicFactory.CreateCinematic<IQ3963MoreImportantThanRevengeDepartureCinematic>();
                if (!Current())
                {
                    Retire();
                    return;
                }
                cinematics.QueueCinematic(cinematic);
                if (!Current())
                {
                    Retire();
                    return;
                }

                log.LogInformation("Quest {QuestId} achieved - queued Northern Wilds departure cinematic for player {CharacterId}; delayed Algoroc transport to world location {WorldLocationId}.",
                    quest.Id, character, AlgorocDepartureWorldLocationId);

                events.EnqueueEvent(new DelayEvent(AlgorocDepartureTeleportDelay, () =>
                {
                    if (departure.Executed)
                        return;
                    departure.Executed = true;
                    try
                    {
                        void Depart()
                        {
                            if (Current() && player.CanTeleport() && Current())
                                TeleportToAlgoroc(player, destination, Current);
                        }
                        if (player is IPlayerMapAdmission admitted)
                        {
                            lock (admitted.MapAdmissionSyncRoot)
                                Depart();
                        }
                        else
                            Depart();
                    }
                    finally { Retire(); }
                }));
            }
            catch
            {
                Retire();
                throw;
            }
        }

        private void TeleportToAlgoroc(IPlayer player, WorldLocation2Entry destination, Func<bool> current)
        {
            player.Rotation = ToEulerDegrees(new Quaternion(destination.Facing0, destination.Facing1, destination.Facing2, destination.Facing3));
            if (!current())
                return;
            player.TeleportTo(
                (ushort)destination.WorldId,
                destination.Position0,
                destination.Position1,
                destination.Position2,
                reason: TeleportReason.Relocate);

            log.LogInformation("Quest {QuestId} achieved - teleported player {CharacterId} to Algoroc departure world location {WorldLocationId}: world={WorldId}, position=({X}, {Y}, {Z}).",
                3963u,
                player.CharacterId,
                AlgorocDepartureWorldLocationId,
                destination.WorldId,
                destination.Position0,
                destination.Position1,
                destination.Position2);
        }

        private static Vector3 ToEulerDegrees(Quaternion q)
        {
            Vector3 vector = ToEuler(q);
            vector.X = ToDegrees(vector.X);
            vector.Y = ToDegrees(vector.Y);
            vector.Z = ToDegrees(vector.Z);
            return vector;
        }

        private static Vector3 ToEuler(Quaternion q)
        {
            float xx = q.X * q.X;
            float xy = q.X * q.Y;
            float xz = q.X * q.Z;
            float xw = q.X * q.W;
            float yy = q.Y * q.Y;
            float yz = q.Y * q.Z;
            float yw = q.Y * q.W;
            float zz = q.Z * q.Z;
            float zw = q.Z * q.W;

            float p = MathF.Asin(-2f * (yz - xw));
            float y = MathF.Atan2(2f * (xz + yw), 1f - 2f * (xx + yy));
            float r = MathF.Atan2(2f * (xy + zw), 1f - 2f * (xx + zz));
            return new Vector3(y, p, r);
        }

        private static float ToDegrees(float radians)
        {
            return radians * 180f / MathF.PI;
        }
    }
}
