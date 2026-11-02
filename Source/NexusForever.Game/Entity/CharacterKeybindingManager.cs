using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Option;
using NexusForever.Game.Option;
using NexusForever.Network.World.Message.Model.Option;

namespace NexusForever.Game.Entity
{
    public class CharacterKeybindingManager : ICharacterKeybindingManager
    {
        private readonly IPlayer player;
        private readonly KeybindingSet bindingSet;
        private readonly ulong character;

        public CharacterKeybindingManager(IPlayer player, CharacterModel model)
        {
            this.player = player;
            character = model.Id;
            var origin = CharacterLoadOrigin.Get(model);
            if (character != player.CharacterId || origin != null && origin.AccountId != player.Account?.Id)
                throw new InvalidOperationException("Keybindings changed their loaded Character/account owner.");
            bindingSet  = new KeybindingSet(model, ValidateSaveOwner, () => (sbyte)player.InputKeySet);
        }

        public void Save(CharacterContext context)
        {
            bindingSet.Save(context);
        }

        internal Action<CharacterContext> CaptureNormalSave(object token, string database) => bindingSet.CaptureNormalSave(token, database);
        private void ValidateSaveOwner()
        {
            if (player.CharacterId != character || player is Player owner && !ReferenceEquals(owner.KeybindingManager, this))
                throw new InvalidOperationException("Keybindings lost their loaded Character manager.");
        }

        public void Update(BiInputKeySet inputKeySet)
        {
            bindingSet.Update(inputKeySet);
        }

        public BiInputKeySet Build()
        {
            return new BiInputKeySet
            {
                Bindings    = bindingSet.Select(b => b.Build()).ToList(),
                CharacterId = player.CharacterId
            };
        }
    }
}
