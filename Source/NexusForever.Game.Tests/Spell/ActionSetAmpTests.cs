using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Spell;
using NexusForever.GameTable.Model;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace NexusForever.Game.Tests.Spell;

public class ActionSetAmpTests
{
    [Fact]
    public void Save_WithHighClientAmpId_PersistsFullUShortId()
    {
        DbContextOptions<CharacterContext> options = new DbContextOptionsBuilder<CharacterContext>()
            .UseMySql(
                "Server=127.0.0.1;Database=nexus_forever_character;User ID=nexus_forever;Password=nexus_forever;",
                new MySqlServerVersion(new Version(8, 0, 0)))
            .Options;

        using var context = new CharacterContext(options);
        var actionSet = (ActionSet)RuntimeHelpers.GetUninitializedObject(typeof(ActionSet));
        var amp = new ActionSetAmp(
            actionSet,
            new EldanAugmentationEntry
            {
                Id = 976
            },
            true);

        amp.Save(context);

        CharacterActionSetAmpModel model = Assert.Single(context.ChangeTracker.Entries<CharacterActionSetAmpModel>()).Entity;
        Assert.Equal((ushort)976, model.AmpId);
    }
}
