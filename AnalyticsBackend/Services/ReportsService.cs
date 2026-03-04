using Microsoft.EntityFrameworkCore;
using AnalyticsBackend.Data;

namespace AnalyticsBackend.Services
{
    // dataTypeID: 1=Продажи, 2=Уходимость, 3=План уходимости, 4=Остатки
    public class ReportsService
    {
        private readonly ApplicationDbContext _db;
        public ReportsService(ApplicationDbContext db) => _db = db;

        // ─────────────────────────────────────────────────────────────
        // 4. Продажи vs Уходимость по месяцам
        // ─────────────────────────────────────────────────────────────
        public async Task<IEnumerable<object>> GetSalesVsConsumption(DateTime? from, DateTime? to)
        {
            var data = await _db.EmployeeData
                .Where(x => (x.DataTypeID == 1 || x.DataTypeID == 2)
                         && (!from.HasValue || x.MonthDate >= from.Value)
                         && (!to.HasValue   || x.MonthDate <= to.Value))
                .ToListAsync();

            var sales = data.Where(x => x.DataTypeID == 1)
                .GroupBy(x => new { x.MonthDate, x.RegID, x.AreaID })
                .Select(g => new { g.Key.MonthDate, g.Key.RegID, g.Key.AreaID, Vol = g.Sum(x => x.Vol) })
                .ToList();

            var cons = data.Where(x => x.DataTypeID == 2)
                .GroupBy(x => new { x.MonthDate, x.RegID, x.AreaID })
                .Select(g => new { g.Key.MonthDate, g.Key.RegID, g.Key.AreaID, Vol = g.Sum(x => x.Vol) })
                .ToList();

            var result = from s in sales
                         join c in cons on new { s.MonthDate, s.RegID } equals new { c.MonthDate, c.RegID } into cj
                         from c in cj.DefaultIfEmpty()
                         select new
                         {
                             Month       = s.MonthDate.ToString("yyyy-MM"),
                             RegID       = s.RegID,
                             AreaID      = s.AreaID,
                             Sales       = s.Vol,
                             Consumption = c?.Vol ?? 0m
                         };

            return result.OrderBy(x => x.Month).ThenBy(x => x.RegID);
        }

        // ─────────────────────────────────────────────────────────────
        // 3. Прогноз остатков (с выбранного месяца до конца года)
        //    Остаток(мес) = Остаток(пред.мес) + Продажи(мес) - Уходимость(мес)
        // ─────────────────────────────────────────────────────────────
        public async Task<IEnumerable<object>> GetStockForecast(int year, int fromMonth = 7, int? regID = null)
        {
            var forecastStart = new DateTime(year, fromMonth, 1);
            var yearEnd       = new DateTime(year, 12, 1);

            var query = _db.EmployeeData.AsQueryable();
            if (regID.HasValue) query = query.Where(x => x.RegID == regID.Value);

            var data = await query
                .Where(x => x.DataTypeID == 1 || x.DataTypeID == 2 || x.DataTypeID == 4)
                .ToListAsync();

            // Начальные остатки — последний факт до начала прогноза
            var initialStocks = data
                .Where(x => x.DataTypeID == 4 && x.MonthDate < forecastStart)
                .GroupBy(x => x.RegID)
                .Select(g => new { RegID = g.Key, AreaID = g.First().AreaID, Stock = g.OrderByDescending(x => x.MonthDate).First().Vol })
                .ToList();

            var forecastSales = data
                .Where(x => x.DataTypeID == 1 && x.MonthDate >= forecastStart && x.MonthDate <= yearEnd)
                .GroupBy(x => new { x.RegID, x.MonthDate })
                .Select(g => new { g.Key.RegID, g.Key.MonthDate, Vol = g.Sum(x => x.Vol) })
                .ToList();

            var forecastCons = data
                .Where(x => x.DataTypeID == 2 && x.MonthDate >= forecastStart && x.MonthDate <= yearEnd)
                .GroupBy(x => new { x.RegID, x.MonthDate })
                .Select(g => new { g.Key.RegID, g.Key.MonthDate, Vol = g.Sum(x => x.Vol) })
                .ToList();

            int monthCount = 12 - fromMonth + 1;
            var months  = Enumerable.Range(fromMonth, monthCount).Select(m => new DateTime(year, m, 1)).ToList();
            var result  = new List<object>();

            foreach (var reg in initialStocks)
            {
                decimal runningStock = reg.Stock;
                foreach (var month in months)
                {
                    var sales = forecastSales.FirstOrDefault(x => x.RegID == reg.RegID && x.MonthDate == month)?.Vol ?? 0m;
                    var cons  = forecastCons.FirstOrDefault(x => x.RegID == reg.RegID && x.MonthDate == month)?.Vol ?? 0m;
                    runningStock = runningStock + sales - cons;

                    result.Add(new
                    {
                        Month       = month.ToString("yyyy-MM"),
                        RegID       = reg.RegID,
                        AreaID      = reg.AreaID,
                        Stock       = Math.Round(runningStock, 2),
                        Sales       = sales,
                        Consumption = cons
                    });
                }
            }

            return result.OrderBy(x => ((dynamic)x).RegID).ThenBy(x => ((dynamic)x).Month);
        }

        // ─────────────────────────────────────────────────────────────
        // 5. Запасы в месяцах
        //    Формула: Остаток / Средняя Уходимость(последние 3 мес)
        // ─────────────────────────────────────────────────────────────
        public async Task<IEnumerable<object>> GetStockInMonths(DateTime asOf)
        {
            var threeMonthsAgo = asOf.AddMonths(-3);

            var data = await _db.EmployeeData
                .Where(x => (x.DataTypeID == 4 && x.MonthDate <= asOf) ||
                            (x.DataTypeID == 2 && x.MonthDate > threeMonthsAgo && x.MonthDate <= asOf))
                .ToListAsync();

            var stocks = data
                .Where(x => x.DataTypeID == 4)
                .GroupBy(x => x.RegID)
                .Select(g => new { RegID = g.Key, AreaID = g.First().AreaID, Stock = g.OrderByDescending(x => x.MonthDate).First().Vol })
                .ToList();

            var avgCons = data
                .Where(x => x.DataTypeID == 2)
                .GroupBy(x => x.RegID)
                .Select(g => new { RegID = g.Key, AvgVol = g.Average(x => x.Vol) })
                .ToList();

            var result = from s in stocks
                         join a in avgCons on s.RegID equals a.RegID into aj
                         from a in aj.DefaultIfEmpty()
                         let avg = a?.AvgVol ?? 0m
                         select new
                         {
                             RegID         = s.RegID,
                             AreaID        = s.AreaID,
                             StockQty      = s.Stock,
                             AvgConsumption = Math.Round(avg, 2),
                             StockInMonths = avg > 0 ? Math.Round(s.Stock / avg, 2) : (decimal?)null
                         };

            return result.OrderBy(x => x.RegID);
        }

        // ─────────────────────────────────────────────────────────────
        // 6.1 Выполнение плана по сотрудникам
        // ─────────────────────────────────────────────────────────────
        public async Task<IEnumerable<object>> GetPlanFulfillmentByEmployee(int? empID, DateTime? from, DateTime? to)
        {
            var data = await _db.EmployeeData
                .Where(x => (x.DataTypeID == 2 || x.DataTypeID == 3)
                         && (!empID.HasValue || x.EmpID == empID.Value)
                         && (!from.HasValue  || x.MonthDate >= from.Value)
                         && (!to.HasValue    || x.MonthDate <= to.Value))
                .ToListAsync();

            var actual = data.Where(x => x.DataTypeID == 2)
                .GroupBy(x => x.EmpID)
                .Select(g => new { EmpID = g.Key, Actual = g.Sum(x => x.Vol) })
                .ToList();

            var plan = data.Where(x => x.DataTypeID == 3)
                .GroupBy(x => x.EmpID)
                .Select(g => new { EmpID = g.Key, Plan = g.Sum(x => x.Vol) })
                .ToList();

            var allEmps = actual.Select(x => x.EmpID).Union(plan.Select(x => x.EmpID)).Distinct();

            return allEmps.Select(empId =>
            {
                var a = actual.FirstOrDefault(x => x.EmpID == empId)?.Actual ?? 0m;
                var p = plan.FirstOrDefault(x => x.EmpID == empId)?.Plan ?? 0m;
                var pct = p > 0 ? Math.Round(a / p * 100, 1) : (decimal?)null;
                return (object)new
                {
                    EmpID          = empId,
                    Actual         = a,
                    Plan           = p,
                    FulfillmentPct = pct,
                    BonusPct       = CalcBonus(pct)
                };
            }).OrderBy(x => ((dynamic)x).EmpID);
        }

        // ─────────────────────────────────────────────────────────────
        // 6.2 Выполнение плана по макро-территориям
        // ─────────────────────────────────────────────────────────────
        public async Task<IEnumerable<object>> GetPlanFulfillmentByMacroTerritory(int? areaID, DateTime? from, DateTime? to)
        {
            var data = await _db.EmployeeData
                .Where(x => (x.DataTypeID == 2 || x.DataTypeID == 3)
                         && (!areaID.HasValue || x.AreaID == areaID.Value)
                         && (!from.HasValue   || x.MonthDate >= from.Value)
                         && (!to.HasValue     || x.MonthDate <= to.Value))
                .ToListAsync();

            var actual = data.Where(x => x.DataTypeID == 2)
                .GroupBy(x => x.AreaID)
                .Select(g => new { AreaID = g.Key, Actual = g.Sum(x => x.Vol) })
                .ToList();

            var plan = data.Where(x => x.DataTypeID == 3)
                .GroupBy(x => x.AreaID)
                .Select(g => new { AreaID = g.Key, Plan = g.Sum(x => x.Vol) })
                .ToList();

            var allAreas = actual.Select(x => x.AreaID).Union(plan.Select(x => x.AreaID)).Distinct();

            return allAreas.Select(area =>
            {
                var a = actual.FirstOrDefault(x => x.AreaID == area)?.Actual ?? 0m;
                var p = plan.FirstOrDefault(x => x.AreaID == area)?.Plan ?? 0m;
                var pct = p > 0 ? Math.Round(a / p * 100, 1) : (decimal?)null;
                return (object)new
                {
                    AreaID         = area,
                    Actual         = a,
                    Plan           = p,
                    FulfillmentPct = pct,
                    BonusPct       = CalcBonus(pct)
                };
            }).OrderBy(x => ((dynamic)x).AreaID);
        }

        // ─────────────────────────────────────────────────────────────
        // 6.3 Выполнение плана по регионам
        // ─────────────────────────────────────────────────────────────
        public async Task<IEnumerable<object>> GetPlanFulfillmentByRegion(int? regID, DateTime? from, DateTime? to)
        {
            var data = await _db.EmployeeData
                .Where(x => (x.DataTypeID == 2 || x.DataTypeID == 3)
                         && (!regID.HasValue || x.RegID == regID.Value)
                         && (!from.HasValue  || x.MonthDate >= from.Value)
                         && (!to.HasValue    || x.MonthDate <= to.Value))
                .ToListAsync();

            var actual = data.Where(x => x.DataTypeID == 2)
                .GroupBy(x => x.RegID)
                .Select(g => new { RegID = g.Key, AreaID = g.First().AreaID, Actual = g.Sum(x => x.Vol) })
                .ToList();

            var plan = data.Where(x => x.DataTypeID == 3)
                .GroupBy(x => x.RegID)
                .Select(g => new { RegID = g.Key, Plan = g.Sum(x => x.Vol) })
                .ToList();

            var allRegs = actual.Select(x => x.RegID).Union(plan.Select(x => x.RegID)).Distinct();

            return allRegs.Select(reg =>
            {
                var a    = actual.FirstOrDefault(x => x.RegID == reg);
                var p    = plan.FirstOrDefault(x => x.RegID == reg)?.Plan ?? 0m;
                var act  = a?.Actual ?? 0m;
                var pct  = p > 0 ? Math.Round(act / p * 100, 1) : (decimal?)null;
                return (object)new
                {
                    RegID          = reg,
                    AreaID         = a?.AreaID ?? 0,
                    Actual         = act,
                    Plan           = p,
                    FulfillmentPct = pct,
                    BonusPct       = CalcBonus(pct)
                };
            }).OrderBy(x => ((dynamic)x).RegID);
        }

        // ─────────────────────────────────────────────────────────────
        // 7. Бонус
        //    < 95% → 0%, 95-100% → 0.5%, 100-105% → 1.0%, 105-110% → 1.3%, >=110% → 2.0%
        // ─────────────────────────────────────────────────────────────
        private static decimal CalcBonus(decimal? pct)
        {
            if (pct == null) return 0m;
            return pct switch
            {
                < 95m  => 0m,
                < 100m => 0.5m,
                < 105m => 1.0m,
                < 110m => 1.3m,
                _      => 2.0m
            };
        }

        // ─────────────────────────────────────────────────────────────
        // 8. Прогнозные продажи для достижения запасов = targetStockMonths
        //    X = target * AvgCons - Остаток(пред) + Уходимость(мес)
        // ─────────────────────────────────────────────────────────────
        public async Task<IEnumerable<object>> GetForecastSalesForTargetStockMonths(int year, decimal targetStockMonths = 2.1m)
        {
            var julyStart      = new DateTime(year, 7, 1);
            var yearEnd        = new DateTime(year, 12, 1);
            var threeMonthsAgo = julyStart.AddMonths(-3);

            var data = await _db.EmployeeData
                .Where(x => x.DataTypeID == 1 || x.DataTypeID == 2 || x.DataTypeID == 4)
                .ToListAsync();

            var initialStocks = data
                .Where(x => x.DataTypeID == 4 && x.MonthDate < julyStart)
                .GroupBy(x => x.RegID)
                .Select(g => new { RegID = g.Key, AreaID = g.First().AreaID, Stock = g.OrderByDescending(x => x.MonthDate).First().Vol })
                .ToList();

            var avgCons = data
                .Where(x => x.DataTypeID == 2 && x.MonthDate >= threeMonthsAgo && x.MonthDate < julyStart)
                .GroupBy(x => x.RegID)
                .Select(g => new { RegID = g.Key, AvgVol = g.Average(x => x.Vol) })
                .ToList();

            var forecastCons = data
                .Where(x => x.DataTypeID == 2 && x.MonthDate >= julyStart && x.MonthDate <= yearEnd)
                .GroupBy(x => new { x.RegID, x.MonthDate })
                .Select(g => new { g.Key.RegID, g.Key.MonthDate, Vol = g.Sum(x => x.Vol) })
                .ToList();

            var months = Enumerable.Range(7, 6).Select(m => new DateTime(year, m, 1)).ToList();
            var result = new List<object>();

            foreach (var reg in initialStocks)
            {
                decimal runningStock = reg.Stock;
                var avg = avgCons.FirstOrDefault(x => x.RegID == reg.RegID)?.AvgVol ?? 0m;

                foreach (var month in months)
                {
                    var cons = forecastCons.FirstOrDefault(x => x.RegID == reg.RegID && x.MonthDate == month)?.Vol ?? 0m;
                    var requiredSales = avg > 0 ? Math.Max(0m, targetStockMonths * avg - runningStock + cons) : 0m;
                    runningStock = runningStock + requiredSales - cons;

                    result.Add(new
                    {
                        Month          = month.ToString("yyyy-MM"),
                        RegID          = reg.RegID,
                        AreaID         = reg.AreaID,
                        RequiredSales  = Math.Round(requiredSales, 2),
                        ProjectedStock = Math.Round(runningStock, 2),
                        AvgConsumption = Math.Round(avg, 2),
                        StockInMonths  = avg > 0 ? Math.Round(runningStock / avg, 2) : (decimal?)null,
                        Consumption    = cons
                    });
                }
            }

            return result.OrderBy(x => ((dynamic)x).RegID).ThenBy(x => ((dynamic)x).Month);
        }
    }
}