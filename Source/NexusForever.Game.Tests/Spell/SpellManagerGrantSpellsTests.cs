using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Abilities;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Tests.Spell;

public class SpellManagerGrantSpellsTests
{
    private const uint Spell4Id = 2001u;
    private const uint Spell4BaseId = 1001u;
    private const uint OccupiedSpell4BaseId = 9001u;
    private const uint LockedSlotPrerequisiteId = 42u;
    private const uint UnlockedSlotPrerequisiteId = 43u;

    [Fact]
    public void GrantSpells_WithNewLevelSpellAddsShortcutToFirstEmptyUnlockedLasSlot()
    {
        IPlayer player = CreatePlayer(
            level: 1u,
            isLoading: false,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<IGameSession> sessionProxy);
        var manager = new global::NexusForever.Game.Entity.SpellManager(
            player,
            new CharacterModel
            {
                ActiveSpec = 0,
                ActionSetShortcut =
                {
                    CreateShortcut(UILocation.LAS1, OccupiedSpell4BaseId)
                }
            },
            CreatePrerequisiteManager(id => id == UnlockedSlotPrerequisiteId),
            CreateGlobalSpellManager(CreateSpellBaseInfo(Spell4BaseId, Spell4Id)),
            CreateGameTableManager(
                spellLevels:
                [
                    new SpellLevelEntry
                    {
                        Id             = 1u,
                        ClassId        = (byte)Class.Warrior,
                        CharacterLevel = 2u,
                        Spell4Id       = Spell4Id
                    }
                ],
                actionSlotPrerequisites:
                [
                    CreateActionSlotPrerequisite(UILocation.LAS1, UnlockedSlotPrerequisiteId),
                    CreateActionSlotPrerequisite(UILocation.LAS2, LockedSlotPrerequisiteId),
                    CreateActionSlotPrerequisite(UILocation.LAS3, UnlockedSlotPrerequisiteId)
                ]));

        playerProxy.SetProperty(nameof(IPlayer.Level), 2u);

        manager.GrantSpells();

        IActionSetShortcut shortcut = manager.GetActionSet(0).GetShortcut(UILocation.LAS3);
        Assert.NotNull(shortcut);
        Assert.Equal(ShortcutType.SpellbookItem, shortcut.ShortcutType);
        Assert.Equal(Spell4BaseId, shortcut.ObjectId);
        Assert.Equal((byte)1, shortcut.Tier);

        IReadOnlyList<RecordingDispatchProxy<IGameSession>.Invocation> messages =
            sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted));
        Assert.IsType<ServerSpellUpdate>(messages[0].Arguments[0]);
        Assert.IsType<ServerActionSetClearCache>(messages[1].Arguments[0]);
        ServerActionSet actionSet = Assert.IsType<ServerActionSet>(messages[2].Arguments[0]);
        Assert.Equal(Spell4BaseId, actionSet.Actions[(int)UILocation.LAS3].ObjectId);

        Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.RequestSave)));
    }

    [Fact]
    public void GrantSpells_DuringInitialLoadDoesNotRewriteActionSet()
    {
        IPlayer player = CreatePlayer(
            level: 2u,
            isLoading: true,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<IGameSession> sessionProxy);

        var manager = new global::NexusForever.Game.Entity.SpellManager(
            player,
            new CharacterModel { ActiveSpec = 0 },
            CreatePrerequisiteManager(_ => true),
            CreateGlobalSpellManager(CreateSpellBaseInfo(Spell4BaseId, Spell4Id)),
            CreateGameTableManager(
                spellLevels:
                [
                    new SpellLevelEntry
                    {
                        Id             = 1u,
                        ClassId        = (byte)Class.Warrior,
                        CharacterLevel = 2u,
                        Spell4Id       = Spell4Id
                    }
                ],
                actionSlotPrerequisites:
                [
                    CreateActionSlotPrerequisite(UILocation.LAS1, UnlockedSlotPrerequisiteId)
                ]));

        Assert.NotNull(manager.GetSpell(Spell4BaseId));
        Assert.Null(manager.GetActionSet(0).GetShortcut(UILocation.LAS1));
        Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.RequestSave)));
    }

    private static IPlayer CreatePlayer(
        uint level,
        bool isLoading,
        out RecordingDispatchProxy<IPlayer> playerProxy,
        out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out RecordingDispatchProxy<IInventory> inventoryProxy);

        inventoryProxy.SetMethodHandler(nameof(IInventory.SpellCreate), args => CreateItem(((Spell4BaseEntry)args[0]).Id));

        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 7ul);
        playerProxy.SetProperty(nameof(IPlayer.Class), Class.Warrior);
        playerProxy.SetProperty(nameof(IPlayer.Level), level);
        playerProxy.SetProperty(nameof(IPlayer.IsLoading), isLoading);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);

        return player;
    }

    private static IItem CreateItem(uint id)
    {
        IItem item = RecordingDispatchProxy<IItem>.Create(out RecordingDispatchProxy<IItem> itemProxy);
        itemProxy.SetProperty(nameof(IItem.Id), id);
        return item;
    }

    private static CharacterActionSetShortcutModel CreateShortcut(UILocation location, uint spell4BaseId)
    {
        return new CharacterActionSetShortcutModel
        {
            Id           = 7ul,
            SpecIndex    = 0,
            Location     = (ushort)location,
            ShortcutType = (byte)ShortcutType.SpellbookItem,
            ObjectId     = spell4BaseId,
            Tier         = 1
        };
    }

    private static ActionSlotPrereqEntry CreateActionSlotPrerequisite(UILocation location, uint prerequisiteId)
    {
        return new ActionSlotPrereqEntry
        {
            Id                   = (uint)location + 1u,
            SlotIndex            = (uint)location,
            PrerequisiteIdUnlock = prerequisiteId
        };
    }

    private static IPrerequisiteManager CreatePrerequisiteManager(Func<uint, bool> meets)
    {
        IPrerequisiteManager prerequisiteManager = RecordingDispatchProxy<IPrerequisiteManager>.Create(
            out RecordingDispatchProxy<IPrerequisiteManager> prerequisiteProxy);
        prerequisiteProxy.SetMethodHandler(nameof(IPrerequisiteManager.Meets), args => meets((uint)args[1]));
        return prerequisiteManager;
    }

    private static IGlobalSpellManager CreateGlobalSpellManager(params ISpellBaseInfo[] spellBaseInfos)
    {
        Dictionary<uint, ISpellBaseInfo> spellBaseInfoById = spellBaseInfos.ToDictionary(info => info.Entry.Id);
        IGlobalSpellManager globalSpellManager = RecordingDispatchProxy<IGlobalSpellManager>.Create(
            out RecordingDispatchProxy<IGlobalSpellManager> globalSpellProxy);
        globalSpellProxy.SetMethodHandler(nameof(IGlobalSpellManager.GetSpellBaseInfo), args => spellBaseInfoById[(uint)args[0]]);
        return globalSpellManager;
    }

    private static ISpellBaseInfo CreateSpellBaseInfo(uint spell4BaseId, uint spell4Id)
    {
        ISpellBaseInfo spellBaseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(
            out RecordingDispatchProxy<ISpellBaseInfo> spellBaseProxy);
        ISpellInfo spellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out RecordingDispatchProxy<ISpellInfo> spellInfoProxy);

        var spellEntry = new Spell4Entry
        {
            Id                    = spell4Id,
            Spell4BaseIdBaseSpell = spell4BaseId,
            TierIndex             = 1u
        };

        spellBaseProxy.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry
        {
            Id = spell4BaseId
        });
        spellBaseProxy.SetProperty(nameof(ISpellBaseInfo.SpellType), new Spell4SpellTypesEntry
        {
            Id = 5u
        });
        spellBaseProxy.SetMethodHandler(nameof(ISpellBaseInfo.GetSpellInfo), args => (byte)args[0] == 1 ? spellInfo : null);

        spellInfoProxy.SetProperty(nameof(ISpellInfo.Entry), spellEntry);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.BaseInfo), spellBaseInfo);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.PrerequisiteRunners), new List<PrerequisiteEntry>());
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Thresholds), new List<Spell4ThresholdsEntry>());

        return spellBaseInfo;
    }

    private static IGameTableManager CreateGameTableManager(
        IReadOnlyList<SpellLevelEntry> spellLevels,
        IReadOnlyList<ActionSlotPrereqEntry> actionSlotPrerequisites)
    {
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(
            out RecordingDispatchProxy<IGameTableManager> gameTableProxy);
        gameTableProxy.SetProperty(nameof(IGameTableManager.SpellLevel), CreateGameTable(spellLevels));
        gameTableProxy.SetProperty(nameof(IGameTableManager.Spell4), CreateGameTable(new Spell4Entry
        {
            Id                    = Spell4Id,
            Spell4BaseIdBaseSpell = Spell4BaseId,
            TierIndex             = 1u
        }));
        gameTableProxy.SetProperty(nameof(IGameTableManager.Class), CreateGameTable(new ClassEntry
        {
            Id                            = (uint)Class.Warrior,
            Spell4IdInnateAbilityActive  = [],
            Spell4IdInnateAbilityPassive = [],
            Spell4IdAttackPrimary        = [],
            Spell4IdAttackUnarmed        = []
        }));
        gameTableProxy.SetProperty(nameof(IGameTableManager.ActionSlotPrereq), CreateGameTable(actionSlotPrerequisites));
        return gameTableManager;
    }

    private static GameTable<T> CreateGameTable<T>(IReadOnlyList<T> entries) where T : class, new()
    {
        T[] entryArray = entries.ToArray();
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entryArray);
        SetPrivateField(table, "header", new GameTableHeader
        {
            MaxId = entryArray.Length == 0 ? 0u : entryArray.Max(GetEntryId) + 1u
        });
        SetPrivateField(table, "lookup", BuildLookup(entryArray));
        return table;
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        return CreateGameTable((IReadOnlyList<T>)entries);
    }

    private static void SetAutoProperty<T>(T instance, string propertyName, object value)
    {
        SetPrivateField(instance, $"<{propertyName}>k__BackingField", value);
    }

    private static void SetPrivateField<T>(T instance, string fieldName, object value)
    {
        typeof(T).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(instance, value);
    }

    private static int[] BuildLookup<T>(IReadOnlyList<T> entries)
    {
        if (entries.Count == 0)
            return [];

        int[] lookup = Enumerable.Repeat(-1, (int)(entries.Max(GetEntryId) + 1u)).ToArray();
        for (int i = 0; i < entries.Count; i++)
            lookup[GetEntryId(entries[i])] = i;

        return lookup;
    }

    private static uint GetEntryId<T>(T entry)
    {
        return (uint)typeof(T).GetField("Id", BindingFlags.Instance | BindingFlags.Public)!.GetValue(entry)!;
    }
}
