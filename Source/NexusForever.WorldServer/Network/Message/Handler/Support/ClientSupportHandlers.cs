using System;
using Microsoft.Extensions.Logging;
using NexusForever.Game;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Housing;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Map;
using NexusForever.Game.Static;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Static.Support;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Support;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Support;

namespace NexusForever.WorldServer.Network.Message.Handler.Support
{
    public class ClientIncidentReportHandler : IMessageHandler<IWorldSession, ClientIncidentReport>
    {
        private readonly ILogger<ClientIncidentReportHandler> log;
        private readonly ISupportSubmissionStore submissionStore;

        public ClientIncidentReportHandler(
            ILogger<ClientIncidentReportHandler> log,
            ISupportSubmissionStore submissionStore)
        {
            this.log             = log;
            this.submissionStore = submissionStore;
        }

        public void HandleMessage(IWorldSession session, ClientIncidentReport incidentReport)
        {
            if (!SupportPacketValidation.IsDefined(incidentReport.Reason) ||
                !SupportPacketValidation.IsDefined(incidentReport.Source))
            {
                log.LogWarning("Ignoring incident report from player {PlayerGuid}: invalid reason {Reason} or source {Source}.",
                    session.Player?.Guid, incidentReport.Reason, incidentReport.Source);
                return;
            }

            bool stored = submissionStore.TryAppend(session, "incident", new
            {
                incidentReport.Identity,
                incidentReport.Reason,
                incidentReport.Source,
                incidentReport.ObjectId,
                incidentReport.DaysAgo,
                incidentReport.PermanentIgnore,
                incidentReport.Note
            });

            log.LogDebug("Stored incident report from player {PlayerGuid}: identity {Identity}, reason {Reason}, source {Source}, object id {ObjectId}, days ago {DaysAgo}, permanent ignore {PermanentIgnore}, note length {NoteLength}, stored {Stored}.",
                session.Player?.Guid, incidentReport.Identity, incidentReport.Reason, incidentReport.Source,
                incidentReport.ObjectId, incidentReport.DaysAgo, incidentReport.PermanentIgnore, incidentReport.Note?.Length ?? 0, stored);
        }
    }

    public class ClientSupportTicketHandler : IMessageHandler<IWorldSession, ClientSupportTicket>
    {
        private readonly ILogger<ClientSupportTicketHandler> log;
        private readonly ISupportSubmissionStore submissionStore;

        public ClientSupportTicketHandler(
            ILogger<ClientSupportTicketHandler> log,
            ISupportSubmissionStore submissionStore)
        {
            this.log             = log;
            this.submissionStore = submissionStore;
        }

        public void HandleMessage(IWorldSession session, ClientSupportTicket supportTicket)
        {
            if (!SupportPacketValidation.IsDefined(supportTicket.LanguageId))
            {
                log.LogWarning("Rejecting support ticket from player {PlayerGuid}: invalid language {Language}.",
                    session.Player?.Guid, supportTicket.LanguageId);
                SendResult(session, false);
                return;
            }

            bool stored = submissionStore.TryAppend(session, "ticket", new
            {
                supportTicket.TicketCategoryId,
                supportTicket.TicketSubCategoryId,
                Position = new
                {
                    supportTicket.Position.X,
                    supportTicket.Position.Y,
                    supportTicket.Position.Z
                },
                supportTicket.Subject,
                supportTicket.Body,
                supportTicket.LanguageId
            });

            log.LogDebug("Stored support ticket from player {PlayerGuid}: category {Category}, subcategory {SubCategory}, position ({X}, {Y}, {Z}), subject length {SubjectLength}, body length {BodyLength}, language {Language}, stored {Stored}.",
                session.Player?.Guid, supportTicket.TicketCategoryId, supportTicket.TicketSubCategoryId,
                supportTicket.Position.X, supportTicket.Position.Y, supportTicket.Position.Z,
                supportTicket.Subject?.Length ?? 0, supportTicket.Body?.Length ?? 0, supportTicket.LanguageId, stored);

            SendResult(session, stored);
        }

        private static void SendResult(IWorldSession session, bool success)
        {
            session.EnqueueMessageEncrypted(new ServerSupportTicketResult
            {
                Success = success
            });
        }
    }

    public class ClientReportBugHandler : IMessageHandler<IWorldSession, ClientReportBug>
    {
        private readonly ILogger<ClientReportBugHandler> log;
        private readonly ISupportSubmissionStore submissionStore;

        public ClientReportBugHandler(
            ILogger<ClientReportBugHandler> log,
            ISupportSubmissionStore submissionStore)
        {
            this.log             = log;
            this.submissionStore = submissionStore;
        }

        public void HandleMessage(IWorldSession session, ClientReportBug reportBug)
        {
            if (!SupportPacketValidation.IsDefined(reportBug.BugCategoryId))
            {
                log.LogWarning("Ignoring bug report from player {PlayerGuid}: invalid category {Category}.",
                    session.Player?.Guid, reportBug.BugCategoryId);
                return;
            }

            bool stored = submissionStore.TryAppend(session, "bug", new
            {
                reportBug.BugCategoryId,
                reportBug.SelectedUnitId,
                reportBug.Quest2Id,
                reportBug.Description
            });

            log.LogDebug("Stored bug report from player {PlayerGuid}: category {Category}, selected unit {SelectedUnitId}, quest {Quest2Id}, description length {DescriptionLength}, stored {Stored}.",
                session.Player?.Guid, reportBug.BugCategoryId, reportBug.SelectedUnitId, reportBug.Quest2Id,
                reportBug.Description?.Length ?? 0, stored);
        }
    }

    public class ClientStuckHandler : IMessageHandler<IWorldSession, ClientStuck>
    {
        private readonly ILogger<ClientStuckHandler> log;
        private readonly IGameTableManager gameTableManager;
        private readonly IGlobalResidenceManager globalResidenceManager;
        private readonly IMapLockManager mapLockManager;

        public ClientStuckHandler(
            ILogger<ClientStuckHandler> log,
            IGameTableManager gameTableManager,
            IGlobalResidenceManager globalResidenceManager,
            IMapLockManager mapLockManager)
        {
            this.log                    = log;
            this.gameTableManager       = gameTableManager;
            this.globalResidenceManager = globalResidenceManager;
            this.mapLockManager         = mapLockManager;
        }

        public void HandleMessage(IWorldSession session, ClientStuck stuck)
        {
            if (!SupportPacketValidation.IsDefined(stuck.UnstickingType))
            {
                log.LogWarning("Rejecting stuck request from player {PlayerGuid}: invalid unstick type {UnstickType}, context token {ContextToken}.",
                    session.Player?.Guid, stuck.UnstickingType, stuck.ContextToken);
                SendStuckCastResult(session, stuck.ContextToken, 0u, CastResult.SpellUnknown);
                return;
            }

            switch (stuck.UnstickingType)
            {
                case UnstickType.RecallTransmat:
                    RecallToZoneExit(session, stuck.ContextToken);
                    break;
                case UnstickType.RecallHouse:
                    RecallToResidence(session, stuck.ContextToken);
                    break;
                case UnstickType.FreeSuicide:
                    FreeSuicide(session, stuck.ContextToken);
                    break;
            }
        }

        private void RecallToZoneExit(IWorldSession session, uint contextToken)
        {
            uint spell4Id = SupportStuckSpell4Ids.RecallTransmat;
            if (!session.Player.CanTeleport())
            {
                SendStuckCastResult(session, contextToken, spell4Id, CastResult.PendingSpellCast);
                return;
            }

            uint worldLocation2Id = session.Player.Zone?.WorldLocation2IdExit ?? 0u;
            WorldLocation2Entry location = worldLocation2Id == 0u
                ? null
                : gameTableManager.WorldLocation2.GetEntry(worldLocation2Id);
            if (location == null)
            {
                log.LogWarning("Unable to process transmat stuck request for player {PlayerGuid}: zone exit world location {WorldLocation2Id} was not found, context token {ContextToken}.",
                    session.Player?.Guid, worldLocation2Id, contextToken);
                SendStuckCastResult(session, contextToken, spell4Id, CastResult.SpellPreRequisites);
                return;
            }

            log.LogDebug("Processing transmat stuck request for player {PlayerGuid}: zone {ZoneId}, world location {WorldLocation2Id}, context token {ContextToken}.",
                session.Player?.Guid, session.Player.Zone?.Id, worldLocation2Id, contextToken);
            session.Player.TeleportTo((ushort)location.WorldId, location.Position0, location.Position1, location.Position2);
        }

        private void RecallToResidence(IWorldSession session, uint contextToken)
        {
            uint spell4Id = SupportStuckSpell4Ids.RecallHouse;
            if (!session.Player.CanTeleport())
            {
                SendStuckCastResult(session, contextToken, spell4Id, CastResult.PendingSpellCast);
                return;
            }

            IResidence residence = globalResidenceManager.GetResidenceByOwner(session.Player.Name)
                ?? globalResidenceManager.CreateResidence(session.Player);
            if (residence == null)
            {
                log.LogWarning("Unable to process house stuck request for player {PlayerGuid}: residence could not be resolved, context token {ContextToken}.",
                    session.Player?.Guid, contextToken);
                SendStuckCastResult(session, contextToken, spell4Id, CastResult.SpellPreRequisites);
                return;
            }

            IResidenceEntrance entrance;
            try
            {
                entrance = globalResidenceManager.GetResidenceEntrance(residence.PropertyInfoId);
            }
            catch (HousingException)
            {
                log.LogWarning("Unable to process house stuck request for player {PlayerGuid}: residence entrance was not found for property {PropertyInfoId}, context token {ContextToken}.",
                    session.Player?.Guid, residence.PropertyInfoId, contextToken);
                SendStuckCastResult(session, contextToken, spell4Id, CastResult.SpellPreRequisites);
                return;
            }

            IMapLock mapLock = mapLockManager.GetResidenceLock(residence.Parent ?? residence);

            log.LogDebug("Processing house stuck request for player {PlayerGuid}: residence {ResidenceId}, context token {ContextToken}.",
                session.Player?.Guid, residence.Id, contextToken);
            session.Player.Rotation = entrance.Rotation.ToEuler();
            session.Player.TeleportTo(new MapPosition
            {
                Info = new MapInfo
                {
                    Entry   = entrance.Entry,
                    MapLock = mapLock
                },
                Position = entrance.Position
            });
        }

        private void FreeSuicide(IWorldSession session, uint contextToken)
        {
            uint spell4Id = SupportStuckSpell4Ids.FreeSuicide;
            if (!session.Player.IsAlive)
            {
                SendStuckCastResult(session, contextToken, spell4Id, CastResult.CasterCannotBeDead);
                return;
            }

            log.LogDebug("Processing free-suicide stuck request for player {PlayerGuid}: context token {ContextToken}.",
                session.Player?.Guid, contextToken);
            session.Player.ModifyHealth(Math.Max(session.Player.Health, 1u), DamageType.Physical, session.Player);
        }

        private static void SendStuckCastResult(IWorldSession session, uint contextToken, uint spell4Id, CastResult castResult)
        {
            session.EnqueueMessageEncrypted(new ServerSpellCastResult
            {
                Unknown0   = contextToken,
                Spell4Id   = spell4Id,
                CastResult = castResult
            });
        }
    }

    public class ClientSuggestHandler : IMessageHandler<IWorldSession, ClientSuggest>
    {
        private readonly ILogger<ClientSuggestHandler> log;
        private readonly ISupportSubmissionStore submissionStore;

        public ClientSuggestHandler(
            ILogger<ClientSuggestHandler> log,
            ISupportSubmissionStore submissionStore)
        {
            this.log             = log;
            this.submissionStore = submissionStore;
        }

        public void HandleMessage(IWorldSession session, ClientSuggest suggest)
        {
            bool stored = submissionStore.TryAppend(session, "suggestion", new
            {
                suggest.SuggestionText
            });

            log.LogDebug("Stored suggestion from player {PlayerGuid}: text length {SuggestionLength}, stored {Stored}.",
                session.Player?.Guid, suggest.SuggestionText?.Length ?? 0, stored);
        }
    }

    internal static class SupportPacketValidation
    {
        public static bool IsDefined<T>(T value) where T : struct, Enum
        {
            return Enum.IsDefined(typeof(T), value);
        }
    }
}
