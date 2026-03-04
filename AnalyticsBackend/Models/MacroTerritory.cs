using System;
using System.ComponentModel.DataAnnotations;

namespace AnalyticsBackend.Models
{
    public class MacroTerritory
    {
        [Key]
        public int MacroTerritoryID { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
