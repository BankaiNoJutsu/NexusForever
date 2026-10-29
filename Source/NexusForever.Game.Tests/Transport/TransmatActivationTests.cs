using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Entity;

namespace NexusForever.Game.Tests.Transport;

public class TransmatActivationTests
{
    [Theory]
    [InlineData(false, 1f, true)]
    [InlineData(true, 1f, false)]
    [InlineData(false, 100f, false)]
    public void BindingUsesExistingBusyAndRangeGuards(bool busy, float distance, bool expected)
    {
        var player = RecordingDispatchProxy<IPlayer>.Create(out var pp);
        var entity = RecordingDispatchProxy<IWorldEntity>.Create(out var ep);
        var map = RecordingDispatchProxy<IBaseMap>.Create(out var mp);
        mp.SetProperty(nameof(IBaseMap.Entry), new WorldEntry { Id = 51 });
        pp.SetProperty(nameof(IPlayer.Map), map);
        pp.SetProperty(nameof(IPlayer.Position), new Vector3(1, 2, 3));
        pp.SetMethodReturn(nameof(IPlayer.GetVisible), entity);
        ep.SetProperty(nameof(IWorldEntity.CreatureEntry), new Creature2Entry { BindPointId = 7 });
        ep.SetProperty(nameof(IWorldEntity.Position), new Vector3(1 + distance, 2, 3));
        ep.SetProperty(nameof(IWorldEntity.IsBusy), busy);
        var session = RecordingDispatchProxy<IWorldSession>.Create(out var sp);
        sp.SetProperty(nameof(IWorldSession.Player), player);
        var request = new ClientActivateUnitCastTarget();
        typeof(ClientActivateUnitCastTarget).GetProperty(nameof(ClientActivateUnitCastTarget.TargetField))!.SetValue(request, 123u);
        var handler = new ClientActivateUnitCastTargetHandler(new ClientActivateUnitCastHandler(null, null));

        handler.HandleMessage(session, request);

        var calls = pp.GetInvocations(nameof(IPlayer.SetBindPoint));
        Assert.Equal(expected ? 1 : 0, calls.Count);
        if (expected)
        {
            Assert.Equal((ushort)7, calls[0].Arguments[0]);
            Assert.Equal((ushort)51, calls[0].Arguments[1]);
            Assert.Equal(new Vector3(1, 2, 3), calls[0].Arguments[2]);
        }
    }
}
