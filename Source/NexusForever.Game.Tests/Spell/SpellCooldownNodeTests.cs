using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Spell;

public class SpellCooldownNodeTests
{
    private const uint PrimarySpell4Id = 1000u;
    private const uint SiblingSpell4Id = 1001u;
    private const uint FallbackSpell4Id = 2000u;
    private const uint OtherFallbackSpell4Id = 2001u;
    private const uint SharedCooldownNodeId = 35u;
    private const uint FirstGlobalCooldownId = 121u;
    private const uint ShorterGlobalCooldownId = 107u;
    private const uint LongerGlobalCooldownId = 245u;

    [Fact]
    public void SetSpellCooldown_WithSharedCooldownNode_BlocksSiblingAndSendsNodePacket()
    {
        IGameTableManager gameTableManager = CreateGameTableManager(CreateGameTable(
            new Spell4Entry { Id = PrimarySpell4Id, SpellCoolDownId00 = SharedCooldownNodeId },
            new Spell4Entry { Id = SiblingSpell4Id, SpellCoolDownId00 = SharedCooldownNodeId }));
        global::NexusForever.Game.Entity.SpellManager manager = CreateSpellManager(gameTableManager, out RecordingDispatchProxy<IGameSession> sessionProxy);

        manager.SetSpellCooldown(PrimarySpell4Id, 5d);

        Assert.Equal(5d, manager.GetSpellCooldown(PrimarySpell4Id));
        Assert.Equal(5d, manager.GetSpellCooldown(SiblingSpell4Id));
        Assert.True(manager.HasActiveCoolDownNode(SharedCooldownNodeId, gameTableManager));

        ServerCooldown packet = Assert.Single(GetSessionMessages<ServerCooldown>(sessionProxy));
        Assert.Equal(1, packet.Cooldown.Type);
        Assert.Equal(PrimarySpell4Id, packet.Cooldown.SpellId);
        Assert.Equal(SharedCooldownNodeId, packet.Cooldown.TypeId);
        Assert.Equal(5000u, packet.Cooldown.TimeRemaining);
    }

    [Fact]
    public void SetSpellCooldown_WithoutCooldownNode_FallsBackToConcreteSpellId()
    {
        IGameTableManager gameTableManager = CreateGameTableManager(CreateGameTable(
            new Spell4Entry { Id = FallbackSpell4Id },
            new Spell4Entry { Id = OtherFallbackSpell4Id }));
        global::NexusForever.Game.Entity.SpellManager manager = CreateSpellManager(gameTableManager, out RecordingDispatchProxy<IGameSession> sessionProxy);

        manager.SetSpellCooldown(FallbackSpell4Id, 6d);

        Assert.Equal(6d, manager.GetSpellCooldown(FallbackSpell4Id));
        Assert.Equal(0d, manager.GetSpellCooldown(OtherFallbackSpell4Id));
        Assert.False(manager.HasActiveCoolDownNode(FallbackSpell4Id, gameTableManager));

        ServerCooldown packet = Assert.Single(GetSessionMessages<ServerCooldown>(sessionProxy));
        Assert.Equal(1, packet.Cooldown.Type);
        Assert.Equal(FallbackSpell4Id, packet.Cooldown.SpellId);
        Assert.Equal(FallbackSpell4Id, packet.Cooldown.TypeId);
        Assert.Equal(6000u, packet.Cooldown.TimeRemaining);
    }

    [Fact]
    public void Update_ReducesCooldownsByServerTickAndExpiresNodes()
    {
        IGameTableManager gameTableManager = CreateGameTableManager(CreateGameTable(
            new Spell4Entry { Id = PrimarySpell4Id, SpellCoolDownId00 = SharedCooldownNodeId },
            new Spell4Entry { Id = SiblingSpell4Id, SpellCoolDownId00 = SharedCooldownNodeId }));
        global::NexusForever.Game.Entity.SpellManager manager = CreateSpellManager(gameTableManager, out _);

        manager.SetSpellCooldown(PrimarySpell4Id, 5d);
        manager.Update(2d);

        Assert.Equal(3d, manager.GetSpellCooldown(PrimarySpell4Id));
        Assert.Equal(3d, manager.GetSpellCooldown(SiblingSpell4Id));

        manager.Update(3d);

        Assert.Equal(0d, manager.GetSpellCooldown(PrimarySpell4Id));
        Assert.False(manager.HasActiveCoolDownNode(SharedCooldownNodeId, gameTableManager));
    }

    [Fact]
    public void SetGlobalSpellCooldown_UsesTypeZeroCooldownNodeAndDoesNotShortenActiveGlobal()
    {
        IGameTableManager gameTableManager = CreateGameTableManager(CreateGameTable<Spell4Entry>());
        global::NexusForever.Game.Entity.SpellManager manager = CreateSpellManager(gameTableManager, out RecordingDispatchProxy<IGameSession> sessionProxy);

        manager.SetGlobalSpellCooldown(FirstGlobalCooldownId, 0.75d);
        manager.SetGlobalSpellCooldown(ShorterGlobalCooldownId, 0.333d);
        manager.SetGlobalSpellCooldown(LongerGlobalCooldownId, 1d);

        Assert.Equal(1d, manager.GetGlobalSpellCooldown());

        IReadOnlyList<ServerCooldown> packets = GetSessionMessages<ServerCooldown>(sessionProxy);
        Assert.Equal(2, packets.Count);
        Assert.Equal(0, packets[0].Cooldown.Type);
        Assert.Equal(0u, packets[0].Cooldown.SpellId);
        Assert.Equal(FirstGlobalCooldownId, packets[0].Cooldown.TypeId);
        Assert.Equal(750u, packets[0].Cooldown.TimeRemaining);
        Assert.Equal(0, packets[1].Cooldown.Type);
        Assert.Equal(0u, packets[1].Cooldown.SpellId);
        Assert.Equal(LongerGlobalCooldownId, packets[1].Cooldown.TypeId);
        Assert.Equal(1000u, packets[1].Cooldown.TimeRemaining);
    }

    [Fact]
    public void ResetAllSpellCooldowns_ClearsOnlySpellCooldownNodes()
    {
        IGameTableManager gameTableManager = CreateGameTableManager(CreateGameTable(
            new Spell4Entry { Id = PrimarySpell4Id, SpellCoolDownId00 = SharedCooldownNodeId }));
        global::NexusForever.Game.Entity.SpellManager manager = CreateSpellManager(gameTableManager, out RecordingDispatchProxy<IGameSession> sessionProxy);

        manager.SetSpellCooldown(PrimarySpell4Id, 5d);
        manager.SetGlobalSpellCooldown(FirstGlobalCooldownId, 0.75d);

        manager.ResetAllSpellCooldowns();

        Assert.Equal(0d, manager.GetSpellCooldown(PrimarySpell4Id));
        Assert.Equal(0.75d, manager.GetGlobalSpellCooldown());
        Assert.False(manager.HasActiveCoolDownNode(SharedCooldownNodeId, gameTableManager));

        ServerCooldown resetPacket = GetSessionMessages<ServerCooldown>(sessionProxy).Last();
        Assert.Equal(1, resetPacket.Cooldown.Type);
        Assert.Equal(SharedCooldownNodeId, resetPacket.Cooldown.TypeId);
        Assert.Equal(0u, resetPacket.Cooldown.TimeRemaining);
    }

    private static global::NexusForever.Game.Entity.SpellManager CreateSpellManager(
        IGameTableManager gameTableManager,
        out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.IsLoading), false);

        var manager = (global::NexusForever.Game.Entity.SpellManager)RuntimeHelpers.GetUninitializedObject(
            typeof(global::NexusForever.Game.Entity.SpellManager));
        InitialisePrivateField(manager, "activeCooldowns");
        SetPrivateField(manager, "spells", new Dictionary<uint, ICharacterSpell>());
        SetPrivateField(manager, "player", player);
        SetPrivateField(manager, "gameTableManager", gameTableManager);
        return manager;
    }

    private static IGameTableManager CreateGameTableManager(GameTable<Spell4Entry> spell4Entries)
    {
        IGameTableManager manager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> proxy);
        proxy.SetProperty(nameof(IGameTableManager.Spell4), spell4Entries);
        return manager;
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

    private static IReadOnlyList<TMessage> GetSessionMessages<TMessage>(RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        return sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(i => i.Arguments[0])
            .OfType<TMessage>()
            .ToList();
    }

    private static void InitialisePrivateField<T>(T instance, string fieldName)
    {
        FieldInfo field = typeof(T).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(instance, Activator.CreateInstance(field.FieldType));
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
