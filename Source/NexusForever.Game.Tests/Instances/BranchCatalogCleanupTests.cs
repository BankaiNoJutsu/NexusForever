namespace NexusForever.Game.Tests.Instances;

public class BranchCatalogCleanupTests
{
    [Theory]
    [InlineData("HydrofluxLogicAndWaterEntityScript")]
    [InlineData("MnemesisLogicAndWaterEntityScript")]
    public void RejectedAllInOneDatascapeMechanicScripts_AreNotPortedWithoutBehaviorProof(string scriptName)
    {
        string scriptRoot = Path.Combine(FindRepositoryRoot(), "Source", "NexusForever.Script.Instance");

        foreach (string sourceFile in Directory.EnumerateFiles(scriptRoot, "*.cs", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(sourceFile);

            Assert.DoesNotContain(scriptName, source, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("Adventure", "WarOfTheWilds", "PublicEventCreature.cs")]
    [InlineData("Dungeon", "ProtogamesAcademy", "PublicEventCreature.cs")]
    [InlineData("Dungeon", "RuinsOfKelVoreth", "PublicEventCreature.cs")]
    [InlineData("Dungeon", "Skullcano", "PublicEventCreature.cs")]
    [InlineData("EventInstance", "ShadesEve", "PublicEventCreature.cs")]
    [InlineData("Expedition", "Gauntlet", "PublicEventCreature.cs")]
    [InlineData("Expedition", "FragmentZero", "PublicEventCreature.cs")]
    [InlineData("Expedition", "Infestation", "PublicEventCreature.cs")]
    [InlineData("Expedition", "OutpostM13", "PublicEventCreature.cs")]
    [InlineData("Expedition", "SpaceMadness", "PublicEventCreature.cs")]
    [InlineData("Expedition", "SpaceMadness", "CommunicatorMessage.cs")]
    [InlineData("Raid", "Datascape", "PublicEventCreature.cs")]
    [InlineData("Raid", "GeneticArchives", "PublicEventCreature.cs")]
    [InlineData("Raid", "InitializationCoreY83", "PublicEventCreature.cs")]
    [InlineData("Raid", "RedMoonTerror", "PublicEventCreature.cs")]
    public void BranchOnlyDoorMarkerAndEmptyCatalogs_StayUnportedUntilConsumed(
        string contentType,
        string contentName,
        string catalogFile)
    {
        string catalogPath = Path.Combine(
            FindRepositoryRoot(),
            "Source",
            "NexusForever.Script.Instance",
            contentType,
            contentName,
            catalogFile);

        Assert.False(File.Exists(catalogPath), $"{catalogPath} should stay absent until a mapped runtime consumer needs it.");
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo directory = new(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Source", "NexusForever.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate NexusForever repository root.");
    }
}
