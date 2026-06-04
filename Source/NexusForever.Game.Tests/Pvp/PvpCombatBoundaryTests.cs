using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Pvp;
using NexusForever.Game.Entity;
using NexusForever.Game.Pvp;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Pvp;

public class PvpCombatBoundaryTests
{
    [Fact]
    public void OpenWorldPlayerTargetWithoutActiveDuel_IsNotAttackable()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        var services = new ServiceCollection();
        services.AddSingletonLegacy<IDuelManager, DuelManager>();
        LegacyServiceProvider.Provider = services.BuildServiceProvider();

        try
        {
            var attacker = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
            IPlayer target = RecordingDispatchProxy<IPlayer>.Create(out _);

            Assert.False(attacker.CanAttack(target));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }
}
