using System;
using System.ComponentModel.DataAnnotations;

namespace AnalyticsBackend.Models
{
    public class Consumption
    {
        [Key]
        public int ConsumptionID { get; set; }
        public int RegionID { get; set; }
        public int ProductID { get; set; }
        public DateTime Month { get; set; }
        public decimal Quantity { get; set; }
    }
}