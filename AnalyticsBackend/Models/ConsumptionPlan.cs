using System;
using System.ComponentModel.DataAnnotations;

namespace AnalyticsBackend.Models
{
    public class ConsumptionPlan
    {
        [Key]
        public int ConsumptionPlanID { get; set; }
        public int RegionID { get; set; }
        public int ProductID { get; set; }
        public DateTime Month { get; set; }
        public decimal PlannedQuantity { get; set; }
    }
}
