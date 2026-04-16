using Microsoft.EntityFrameworkCore;
using sandbattery_backend.Data;
using sandbattery_backend.Filters;
using sandbattery_backend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found in configuration.");

builder.Services.AddDbContext<SandbatteryDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDataService, DataService>();
builder.Services.AddScoped<IControlService, ControlService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IEventsService, EventsService>();

// ProductKeyAuthFilter is instantiated per-request via [TypeFilter] — no explicit registration needed.

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
