using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Combat;

public partial class CombatAITests
{
    [Theory]
    [InlineData("packet")]
    [InlineData("callback")]
    public void ThreatReset_LeashResetCompletesBothRelationsDespiteRemovalFailure(string failure)
    {
        var harness = CreateHarness(new Vector3(55, 0, 0), targetSelected: true,
            creaturePosition: new Vector3(10, 0, 0), leashPosition: Vector3.Zero);
        harness.CreatureThreat.UpdateThreat(harness.Player, 10);
        harness.CreatureProxy.Invocations.Clear();
        harness.MovementProxy.Invocations.Clear();
        harness.SessionProxy.Invocations.Clear();
        if (failure == "packet")
            harness.CreatureProxy.SetMethodHandler(nameof(ICreatureEntity.EnqueueToVisible), args =>
                args[0] is ServerEntityThreatUpdate ? throw new IOException("Owned NPC threat enqueue refusal.") : null);
        else
            harness.CreatureProxy.SetMethodHandler(nameof(ICreatureEntity.OnThreatRemoveTarget), _ =>
                throw new IOException("Owned NPC threat removal script refusal."));
        Exception error = Record.Exception(() =>
        {
            harness.Script.Update(.25);
            harness.Script.Update(3);
        });
        Assert.Null(error);
        Assert.Empty(harness.CreatureThreat);
        Assert.Empty(harness.Player.ThreatManager);
        Assert.Null(harness.Creature.TargetGuid);
        Assert.Single(harness.CreatureProxy.GetInvocations(nameof(ICreatureEntity.ModifyHealth)));
        Assert.Single(harness.SessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        Assert.Single(harness.MovementProxy.GetInvocations(nameof(IMovementManager.LaunchPath)));
    }
}
