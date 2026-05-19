using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;

namespace NexusForever.Game.Entity
{
    public class EsperPetEntity : WorldEntity, IEsperPetEntity
    {
        public override EntityType Type => EntityType.EsperPet;

        #region Dependency Injection

        public EsperPetEntity(IMovementManager movementManager)
            : base(movementManager)
        {
        }

        #endregion

        protected override IEntityModel BuildEntityModel()
        {
            return new EsperPetEntityModel
            {
                CreatureId = CreatureId,
                OwnerId    = SummonerGuid ?? 0u
            };
        }
    }
}
