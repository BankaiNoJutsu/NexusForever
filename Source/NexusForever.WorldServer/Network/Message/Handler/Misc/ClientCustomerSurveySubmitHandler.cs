using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Support;
using NexusForever.WorldServer.Support;

namespace NexusForever.WorldServer.Network.Message.Handler.Misc
{
    public class ClientCustomerSurveySubmitHandler : IMessageHandler<IWorldSession, ClientCustomerSurveySubmit>
    {
        private readonly ILogger<ClientCustomerSurveySubmitHandler> log;

        public ClientCustomerSurveySubmitHandler(ILogger<ClientCustomerSurveySubmitHandler> log)
        {
            this.log = log;
        }

        /// <summary>
        /// Client sends this when the user has filled out any customer survey.
        /// The response object contains the type of the survey, additional parameters and the answers of the user.
        /// </summary>
        public void HandleMessage(IWorldSession session, ClientCustomerSurveySubmit surveyResponse)
        {
            bool stored = SupportSubmissionStore.TryAppend(log, session, "customer-survey", new
            {
                surveyResponse.CustomerSurveyId,
                surveyResponse.Comment,
                SurveyModel = surveyResponse.Survey
            });

            log.LogDebug("Stored customer survey submit from player {PlayerGuid}: survey {SurveyType}, comment length {CommentLength}, stored {Stored}.",
                session.Player?.Guid, surveyResponse.CustomerSurveyId, surveyResponse.Comment?.Length ?? 0, stored);
        }
    }
}
