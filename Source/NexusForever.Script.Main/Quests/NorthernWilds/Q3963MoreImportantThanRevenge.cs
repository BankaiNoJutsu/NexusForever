using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
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
        private readonly HashSet<ulong> pendingDeparturePlayers = [];
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
            log.LogDebug("Loaded quest {QuestId} for character {CharacterId}.", owner.Id, owner.Player.CharacterId);
        }

        public void OnQuestStateChange(QuestState newState, QuestState oldState)
        {
            log.LogDebug("Quest {QuestId} state: {OldState} -> {NewState}.", owner.Id, oldState, newState);

            if (newState != QuestState.Achieved || oldState is QuestState.Achieved or QuestState.Completed)
                return;

            TryDepartToAlgoroc();
        }

        private void TryDepartToAlgoroc()
        {
            IPlayer player = owner.Player;
            WorldLocation2Entry destination = gameTableManager.WorldLocation2.GetEntry(AlgorocDepartureWorldLocationId);
            if (destination == null)
            {
                log.LogWarning("Quest {QuestId} achieved but Algoroc departure world location {WorldLocationId} is missing.",
                    owner.Id,
                    AlgorocDepartureWorldLocationId);
                return;
            }

            if (!player.CanTeleport())
            {
                log.LogWarning("Quest {QuestId} achieved but player {CharacterId} cannot depart to Algoroc - CanTeleport returned false.",
                    owner.Id,
                    player.CharacterId);
                return;
            }

            if (!pendingDeparturePlayers.Add(player.CharacterId))
            {
                log.LogDebug("Quest {QuestId} ignored duplicate Algoroc departure request for player {CharacterId}.",
                    owner.Id,
                    player.CharacterId);
                return;
            }

            player.CinematicManager.QueueCinematic(cinematicFactory.CreateCinematic<IQ3963MoreImportantThanRevengeDepartureCinematic>());
            log.LogInformation("Quest {QuestId} achieved - queued Northern Wilds departure cinematic for player {CharacterId}; delayed Algoroc transport to world location {WorldLocationId}.",
                owner.Id,
                player.CharacterId,
                AlgorocDepartureWorldLocationId);

            player.Session.Events.EnqueueEvent(new DelayEvent(AlgorocDepartureTeleportDelay, () =>
            {
                pendingDeparturePlayers.Remove(player.CharacterId);

                if (player.Map?.Entry?.Id != NorthernWildsWorldId)
                {
                    log.LogDebug("Quest {QuestId} skipped delayed Algoroc transport for player {CharacterId}: map={MapId}.",
                        owner.Id,
                        player.CharacterId,
                        player.Map?.Entry?.Id ?? 0u);
                    return;
                }

                if (!player.CanTeleport())
                {
                    log.LogDebug("Quest {QuestId} skipped delayed Algoroc transport for player {CharacterId}: teleport currently unavailable.",
                        owner.Id,
                        player.CharacterId);
                    return;
                }

                TeleportToAlgoroc(player, destination);
            }));
        }

        private void TeleportToAlgoroc(IPlayer player, WorldLocation2Entry destination)
        {
            player.Rotation = ToEulerDegrees(new Quaternion(destination.Facing0, destination.Facing1, destination.Facing2, destination.Facing3));
            player.TeleportTo(
                (ushort)destination.WorldId,
                destination.Position0,
                destination.Position1,
                destination.Position2,
                reason: TeleportReason.Relocate);

            log.LogInformation("Quest {QuestId} achieved - teleported player {CharacterId} to Algoroc departure world location {WorldLocationId}: world={WorldId}, position=({X}, {Y}, {Z}).",
                owner.Id,
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
