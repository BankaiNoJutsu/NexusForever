using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using Microsoft.Extensions.Logging;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Abilities;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Spell;
using NetworkItem = NexusForever.Network.World.Message.Model.Shared.Item;
using NetworkItemLocation = NexusForever.Network.World.Message.Model.Shared.ItemLocation;

namespace NexusForever.Game.Tests.Spell;

public class ActionSetSaveRequestTests
{
    [Fact]
    public void AddShortcut_RequestsOwnerSave()
    {
        ActionSet actionSet = CreateActionSet(out RecordingDispatchProxy<IPlayer> playerProxy);

        actionSet.AddShortcut(UILocation.LAS1, ShortcutType.GameCommand, 1u, 0);

        Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.RequestSave)));
    }

    [Fact]
    public void UpdateSpellShortcut_RequestsOwnerSave()
    {
        ActionSet actionSet = CreateActionSet(out RecordingDispatchProxy<IPlayer> playerProxy);
        actionSet.AddShortcut(CreateShortcutModel(UILocation.LAS1, 1234u, tier: 1));
        playerProxy.Invocations.Clear();

        actionSet.UpdateSpellShortcut(1234u, 3);

        Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.RequestSave)));
    }

    [Fact]
    public void RemovePersistedShortcut_RequestsOwnerSave()
    {
        ActionSet actionSet = CreateActionSet(out RecordingDispatchProxy<IPlayer> playerProxy);
        actionSet.AddShortcut(CreateShortcutModel(UILocation.LAS1, 1234u, tier: 1));
        playerProxy.Invocations.Clear();

        actionSet.RemoveShortcut(UILocation.LAS1);

        Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.RequestSave)));
    }

    [Fact]
    public void AddShortcutFromExistingModel_DoesNotRequestOwnerSave()
    {
        ActionSet actionSet = CreateActionSet(out RecordingDispatchProxy<IPlayer> playerProxy);

        actionSet.AddShortcut(CreateShortcutModel(UILocation.LAS1, 1234u, tier: 1));

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.RequestSave)));
    }

    [Fact]
    public void BuildServerActionSet_UsesLasSlotLocationForSpellShortcut()
    {
        ActionSet actionSet = CreateActionSetWithSpellItem(1234u, abilityBagIndex: 27u);
        actionSet.AddShortcut(CreateShortcutModel(UILocation.LAS2, 1234u, tier: 1));

        ServerActionSet serverActionSet = actionSet.BuildServerActionSet();

        ServerActionSet.Action action = serverActionSet.Actions[(int)UILocation.LAS2];
        Assert.Equal(ShortcutType.SpellbookItem, action.ShortcutType);
        Assert.Equal(1234u, action.ObjectId);
        Assert.Equal(InventoryLocation.Ability, action.Location.Location);
        Assert.Equal((uint)UILocation.LAS2, action.Location.BagIndex);
    }

    [Fact]
    public void ClientSetSpecHandler_RequestsOwnerSaveWhenSpecChanges()
    {
        IWorldSession session = CreateSetSpecSession(SpecError.Ok, activeActionSet: 1, out RecordingDispatchProxy<IPlayer> playerProxy);
        var handler = new ClientSetSpecHandler();

        handler.HandleMessage(session, ReadSetSpec(1));

        Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.RequestSave)));
    }

    [Fact]
    public void ClientSetSpecHandler_DoesNotRequestOwnerSaveWhenSpecChangeFails()
    {
        IWorldSession session = CreateSetSpecSession(SpecError.InCombat, activeActionSet: 0, out RecordingDispatchProxy<IPlayer> playerProxy);
        var handler = new ClientSetSpecHandler();

        handler.HandleMessage(session, ReadSetSpec(1));

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.RequestSave)));
    }

    [Fact]
    public void ClientRequestActionSetChangesHandler_RequestsOwnerSaveWhenUseSetClearsSlot()
    {
        IWorldSession session = CreateUseSetSession(out RecordingDispatchProxy<IWorldSession> sessionProxy, out RecordingDispatchProxy<IPlayer> playerProxy);
        var handler = new ClientRequestActionSetChangesHandler(CreateMatchManager(), CreateGameTableManager(), CreateGlobalSpellManager(), CreateActionSetChangesLogger());

        handler.HandleMessage(session, ReadActionSetChanges(new uint[ClientRequestActionSetChanges.ActionCount]));

        Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.RequestSave)));
        IReadOnlyList<RecordingDispatchProxy<IWorldSession>.Invocation> encryptedMessages = sessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted));
        Assert.Contains(encryptedMessages, i => i.Arguments[0] is ServerActionSetClearCache);
        Assert.Contains(encryptedMessages, i => i.Arguments[0] is ServerActionSet);
    }

    [Fact]
    public void ClientRequestActionSetChangesHandler_StoresSpell4BaseIdWhenClientSendsSpell4Id()
    {
        const uint spell4Id     = 2001u;
        const uint spell4BaseId = 1234u;

        IWorldSession session = CreateUseSetSessionWithSpell(
            spell4BaseId,
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out _,
            out ActionSet actionSet);
        var handler = new ClientRequestActionSetChangesHandler(
            CreateMatchManager(),
            CreateGameTableManager(new Spell4Entry
            {
                Id                    = spell4Id,
                Spell4BaseIdBaseSpell = spell4BaseId
            }),
            CreateGlobalSpellManager(spell4BaseId),
            CreateActionSetChangesLogger());

        uint[] actions = new uint[ClientRequestActionSetChanges.ActionCount];
        actions[(int)UILocation.LAS1] = spell4Id;
        handler.HandleMessage(session, ReadActionSetChanges(actions));

        IActionSetShortcut shortcut = actionSet.GetShortcut(UILocation.LAS1);
        Assert.NotNull(shortcut);
        Assert.Equal(ShortcutType.SpellbookItem, shortcut.ShortcutType);
        Assert.Equal(spell4BaseId, shortcut.ObjectId);

        IReadOnlyList<RecordingDispatchProxy<IWorldSession>.Invocation> encryptedMessages = sessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted));
        Assert.Contains(encryptedMessages, i => i.Arguments[0] is ServerActionSetClearCache);
        Assert.Contains(encryptedMessages, i => i.Arguments[0] is ServerActionSet);
    }

    [Fact]
    public void ClientRequestActionSetChangesHandler_PrefersKnownBaseIdWhenIdAlsoExistsAsSpell4Id()
    {
        const uint knownSpell4BaseId   = 1234u;
        const uint collidingSpell4Base = 5678u;

        IWorldSession session = CreateUseSetSessionWithSpell(
            knownSpell4BaseId,
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out _,
            out ActionSet actionSet);
        var handler = new ClientRequestActionSetChangesHandler(
            CreateMatchManager(),
            CreateGameTableManager(new Spell4Entry
            {
                Id                    = knownSpell4BaseId,
                Spell4BaseIdBaseSpell = collidingSpell4Base
            }),
            CreateGlobalSpellManager(knownSpell4BaseId, collidingSpell4Base),
            CreateActionSetChangesLogger());

        uint[] actions = new uint[ClientRequestActionSetChanges.ActionCount];
        actions[(int)UILocation.LAS1] = knownSpell4BaseId;
        handler.HandleMessage(session, ReadActionSetChanges(actions));

        IActionSetShortcut shortcut = actionSet.GetShortcut(UILocation.LAS1);
        Assert.NotNull(shortcut);
        Assert.Equal(knownSpell4BaseId, shortcut.ObjectId);

        IReadOnlyList<RecordingDispatchProxy<IWorldSession>.Invocation> encryptedMessages = sessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted));
        Assert.Contains(encryptedMessages, i => i.Arguments[0] is ServerActionSetClearCache);
        Assert.Contains(encryptedMessages, i => i.Arguments[0] is ServerActionSet);
    }

    [Fact]
    public void ClientRequestActionSetChangesHandler_AllowsRuntimeLasSpellWeaponSlot()
    {
        const uint spell4BaseId = 37968u;

        IWorldSession session = CreateUseSetSessionWithSpell(
            spell4BaseId,
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out _,
            out ActionSet actionSet);
        var handler = new ClientRequestActionSetChangesHandler(
            CreateMatchManager(),
            CreateGameTableManager(),
            CreateGlobalSpellManagerWithWeaponSlot((spell4BaseId, 1u)),
            CreateActionSetChangesLogger());

        uint[] actions = new uint[ClientRequestActionSetChanges.ActionCount];
        actions[(int)UILocation.LAS1] = spell4BaseId;
        handler.HandleMessage(session, ReadActionSetChanges(actions));

        IActionSetShortcut shortcut = actionSet.GetShortcut(UILocation.LAS1);
        Assert.NotNull(shortcut);
        Assert.Equal(spell4BaseId, shortcut.ObjectId);

        IReadOnlyList<RecordingDispatchProxy<IWorldSession>.Invocation> encryptedMessages = sessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted));
        Assert.Contains(encryptedMessages, i => i.Arguments[0] is ServerActionSetClearCache);
        Assert.Contains(encryptedMessages, i => i.Arguments[0] is ServerActionSet);
    }

    [Fact]
    public void ClientRequestActionSetChangesHandler_RefreshesAbilityBookAndSelectedAbilityItemsBeforeActionSet()
    {
        const uint spell4BaseId = 37968u;

        IWorldSession session = CreateUseSetSessionWithSpell(
            spell4BaseId,
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out _,
            out _,
            abilityBagIndex: 10u);
        var handler = new ClientRequestActionSetChangesHandler(
            CreateMatchManager(),
            CreateGameTableManager(),
            CreateGlobalSpellManager(spell4BaseId),
            CreateActionSetChangesLogger());

        uint[] actions = new uint[ClientRequestActionSetChanges.ActionCount];
        actions[(int)UILocation.LAS1] = spell4BaseId;
        handler.HandleMessage(session, ReadActionSetChanges(actions));

        List<object> encryptedMessages = sessionProxy
            .GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0])
            .ToList();

        int abilityBookIndex = encryptedMessages.FindIndex(message => message is ServerAbilityBook);
        int clearCacheIndex = encryptedMessages.FindIndex(message => message is ServerActionSetClearCache);
        int itemAddIndex = encryptedMessages.FindIndex(message => message is ServerItemAdd);
        int actionSetIndex = encryptedMessages.FindIndex(message => message is ServerActionSet);
        Assert.True(abilityBookIndex >= 0);
        Assert.True(clearCacheIndex > abilityBookIndex);
        Assert.True(itemAddIndex > clearCacheIndex);
        Assert.True(actionSetIndex > itemAddIndex);

        var itemAdd = Assert.IsType<ServerItemAdd>(encryptedMessages[itemAddIndex]);
        Assert.Equal(spell4BaseId, itemAdd.InventoryItem.Item.Item2Id);
        Assert.Equal(InventoryLocation.Ability, itemAdd.InventoryItem.Item.LocationData.Location);
        Assert.Equal(10u, itemAdd.InventoryItem.Item.LocationData.BagIndex);

        var actionSet = Assert.IsType<ServerActionSet>(encryptedMessages[actionSetIndex]);
        ServerActionSet.Action selectedAction = actionSet.Actions[(int)UILocation.LAS1];
        Assert.Equal(spell4BaseId, selectedAction.ObjectId);
        Assert.Equal(InventoryLocation.Ability, selectedAction.Location.Location);
        Assert.Equal((uint)UILocation.LAS1, selectedAction.Location.BagIndex);
    }

    private static ActionSet CreateActionSet(out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);

        return new ActionSet(0, player);
    }

    private static ActionSet CreateActionSetWithSpellItem(uint spell4BaseId, uint abilityBagIndex)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        ISpellManager spellManager = RecordingDispatchProxy<ISpellManager>.Create(out RecordingDispatchProxy<ISpellManager> spellManagerProxy);
        ICharacterSpell spell = RecordingDispatchProxy<ICharacterSpell>.Create(out RecordingDispatchProxy<ICharacterSpell> spellProxy);
        IItem item = RecordingDispatchProxy<IItem>.Create(out RecordingDispatchProxy<IItem> itemProxy);

        itemProxy.SetProperty(nameof(IItem.Location), InventoryLocation.Ability);
        itemProxy.SetProperty(nameof(IItem.BagIndex), abilityBagIndex);
        spellProxy.SetProperty(nameof(ICharacterSpell.Item), item);
        spellManagerProxy.SetMethodHandler(nameof(ISpellManager.GetSpell), args => (uint)args[0] == spell4BaseId ? spell : null);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty(nameof(IPlayer.SpellManager), spellManager);

        return new ActionSet(0, player);
    }

    private static CharacterActionSetShortcutModel CreateShortcutModel(UILocation location, uint objectId, byte tier)
    {
        return new CharacterActionSetShortcutModel
        {
            Id           = 42ul,
            SpecIndex    = 0,
            Location     = (ushort)location,
            ShortcutType = (byte)ShortcutType.SpellbookItem,
            ObjectId     = objectId,
            Tier         = tier
        };
    }

    private static IWorldSession CreateSetSpecSession(
        SpecError specError,
        byte activeActionSet,
        out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        ISpellManager spellManager = RecordingDispatchProxy<ISpellManager>.Create(out RecordingDispatchProxy<ISpellManager> spellManagerProxy);

        spellManagerProxy.SetMethodReturn(nameof(ISpellManager.SetActiveActionSet), specError);
        spellManagerProxy.SetProperty(nameof(ISpellManager.ActiveActionSet), activeActionSet);
        playerProxy.SetProperty(nameof(IPlayer.SpellManager), spellManager);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);

        return session;
    }

    private static IWorldSession CreateUseSetSession(
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        ISpellManager spellManager = RecordingDispatchProxy<ISpellManager>.Create(out RecordingDispatchProxy<ISpellManager> spellManagerProxy);

        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty(nameof(IPlayer.Identity), new NexusForever.Game.Abstract.Identity { RealmId = 1, Id = 42ul });
        playerProxy.SetProperty(nameof(IPlayer.IsAlive), true);
        playerProxy.SetProperty(nameof(IPlayer.InCombat), false);
        playerProxy.SetProperty(nameof(IPlayer.SpellManager), spellManager);

        var actionSet = new ActionSet(0, player);
        actionSet.AddShortcut(CreateShortcutModel(UILocation.LAS1, 1u, tier: 1));

        spellManagerProxy.SetProperty(nameof(ISpellManager.ActiveActionSet), (byte)0);
        spellManagerProxy.SetMethodReturn(nameof(ISpellManager.GetActionSet), actionSet);
        spellManagerProxy.SetMethodHandler(nameof(ISpellManager.SendServerSpellList), _ =>
        {
            session.EnqueueMessageEncrypted(new ServerAbilityBook());
            return null;
        });
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);

        return session;
    }

    private static IWorldSession CreateUseSetSessionWithSpell(
        uint spell4BaseId,
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        out RecordingDispatchProxy<IPlayer> playerProxy,
        out ActionSet actionSet,
        uint? abilityBagIndex = null)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        ISpellManager spellManager = RecordingDispatchProxy<ISpellManager>.Create(out RecordingDispatchProxy<ISpellManager> spellManagerProxy);
        ICharacterSpell spell = RecordingDispatchProxy<ICharacterSpell>.Create(out RecordingDispatchProxy<ICharacterSpell> spellProxy);

        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty(nameof(IPlayer.Identity), new NexusForever.Game.Abstract.Identity { RealmId = 1, Id = 42ul });
        playerProxy.SetProperty(nameof(IPlayer.IsAlive), true);
        playerProxy.SetProperty(nameof(IPlayer.InCombat), false);
        playerProxy.SetProperty(nameof(IPlayer.SpellManager), spellManager);

        if (abilityBagIndex.HasValue)
        {
            IItem item = RecordingDispatchProxy<IItem>.Create(out RecordingDispatchProxy<IItem> itemProxy);
            var networkItem = new NetworkItem
            {
                Item2Id = spell4BaseId,
                LocationData = new NetworkItemLocation
                {
                    Location = InventoryLocation.Ability,
                    BagIndex = abilityBagIndex.Value
                },
                SellPrices =
                [
                    new NetworkItem.PriceInfo(),
                    new NetworkItem.PriceInfo()
                ]
            };

            itemProxy.SetProperty(nameof(IItem.Id), spell4BaseId);
            itemProxy.SetProperty(nameof(IItem.Location), InventoryLocation.Ability);
            itemProxy.SetProperty(nameof(IItem.BagIndex), abilityBagIndex.Value);
            itemProxy.SetMethodReturn(nameof(IItem.Build), networkItem);
            spellProxy.SetProperty(nameof(ICharacterSpell.Item), item);
        }

        actionSet = new ActionSet(0, player);

        spellManagerProxy.SetProperty(nameof(ISpellManager.ActiveActionSet), (byte)0);
        spellManagerProxy.SetMethodReturn(nameof(ISpellManager.GetActionSet), actionSet);
        spellManagerProxy.SetMethodHandler(nameof(ISpellManager.GetSpell), args => (uint)args[0] == spell4BaseId ? spell : null);
        spellManagerProxy.SetMethodHandler(nameof(ISpellManager.SendServerSpellList), _ =>
        {
            session.EnqueueMessageEncrypted(new ServerAbilityBook());
            return null;
        });
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);

        return session;
    }

    private static IGameTableManager CreateGameTableManager(params Spell4Entry[] spell4Entries)
    {
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> gameTableManagerProxy);
        gameTableManagerProxy.SetProperty(nameof(IGameTableManager.Spell4), CreateGameTable(spell4Entries));
        gameTableManagerProxy.SetProperty(nameof(IGameTableManager.EldanAugmentation), CreateGameTable<EldanAugmentationEntry>());
        return gameTableManager;
    }

    private static IGlobalSpellManager CreateGlobalSpellManager(params uint[] spell4BaseIds)
    {
        return CreateGlobalSpellManagerWithWeaponSlot(spell4BaseIds
            .Select(spell4BaseId => (spell4BaseId, 5u))
            .ToArray());
    }

    private static IGlobalSpellManager CreateGlobalSpellManagerWithWeaponSlot(params (uint Spell4BaseId, uint WeaponSlot)[] spells)
    {
        IGlobalSpellManager globalSpellManager = RecordingDispatchProxy<IGlobalSpellManager>.Create(out RecordingDispatchProxy<IGlobalSpellManager> globalSpellManagerProxy);
        Dictionary<uint, uint> spellWeaponSlots = spells.ToDictionary(spell => spell.Spell4BaseId, spell => spell.WeaponSlot);
        globalSpellManagerProxy.SetMethodHandler(nameof(IGlobalSpellManager.GetSpellBaseInfo), args =>
        {
            uint requestedSpell4BaseId = (uint)args[0];
            if (spellWeaponSlots.TryGetValue(requestedSpell4BaseId, out uint weaponSlot))
                return CreateSpellBaseInfo(requestedSpell4BaseId, weaponSlot);

            throw new ArgumentOutOfRangeException(nameof(requestedSpell4BaseId));
        });

        return globalSpellManager;
    }

    private static ILogger<ClientRequestActionSetChangesHandler> CreateActionSetChangesLogger()
    {
        return RecordingDispatchProxy<ILogger<ClientRequestActionSetChangesHandler>>.Create(out _);
    }

    private static ISpellBaseInfo CreateSpellBaseInfo(uint spell4BaseId, uint weaponSlot = 5u)
    {
        ISpellBaseInfo spellBaseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out RecordingDispatchProxy<ISpellBaseInfo> spellBaseInfoProxy);
        ISpellInfo spellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out _);

        spellBaseInfoProxy.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry
        {
            Id         = spell4BaseId,
            WeaponSlot = weaponSlot
        });
        spellBaseInfoProxy.SetMethodHandler(nameof(ISpellBaseInfo.GetSpellInfo), args => (byte)args[0] <= ActionSet.MaxTier ? spellInfo : null);

        return spellBaseInfo;
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        SetPrivateField(table, "header", new GameTableHeader
        {
            MaxId = entries.Length == 0 ? 0u : entries.Max(GetEntryId) + 1u
        });
        SetPrivateField(table, "lookup", BuildLookup(entries));
        return table;
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(instance, value);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(instance, value);
    }

    private static int[] BuildLookup<T>(IReadOnlyList<T> entries)
    {
        if (entries.Count == 0)
            return [];

        var lookup = new int[(int)(entries.Max(GetEntryId) + 1u)];
        Array.Fill(lookup, -1);
        for (int i = 0; i < entries.Count; i++)
            lookup[(int)GetEntryId(entries[i])] = i;

        return lookup;
    }

    private static uint GetEntryId<T>(T entry)
    {
        FieldInfo idField = typeof(T).GetField("Id", BindingFlags.Instance | BindingFlags.Public)!;
        return (uint)idField.GetValue(entry)!;
    }

    private static IMatchManager CreateMatchManager()
    {
        IMatchManager matchManager = RecordingDispatchProxy<IMatchManager>.Create(out RecordingDispatchProxy<IMatchManager> matchManagerProxy);
        IMatchCharacter matchCharacter = RecordingDispatchProxy<IMatchCharacter>.Create(out RecordingDispatchProxy<IMatchCharacter> matchCharacterProxy);
        matchCharacterProxy.SetProperty(nameof(IMatchCharacter.Match), null);
        matchManagerProxy.SetMethodReturn(nameof(IMatchManager.GetMatchCharacter), matchCharacter);
        return matchManager;
    }

    private static ClientSetSpec ReadSetSpec(byte specIndex)
    {
        byte[] packetData;
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            writer.Write(specIndex, 3u);
            writer.FlushBits();
            packetData = stream.ToArray();
        }

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientSetSpec();
        packet.Read(reader);
        return packet;
    }

    private static ClientRequestActionSetChanges ReadActionSetChanges(IReadOnlyList<uint> actions)
    {
        byte[] packetData;
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            writer.Write((byte)actions.Count, 4u);
            foreach (uint action in actions)
                writer.Write(action);

            writer.Write((byte)0, 3u);
            writer.Write((byte)0, 5u);
            writer.Write((byte)0, 7u);
            writer.FlushBits();
            packetData = stream.ToArray();
        }

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientRequestActionSetChanges();
        packet.Read(reader);
        return packet;
    }
}
