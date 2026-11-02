using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Database.Chat;
using NexusForever.Database.Chat.Model;
using NexusForever.Database.Chat.Repository;
using NexusForever.Game.Static.Chat;
using NexusForever.Game.Static.Guild;
using NexusForever.Network.Internal.Message.Guild;
using NexusForever.Server.ChatServer.Chat;
using NexusForever.Server.ChatServer.Network.Internal.Handler.Guild;

namespace NexusForever.Game.Tests.Guild;

public class GuildDisbandChatCleanupTests
{
    [Theory]
    [InlineData(GuildType.Guild, 2)]
    [InlineData(GuildType.Circle, 1)]
    [InlineData(GuildType.WarParty, 2)]
    [InlineData(GuildType.Community, 1)]
    public async Task CurrentHandlersReleaseOnlyDisbandedGuildChannelsAndPermitRecreation(GuildType type, int channelCount)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ChatContext>().UseSqlite(connection).Options;
        await using var context = new ChatContext(options);
        await context.Database.EnsureCreatedAsync();
        var registrations = new ServiceCollection();
        registrations.AddTransient(provider => new ChatChannel(NullLogger<ChatChannel>.Instance, provider, null, null, null, null));
        registrations.AddTransient(_ => new ChatChannelMember(null));
        using var services = registrations.BuildServiceProvider();
        var manager = new ChatChannelManager(services, new ChatChannelRepository(context));
        var created = new GuildCreatedHandler(context, manager, new GuildChannelDataManager());
        var disbanded = new GuildDisbandedHandler(context, manager);
        await created.Handle(new GuildCreatedMessage { GuildId = 77, Type = type });
        await created.Handle(new GuildCreatedMessage { GuildId = 88, Type = type });
        ChatChannel custom = manager.CreateChatChannel(ChatChannelType.Custom, "Keep", null);
        context.Character.Add(new CharacterModel { CharacterId = 42, RealmId = 1, Name = "Leader" });
        ChatChannelModel oldMain = await context.ChatChannel.FirstAsync(channel => channel.ReferenceValue == 77);
        context.ChatChannelMember.Add(new ChatChannelMemberModel { ChatId = oldMain.ChatId, CharacterId = 42, RealmId = 1 });
        await context.SaveChangesAsync();
        ulong customId = custom.ChatId;
        ulong[] oldIds = await context.ChatChannel.Where(channel => channel.ReferenceValue == 77).Select(channel => channel.ChatId).ToArrayAsync();

        await disbanded.Handle(new GuildDisbandedMessage { GuildId = 77, Type = type });
        await disbanded.Handle(new GuildDisbandedMessage { GuildId = 77, Type = type });

        Assert.Empty(await context.ChatChannel.Where(channel => channel.ReferenceValue == 77).ToListAsync());
        Assert.Empty(await context.ChatChannelMember.ToListAsync());
        Assert.Equal(channelCount, await context.ChatChannel.CountAsync(channel => channel.ReferenceValue == 88));
        Assert.Equal("Keep", (await context.ChatChannel.SingleAsync(channel => channel.ChatId == customId)).Name);
        Assert.Single(await context.Character.ToListAsync());
        await created.Handle(new GuildCreatedMessage { GuildId = 77, Type = type });
        ChatChannelModel[] replacements = await context.ChatChannel.Where(channel => channel.ReferenceValue == 77).ToArrayAsync();
        Assert.Equal(channelCount, replacements.Length);
        Assert.All(replacements, channel =>
        {
            Assert.DoesNotContain(channel.ChatId, oldIds);
            Assert.Null(channel.Name); // Current guild channels use the guild reference, not a name cache.
        });
    }
}
