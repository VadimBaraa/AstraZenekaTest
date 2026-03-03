using Microsoft.EntityFrameworkCore;
using AnalyticsBackend.Models;


namespace AnalyticsBackend.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Sales> Sales { get; set; }
        public DbSet<Consumption> Consumption { get; set; }
        public DbSet<Stock> Stock { get; set; }
        public DbSet<ConsumptionPlan> ConsumptionPlans { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Region> Regions { get; set; }
        public DbSet<MacroTerritory> MacroTerritories { get; set; }
    }
}