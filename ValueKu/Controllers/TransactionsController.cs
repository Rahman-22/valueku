using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ValueKu.Core.Entities;
using ValueKu.Core.Enums;
using ValueKu.Core.Interfaces;
using ValueKu.Core.Specifications;
using ValueKu.ViewModels;

namespace ValueKu.Controllers;

public class TransactionsController : AppControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly INetWorthService _netWorth;

    public TransactionsController(IUnitOfWork uow, INetWorthService netWorth)
    {
        _uow = uow;
        _netWorth = netWorth;
    }

    private const int PageSize = 20;

    public async Task<IActionResult> Index(
        int? accountId, DateTime? from, DateTime? to,
        TransactionType? type, TransactionCategory? category, string? search,
        int page = 1, CancellationToken ct = default)
    {
        var fromDate = from?.Date;
        var toDate = to?.Date.AddDays(1).AddTicks(-1);

        var all = await _uow.Repository<Transaction>()
            .ListAsync(new TransactionsByUserSpec(CurrentUserId, fromDate, toDate), ct);

        IEnumerable<Transaction> filtered = all;
        if (accountId is { } aid) filtered = filtered.Where(t => t.AccountId == aid);
        if (type is { } ty) filtered = filtered.Where(t => t.Type == ty);
        if (category is { } cat) filtered = filtered.Where(t => t.Category == cat);
        if (!string.IsNullOrWhiteSpace(search))
            filtered = filtered.Where(t => t.Description != null &&
                                           t.Description.Contains(search, StringComparison.OrdinalIgnoreCase));

        var list = filtered.ToList();
        var totalPages = Math.Max(1, (int)Math.Ceiling(list.Count / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        var vm = new TransactionIndexViewModel
        {
            Items = list.Skip((page - 1) * PageSize).Take(PageSize).ToList(),
            Accounts = await AccountSelectListAsync(ct, accountId),
            AccountId = accountId,
            From = from,
            To = to,
            Type = type,
            Category = category,
            Search = search,
            Page = page,
            TotalPages = totalPages,
            TotalCount = list.Count,
            TotalIncome = list.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
            TotalExpense = list.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount)
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var model = new TransactionFormViewModel
        {
            TransactionDate = DateTime.Today,
            Accounts = await AccountSelectListAsync(ct)
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TransactionFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            model.Accounts = await AccountSelectListAsync(ct, model.AccountId);
            return View(model);
        }

        var account = await GetOwnedAccountAsync(model.AccountId, ct);
        if (account is null)
        {
            ModelState.AddModelError(nameof(model.AccountId), "Invalid account.");
            model.Accounts = await AccountSelectListAsync(ct, model.AccountId);
            return View(model);
        }

        var transaction = new Transaction
        {
            AccountId = model.AccountId,
            Amount = model.Amount,
            Type = model.Type,
            Category = model.Category,
            TransactionDate = model.TransactionDate,
            Description = model.Description,
            IsRecurring = model.IsRecurring
        };

        account.Balance += Signed(model.Type, model.Amount);

        await _uow.Repository<Transaction>().AddAsync(transaction, ct);
        _uow.Repository<Account>().Update(account);
        await _uow.SaveChangesAsync(ct);
        await _netWorth.InvalidateAsync(CurrentUserId, ct);

        TempData["Success"] = "Transaction recorded.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var transaction = await _uow.Repository<Transaction>().GetByIdAsync(id, ct);
        if (transaction is null || await GetOwnedAccountAsync(transaction.AccountId, ct) is null)
            return NotFound();

        var model = new TransactionFormViewModel
        {
            Id = transaction.Id,
            AccountId = transaction.AccountId,
            Amount = transaction.Amount,
            Type = transaction.Type,
            Category = transaction.Category,
            TransactionDate = transaction.TransactionDate,
            Description = transaction.Description,
            IsRecurring = transaction.IsRecurring,
            Accounts = await AccountSelectListAsync(ct, transaction.AccountId)
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TransactionFormViewModel model, CancellationToken ct)
    {
        if (id != model.Id)
            return BadRequest();
        if (!ModelState.IsValid)
        {
            model.Accounts = await AccountSelectListAsync(ct, model.AccountId);
            return View(model);
        }

        var transaction = await _uow.Repository<Transaction>().GetByIdAsync(id, ct);
        if (transaction is null)
            return NotFound();

        var oldAccount = await GetOwnedAccountAsync(transaction.AccountId, ct);
        var newAccount = transaction.AccountId == model.AccountId
            ? oldAccount
            : await GetOwnedAccountAsync(model.AccountId, ct);

        if (oldAccount is null || newAccount is null)
            return NotFound();

        // Reverse the original effect, then apply the new one (handles account changes too).
        oldAccount.Balance -= Signed(transaction.Type, transaction.Amount);

        transaction.AccountId = model.AccountId;
        transaction.Amount = model.Amount;
        transaction.Type = model.Type;
        transaction.Category = model.Category;
        transaction.TransactionDate = model.TransactionDate;
        transaction.Description = model.Description;
        transaction.IsRecurring = model.IsRecurring;

        newAccount.Balance += Signed(model.Type, model.Amount);

        _uow.Repository<Transaction>().Update(transaction);
        _uow.Repository<Account>().Update(oldAccount);
        _uow.Repository<Account>().Update(newAccount);
        await _uow.SaveChangesAsync(ct);
        await _netWorth.InvalidateAsync(CurrentUserId, ct);

        TempData["Success"] = "Transaction updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var transaction = await _uow.Repository<Transaction>().GetByIdAsync(id, ct);
        if (transaction is null)
            return NotFound();

        var account = await GetOwnedAccountAsync(transaction.AccountId, ct);
        if (account is null)
            return NotFound();

        transaction.Account = account;
        return View(transaction);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken ct)
    {
        var transaction = await _uow.Repository<Transaction>().GetByIdAsync(id, ct);
        if (transaction is null)
            return NotFound();

        var account = await GetOwnedAccountAsync(transaction.AccountId, ct);
        if (account is null)
            return NotFound();

        account.Balance -= Signed(transaction.Type, transaction.Amount);

        _uow.Repository<Transaction>().Delete(transaction);
        _uow.Repository<Account>().Update(account);
        await _uow.SaveChangesAsync(ct);
        await _netWorth.InvalidateAsync(CurrentUserId, ct);

        TempData["Success"] = "Transaction deleted.";
        return RedirectToAction(nameof(Index));
    }

    private static decimal Signed(TransactionType type, decimal amount)
        => type == TransactionType.Income ? amount : -amount;

    private async Task<Account?> GetOwnedAccountAsync(int accountId, CancellationToken ct)
    {
        var account = await _uow.Repository<Account>().GetByIdAsync(accountId, ct);
        return account is not null && account.UserId == CurrentUserId ? account : null;
    }

    private async Task<IEnumerable<SelectListItem>> AccountSelectListAsync(CancellationToken ct, int? selected = null)
    {
        var accounts = await _uow.Repository<Account>().ListAsync(new AccountsByUserSpec(CurrentUserId), ct);
        return accounts.Select(a => new SelectListItem
        {
            Value = a.Id.ToString(),
            Text = a.Name,
            Selected = selected.HasValue && selected.Value == a.Id
        });
    }
}
