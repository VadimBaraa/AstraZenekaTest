using System;
using System.ComponentModel.DataAnnotations;

namespace AnalyticsBackend.Models
{
    public class Region
    {
        [Key]
        public int RegionID { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MacroTerritoryID { get; set; }
    }
}
