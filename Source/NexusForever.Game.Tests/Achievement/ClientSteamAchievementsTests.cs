using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Achievement;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Misc;

namespace NexusForever.Game.Tests.Achievement;

public class ClientSteamAchievementsTests
{
    private const uint WildStarSteamGameId = 376570u;

    [Fact]
    public void Read_ParsesSteamGameIdAndAsciiPayload()
    {
        ClientSteamAchievements achievements = ReadPacket(BuildPacket("ACH_WILDSTAR_LOGIN=1;ACH_PATH=0"));

        Assert.Equal(WildStarSteamGameId, achievements.SteamGameId);
        Assert.Equal("ACH_WILDSTAR_LOGIN=1;ACH_PATH=0", achievements.AchievementData);
    }

    [Fact]
    public void Handler_AcceptsParsedPayloadWithoutMutatingAchievementState()
    {
        IWorldSession session = CreateSession(
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy);
        ClientSteamAchievements achievements = ReadPacket(BuildPacket("ACH_WILDSTAR_LOGIN=1"));
        var handler = new ClientSteamAchievementsHandler(NullLogger<ClientSteamAchievementsHandler>.Instance);

        handler.HandleMessage(session, achievements);

        Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        Assert.Empty(achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements)));
        Assert.Empty(achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.SetAchievementProgress)));
    }

    private static IWorldSession CreateSession(
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        ICharacterAchievementManager achievementManager =
            RecordingDispatchProxy<ICharacterAchievementManager>.Create(out achievementProxy);

        playerProxy.SetProperty(nameof(IPlayer.Guid), 4242u);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievementManager);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        return session;
    }

    private static ClientSteamAchievements ReadPacket(byte[] packetData)
    {
        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);

        var achievements = new ClientSteamAchievements();
        achievements.Read(reader);
        return achievements;
    }

    private static byte[] BuildPacket(string achievementData)
    {
        byte[] payload = Encoding.ASCII.GetBytes(achievementData);

        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            writer.Write(WildStarSteamGameId);
            writer.Write((uint)payload.Length);
            writer.WriteBytes(payload);
            writer.FlushBits();
        }

        return stream.ToArray();
    }
}
