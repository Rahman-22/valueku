using Microsoft.AspNetCore.Mvc;
using ValueKu.Core.Entities;
using ValueKu.Core.Interfaces;
using ValueKu.Core.Specifications;
using ValueKu.ViewModels;

namespace ValueKu.Controllers;

public class AccountsController : AppControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly INetWorthService _netWorth;

    public AccountsController(IUnitOfWork uow, INetWorthService netWorth)
    {
        _uow = uow;
        _netWorth = netWorth;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var accounts = await _uow.Repository<Account>().ListAsync(new AccountsByUserSpec(CurrentUserId), ct);
        return View(accounts);
    }

    [HttpGet]
    public IActionResult Create() => View(new AccountFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AccountFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        var account = new Account
        {
            UserId = CurrentUserId,
            Name = model.Name,
            Type = model.Type,
            Balance = model.Balance,
            Currency = "MYR"
        };

        await _uow.Repository<Account>().AddAsync(account, ct);
        await _uow.SaveChangesAsync(ct);
        await _netWorth.InvalidateAsync(CurrentUserId, ct);

        TempData["Success"] = $"Account '{account.Name}' added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var account = await _uow.Repository<Account>().GetByIdAsync(id, ct);
        if (account is null || account.UserId != CurrentUserId)
            return NotFound();

        return View(ToForm(account));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AccountFormViewModel model, CancellationToken ct)
    {
        if (id != model.Id)
            return BadRequest();
        if (!ModelState.IsValid)
            return View(model);

        var account = await _uow.Repository<Account>().GetByIdAsync(id, ct);
        if (account is null || account.UserId != CurrentUserId)
            return NotFound();

        account.Name = model.Name;
        account.Type = model.Type;
        account.Balance = model.Balance;

        _uow.Repository<Account>().Update(account);
        await _uow.SaveChangesAsync(ct);
        await _netWorth.InvalidateAsync(CurrentUserId, ct);

        TempData["Success"] = $"Account '{account.Name}' updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var account = await _uow.Repository<Account>().GetByIdAsync(id, ct);
        if (account is null || account.UserId != CurrentUserId)
            return NotFound();

        return View(account);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken ct)
    {
        var account = await _uow.Repository<Account>().GetByIdAsync(id, ct);
        if (account is null || account.UserId != CurrentUserId)
            return NotFound();

        _uow.Repository<Account>().Delete(account);
        await _uow.SaveChangesAsync(ct);
        await _netWorth.InvalidateAsync(CurrentUserId, ct);

        TempData["Success"] = $"Account '{account.Name}' deleted.";
        return RedirectToAction(nameof(Index));
    }

    private static AccountFormViewModel ToForm(Account a) => new()
    {
        Id = a.Id,
        Name = a.Name,
        Type = a.Type,
        Balance = a.Balance
    };
}
