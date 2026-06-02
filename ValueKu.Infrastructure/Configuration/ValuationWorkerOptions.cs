namespace ValueKu.Infrastructure.Configuration;

public sealed class ValuationWorkerOptions
{
    public const string SectionName = "ValuationWorker";

    /// <summary>How often the worker re-values assets. Defaults to once per day.</summary>
    public int IntervalHours { get; set; } = 24;

    /// <summary>Run one valuation pass immediately on startup (so fresh databases are populated).</summary>
    public bool RunOnStartup { get; set; } = true;
}
