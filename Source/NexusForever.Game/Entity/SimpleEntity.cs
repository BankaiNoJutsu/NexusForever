using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Creature;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;
using NexusForever.Script;

using NexusForever.Game.Quest;

namespace NexusForever.Game.Entity
{
    public class SimpleEntity : UnitEntity, ISimpleEntity
    {
        public override EntityType Type => EntityType.Simple;

        #region Dependency Injection

        public SimpleEntity(IMovementManager movementManager)
            : base(movementManager)
        {
        }

        #endregion

        public override void Initialise(EntityModel model)
        {
            base.Initialise(model);
            scriptCollection = GetScriptManager().InitialiseEntityScripts<ISimpleEntity>(this);
            QuestChecklistIdx = model.QuestChecklistIdx;
        }

        public override void Initialise(ICreatureInfo creatureInfo)
        {
            base.Initialise(creatureInfo);
            scriptCollection = GetScriptManager().InitialiseEntityScripts<ISimpleEntity>(this);
        }

        public override void Initialise(ICreatureInfo creatureInfo, EntityModel model)
        {
            base.Initialise(creatureInfo, model);
            scriptCollection = GetScriptManager().InitialiseEntityScripts<ISimpleEntity>(this);
            QuestChecklistIdx = model.QuestChecklistIdx;
        }

        protected override IEntityModel BuildEntityModel()
        {
            return new SimpleEntityModel
            {
                CreatureId        = CreatureId,
                QuestChecklistIdx = QuestChecklistIdx
            };
        }

        private void UnlockInteractArchiveArticle(IPlayer activator)
        {
            if (CreatureEntry.ArchiveArticleIdInteractUnlock == 0u)
                return;

            activator.GalacticArchiveManager.UnlockArticle(CreatureEntry.ArchiveArticleIdInteractUnlock, grantRewards: false);
        }

        private void CompleteScientistDatacubeDiscoveryMission(IPlayer activator)
        {
            if (CreatureEntry.DatacubeId == 0u && CreatureEntry.DatacubeVolumeId == 0u)
                return;

            activator.PathManager.CompleteCurrentScientistDatacubeDiscoveryMission();
        }

        /// <summary>
        /// Credits the activation to the player's checklist path missions (Explorer_ActivateChecklist, Soldier_Demolition and Soldier_Rescue): the bit of this entity's
        /// <c>QuestChecklistIdx</c> is set in the mission's progress word. See <see cref="IPathManager.ProgressActivateChecklistMissions"/>.
        /// </summary>
        private void ProgressPathChecklistMissions(IPlayer activator)
        {
            activator.PathManager.ProgressActivateChecklistMissions(CreatureId, GetCreatureTargetGroupIds(), QuestChecklistIdx);
            SoldierActivateQuestObjectiveUpdater.OnActivate(activator, CreatureId, GetCreatureTargetGroupIds(), QuestChecklistIdx, TryGetGameTableManager());
        }

        private bool ShouldCastInteractionCompletionSpell()
        {
            return CreatureEntry.ArchiveArticleIdInteractUnlock != 0u
                || CreatureEntry.DatacubeId != 0u
                || CreatureEntry.DatacubeVolumeId != 0u;
        }

        public override void OnActivate(IPlayer activator)
        {
            activator.AchievementManager.CheckAchievements(activator, AchievementType.ActivateCreature, CreatureId);
            UnlockInteractArchiveArticle(activator);
            ProgressPathChecklistMissions(activator);

            if (CreatureEntry.DatacubeId != 0u)
                activator.DatacubeManager.AddDatacube((ushort)CreatureEntry.DatacubeId, int.MaxValue);

            CompleteScientistDatacubeDiscoveryMission(activator);
        }

        public override void OnActivateCast(IPlayer activator)
        {
            uint progress = (uint)(1 << QuestChecklistIdx);

            activator.AchievementManager.CheckAchievements(activator, AchievementType.ActivateCreature, CreatureId);
            UnlockInteractArchiveArticle(activator);
            ProgressPathChecklistMissions(activator);

            if (CreatureEntry.DatacubeId != 0u)
            {
                IDatacube datacube = activator.DatacubeManager.GetDatacube((ushort)CreatureEntry.DatacubeId, DatacubeType.Datacube);
                if (datacube == null)
                    activator.DatacubeManager.AddDatacube((ushort)CreatureEntry.DatacubeId, progress);
                else
                {
                    datacube.Progress |= progress;
                    activator.DatacubeManager.SendDatacube(datacube);
                }
            }

            if (CreatureEntry.DatacubeVolumeId != 0u)
            {
                IDatacube datacube = activator.DatacubeManager.GetDatacube((ushort)CreatureEntry.DatacubeVolumeId, DatacubeType.Journal);
                if (datacube == null)
                    activator.DatacubeManager.AddDatacubeVolume((ushort)CreatureEntry.DatacubeVolumeId, progress);
                else
                {
                    datacube.Progress |= progress;
                    activator.DatacubeManager.SendDatacubeVolume(datacube);
                }
            }

            CompleteScientistDatacubeDiscoveryMission(activator);

            if (ShouldCastInteractionCompletionSpell())
                activator.CastSpell(116u, new SpellParameters
                {
                    PrimaryTargetId = Guid
                });
        }
    }
}
