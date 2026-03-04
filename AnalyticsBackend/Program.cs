using System.Collections.Generic;     
using Microsoft.Azure.Devices.Shared; 
using Newtonsoft.Json; 
using AnalyticsBackend.Data;      
using AnalyticsBackend.Services;  
using Microsoft.EntityFrameworkCore; 

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ReportsService>();
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}
app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.Run();