using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.World.Message.Model;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Misc;

namespace NexusForever.Game.Tests.Entity;

public class ClientDashCastHandlerTests
{
    [Fact]
    public void HandleMessage_WithDashEnergy_ConsumesOneDashCharge()
    {
        IWorldSession session = CreateSession(200f, out RecordingDispatchProxy<IPlayer> playerProxy);
        var handler = new ClientDashCastHandler(NullLogger<ClientDashCastHandler>.Instance);

        handler.HandleMessage(session, new ClientDashCast());

        RecordingDispatchProxy<IPlayer>.Invocation consume =
            Assert.Single(playerProxy.GetInvocations(nameof(IUnitEntity.TryModifyVital)));
        Assert.Equal(Vital.Resource7, consume.Arguments[0]);
        Assert.Equal(-DashEnergyRules.ChargeCost, consume.Arguments[1]);
    }

    [Fact]
    public void HandleMessage_WithoutFullDashCharge_DoesNotConsumeDashEnergy()
    {
        IWorldSession session = CreateSession(DashEnergyRules.ChargeCost - 1f, out RecordingDispatchProxy<IPlayer> playerProxy);
        var handler = new ClientDashCastHandler(NullLogger<ClientDashCastHandler>.Instance);

        handler.HandleMessage(session, new ClientDashCast());

        Assert.Empty(playerProxy.GetInvocations(nameof(IUnitEntity.TryModifyVital)));
    }

    private static IWorldSession CreateSession(float dashEnergy, out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        float currentDashEnergy = dashEnergy;
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        playerProxy.SetMethodHandler(nameof(IUnitEntity.TryGetVitalValue), args =>
        {
            if ((Vital)args[0] != Vital.Resource7)
            {
                args[1] = 0f;
                return false;
            }

            args[1] = currentDashEnergy;
            return true;
        });
        playerProxy.SetMethodHandler(nameof(IUnitEntity.TryModifyVital), args =>
        {
            if ((Vital)args[0] != Vital.Resource7)
            {
                args[2] = 0f;
                return false;
            }

            float oldValue = currentDashEnergy;
            currentDashEnergy = Math.Max(0f, currentDashEnergy + (float)args[1]);
            args[2] = currentDashEnergy - oldValue;
            return true;
        });

        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        return session;
    }
}
