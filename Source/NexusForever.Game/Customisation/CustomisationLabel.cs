using NexusForever.Game.Abstract.Customisation;
using NexusForever.Game.Static.Reputation;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Customisation
{
    public class CustomisationLabel : ICustomisationLabel
    {
        public uint Id { get; }
        public string Name { get; }
        public Faction Faction { get; }

        public CustomisationLabel(CharacterCustomizationLabelEntry entry, string name)
        {
            Id      = entry.Id;
            Name    = name;
            Faction = (Faction)entry.Faction2Id;
        }
    }
}
