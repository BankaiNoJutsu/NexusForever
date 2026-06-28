using System.ComponentModel.DataAnnotations;

namespace NexusForever.GameTable.Configuration.Model
{
    public class GameTableConfig
    {
        [Required]
        public string GameTablePath { get; set; }
        public bool ValidateRequiredTablesOnStartup { get; set; } = true;
        public bool StrictRequiredTables { get; set; }
        public bool LoadOptionalTablesOnStartup { get; set; }
        public CacheConfig Cache { get; set; }
    }
}
