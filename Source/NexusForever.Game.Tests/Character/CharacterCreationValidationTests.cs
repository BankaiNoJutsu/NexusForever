using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Character;
using NexusForever.Game.Entity;
using NexusForever.Game.Static;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Reputation;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Character;

public class CharacterCreationValidationTests
{
    [Theory]
    [InlineData(CharacterCreationStart.Nexus)]
    [InlineData(CharacterCreationStart.PreTutorial)]
    [InlineData(CharacterCreationStart.Level50)]
    public void IsSupportedStartAllowsSeededLiveStarts(CharacterCreationStart start)
    {
        Assert.True(CharacterCreationValidation.IsSupportedStart(start));
    }

    [Theory]
    [InlineData(CharacterCreationStart.Arkship)]
    [InlineData(CharacterCreationStart.Demo01)]
    [InlineData(CharacterCreationStart.Demo02)]
    [InlineData(CharacterCreationStart.CostumeOnly)]
    public void IsSupportedStartRejectsUnseededOrNonCharacterStarts(CharacterCreationStart start)
    {
        Assert.False(CharacterCreationValidation.IsSupportedStart(start));
    }

    [Fact]
    public void HasSupportedStartingLocationRejectsCostumeOnlyRows()
    {
        CharacterCreationEntry entry = CreateEntry(CharacterCreationStart.Nexus);
        entry.CostumeOnly = true;

        Assert.False(CharacterCreationValidation.HasSupportedStartingLocation(entry, new CharacterManagerStub(true)));
    }

    [Fact]
    public void HasSupportedStartingLocationRejectsMissingLocation()
    {
        CharacterCreationEntry entry = CreateEntry(CharacterCreationStart.Nexus);

        Assert.False(CharacterCreationValidation.HasSupportedStartingLocation(entry, new CharacterManagerStub(false)));
    }

    [Fact]
    public void HasSupportedStartingLocationAllowsSupportedRowsWithLocation()
    {
        CharacterCreationEntry entry = CreateEntry(CharacterCreationStart.Nexus);

        Assert.True(CharacterCreationValidation.HasSupportedStartingLocation(entry, new CharacterManagerStub(true)));
    }

    private static CharacterCreationEntry CreateEntry(CharacterCreationStart start)
    {
        return new CharacterCreationEntry
        {
            RaceId                     = Race.Human,
            FactionId                  = Faction.Exile,
            CharacterCreationStartEnum = start
        };
    }

    private sealed class CharacterManagerStub : ICharacterManager
    {
        private readonly bool hasLocation;

        public CharacterManagerStub(bool hasLocation)
        {
            this.hasLocation = hasLocation;
        }

        public ulong NextCharacterId => 1ul;

        public void Initialise()
        {
        }

        public void AddCharacter(CharacterModel character)
        {
        }

        public void DeleteCharacter(ulong id, string name)
        {
        }

        public bool IsCharacter(string name)
        {
            return false;
        }

        public ulong? GetCharacterIdByName(string name)
        {
            return 0ul;
        }

        public ICharacter GetCharacter(ulong characterId)
        {
            return null;
        }

        public ICharacter GetCharacter(string name)
        {
            return null;
        }

        public ILocation GetStartingLocation(Race race, Faction faction, CharacterCreationStart creationStart)
        {
            return hasLocation ? new Location(new WorldEntry(), default, default) : null;
        }

        public IEnumerable<IPropertyModifier> GetCharacterBaseProperties()
        {
            return [];
        }

        public IEnumerable<IPropertyModifier> GetCharacterClassBaseProperties(Class @class)
        {
            return [];
        }
    }
}
