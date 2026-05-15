using System;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Static;
using NexusForever.Game.Static.Support;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Support;

namespace NexusForever.WorldServer.Network.Message.Handler.Support
{
    public class ClientIncidentReportHandler : IMessageHandler<IWorldSession, ClientIncidentReport>
    {
        private readonly ILogger<ClientIncidentReportHandler> log;

        public ClientIncidentReportHandler(ILogger<ClientIncidentReportHandler> log)
        {
            this.log = log;
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

            log.LogDebug("Ignoring unsupported incident report from player {PlayerGuid}: identity {Identity}, reason {Reason}, source {Source}, object id {ObjectId}, days ago {DaysAgo}, permanent ignore {PermanentIgnore}, note length {NoteLength}.",
                session.Player?.Guid, incidentReport.Identity, incidentReport.Reason, incidentReport.Source,
                incidentReport.ObjectId, incidentReport.DaysAgo, incidentReport.PermanentIgnore, incidentReport.Note?.Length ?? 0);
        }
    }

    public class ClientSupportTicketHandler : IMessageHandler<IWorldSession, ClientSupportTicket>
    {
        private readonly ILogger<ClientSupportTicketHandler> log;

        public ClientSupportTicketHandler(ILogger<ClientSupportTicketHandler> log)
        {
            this.log = log;
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

            log.LogDebug("Rejecting unsupported support ticket from player {PlayerGuid}: category {Category}, subcategory {SubCategory}, position ({X}, {Y}, {Z}), subject length {SubjectLength}, body length {BodyLength}, language {Language}.",
                session.Player?.Guid, supportTicket.TicketCategoryId, supportTicket.TicketSubCategoryId,
                supportTicket.Position.X, supportTicket.Position.Y, supportTicket.Position.Z,
                supportTicket.Subject?.Length ?? 0, supportTicket.Body?.Length ?? 0, supportTicket.LanguageId);

            SendResult(session, false);
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

        public ClientReportBugHandler(ILogger<ClientReportBugHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientReportBug reportBug)
        {
            if (!SupportPacketValidation.IsDefined(reportBug.BugCategoryId))
            {
                log.LogWarning("Ignoring bug report from player {PlayerGuid}: invalid category {Category}.",
                    session.Player?.Guid, reportBug.BugCategoryId);
                return;
            }

            log.LogDebug("Ignoring unsupported bug report from player {PlayerGuid}: category {Category}, selected unit {SelectedUnitId}, quest {Quest2Id}, description length {DescriptionLength}.",
                session.Player?.Guid, reportBug.BugCategoryId, reportBug.SelectedUnitId, reportBug.Quest2Id,
                reportBug.Description?.Length ?? 0);
        }
    }

    public class ClientStuckHandler : IMessageHandler<IWorldSession, ClientStuck>
    {
        private readonly ILogger<ClientStuckHandler> log;

        public ClientStuckHandler(ILogger<ClientStuckHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientStuck stuck)
        {
            if (!SupportPacketValidation.IsDefined(stuck.UnstickingType))
            {
                log.LogWarning("Ignoring stuck request from player {PlayerGuid}: invalid unstick type {UnstickType}, context token {ContextToken}.",
                    session.Player?.Guid, stuck.UnstickingType, stuck.ContextToken);
                return;
            }

            log.LogDebug("Ignoring unsupported stuck request from player {PlayerGuid}: type {UnstickType}, context token {ContextToken}.",
                session.Player?.Guid, stuck.UnstickingType, stuck.ContextToken);
        }
    }

    public class ClientSuggestHandler : IMessageHandler<IWorldSession, ClientSuggest>
    {
        private readonly ILogger<ClientSuggestHandler> log;

        public ClientSuggestHandler(ILogger<ClientSuggestHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientSuggest suggest)
        {
            log.LogDebug("Ignoring unsupported suggestion from player {PlayerGuid}: text length {SuggestionLength}.",
                session.Player?.Guid, suggest.SuggestionText?.Length ?? 0);
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
