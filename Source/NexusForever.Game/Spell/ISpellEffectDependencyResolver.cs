using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement.Force;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;

namespace NexusForever.Game.Spell
{
    internal interface ISpellEffectDependencyResolver
    {
        IDamageCalculator CreateDamageCalculator();
        IEntityFactory GetEntityFactory();
        IForcedMovementGenerator GetForcedMovementGenerator();
        IAssetManager GetAssetManager();
        IGlobalAchievementManager GetGlobalAchievementManager();
        IGlobalResidenceManager GetGlobalResidenceManager();
        IMapLockManager GetMapLockManager();
        IGlobalSpellManager GetGlobalSpellManager();
        IGameTableManager GetGameTableManager();
    }
}
