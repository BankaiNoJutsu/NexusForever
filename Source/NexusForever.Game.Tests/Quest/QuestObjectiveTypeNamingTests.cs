using NexusForever.Game.Static.Quest;

namespace NexusForever.Game.Tests.Quest;

public class QuestObjectiveTypeNamingTests
{
    [Theory]
    [InlineData(27, nameof(QuestObjectiveType.Unknown27))]
    [InlineData(29, nameof(QuestObjectiveType.Unknown29))]
    public void PlaceholderObjectiveTypes_RemainUnknownUntilObjectiveDataOwnerIsProven(int tableId, string expectedName)
    {
        Assert.Equal((QuestObjectiveType)tableId, Enum.Parse<QuestObjectiveType>(expectedName));
        Assert.Equal(expectedName, Enum.GetName((QuestObjectiveType)tableId));
    }
}
