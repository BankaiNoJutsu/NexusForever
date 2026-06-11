using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Pvp;

public class PvpCombatBoundaryTests
{
    [Fact]
    public void OpenWorldPlayerTargetWithoutActiveDuel_IsNotAttackable()
    {
        var attacker = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        IPlayer target = RecordingDispatchProxy<IPlayer>.Create(out _);

        Assert.False(attacker.CanAttack(target));
    }
}
