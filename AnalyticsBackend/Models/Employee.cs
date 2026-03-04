using System;
using System.ComponentModel.DataAnnotations;

namespace AnalyticsBackend.Models
{
    public class Employee
    {
        [Key]
        public int EmployeeID { get; set; }
        public string Name { get; set; } = string.Empty;
        public int RegionID { get; set; }
    }
}
