using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Retail;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Pvp;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Pvp;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace NexusForever.Game.Tests.Pvp;

public class PvpFlagCooldownTests
{
    [Fact]
    public void ToggleOff_RequestsPendingDisableWithoutClearingFlagImmediately()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.PvPFlag), PvPFlag.Enabled);
        IWorldSession session = CreateWorldSession(player, out RecordingDispatchProxy<IWorldSession> sessionProxy);
        var handler = new ClientPvpToggleFlagsHandler(NullLogger<ClientPvpToggleFlagsHandler>.Instance);

        handler.HandleMessage(session, CreateToggleFlags(false));

        RecordingDispatchProxy<IPlayer>.Invocation request = Assert.Single(
            playerProxy.GetInvocations(nameof(IPlayer.RequestPvPFlagDisable)));
        Assert.Equal(RetailCertainRules.PvpFlagCooldownMs, Assert.IsType<uint>(request.Arguments[0]));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.SetPvPFlag)));
        Assert.Empty(sessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)));
    }

    [Fact]
    public void ToggleOn_CancelsPendingDisableAndEnablesPvp()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.PvPFlag), PvPFlag.Forced);
        IWorldSession session = CreateWorldSession(player, out _);
        var handler = new ClientPvpToggleFlagsHandler(NullLogger<ClientPvpToggleFlagsHandler>.Instance);

        handler.HandleMessage(session, CreateToggleFlags(true));

        Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.CancelPvPFlagDisable)));
        RecordingDispatchProxy<IPlayer>.Invocation setFlag = Assert.Single(
            playerProxy.GetInvocations(nameof(IPlayer.SetPvPFlag)));
        Assert.Equal(PvPFlag.Enabled | PvPFlag.Forced, Assert.IsType<PvPFlag>(setFlag.Arguments[0]));
    }

    [Fact]
    public void PlayerPendingDisable_KeepsPvpEnabledUntilCooldownExpires()
    {
        Player player = CreatePlayer(out RecordingDispatchProxy<IGameSession> sessionProxy);
        player.SetPvPFlag(PvPFlag.Enabled);

        player.RequestPvPFlagDisable(5000u);

        Assert.Equal(PvPFlag.Enabled, player.PvPFlag);
        Assert.NotNull(GetPrivateField<DateTime?>(typeof(Player), player, "pvpFlagDisableUntilUtc"));
        Assert.True((GetPrivateField<Player.PlayerSaveMask>(typeof(Player), player, "saveMask") & Player.PlayerSaveMask.PvP) != 0);
        ServerPvpCooldownUpdate update = Assert.Single(GetSessionMessages<ServerPvpCooldownUpdate>(sessionProxy));
        Assert.Equal(5000u, update.CooldownRemaining);

        UpdatePvPFlagDisable(player, 4.99d);

        Assert.Equal(PvPFlag.Enabled, player.PvPFlag);
        Assert.Empty(GetSessionMessages<ServerPvpCooldownClear>(sessionProxy));

        UpdatePvPFlagDisable(player, 0.01d);

        Assert.Equal(PvPFlag.Disabled, player.PvPFlag);
        Assert.Null(GetPrivateField<DateTime?>(typeof(Player), player, "pvpFlagDisableUntilUtc"));
        Assert.Single(GetSessionMessages<ServerPvpCooldownClear>(sessionProxy));
    }

    [Fact]
    public void PlayerCancelPendingDisable_LeavesPvpEnabledAndStopsTimer()
    {
        Player player = CreatePlayer(out RecordingDispatchProxy<IGameSession> sessionProxy);
        player.SetPvPFlag(PvPFlag.Enabled);
        player.RequestPvPFlagDisable(5000u);

        player.CancelPvPFlagDisable();
        UpdatePvPFlagDisable(player, 5d);

        Assert.Equal(PvPFlag.Enabled, player.PvPFlag);
        Assert.Null(GetPrivateField<DateTime?>(typeof(Player), player, "pvpFlagDisableUntilUtc"));
        Assert.Single(GetSessionMessages<ServerPvpCooldownClear>(sessionProxy));
    }

    [Fact]
    public void PlayerRequestPendingDisable_StoresDurableUtcExpiry()
    {
        Player player = CreatePlayer(out _);
        player.SetPvPFlag(PvPFlag.Enabled);

        DateTime beforeRequest = DateTime.UtcNow;
        player.RequestPvPFlagDisable(5000u);

        DateTime? disableUntilUtc = GetPrivateField<DateTime?>(typeof(Player), player, "pvpFlagDisableUntilUtc");
        Assert.NotNull(disableUntilUtc);
        Assert.InRange(disableUntilUtc.Value, beforeRequest.AddMilliseconds(4990d), DateTime.UtcNow.AddMilliseconds(5100d));
        Assert.True((GetPrivateField<Player.PlayerSaveMask>(typeof(Player), player, "saveMask") & Player.PlayerSaveMask.PvP) != 0);
    }

    [Fact]
    public void PlayerInitialisePendingDisable_RestoresEnabledFlagAndSendsRemainingCooldown()
    {
        Player player = CreatePlayer(out RecordingDispatchProxy<IGameSession> sessionProxy);

        InitialisePvPFlagDisable(player, DateTime.UtcNow.AddSeconds(5d));
        SendPendingPvPFlagDisableCooldown(player);

        Assert.Equal(PvPFlag.Enabled, player.PvPFlag);
        Assert.NotNull(GetPrivateField<DateTime?>(typeof(Player), player, "pvpFlagDisableUntilUtc"));
        ServerPvpCooldownUpdate update = Assert.Single(GetSessionMessages<ServerPvpCooldownUpdate>(sessionProxy));
        Assert.InRange(update.CooldownRemaining, 1u, 5000u);
    }

    [Fact]
    public void PlayerInitialiseExpiredPendingDisable_ClearsStoredExpiryForSave()
    {
        Player player = CreatePlayer(out _);

        InitialisePvPFlagDisable(player, DateTime.UtcNow.AddSeconds(-1d));

        Assert.Equal(PvPFlag.Disabled, player.PvPFlag);
        Assert.Null(GetPrivateField<DateTime?>(typeof(Player), player, "pvpFlagDisableUntilUtc"));
        Assert.True((GetPrivateField<Player.PlayerSaveMask>(typeof(Player), player, "saveMask") & Player.PlayerSaveMask.PvP) != 0);
    }

    [Fact]
    public void CharacterContext_MapsPvpFlagDisableCooldownColumn()
    {
        using CharacterContext context = CreateCharacterContext();
        IEntityType entityType = context.Model.FindEntityType(typeof(CharacterModel))!;
        StoreObjectIdentifier table = StoreObjectIdentifier.Table("character", null);
        IProperty property = entityType.FindProperty(nameof(CharacterModel.PvpFlagDisableUntilUtc))!;

        Assert.Equal("pvpFlagDisableUntilUtc", property.GetColumnName(table));
        Assert.Equal("datetime", property.GetColumnType());
        Assert.Null(property.GetDefaultValue());
    }

    private static IWorldSession CreateWorldSession(IPlayer player, out RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        return session;
    }

    private static ClientPvpToggleFlags CreateToggleFlags(bool value)
    {
        byte[] data = WritePacket(writer => writer.Write(value));
        using var reader = new GamePacketReader(new MemoryStream(data));
        var packet = new ClientPvpToggleFlags();
        packet.Read(reader);
        return packet;
    }

    private static Player CreatePlayer(out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
        SetPrivateProperty(player, nameof(Player.Session), session);
        SetPrivateField(typeof(GridEntity), player, "visibleEntities", new Dictionary<uint, IGridEntity>());
        return player;
    }

    private static CharacterContext CreateCharacterContext()
    {
        DbContextOptions<CharacterContext> options = new DbContextOptionsBuilder<CharacterContext>()
            .UseMySql(
                "Server=127.0.0.1;Database=nexus_forever_character;User ID=nexus_forever;Password=nexus_forever;",
                new MySqlServerVersion(new Version(8, 0, 0)))
            .Options;

        return new CharacterContext(options);
    }

    private static IReadOnlyList<T> GetSessionMessages<T>(RecordingDispatchProxy<IGameSession> sessionProxy)
        where T : class, IWritable
    {
        return sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Where(i => i.Arguments.Length == 1)
            .Select(i => i.Arguments[0])
            .OfType<T>()
            .ToList();
    }

    private static void UpdatePvPFlagDisable(Player player, double lastTick)
    {
        MethodInfo method = typeof(Player).GetMethod("UpdatePvPFlagDisable", BindingFlags.Instance | BindingFlags.NonPublic)!;
        method.Invoke(player, [lastTick]);
    }

    private static void InitialisePvPFlagDisable(Player player, DateTime? disableUntilUtc)
    {
        MethodInfo method = typeof(Player).GetMethod("InitialisePvPFlagDisable", BindingFlags.Instance | BindingFlags.NonPublic)!;
        method.Invoke(player, [disableUntilUtc]);
    }

    private static void SendPendingPvPFlagDisableCooldown(Player player)
    {
        MethodInfo method = typeof(Player).GetMethod("SendPendingPvPFlagDisableCooldown", BindingFlags.Instance | BindingFlags.NonPublic)!;
        method.Invoke(player, []);
    }

    private static void SetPrivateProperty<T>(object instance, string propertyName, T value)
    {
        PropertyInfo property = instance.GetType().GetProperty(propertyName)!;
        property.GetSetMethod(true)!.Invoke(instance, [value]);
    }

    private static void SetPrivateField(Type declaringType, object instance, string fieldName, object value)
    {
        FieldInfo field = declaringType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(instance, value);
    }

    private static T GetPrivateField<T>(Type declaringType, object instance, string fieldName)
    {
        FieldInfo field = declaringType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (T)field.GetValue(instance);
    }

    private static byte[] WritePacket(Action<GamePacketWriter> write)
    {
        using var stream = new MemoryStream();
        using var writer = new GamePacketWriter(stream);
        write(writer);
        writer.FlushBits();
        return stream.ToArray();
    }
}
