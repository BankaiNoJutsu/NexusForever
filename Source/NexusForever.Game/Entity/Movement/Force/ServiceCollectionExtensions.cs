using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Entity.Movement.Force;

namespace NexusForever.Game.Entity.Movement.Force
{
    public static class ServiceCollectionExtensions
    {
        public static void AddGameEntityMovementForce(this IServiceCollection sc)
        {
            sc.AddTransient<IForcedMovementGenerator, ForcedMovementGenerator>();
            sc.AddTransient<IRotationKeyGenerator, RotationKeyGenerator>();
            sc.AddTransient<IProjectileKeyGenerator, ProjectileKeyGenerator>();
            sc.AddTransient<IStateKeyGenerator, StateKeyGenerator>();
        }
    }
}
