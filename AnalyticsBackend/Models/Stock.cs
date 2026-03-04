using System;
using System.ComponentModel.DataAnnotations;

namespace AnalyticsBackend.Models
{
    public class Stock
    {
        [Key]
        public int StockID { get; set; }
        public int ProductID { get; set; }
        public int RegionID { get; set; }
        public decimal Quantity { get; set; }
        public DateTime Date { get; set; }
    }
}
