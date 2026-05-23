using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model.Crafting;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Crafting;

namespace NexusForever.Game.Tests.Crafting;

public class CraftingAdditiveHandlerTests
{
    [Fact]
    public void Additive_WithZeroStation_ThrowsInvalidPacketValueException()
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);

        var handler = new ClientCraftingAdditiveHandler(
            NullLogger<ClientCraftingAdditiveHandler>.Instance,
            new GameTableManager(Options.Create(new GameTableConfig
            {
                GameTablePath = string.Empty
            })));

        Assert.Throws<InvalidPacketValueException>(() => handler.HandleMessage(session, CreateRequest(0u)));
    }

    private static ClientCraftingAdditive CreateRequest(uint craftingStationUnitId)
    {
        var request = (ClientCraftingAdditive)RuntimeHelpers.GetUninitializedObject(typeof(ClientCraftingAdditive));
        SetAutoProperty(request, nameof(ClientCraftingAdditive.CraftingStationUnitId), craftingStationUnitId);
        SetAutoProperty(request, nameof(ClientCraftingAdditive.AdditiveItem2Id), 0u);
        SetAutoProperty(request, nameof(ClientCraftingAdditive.CatalystItem2Id), 0u);
        return request;
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(instance, value);
    }
}
