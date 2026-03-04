using System;
using System.ComponentModel.DataAnnotations;

namespace AnalyticsBackend.Models
{
    public class Sales
    {
        [Key]
        public int SaleID { get; set; }
        public int EmployeeID { get; set; }
        public int RegionID { get; set; }
        public int ProductID { get; set; }
        public DateTime SaleMonth { get; set; }
        public decimal Quantity { get; set; }
    }
}