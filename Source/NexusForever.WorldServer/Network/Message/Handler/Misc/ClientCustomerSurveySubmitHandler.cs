using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Support;

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
            log.LogDebug("Ignoring unsupported customer survey submit from player {PlayerGuid}: survey {SurveyType}, comment length {CommentLength}.",
                session.Player?.Guid, surveyResponse.CustomerSurveyId, surveyResponse.Comment?.Length ?? 0);
        }
    }
}
