using AnalyticsBackend.Data;
using Microsoft.EntityFrameworkCore;

namespace AnalyticsBackend.Services
{
    public class ReportsService
    {
        private readonly ApplicationDbContext _db;
        public ReportsService(ApplicationDbContext db) => _db = db;

        public async Task<IEnumerable<object>> GetSalesVsConsumption(DateTime? from, DateTime? to)
        {
            var query = from s in _db.Sales
                        join c in _db.Consumption
                        on new { s.RegionID, s.ProductID, SaleMonth = s.SaleMonth }
                        equals new { c.RegionID, c.ProductID, SaleMonth = c.Month }
                        select new
                        {
                            s.SaleMonth,
                            s.RegionID,
                            Sales = s.Quantity,
                            Consumption = c.Quantity
                        };

            if (from.HasValue) query = query.Where(x => x.SaleMonth >= from.Value);
            if (to.HasValue) query = query.Where(x => x.SaleMonth <= to.Value);

            return await query.ToListAsync();
        }
    }
}