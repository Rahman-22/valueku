using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ValueKu.Core.Entities;
using ValueKu.Core.Enums;
using ValueKu.Core.Interfaces;
using ValueKu.Core.Specifications;

namespace ValueKu.Controllers;

public class ZakatController : AppControllerBase
{
    private readonly IZakatService _zakat;
    private readonly IUnitOfWork _uow;
    private readonly INetWorthService _netWorth;

    public ZakatController(IZakatService zakat, IUnitOfWork uow, INetWorthService netWorth)
    {
        _zakat = zakat;
        _uow = uow;
        _netWorth = netWorth;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var result = await _zakat.CalculateAsync(CurrentUserId, ct);
        ViewBag.Accounts = await PayableAccountsAsync(ct);
        return View(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(int accountId, decimal amount, CancellationToken ct)
    {
        var account = await _uow.Repository<Account>().GetByIdAsync(accountId, ct);
        if (account is null || account.UserId != CurrentUserId)
            return NotFound();

        if (amount <= 0)
        {
            TempData["Success"] = "Enter an amount greater than zero.";
            return RedirectToAction(nameof(Index));
        }

        await _uow.Repository<Transaction>().AddAsync(new Transaction
        {
            AccountId = accountId,
            Amount = amount,
            Type = TransactionType.Expense,
            Category = TransactionCategory.Zakat,
            TransactionDate = DateTime.Now,
            Description = "Zakat payment"
        }, ct);

        account.Balance -= amount;
        _uow.Repository<Account>().Update(account);
        await _uow.SaveChangesAsync(ct);
        await _netWorth.InvalidateAsync(CurrentUserId, ct);

        TempData["Success"] = "Zakat payment recorded.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IEnumerable<SelectListItem>> PayableAccountsAsync(CancellationToken ct)
    {
        var accounts = await _uow.Repository<Account>().ListAsync(new AccountsByUserSpec(CurrentUserId), ct);
        return accounts
            .Where(a => !a.IsLiability)
            .Select(a => new SelectListItem { Value = a.Id.ToString(), Text = a.Name });
    }
}
