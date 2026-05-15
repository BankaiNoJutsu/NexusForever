using NexusForever.Game.Static.Support;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Network.World.Message.Model.Support
{
    [Message(GameMessageOpcode.ClientCustomerSurveySubmit)]
    public class ClientCustomerSurveySubmit : IReadable
    {
        public SurveyType CustomerSurveyId { get; private set; }
        public ISurvey Survey { get; private set; }
        public string Comment { get; private set; }

        public void Read(GamePacketReader reader)
        {
            CustomerSurveyId = (SurveyType)reader.ReadInt(14);

            Survey = CustomerSurveyId switch
            {
                SurveyType.QuestGeneric     => new Survey.QuestDifficultySurvey(),
                SurveyType.TSpellQuest      => new Survey.QuestTSpellSurvey(),
                SurveyType.HoldoutQuest     => new Survey.QuestHoldoutSurvey(),
                SurveyType.LevelUp          => new Survey.LevelingSurvey(),
                SurveyType.GenericChallenge => new Survey.ChallengesSurvey(),
                _                           => throw new InvalidPacketValueException($"Unsupported customer survey type: {CustomerSurveyId}")
            };

            Survey.Read(reader);

            Comment = reader.ReadWideString();
        }
    }
}
