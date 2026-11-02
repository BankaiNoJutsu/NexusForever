using System.Collections;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Option;
using NexusForever.Game.Static.Option;
using NexusForever.Network.World.Message.Model.Option;
using NetworkBinding = NexusForever.Network.World.Message.Model.Shared.Binding;

namespace NexusForever.Game.Option
{
    public class KeybindingSet : IKeybindingSet
    {
        public ulong Owner { get; }
        public InputSets InputSet { get; }
        public uint Count
        {
            get
            {
                lock (PersistenceGate) return (uint)bindings.Count;
            }
        }

        private bool isDirty;

        private readonly Dictionary<ushort, IKeybinding> bindings = new();
        private readonly CharacterKeybindingPersistence characterPersistence;
        private readonly AccountKeybindingPersistence accountPersistence;
        private object PersistenceGate => characterPersistence?.Gate ?? accountPersistence.Gate;
        private readonly Action validateCharacterOwner;
        private readonly Func<sbyte> selectedInputSet;

        /// <summary>
        /// Create a new <see cref="KeybindingSet"/> from an existing database model.
        /// </summary>
        public KeybindingSet(CharacterModel model)
            : this(model, null)
        {
        }

        internal KeybindingSet(CharacterModel model, Action validateCharacterOwner, Func<sbyte> selectedInputSet = null)
        {
            Owner    = model.Id;
            InputSet = InputSets.Character;
            this.validateCharacterOwner = validateCharacterOwner;
            this.selectedInputSet = selectedInputSet;

            foreach (CharacterKeybindingModel binding in model.Keybinding)
                bindings.Add(binding.InputActionId, BindOwner(new Keybinding(Owner, binding)));
            characterPersistence = new(Owner, new(selectedInputSet == null ? null : model.InputKeySet, model.Keybinding.OrderBy(row => row.InputActionId).Select(CharacterKeybindingPersistence.FromModel).ToArray()),
                CaptureCharacterBindings, AcceptCharacterBindings, CharacterLoadOrigin.Get(model)?.Database, validateCharacterOwner);
        }

        /// <summary>
        /// Create a new <see cref="KeybindingSet"/> from an existing database model.
        /// </summary>
        public KeybindingSet(AccountModel model)
            : this(model, null)
        {
        }

        internal KeybindingSet(AccountModel model, Action validateAccountOwner)
        {
            Owner    = model.Id;
            InputSet = InputSets.Account;

            foreach (AccountKeybindingModel binding in model.AccountKeybinding)
                bindings.Add(binding.InputActionId, BindOwner(new Keybinding(Owner, binding)));
            accountPersistence = new(model.Id, model.WorldSessionEpoch,
                new(model.AccountKeybinding.OrderBy(row => row.InputActionId).Select(AccountKeybindingPersistence.FromModel).ToArray()),
                CaptureAccountBindings, AcceptAccountBindings, validateAccountOwner);
        }

        public void Save(CharacterContext context)
        {
            if (characterPersistence != null) { characterPersistence.Save(context); return; }
            if (!isDirty)
                return;

            foreach (IKeybinding binding in bindings.Values.ToList())
            {
                if (binding.PendingDelete)
                    bindings.Remove(binding.InputActionId);

                binding.Save(context);
            }

            isDirty = false;
        }

        public void Save(AuthContext context)
        {
            if (accountPersistence != null) { accountPersistence.Save(context); return; }
            if (!isDirty)
                return;

            foreach (IKeybinding binding in bindings.Values.ToList())
            {
                if (binding.PendingDelete)
                    bindings.Remove(binding.InputActionId);

                binding.Save(context);
            }

            isDirty = false;
        }

        /// <summary>
        /// Update <see cref="KeybindingSet"/> with information from supplied <see cref="BiInputKeySet"/> from client.
        /// </summary>
        public void Update(BiInputKeySet biInputKeySet)
        {
            lock (PersistenceGate) UpdateBindings(biInputKeySet);
        }

        private void UpdateBindings(BiInputKeySet biInputKeySet)
        {
            if (bindings.Count + biInputKeySet.Bindings.Count == 0)
                return;

            isDirty = true;

            foreach (ushort inputActionId in bindings.Keys
                .Except(biInputKeySet.Bindings.Select(b => b.InputActionId))
                .ToList())
            {
                IKeybinding binding = bindings[inputActionId];
                if (binding.PendingCreate)
                    bindings.Remove(inputActionId);
                else
                    binding.EnqueueDelete(true);
            }

            foreach (NetworkBinding networkBinding in biInputKeySet.Bindings)
            {
                if (!bindings.TryGetValue(networkBinding.InputActionId, out IKeybinding binding))
                    bindings.Add(networkBinding.InputActionId, BindOwner(new Keybinding(Owner, networkBinding)));
                else
                {
                    if (binding.PendingDelete)
                        binding.EnqueueDelete(false);

                    binding.Update(networkBinding);
                }   
            }
        }

        internal Action<CharacterContext> CaptureNormalSave(object token, string database) => characterPersistence.CaptureNormal(token, database);
        private Keybinding BindOwner(Keybinding binding)
        {
            if (InputSet == InputSets.Character) binding.CharacterSave = context =>
            {
                lock (characterPersistence.Gate)
                {
                    if (!bindings.TryGetValue(binding.InputActionId, out var owned) || !ReferenceEquals(owned, binding))
                    {
                        var error = new InvalidOperationException("Keybinding lost its input-set owner.");
                        context.RejectSave(error); throw error;
                    }
                    Save(context);
                }
            };
            if (InputSet == InputSets.Account) binding.AccountSave = context =>
            {
                lock (accountPersistence.Gate)
                {
                    if (!bindings.TryGetValue(binding.InputActionId, out var owned) || !ReferenceEquals(owned, binding))
                    {
                        context.RejectSaveUnit();
                        throw new InvalidOperationException("Keybinding lost its account input-set owner.");
                    }
                    Save(context);
                }
            };
            return binding;
        }
        private AccountKeybindingPersistence.Snapshot CaptureAccountBindings()
        {
            if (bindings.Any(pair => pair.Key != pair.Value.InputActionId || pair.Value.Owner != Owner))
                throw new InvalidOperationException("Keybinding changed its loaded account or action key.");
            return new(bindings.Values.Where(row => !row.PendingDelete).OrderBy(row => row.InputActionId)
                .Select(row => new AccountKeybindingPersistence.Binding(checked((uint)row.Owner), row.InputActionId, row.DeviceEnum00, row.DeviceEnum01, row.DeviceEnum02,
                    row.Code00, row.Code01, row.Code02, row.MetaKeys00, row.MetaKeys01, row.MetaKeys02, row.EventTypeEnum00, row.EventTypeEnum01, row.EventTypeEnum02)).ToArray());
        }
        private void AcceptAccountBindings(AccountKeybindingPersistence.Snapshot accepted)
        {
            var present = accepted.Bindings.Select(row => row.Action).ToHashSet();
            foreach (var row in bindings.Where(pair => pair.Value.PendingDelete && !present.Contains(pair.Key)).ToArray()) bindings.Remove(row.Key);
        }
        private CharacterKeybindingPersistence.Snapshot CaptureCharacterBindings()
        {
            validateCharacterOwner?.Invoke();
            if (bindings.Any(pair => pair.Key != pair.Value.InputActionId || pair.Value.Owner != Owner))
                throw new InvalidOperationException("Keybinding changed its loaded Character or action key.");
            return new(selectedInputSet?.Invoke(), bindings.Values.Where(row => !row.PendingDelete).OrderBy(row => row.InputActionId)
                .Select(row => new CharacterKeybindingPersistence.Binding(row.Owner, row.InputActionId, row.DeviceEnum00, row.DeviceEnum01, row.DeviceEnum02,
                    row.Code00, row.Code01, row.Code02, row.MetaKeys00, row.MetaKeys01, row.MetaKeys02, row.EventTypeEnum00, row.EventTypeEnum01, row.EventTypeEnum02)).ToArray());
        }
        private void AcceptCharacterBindings(CharacterKeybindingPersistence.Snapshot accepted)
        {
            var present = accepted.Bindings.Select(row => row.Action).ToHashSet();
            foreach (var row in bindings.Where(pair => pair.Value.PendingDelete && !present.Contains(pair.Key)).ToArray()) bindings.Remove(row.Key);
        }

        public IEnumerator<IKeybinding> GetEnumerator()
        {
            lock (PersistenceGate) return ((IEnumerable<IKeybinding>)bindings.Values.Where(b => !b.PendingDelete).ToArray()).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
