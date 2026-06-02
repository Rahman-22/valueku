using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ValueKu.Core.Interfaces;
using ValueKu.Infrastructure.Configuration;

namespace ValueKu.Infrastructure.BackgroundJobs;

/// <summary>
/// Runs the depreciation/appreciation engine on a schedule (daily by default). Re-values
/// every asset and appends the day's AssetValuationHistory row. Runs once on startup so a
/// freshly migrated database is populated immediately.
/// </summary>
public sealed class AssetValuationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AssetValuationWorker> _logger;
    private readonly ValuationWorkerOptions _options;

    public AssetValuationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<AssetValuationWorker> logger,
        IOptions<ValuationWorkerOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.RunOnStartup)
            await RunOnceAsync(stoppingToken);

        var hours = _options.IntervalHours <= 0 ? 24 : _options.IntervalHours;
        using var timer = new PeriodicTimer(TimeSpan.FromHours(hours));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await RunOnceAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IAssetValuationService>();

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var updated = await service.RevalueAllAsync(today, ct);

            _logger.LogInformation("AssetValuationWorker revalued {Count} asset(s) as of {Date}.", updated, today);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "AssetValuationWorker run failed.");
        }
    }
}
