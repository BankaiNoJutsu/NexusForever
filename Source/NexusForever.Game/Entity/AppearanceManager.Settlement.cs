using NexusForever.Database.Character.Model;
using NexusForever.Game.Static.Entity;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Entity
{
    public partial class AppearanceManager
    {
        private AppearanceChangeState confirmedAppearance;
        private AppearanceChangeSettlement paidChange;
        internal AppearanceChangeState ConfirmedAppearance => confirmedAppearance;
        internal bool OwnsChange(AppearanceChangeSettlement change) => ReferenceEquals(paidChange, change);
        internal void AssertAppearanceMutationAllowed()
        {
            if (paidChange != null) throw new InvalidOperationException("Appearance has an accepted paid change.");
        }
        public void RequestPaidChange(Race race, Sex sex, IList<(uint Label, uint Value)> customisations, IList<float> bones, bool tokens, uint fee)
        {
            if (paidChange != null || owner is not Player player) return;
            if (!IsValidInput(customisations, bones)) throw new ArgumentException("Invalid appearance change.");
            var account = player.Account; var session = player.Session; var events = session.Events;
            ulong character = player.CharacterId, epoch = account.WorldSessionEpoch;
            uint accountId = account.Id; var identity = player.Identity; var realm = identity.RealmId;
            var choices = customisations.ToArray(); var shapes = bones.ToArray();
            // Resolve every visual before any fee reservation or mutable field.
            var visuals = customisationManager.GetItemVisuals(race, sex, choices.ToList()).ToArray();
            if (paidChange != null || !ReferenceEquals(player.AppearanceManager, this) || player.CharacterId != character
                || account.Id != accountId || !ReferenceEquals(player.Identity, identity) || identity.RealmId != realm
                || !ReferenceEquals(player.Account, account) || account.WorldSessionEpoch != epoch
                || !ReferenceEquals(player.Session, session) || !ReferenceEquals(account.Session, session)
                || !ReferenceEquals(session.Events, events) || session is not NexusForever.Game.Abstract.Entity.IPlayerSession current
                || !ReferenceEquals(current.Player, player)) return;
            var target = new AppearanceChangeState((byte)race, (byte)sex,
                visuals.GroupBy(row => row.Slot).Select(group => new AppearanceChangeState.Visual(checked((byte)group.Key), group.Last().DisplayId
                    ?? throw new InvalidOperationException("Appearance projection has no display identity."))).OrderBy(row => row.Slot).ToArray(),
                choices.Select(row => new AppearanceChangeState.Choice(row.Label, row.Value)).OrderBy(row => row.Label).ToArray(),
                shapes.Select((value, index) => new AppearanceChangeState.Shape(checked((byte)index), value)).ToArray());
            var change = new AppearanceChangeSettlement(player, this, target, tokens, fee);
            paidChange = change;
            player.Session.Events.EnqueueEvent(new PredicateEvent(() => true, change.Advance));
        }
        internal void ConfirmPaidChange(AppearanceChangeSettlement change, AppearanceChangeState state, bool adopt)
        {
            if (!OwnsChange(change)) throw new InvalidOperationException("Appearance adoption changed its original operation.");
            confirmedAppearance = state;
            var previousSlots = characterAppearances.Keys.ToArray();
            characterAppearances.Clear(); characterCustomisations.Clear(); characterBones.Clear();
            deletedCharacterAppearances.Clear(); deletedCharacterCustomisations.Clear(); deletedCharacterBones.Clear();
            foreach (var row in state.Visuals)
                characterAppearances.Add((ItemSlot)row.Slot, new Appearance(new CharacterAppearanceModel { Id = owner.CharacterId, Slot = row.Slot, DisplayId = row.Display }, AssertAppearanceMutationAllowed));
            foreach (var row in state.Choices)
                characterCustomisations.Add(row.Label, new Customisation(new CharacterCustomisationModel { Id = owner.CharacterId, Label = row.Label, Value = row.Value }, AssertAppearanceMutationAllowed));
            foreach (var row in state.Bones)
                characterBones.Add(row.Index, new Bone(new CharacterBoneModel { Id = owner.CharacterId, BoneIndex = row.Index, Bone = row.Value }, AssertAppearanceMutationAllowed));
            ((Player)owner).AdoptConfirmedAppearanceHeader(state.Race, state.Sex, adopt);
            if (!adopt) return;
            foreach (var slot in previousSlots.Except(state.Visuals.Select(row => (ItemSlot)row.Slot))) owner.RemoveVisual(slot);
            foreach (var visual in state.Visuals) owner.AddVisual((ItemSlot)visual.Slot, visual.Display);
        }
        internal void FinishPaidChange(AppearanceChangeSettlement change)
        {
            if (!OwnsChange(change)) throw new InvalidOperationException("Appearance changed its retained request.");
            paidChange = null;
        }
    }
}
