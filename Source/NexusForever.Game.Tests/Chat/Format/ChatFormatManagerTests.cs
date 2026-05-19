using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Chat.Format;
using NexusForever.Game.Chat.Format;
using NexusForever.Game.Static.Chat;
using NexusForever.Network;
using NexusForever.Network.Internal.Message.Chat.Shared.Format;
using NexusForever.Network.Internal.Message.Chat.Shared.Format.Model;
using NexusForever.Network.World.Chat.Model;
using NexusForever.Network.World.Message.Model.Chat;

namespace NexusForever.Game.Tests.Chat.Format;

public class ChatFormatManagerTests
{
    [Fact]
    public void ToInternal_ConvertsSupportedFormat_WhenFormatterServiceIsRegistered()
    {
        using ServiceProvider provider = BuildProvider();
        IChatFormatManager manager = provider.GetRequiredService<IChatFormatManager>();

        ChatChannelTextFormat format = manager.ToInternal(null, [
            new ChatFormat
            {
                Type = ChatFormatType.Format0,
                StartIndex = 2,
                StopIndex = 4,
                Model = new ChatFormat0
                {
                    Unknown = true
                }
            }
        ]).Single();

        Assert.Equal(ChatFormatType.Format0, format.Type);
        Assert.Equal((ushort)2, format.StartIndex);
        Assert.Equal((ushort)4, format.StopIndex);

        var model = Assert.IsType<ChatChannelTextFormat0Format>(format.Model);
        Assert.True(model.Unknown);
    }

    [Fact]
    public void ToNetwork_ConvertsSupportedFormat_WhenFormatterServiceIsRegistered()
    {
        using ServiceProvider provider = BuildProvider();
        IChatFormatManager manager = provider.GetRequiredService<IChatFormatManager>();

        ChatFormat format = manager.ToNetwork([
            new ChatChannelTextFormat
            {
                Type = ChatFormatType.ItemFull,
                StartIndex = 1,
                StopIndex = 6,
                Model = new ChatChannelTextItemFullFormat
                {
                    ItemGuid = 5ul,
                    Item2Id = 123u,
                    Unknown2 = 7
                }
            }
        ]).Single();

        Assert.Equal(ChatFormatType.ItemFull, format.Type);
        Assert.Equal((ushort)1, format.StartIndex);
        Assert.Equal((ushort)6, format.StopIndex);

        var model = Assert.IsType<ChatFormatItemFull>(format.Model);
        Assert.Equal(5ul, model.ItemGuid);
        Assert.Equal(123u, model.Item2Id);
        Assert.Equal((byte)7, model.Unknown2);
    }

    [Fact]
    public void ToNetwork_ThrowsInvalidPacketValueException_ForUnsupportedFormatType()
    {
        using ServiceProvider provider = BuildProvider();
        IChatFormatManager manager = provider.GetRequiredService<IChatFormatManager>();

        InvalidPacketValueException exception = Assert.Throws<InvalidPacketValueException>(() => manager.ToNetwork([
            new ChatChannelTextFormat
            {
                Type = (ChatFormatType)15,
                Model = new ChatChannelTextFormat0Format()
            }
        ]).ToList());

        Assert.Contains("Unsupported chat format type", exception.Message);
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddGameChatFormat();

        ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<IChatFormatManager>().Initialise();
        return provider;
    }
}
