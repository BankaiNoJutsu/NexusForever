using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Static.Guild;
using NexusForever.GameTable.Text.Filter;
using NexusForever.Network.Internal;

namespace NexusForever.Game.Guild
{
    public class Circle : GuildBase, ICircle
    {
        public override GuildType Type => GuildType.Circle;
        public override uint MaxMembers => 20u;

        #region Dependency Injection

        public Circle(
            IRealmContext realmContext,
            IInternalMessagePublisher messagePublisher,
            ITextFilterManager textFilterManager,
            ICharacterManager characterManager = null,
            IGlobalGuildManager globalGuildManager = null,
            IPlayerManager playerManager = null)
            : base(realmContext, messagePublisher, textFilterManager, characterManager, globalGuildManager, playerManager)
        {
        }

        #endregion
    }
}
