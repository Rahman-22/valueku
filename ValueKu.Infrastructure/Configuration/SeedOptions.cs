namespace ValueKu.Infrastructure.Configuration;

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    /// <summary>
    /// When true, seed the demo portfolio (assets, accounts, transactions, budgets, goals).
    /// The default admin user is always seeded regardless. Set false for a clean production deploy.
    /// </summary>
    public bool DemoData { get; set; } = true;
}
