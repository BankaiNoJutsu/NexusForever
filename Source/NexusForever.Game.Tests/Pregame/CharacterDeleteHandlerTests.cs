using System.Reflection;
using NexusForever.Database;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Pregame;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Character;

namespace NexusForever.Game.Tests.Pregame;

public class CharacterDeleteHandlerTests
{
    [Fact]
    public void HandleMessage_WhenCharacterIsGuildLeaderSendsGuildMasterFailure()
    {
        const ulong characterId = 30ul;

        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        sessionProxy.SetProperty(nameof(IWorldSession.Characters), new List<CharacterModel>
        {
            new()
            {
                Id   = characterId,
                Name = "Joy Ner"
            }
        });

        IGuildBase guild = RecordingDispatchProxy<IGuildBase>.Create(out RecordingDispatchProxy<IGuildBase> guildProxy);
        guildProxy.SetProperty(nameof(IGuildBase.LeaderId), characterId);

        IGlobalGuildManager globalGuildManager = RecordingDispatchProxy<IGlobalGuildManager>.Create(out RecordingDispatchProxy<IGlobalGuildManager> guildManagerProxy);
        guildManagerProxy.SetMethodHandler(nameof(IGlobalGuildManager.GetCharacterGuilds), args =>
        {
            Assert.Equal(characterId, Assert.IsType<ulong>(args[0]));
            return new[] { guild };
        });

        var handler = new CharacterDeleteHandler(
            globalGuildManager,
            RecordingDispatchProxy<IDatabaseManager>.Create(out _),
            RecordingDispatchProxy<IGlobalResidenceManager>.Create(out _),
            RecordingDispatchProxy<ICharacterManager>.Create(out _));

        handler.HandleMessage(session, CreateCharacterDelete(characterId));

        RecordingDispatchProxy<IWorldSession>.Invocation invocation =
            Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        ServerCharacterDeleteResult result = Assert.IsType<ServerCharacterDeleteResult>(invocation.Arguments[0]);
        Assert.Equal(CharacterModifyResult.DeleteFailed_GuildMaster, result.Result);
        Assert.Equal(1u, result.Data);
    }

    private static ClientCharacterDelete CreateCharacterDelete(ulong characterId)
    {
        var message = new ClientCharacterDelete();
        PropertyInfo property = typeof(ClientCharacterDelete).GetProperty(nameof(ClientCharacterDelete.CharacterId))!;
        property.GetSetMethod(true)!.Invoke(message, [characterId]);
        return message;
    }
}
