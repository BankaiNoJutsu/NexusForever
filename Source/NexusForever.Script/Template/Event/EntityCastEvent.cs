using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Shared;

namespace NexusForever.Script.Template.Event
{
    public class EntityCastEvent : IEntityCastEvent
    {
        public uint SpellId { get; private set; }
        private bool target;

        private IUnitEntity entity;

        #region Dependency Injection

        private readonly ISpellParameters spellParameters;

        public EntityCastEvent(
            ISpellParameters spellParameters)
        {
            this.spellParameters = spellParameters;
        }

        #endregion

        public void Initialise<T>(IUnitEntity entity, T spellId, bool target) where T : Enum
        {
            Initialise(entity, spellId.As<T, uint>(), target);
        }

        public void Initialise(IUnitEntity entity, uint spellId, bool target)
        {
            SpellId = spellId;

            this.entity = entity;
            this.target = target;
        }

        public void Invoke()
        {
            if (target && entity.TargetGuid != null)
                spellParameters.PrimaryTargetId = entity.TargetGuid.Value;

            entity.CastSpell(SpellId, spellParameters);
        }
    }
}
