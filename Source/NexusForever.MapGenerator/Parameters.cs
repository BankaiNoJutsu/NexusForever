using CommandLine;

namespace NexusForever.MapGenerator
{
    public class Parameters
    {
        [Option('i', "patchPath",
            HelpText = "The location of the WildStar client patch folder.")]
        public string PatchPath { get; set; }

        [Option('e', "extract",
            HelpText = "Extract all game tables (.tbl) and localisation files (.bin).")]
        public bool Extract { get; set; }

        [Option('g', "generate",
            HelpText = "Generate base map files (.nfmap) from the area files.")]
        public bool Generate { get; set; }

        [Option("prepare", HelpText = "Validate a complete asset cache or repair it through staged extraction and map generation.")]
        public bool Prepare { get; set; }

        [Option("validateTables", HelpText = "Validate runtime table headers without extracting or changing files.")]
        public string ValidateTables { get; set; }

        [Option("validateMaps", HelpText = "Validate generated map files without changing them.")]
        public string ValidateMaps { get; set; }

        [Option('o', "output",
            HelpText = "The base directory where output files will be created.", Default = "")]
        public string OutputDir { get; set; }

        [Option('j', "maxParallelism",
            HelpText = "Maximum number of parallel workers to use. Use 0 to auto-select a bounded value.", Default = 0)]
        public int MaxParallelism { get; set; }

        [Option("debug")]
        public bool Debug { get; set; }

        // generation specific options
        [Option("worldId")]
        public ushort? WorldId { get; set; }

        [Option("gridX")]
        public byte? GridX { get; set; }

        [Option("gridY")]
        public byte? GridY { get; set; }
    }
}
