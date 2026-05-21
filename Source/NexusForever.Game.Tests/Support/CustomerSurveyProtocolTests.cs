using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Static.Support;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model.Support;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Misc;
using NexusForever.WorldServer.Support;

namespace NexusForever.Game.Tests.Support;

public class CustomerSurveyProtocolTests
{
    public static TheoryData<SurveyType> SupportedSurveyTypes => new()
    {
        SurveyType.QuestGeneric,
        SurveyType.TSpellQuest,
        SurveyType.HoldoutQuest,
        SurveyType.LevelUp,
        SurveyType.GenericChallenge
    };

    [Theory]
    [MemberData(nameof(SupportedSurveyTypes))]
    public void ClientCustomerSurveySubmit_ReadsMappedThirtyTwoBitContextField(SurveyType surveyType)
    {
        byte[] packetData = WriteSurveyPacket(surveyType, contextId: 0x89ABCDEFu, comment: "survey comment");

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientCustomerSurveySubmit();
        packet.Read(reader);

        Assert.Equal(surveyType, packet.CustomerSurveyId);
        Assert.Equal(0x89ABCDEFu, GetContextId(packet.Survey));
        Assert.Equal("survey comment", packet.Comment);
    }

    [Fact]
    public void ClientCustomerSurveySubmitHandler_StoresParsedSurvey()
    {
        byte[] packetData = WriteSurveyPacket(SurveyType.QuestGeneric, contextId: 0x01020304u, comment: "done");
        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientCustomerSurveySubmit();
        packet.Read(reader);

        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out _);
        var store = new RecordingSubmissionStore();
        var handler = new ClientCustomerSurveySubmitHandler(
            NullLogger<ClientCustomerSurveySubmitHandler>.Instance,
            store);

        handler.HandleMessage(session, packet);

        RecordingSubmissionStore.Submission submission = Assert.Single(store.Submissions);
        Assert.Equal("customer-survey", submission.Type);
        Assert.Same(session, submission.Session);
        Assert.Equal(SurveyType.QuestGeneric, submission.Payload.GetType().GetProperty(nameof(ClientCustomerSurveySubmit.CustomerSurveyId))!.GetValue(submission.Payload));
        Assert.Equal("done", submission.Payload.GetType().GetProperty(nameof(ClientCustomerSurveySubmit.Comment))!.GetValue(submission.Payload));
        Assert.IsType<Survey.QuestDifficultySurvey>(submission.Payload.GetType().GetProperty("SurveyModel")!.GetValue(submission.Payload));
    }

    private static uint GetContextId(ISurvey survey)
    {
        return survey switch
        {
            Survey.QuestDifficultySurvey questDifficulty => questDifficulty.ObjectId,
            Survey.QuestTSpellSurvey questTSpell        => questTSpell.ObjectId,
            Survey.QuestHoldoutSurvey questHoldout      => questHoldout.ObjectId,
            Survey.LevelingSurvey leveling              => leveling.Level,
            Survey.ChallengesSurvey challenge           => challenge.ChallengeId,
            _                                           => throw new Xunit.Sdk.XunitException($"Unexpected survey type {survey.GetType().Name}.")
        };
    }

    private static byte[] WriteSurveyPacket(SurveyType surveyType, uint contextId, string comment)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            writer.Write(surveyType, 14u);
            writer.Write(contextId);
            writer.Write((byte)1);
            writer.Write((byte)2);
            writer.Write((byte)3);
            writer.Write((byte)4);
            writer.WriteStringWide(comment);
            writer.FlushBits();
        }

        return stream.ToArray();
    }

    private sealed class RecordingSubmissionStore : ISupportSubmissionStore
    {
        public List<Submission> Submissions { get; } = [];

        public bool TryAppend(IWorldSession session, string type, object payload)
        {
            Submissions.Add(new Submission(session, type, payload));
            return true;
        }

        public readonly record struct Submission(IWorldSession Session, string Type, object Payload);
    }
}
