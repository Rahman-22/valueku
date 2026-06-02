using System.Globalization;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ValueKu.Core.Entities;
using ValueKu.Core.Enums;
using ValueKu.Core.Interfaces;
using ValueKu.Core.Models;
using ValueKu.Infrastructure.Persistence;

namespace ValueKu.Infrastructure.Services;

/// <summary>Generates the monthly financial health statement as a PDF using QuestPDF.</summary>
public sealed class ReportService : IReportService
{
    private readonly ApplicationDbContext _db;
    private readonly INetWorthService _netWorth;

    private static readonly CultureInfo Myr = CultureInfo.GetCultureInfo("ms-MY");

    public ReportService(ApplicationDbContext db, INetWorthService netWorth)
    {
        _db = db;
        _netWorth = netWorth;
    }

    public async Task<byte[]> GenerateMonthlyStatementAsync(int userId, int year, int month, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw new InvalidOperationException("User not found.");

        var periodStart = new DateTime(year, month, 1);
        var periodEnd = periodStart.AddMonths(1).AddTicks(-1);

        var assets = await _db.Assets.Where(a => a.UserId == userId).OrderBy(a => a.Name).ToListAsync(ct);
        var accounts = await _db.Accounts.Where(a => a.UserId == userId).OrderBy(a => a.Name).ToListAsync(ct);
        var monthTxns = await _db.Transactions
            .Where(t => t.Account!.UserId == userId && t.TransactionDate >= periodStart && t.TransactionDate <= periodEnd)
            .ToListAsync(ct);

        var snapshot = await _netWorth.GetSnapshotAsync(userId, ct);
        var history = await _netWorth.GetHistoryAsync(userId, 12, ct);

        var incomeByCat = monthTxns
            .Where(t => t.Type == TransactionType.Income)
            .GroupBy(t => t.Category)
            .Select(g => (Category: g.Key.ToString(), Total: g.Sum(t => t.Amount)))
            .OrderByDescending(x => x.Total)
            .ToList();

        var expenseByCat = monthTxns
            .Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => t.Category)
            .Select(g => (Category: g.Key.ToString(), Total: g.Sum(t => t.Amount)))
            .OrderByDescending(x => x.Total)
            .ToList();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(t => t.FontSize(10).FontColor(Colors.Grey.Darken3));

                page.Header().Element(h => ComposeHeader(h, user.Username, periodStart));
                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Spacing(20);
                    ComposeBalanceSheet(col, assets, accounts, snapshot);
                    ComposeCashFlow(col, incomeByCat, expenseByCat);
                    ComposeTrend(col, history);
                });
                page.Footer().AlignCenter().Text(t =>
                {
                    t.DefaultTextStyle(s => s.FontSize(8).FontColor(Colors.Grey.Medium));
                    t.Span($"ValueKu  •  Generated {DateTime.Now:dd MMM yyyy HH:mm}  •  Page ");
                    t.CurrentPageNumber();
                    t.Span(" of ");
                    t.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static string Fmt(decimal value) => value.ToString("C", Myr);

    private static void ComposeHeader(IContainer container, string username, DateTime period)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(c =>
            {
                c.Item().Text("ValueKu").FontSize(22).Bold().FontColor(Colors.Indigo.Darken2);
                c.Item().Text("Monthly Financial Health Statement").FontSize(11).FontColor(Colors.Grey.Darken1);
            });
            row.ConstantItem(190).AlignRight().Column(c =>
            {
                c.Item().AlignRight().Text(period.ToString("MMMM yyyy")).FontSize(14).Bold();
                c.Item().AlignRight().Text($"Prepared for {username}").FontSize(9).FontColor(Colors.Grey.Darken1);
                c.Item().AlignRight().Text("Currency: MYR (RM)").FontSize(9).FontColor(Colors.Grey.Darken1);
            });
        });
    }

    private static void ComposeBalanceSheet(ColumnDescriptor col, List<Asset> assets, List<Account> accounts, NetWorthSnapshot snapshot)
    {
        col.Item().Column(section =>
        {
            section.Spacing(8);
            SectionTitle(section, "Net Worth — Balance Sheet");

            section.Item().Row(row =>
            {
                row.RelativeItem().Column(a =>
                {
                    a.Item().Text("Assets").Bold();
                    a.Item().Table(table =>
                    {
                        TwoColumns(table);
                        foreach (var asset in assets)
                        {
                            table.Cell().Element(BodyCell).Text(asset.Name);
                            table.Cell().Element(BodyCell).AlignRight().Text(Fmt(asset.CurrentValue));
                        }
                        foreach (var acc in accounts.Where(x => !x.IsLiability))
                        {
                            table.Cell().Element(BodyCell).Text($"{acc.Name} (cash)");
                            table.Cell().Element(BodyCell).AlignRight().Text(Fmt(acc.Balance));
                        }
                    });
                    TotalRow(a, "Total Assets", snapshot.TotalAssets);
                });

                row.ConstantItem(24);

                row.RelativeItem().Column(l =>
                {
                    l.Item().Text("Liabilities").Bold();
                    l.Item().Table(table =>
                    {
                        TwoColumns(table);
                        var liabilities = accounts.Where(x => x.IsLiability).ToList();
                        if (liabilities.Count == 0)
                        {
                            table.Cell().Element(BodyCell).Text("None").FontColor(Colors.Grey.Medium);
                            table.Cell().Element(BodyCell).AlignRight().Text(Fmt(0));
                        }
                        foreach (var acc in liabilities)
                        {
                            table.Cell().Element(BodyCell).Text(acc.Name);
                            table.Cell().Element(BodyCell).AlignRight().Text(Fmt(acc.Balance));
                        }
                    });
                    TotalRow(l, "Total Liabilities", snapshot.Liabilities);
                });
            });

            section.Item().PaddingTop(6).Background(Colors.Indigo.Lighten5).Padding(8).Row(r =>
            {
                r.RelativeItem().Text("NET WORTH").FontSize(12).Bold();
                r.RelativeItem().AlignRight().Text(Fmt(snapshot.NetWorth)).FontSize(12).Bold().FontColor(Colors.Indigo.Darken2);
            });
        });
    }

    private static void ComposeCashFlow(ColumnDescriptor col, List<(string Category, decimal Total)> income, List<(string Category, decimal Total)> expense)
    {
        var totalIncome = income.Sum(x => x.Total);
        var totalExpense = expense.Sum(x => x.Total);

        col.Item().Column(section =>
        {
            section.Spacing(8);
            SectionTitle(section, "Cash Flow — This Month");

            section.Item().Row(row =>
            {
                row.RelativeItem().Column(a =>
                {
                    a.Item().Text("Income").Bold().FontColor(Colors.Green.Darken1);
                    a.Item().Table(t => CategoryTable(t, income));
                    TotalRow(a, "Total Income", totalIncome);
                });

                row.ConstantItem(24);

                row.RelativeItem().Column(a =>
                {
                    a.Item().Text("Expenses").Bold().FontColor(Colors.Red.Darken1);
                    a.Item().Table(t => CategoryTable(t, expense));
                    TotalRow(a, "Total Expenses", totalExpense);
                });
            });

            var net = totalIncome - totalExpense;
            section.Item().PaddingTop(6).Background(Colors.Grey.Lighten4).Padding(8).Row(r =>
            {
                r.RelativeItem().Text("Net Cash Flow").FontSize(12).Bold();
                r.RelativeItem().AlignRight().Text(Fmt(net)).FontSize(12).Bold()
                    .FontColor(net >= 0 ? Colors.Green.Darken2 : Colors.Red.Darken2);
            });
        });
    }

    private static void ComposeTrend(ColumnDescriptor col, IReadOnlyList<NetWorthPoint> history)
    {
        col.Item().Column(section =>
        {
            section.Spacing(8);
            SectionTitle(section, "12-Month Net Worth Trend");

            section.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2);
                    c.RelativeColumn(2);
                    c.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("Month");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Net Worth");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Change");
                });

                decimal? previous = null;
                foreach (var point in history)
                {
                    var change = previous is null ? (decimal?)null : point.Value - previous.Value;
                    table.Cell().Element(BodyCell).Text(point.Date.ToString("MMM yyyy"));
                    table.Cell().Element(BodyCell).AlignRight().Text(Fmt(point.Value));
                    table.Cell().Element(BodyCell).AlignRight().Text(change is null ? "—" : Fmt(change.Value));
                    previous = point.Value;
                }
            });
        });
    }

    // ---- small shared building blocks --------------------------------------

    private static void SectionTitle(ColumnDescriptor col, string title)
        => col.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).PaddingBottom(4)
            .Text(title).FontSize(13).Bold().FontColor(Colors.Indigo.Darken2);

    private static void TwoColumns(TableDescriptor table)
        => table.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(2); });

    private static void CategoryTable(TableDescriptor table, List<(string Category, decimal Total)> rows)
    {
        TwoColumns(table);
        if (rows.Count == 0)
        {
            table.Cell().Element(BodyCell).Text("No activity").FontColor(Colors.Grey.Medium);
            table.Cell().Element(BodyCell).AlignRight().Text("—");
            return;
        }
        foreach (var (category, total) in rows)
        {
            table.Cell().Element(BodyCell).Text(category);
            table.Cell().Element(BodyCell).AlignRight().Text(Fmt(total));
        }
    }

    private static void TotalRow(ColumnDescriptor col, string label, decimal value)
        => col.Item().BorderTop(1).BorderColor(Colors.Grey.Lighten1).PaddingTop(3).Row(r =>
        {
            r.RelativeItem().Text(label).Bold();
            r.RelativeItem().AlignRight().Text(Fmt(value)).Bold();
        });

    private static IContainer HeaderCell(IContainer c)
        => c.BorderBottom(1).BorderColor(Colors.Grey.Medium).PaddingVertical(4).DefaultTextStyle(t => t.Bold());

    private static IContainer BodyCell(IContainer c)
        => c.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(3);
}
