using Nexus.Archive;

if (args.Length == 0 || args.Any(a => a is "-h" or "--help"))
    return ShowUsage(args.Length == 0 ? 1 : 0);

if (args.Length < 3)
    return ShowUsage(1);

string patchDirectory = Path.GetFullPath(args[0]);
string queryText = args[1];
string outputDirectory = Path.GetFullPath(args[2]);
bool listOnly = args.Skip(3).Any(a => a.Equals("--list-only", StringComparison.OrdinalIgnoreCase));

if (!Directory.Exists(patchDirectory))
{
    Console.Error.WriteLine($"Patch directory does not exist: {patchDirectory}");
    return 1;
}

string indexPath = Path.Combine(patchDirectory, "ClientData.index");
if (!File.Exists(indexPath))
{
    Console.Error.WriteLine($"ClientData.index was not found below patch directory: {patchDirectory}");
    return 1;
}

if (!listOnly)
    Directory.CreateDirectory(outputDirectory);

string coreDataArchivePath = Path.Combine(patchDirectory, "CoreData.archive");
ArchiveFile? coreDataArchive = File.Exists(coreDataArchivePath)
    ? ArchiveFileBase.FromFile(coreDataArchivePath) as ArchiveFile
    : null;

try
{
    using Archive archive = Archive.FromFile(indexPath, coreDataArchive);
    List<IArchiveFileEntry> entries = GetArchiveEntries(archive, queryText).ToList();

    Console.WriteLine($"Matched {entries.Count:N0} archive file(s).");
    foreach (IArchiveFileEntry entry in entries)
    {
        Console.WriteLine(entry.Path);
        if (listOnly)
            continue;

        string destinationPath = Path.Combine(outputDirectory, entry.Path.Replace('/', Path.DirectorySeparatorChar));
        string? destinationDirectory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(destinationDirectory))
            Directory.CreateDirectory(destinationDirectory);

        using Stream source = archive.OpenFileStream(entry);
        using FileStream destination = File.Create(destinationPath);
        source.CopyTo(destination);
    }
}
finally
{
    coreDataArchive?.Dispose();
}

return 0;

static IEnumerable<IArchiveFileEntry> GetArchiveEntries(Archive archive, string queryText)
{
    if (queryText.StartsWith("contains:", StringComparison.OrdinalIgnoreCase))
    {
        string needle = queryText["contains:".Length..];
        return archive.IndexFile.GetFiles()
            .Where(entry => entry.Path.Contains(needle, StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry => entry.Path, StringComparer.OrdinalIgnoreCase);
    }

    return archive.IndexFile.GetFiles(queryText)
        .OrderBy(entry => entry.Path, StringComparer.OrdinalIgnoreCase);
}

static int ShowUsage(int exitCode)
{
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  dotnet run --project Tools/ArchiveProbe -- <PatchDir> <ArchivePath|contains:Text> <OutputDir> [--list-only]");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Examples:");
    Console.Error.WriteLine(@"  dotnet run --project Tools/ArchiveProbe -- ""D:\Games\WildStar\Patch"" contains:Houston Tools\ArchiveProbe\out --list-only");
    Console.Error.WriteLine(@"  dotnet run --project Tools/ArchiveProbe -- ""D:\Games\WildStar\Patch"" ClientData/Houston/Houston.chm Tools\ArchiveProbe\out");
    return exitCode;
}
