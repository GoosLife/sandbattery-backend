using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using sandbattery_backend.Data;
using sandbattery_backend.Data.Entities;

namespace sandbattery_backend.Services;

public class ElectricityPriceFetcherService : BackgroundService
{
    private static readonly string[] Areas = ["DK1", "DK2"];

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ElectricityPriceFetcherService> _logger;

    public ElectricityPriceFetcherService(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<ElectricityPriceFetcherService> logger)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await FetchAllAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await FetchAllAsync(stoppingToken);
    }

    private async Task FetchAllAsync(CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        foreach (var area in Areas)
        {
            await FetchForDateAsync(today, area, ct);
            await FetchForDateAsync(tomorrow, area, ct); // 404 until ~13:00 CET — silently ignored
        }
    }

    private async Task FetchForDateAsync(DateTime date, string area, CancellationToken ct)
    {
        var url = $"https://www.elprisenligenu.dk/api/v1/prices/{date:yyyy}/{date:MM-dd}_{area}.json";
        var client = _httpClientFactory.CreateClient();

        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(url, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not reach elprisenligenu.dk ({Date} {Area})", date.ToString("yyyy-MM-dd"), area);
            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            if ((int)response.StatusCode != 404)
                _logger.LogWarning("elprisenligenu.dk returned {Code} for {Date} {Area}", (int)response.StatusCode, date.ToString("yyyy-MM-dd"), area);
            return;
        }

        List<ElspotEntry>? entries;
        try
        {
            entries = await response.Content.ReadFromJsonAsync<List<ElspotEntry>>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse elprisenligenu.dk response ({Date} {Area})", date.ToString("yyyy-MM-dd"), area);
            return;
        }

        if (entries is null || entries.Count == 0) return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SandbatteryDbContext>();
        var dateStr = date.ToString("yyyy-MM-dd");

        var existing = await db.ElectricityPrices
            .Include(e => e.Entries)
            .FirstOrDefaultAsync(e => e.Date == dateStr && e.Area == area, ct);

        if (existing is null)
        {
            existing = new ElectricityPriceEntity { Date = dateStr, Area = area, Currency = "DKK" };
            db.ElectricityPrices.Add(existing);
            await db.SaveChangesAsync(ct); // need Id before adding entries
        }
        else
        {
            db.PriceEntries.RemoveRange(existing.Entries);
        }

        existing.LastUpdated = DateTime.UtcNow;

        db.PriceEntries.AddRange(entries.Select(e => new PriceEntryEntity
        {
            ElectricityPriceId = existing.Id,
            Hour = e.TimeStart.UtcDateTime,
            PriceDkkKwh = e.DkkPerKwh
        }));

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Electricity prices updated: {Date} {Area} ({Count} entries)", dateStr, area, entries.Count);
    }

    // ── elprisenligenu.dk response DTO ────────────────────────────────────────

    private sealed class ElspotEntry
    {
        [JsonPropertyName("DKK_per_kWh")]
        public float DkkPerKwh { get; set; }

        [JsonPropertyName("time_start")]
        public DateTimeOffset TimeStart { get; set; }
    }
}
