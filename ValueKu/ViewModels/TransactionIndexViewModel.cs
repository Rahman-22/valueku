using Microsoft.AspNetCore.Mvc.Rendering;
using ValueKu.Core.Entities;
using ValueKu.Core.Enums;

namespace ValueKu.ViewModels;

public class TransactionIndexViewModel
{
    public IReadOnlyList<Transaction> Items { get; set; } = [];
    public IEnumerable<SelectListItem> Accounts { get; set; } = [];

    // Echoed filter values.
    public int? AccountId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public TransactionType? Type { get; set; }
    public TransactionCategory? Category { get; set; }
    public string? Search { get; set; }

    // Paging.
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }

    // Totals for the filtered set.
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
}
