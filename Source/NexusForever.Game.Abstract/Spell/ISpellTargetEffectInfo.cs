using NexusForever.GameTable.Model;
using NexusForever.Network.World.Combat;
using NexusForever.Game.Abstract.Entity;

namespace NexusForever.Game.Abstract.Spell
{
    public interface ISpellTargetEffectInfo
    {
        uint EffectId { get; }
        Spell4EffectsEntry Entry { get; }
        IDamageDescription Damage { get; }
        bool DropEffect { get; set; }
        List<ICombatLog> CombatLogs { get; }
        IReadOnlyCollection<IGridEntity> CreatedEntities { get; }

        void AddDamage(IDamageDescription damage);
        void AddCombatLog(ICombatLog combatLog);
        void AddCreatedEntity(IGridEntity entity);
    }
}
