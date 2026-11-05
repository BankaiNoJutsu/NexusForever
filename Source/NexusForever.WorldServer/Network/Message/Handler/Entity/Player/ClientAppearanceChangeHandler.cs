using System;
using System.Collections.Generic;
using System.Linq;
using NexusForever.Game.Abstract.Customisation;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Entity;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Entity.Player
{
    public class ClientAppearanceChangeHandler : IMessageHandler<IWorldSession, ClientCharacterAppearanceChange>
    {
        #region Dependency Injection

        private readonly ICustomisationManager customisationManager;

        public ClientAppearanceChangeHandler(
            ICustomisationManager customisationManager)
        {
            this.customisationManager = customisationManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientCharacterAppearanceChange appearanceChange)
        {
            var player = session.Player;
            var account = player.Account; var events = session.Events; var identity = player.Identity;
            uint accountId = account.Id; var realm = identity?.RealmId;
            var appearance = player.AppearanceManager;
            var credits = player.CurrencyManager;
            var tokens = account.CurrencyManager;
            ulong character = player.CharacterId, epoch = account.WorldSessionEpoch;
            var race = appearanceChange.Race; var sex = appearanceChange.Sex;
            bool useTokens = appearanceChange.UseServiceTokens;
            List<(uint Label, uint Value)> customisations = appearanceChange.Labels
                .Zip(appearanceChange.Values, ValueTuple.Create).ToList();
            var bones = appearanceChange.Bones?.ToList();
            bool Current() => ReferenceEquals(session.Player, player) && player.CharacterId == character
                && ReferenceEquals(player.Session, session) && ReferenceEquals(account.Session, session) && ReferenceEquals(session.Events, events)
                && account.Id == accountId && ReferenceEquals(player.Identity, identity) && player.Identity?.RealmId == realm
                && ReferenceEquals(player.Account, account) && account.WorldSessionEpoch == epoch
                && ReferenceEquals(player.AppearanceManager, appearance) && ReferenceEquals(player.CurrencyManager, credits)
                && ReferenceEquals(account.CurrencyManager, tokens);

            if (!AppearanceManager.IsValidInput(customisations, bones))
                throw new InvalidPacketValueException();
            if (!customisationManager.Validate(race, sex, player.Faction1, customisations.ToList()))
                throw new InvalidPacketValueException();
            if (!Current()) return;
            uint cost = useTokens ? customisationManager.CalculateCostTokens(player, race, sex)
                : customisationManager.CalculateCostCredits(player, race, sex, customisations.ToList(), bones.ToList());
            if (!Current()) return;
            bool affordable = useTokens ? tokens.CanAfford(AccountCurrencyType.ServiceToken, cost) : credits.CanAfford(CurrencyType.Credits, cost);
            if (!Current() || !affordable) return;
            if (player is NexusForever.Game.Entity.Player && appearance is AppearanceManager concrete)
            {
                concrete.RequestPaidChange(race, sex, customisations, bones, useTokens, cost);
                return;
            }
            if (useTokens) tokens.CurrencySubtractAmount(AccountCurrencyType.ServiceToken, cost);
            else credits.CurrencySubtractAmount(CurrencyType.Credits, cost);
            if (Current()) appearance.Update(race, sex, customisations, bones);
        }
    }
}
