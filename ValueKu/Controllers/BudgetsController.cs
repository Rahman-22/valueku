using Microsoft.AspNetCore.Mvc;
using ValueKu.Core.Entities;
using ValueKu.Core.Interfaces;
using ValueKu.Core.Specifications;
using ValueKu.ViewModels;

namespace ValueKu.Controllers;

public class BudgetsController : AppControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly IBudgetService _budgets;

    public BudgetsController(IUnitOfWork uow, IBudgetService budgets)
    {
        _uow = uow;
        _budgets = budgets;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var now = DateTime.Today;
        var statuses = await _budgets.GetStatusAsync(CurrentUserId, now.Year, now.Month, ct);
        return View(statuses);
    }

    [HttpGet]
    public IActionResult Create() => View(new BudgetFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BudgetFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        var budgets = await _uow.Repository<Budget>().ListAsync(new BudgetsByUserSpec(CurrentUserId), ct);
        if (budgets.Any(b => b.Category == model.Category))
        {
            ModelState.AddModelError(nameof(model.Category), "A budget for this category already exists.");
            return View(model);
        }

        await _uow.Repository<Budget>().AddAsync(new Budget
        {
            UserId = CurrentUserId,
            Category = model.Category,
            MonthlyLimit = model.MonthlyLimit
        }, ct);
        await _uow.SaveChangesAsync(ct);

        TempData["Success"] = "Budget created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var budget = await _uow.Repository<Budget>().GetByIdAsync(id, ct);
        if (budget is null || budget.UserId != CurrentUserId)
            return NotFound();

        return View(new BudgetFormViewModel { Id = budget.Id, Category = budget.Category, MonthlyLimit = budget.MonthlyLimit });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BudgetFormViewModel model, CancellationToken ct)
    {
        if (id != model.Id)
            return BadRequest();
        if (!ModelState.IsValid)
            return View(model);

        var budget = await _uow.Repository<Budget>().GetByIdAsync(id, ct);
        if (budget is null || budget.UserId != CurrentUserId)
            return NotFound();

        budget.Category = model.Category;
        budget.MonthlyLimit = model.MonthlyLimit;
        _uow.Repository<Budget>().Update(budget);
        await _uow.SaveChangesAsync(ct);

        TempData["Success"] = "Budget updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var budget = await _uow.Repository<Budget>().GetByIdAsync(id, ct);
        if (budget is null || budget.UserId != CurrentUserId)
            return NotFound();

        _uow.Repository<Budget>().Delete(budget);
        await _uow.SaveChangesAsync(ct);

        TempData["Success"] = "Budget deleted.";
        return RedirectToAction(nameof(Index));
    }
}
