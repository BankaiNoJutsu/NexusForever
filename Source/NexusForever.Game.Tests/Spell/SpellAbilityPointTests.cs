using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Abilities;

namespace NexusForever.Game.Tests.Spell;

public class SpellAbilityPointTests
{
    [Fact]
    public void CharacterContext_MapsBonusAbilityTierPointColumn()
    {
        using CharacterContext context = CreateCharacterContext();
        IEntityType entityType = context.Model.FindEntityType(typeof(CharacterModel));
        StoreObjectIdentifier table = StoreObjectIdentifier.Table("character", null);
        IProperty property = entityType.FindProperty(nameof(CharacterModel.BonusAbilityTierPoints));

        Assert.Equal("bonusAbilityTierPoints", property.GetColumnName(table));
        Assert.Equal("tinyint(3) unsigned", property.GetColumnType());
        Assert.Equal((byte)0, property.GetDefaultValue());
    }

    [Fact]
    public void ActionSet_WithPersistedBonus_StartsWithExpandedBudget()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);

        var actionSet = new ActionSet(0, player, bonusTierPoints: 1);

        Assert.Equal((byte)43, actionSet.TierPoints);
    }

    [Fact]
    public void AddAbilityTierPoints_PropagatesUnlockAndSendsUpdatedBudgetOnce()
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);

        IActionSet[] actionSets = Enumerable.Range(0, ActionSet.MaxActionSets)
            .Select(index => (IActionSet)new ActionSet((byte)index, player))
            .ToArray();

        var manager = (global::NexusForever.Game.Entity.SpellManager)RuntimeHelpers.GetUninitializedObject(
            typeof(global::NexusForever.Game.Entity.SpellManager));
        SetPrivateField(manager, "player", player);
        SetPrivateField(manager, "actionSets", actionSets);
        SetPrivateField(manager, "spells", new Dictionary<uint, ICharacterSpell>());

        manager.AddAbilityTierPoints(1);
        manager.AddAbilityTierPoints(1);

        Assert.All(actionSets, actionSet => Assert.Equal((byte)43, actionSet.TierPoints));

        RecordingDispatchProxy<IGameSession>.Invocation send = Assert.Single(
            sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        var packet = Assert.IsType<ServerAbilityPoints>(send.Arguments[0]);
        Assert.Equal(43u, packet.AbilityPoints);
        Assert.Equal(43u, packet.TotalAbilityPoints);
        Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.RequestSave)));

        using CharacterContext context = CreateCharacterContext();
        var character = new CharacterModel { Id = 42ul };
        context.Attach(character);

        manager.Save(context);

        Assert.Equal((byte)1, character.BonusAbilityTierPoints);
        Assert.True(context.Entry(character).Property(p => p.BonusAbilityTierPoints).IsModified);
    }

    private static CharacterContext CreateCharacterContext()
    {
        DbContextOptions<CharacterContext> options = new DbContextOptionsBuilder<CharacterContext>()
            .UseMySql(
                "Server=127.0.0.1;Database=nexus_forever_character;User ID=nexus_forever;Password=nexus_forever;",
                new MySqlServerVersion(new Version(8, 0, 0)))
            .Options;

        return new CharacterContext(options);
    }

    private static void SetPrivateField<T>(T instance, string fieldName, object value)
    {
        typeof(T).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(instance, value);
    }
}
