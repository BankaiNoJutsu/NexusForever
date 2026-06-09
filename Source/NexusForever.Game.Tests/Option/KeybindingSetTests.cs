using NexusForever.Database.Auth.Model;
using NexusForever.Game.Option;
using NexusForever.Network.World.Message.Model.Option;
using NetworkBinding = NexusForever.Network.World.Message.Model.Shared.Binding;

namespace NexusForever.Game.Tests.Option;

public class KeybindingSetTests
{
    [Fact]
    public void Update_RemovingPendingCreateBindings_DoesNotInvalidateDictionaryEnumeration()
    {
        var set = new KeybindingSet(new AccountModel
        {
            Id = 42u
        });

        set.Update(new BiInputKeySet
        {
            Bindings =
            [
                CreateBinding(100),
                CreateBinding(200)
            ]
        });

        set.Update(new BiInputKeySet());

        Assert.Equal(0u, set.Count);
        Assert.Empty(set);
    }

    [Fact]
    public void Update_RemovingExistingDatabaseBinding_HidesItUntilSave()
    {
        var set = new KeybindingSet(new AccountModel
        {
            Id = 42u,
            AccountKeybinding =
            [
                new AccountKeybindingModel
                {
                    Id = 42u,
                    InputActionId = 100,
                    DeviceEnum00 = 1u
                }
            ]
        });

        set.Update(new BiInputKeySet());

        Assert.Equal(1u, set.Count);
        Assert.Empty(set);
    }

    private static NetworkBinding CreateBinding(ushort inputActionId)
    {
        return new NetworkBinding
        {
            InputActionId = inputActionId,
            DeviceEnum00 = 1u,
            Code00 = 2u,
            EventTypeEnum00 = 3u
        };
    }
}
