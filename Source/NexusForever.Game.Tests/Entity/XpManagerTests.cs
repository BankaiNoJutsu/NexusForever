using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Entity;

public class XpManagerTests
{
    [Fact]
    public void CalculateLevelForXp_UsesHighestLevelAtOrBelowTotalXp()
    {
        GameTableManager gameTableManager = CreateGameTableManager(
            new XpPerLevelEntry { Id = 1u, MinXpForLevel = 0u },
            new XpPerLevelEntry { Id = 2u, MinXpForLevel = 100u },
            new XpPerLevelEntry { Id = 3u, MinXpForLevel = 300u },
            new XpPerLevelEntry { Id = 4u, MinXpForLevel = 600u });

        Assert.Equal(1, XpManager.CalculateLevelForXp(0u, gameTableManager));
        Assert.Equal(2, XpManager.CalculateLevelForXp(299u, gameTableManager));
        Assert.Equal(3, XpManager.CalculateLevelForXp(300u, gameTableManager));
        Assert.Equal(4, XpManager.CalculateLevelForXp(600u, gameTableManager));
    }

    [Fact]
    public void CalculateLevelForXp_WithMissingXpTableReturnsLevelOne()
    {
        GameTableManager gameTableManager = CreateGameTableManagerWithoutXpPerLevel();

        Assert.Equal(1, XpManager.CalculateLevelForXp(1000u, gameTableManager));
    }

    [Fact]
    public void ResolveStoredLevel_RepairsZeroLevelFromTotalXpWithoutLoweringStoredLevel()
    {
        GameTableManager gameTableManager = CreateGameTableManager(
            new XpPerLevelEntry { Id = 1u, MinXpForLevel = 0u },
            new XpPerLevelEntry { Id = 2u, MinXpForLevel = 100u },
            new XpPerLevelEntry { Id = 3u, MinXpForLevel = 300u },
            new XpPerLevelEntry { Id = 4u, MinXpForLevel = 600u });

        Assert.Equal(3, XpManager.ResolveStoredLevel(0, 300u, gameTableManager));
        Assert.Equal(4, XpManager.ResolveStoredLevel(4, 300u, gameTableManager));
    }

    [Fact]
    public void GrantXp_WithMissingNextLevelRowAddsExperienceWithoutLeveling()
    {
        GameTableManager gameTableManager = CreateGameTableManager(
            new XpPerLevelEntry { Id = 1u, MinXpForLevel = 0u });

        IPlayer player = CreatePlayer(level: 1u, characterClass: Class.Warrior, out var sessionProxy, out var achievementManagerProxy, out var spellManagerProxy);
        var manager = new XpManager(player, new CharacterModel { TotalXp = 0u }, gameTableManager);

        manager.GrantXp(100u);

        Assert.Equal(100u, manager.TotalXp);
        Assert.Equal(1u, player.Level);

        RecordingDispatchProxy<IGameSession>.Invocation sessionCall = Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        var experienceGained = Assert.IsType<ServerExperienceGained>(sessionCall.Arguments[0]);
        Assert.Equal(100u, experienceGained.TotalXpGained);

        Assert.Empty(achievementManagerProxy.GetInvocations(nameof(ICharacterAchievementManager.SetAchievementProgress)));
        Assert.Empty(achievementManagerProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements)));
        Assert.Empty(spellManagerProxy.GetInvocations(nameof(ISpellManager.GrantSpells)));
    }

    [Fact]
    public void SetLevel_IncreasingLevelAwardsProgressionAndPositiveExperienceDelta()
    {
        GameTableManager gameTableManager = CreateGameTableManager(
            new XpPerLevelEntry { Id = 3u, MinXpForLevel = 300u },
            new XpPerLevelEntry { Id = 4u, MinXpForLevel = 600u });

        IPlayer player = CreatePlayer(level: 3u, characterClass: Class.Warrior, out var playerProxy, out var sessionProxy, out var achievementManagerProxy, out var spellManagerProxy);
        var manager = new XpManager(player, new CharacterModel { TotalXp = 300u }, gameTableManager);

        manager.SetLevel(4);

        Assert.Equal(600u, manager.TotalXp);
        Assert.Equal(4u, player.Level);

        RecordingDispatchProxy<IGameSession>.Invocation sessionCall = Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        var experienceGained = Assert.IsType<ServerExperienceGained>(sessionCall.Arguments[0]);
        Assert.Equal(300u, experienceGained.TotalXpGained);

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation achievementCall =
            Assert.Single(achievementManagerProxy.GetInvocations(nameof(ICharacterAchievementManager.SetAchievementProgress)));
        Assert.Same(player, achievementCall.Arguments[0]);
        Assert.Equal(AchievementType.CharacterLevel, achievementCall.Arguments[1]);
        Assert.Equal(0u, achievementCall.Arguments[2]);
        Assert.Equal(0u, achievementCall.Arguments[3]);
        Assert.Equal(4u, achievementCall.Arguments[4]);

        RecordingDispatchProxy<IPlayer>.Invocation fanfareCall =
            Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.CastSpell)));
        Assert.Equal(53378u, fanfareCall.Arguments[0]);
        Assert.Equal((byte)3, fanfareCall.Arguments[1]);
        Assert.IsAssignableFrom<NexusForever.Game.Abstract.Spell.ISpellParameters>(fanfareCall.Arguments[2]);

        Assert.Single(spellManagerProxy.GetInvocations(nameof(ISpellManager.GrantSpells)));
    }

    [Fact]
    public void SetLevel_WithMissingTargetLevelRowDoesNotMutateOrSend()
    {
        GameTableManager gameTableManager = CreateGameTableManager(
            new XpPerLevelEntry { Id = 3u, MinXpForLevel = 300u });

        IPlayer player = CreatePlayer(level: 3u, characterClass: Class.Warrior, out var sessionProxy, out var achievementManagerProxy, out var spellManagerProxy);
        var manager = new XpManager(player, new CharacterModel { TotalXp = 300u }, gameTableManager);

        manager.SetLevel(4);

        Assert.Equal(300u, manager.TotalXp);
        Assert.Equal(3u, player.Level);
        Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        Assert.Empty(achievementManagerProxy.GetInvocations(nameof(ICharacterAchievementManager.SetAchievementProgress)));
        Assert.Empty(achievementManagerProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements)));
        Assert.Empty(spellManagerProxy.GetInvocations(nameof(ISpellManager.GrantSpells)));
    }

    [Fact]
    public void SetLevel_DecreasingLevelDoesNotRegressProgressionOrWrapExperienceDelta()
    {
        GameTableManager gameTableManager = CreateGameTableManager(
            new XpPerLevelEntry { Id = 3u, MinXpForLevel = 300u },
            new XpPerLevelEntry { Id = 4u, MinXpForLevel = 600u });

        IPlayer player = CreatePlayer(level: 4u, characterClass: Class.Warrior, out var sessionProxy, out var achievementManagerProxy, out var spellManagerProxy);
        var manager = new XpManager(player, new CharacterModel { TotalXp = 600u }, gameTableManager);

        manager.SetLevel(3);

        Assert.Equal(300u, manager.TotalXp);
        Assert.Equal(3u, player.Level);

        RecordingDispatchProxy<IGameSession>.Invocation sessionCall = Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        var experienceGained = Assert.IsType<ServerExperienceGained>(sessionCall.Arguments[0]);
        Assert.Equal(0u, experienceGained.TotalXpGained);

        Assert.Empty(achievementManagerProxy.GetInvocations(nameof(ICharacterAchievementManager.SetAchievementProgress)));
        Assert.Empty(achievementManagerProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements)));
        Assert.Empty(spellManagerProxy.GetInvocations(nameof(ISpellManager.GrantSpells)));
    }

    private static IPlayer CreatePlayer(
        uint level,
        Class characterClass,
        out RecordingDispatchProxy<IGameSession> sessionProxy,
        out RecordingDispatchProxy<ICharacterAchievementManager> achievementManagerProxy,
        out RecordingDispatchProxy<ISpellManager> spellManagerProxy)
    {
        return CreatePlayer(level, characterClass, out _, out sessionProxy, out achievementManagerProxy, out spellManagerProxy);
    }

    private static IPlayer CreatePlayer(
        uint level,
        Class characterClass,
        out RecordingDispatchProxy<IPlayer> playerProxy,
        out RecordingDispatchProxy<IGameSession> sessionProxy,
        out RecordingDispatchProxy<ICharacterAchievementManager> achievementManagerProxy,
        out RecordingDispatchProxy<ISpellManager> spellManagerProxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
        ICharacterAchievementManager achievementManager = RecordingDispatchProxy<ICharacterAchievementManager>.Create(out achievementManagerProxy);
        ISpellManager spellManager = RecordingDispatchProxy<ISpellManager>.Create(out spellManagerProxy);

        playerProxy.SetProperty(nameof(IPlayer.Level), level);
        playerProxy.SetProperty(nameof(IPlayer.Class), characterClass);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievementManager);
        playerProxy.SetProperty(nameof(IPlayer.SpellManager), spellManager);
        playerProxy.SetProperty(nameof(IPlayer.SignatureEnabled), false);

        return player;
    }

    private static GameTableManager CreateGameTableManager(params XpPerLevelEntry[] xpPerLevelEntries)
    {
        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.XpPerLevel), CreateGameTable(xpPerLevelEntries));

        return gameTableManager;
    }

    private static GameTableManager CreateGameTableManagerWithoutXpPerLevel()
    {
        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));

        return gameTableManager;
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);

        FieldInfo idField = typeof(T).GetField("Id", BindingFlags.Instance | BindingFlags.Public)!;
        uint maxId = entries.Select(entry => (uint)idField.GetValue(entry)!).DefaultIfEmpty().Max();
        var lookup = Enumerable.Repeat(-1, (int)maxId + 1).ToArray();
        for (int i = 0; i < entries.Length; i++)
            lookup[(int)(uint)idField.GetValue(entries[i])!] = i;

        SetField(table, "lookup", lookup);
        SetField(table, "header", new GameTableHeader
        {
            MaxId = (ulong)lookup.Length
        });

        return table;
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(instance, value);
    }

    private static void SetField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(instance, value);
    }
}
