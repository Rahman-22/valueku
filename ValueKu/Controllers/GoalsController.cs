using Microsoft.AspNetCore.Mvc;
using ValueKu.Common;
using ValueKu.Core.Entities;
using ValueKu.Core.Interfaces;
using ValueKu.Core.Specifications;
using ValueKu.ViewModels;

namespace ValueKu.Controllers;

public class GoalsController : AppControllerBase
{
    private readonly IUnitOfWork _uow;

    public GoalsController(IUnitOfWork uow) => _uow = uow;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var goals = await _uow.Repository<SavingsGoal>().ListAsync(new SavingsGoalsByUserSpec(CurrentUserId), ct);
        return View(goals);
    }

    [HttpGet]
    public IActionResult Create() => View(new SavingsGoalFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SavingsGoalFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        await _uow.Repository<SavingsGoal>().AddAsync(new SavingsGoal
        {
            UserId = CurrentUserId,
            Name = model.Name,
            TargetAmount = model.TargetAmount,
            CurrentAmount = model.CurrentAmount,
            TargetDate = model.TargetDate
        }, ct);
        await _uow.SaveChangesAsync(ct);

        TempData["Success"] = "Savings goal created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var goal = await _uow.Repository<SavingsGoal>().GetByIdAsync(id, ct);
        if (goal is null || goal.UserId != CurrentUserId)
            return NotFound();

        return View(new SavingsGoalFormViewModel
        {
            Id = goal.Id,
            Name = goal.Name,
            TargetAmount = goal.TargetAmount,
            CurrentAmount = goal.CurrentAmount,
            TargetDate = goal.TargetDate
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SavingsGoalFormViewModel model, CancellationToken ct)
    {
        if (id != model.Id)
            return BadRequest();
        if (!ModelState.IsValid)
            return View(model);

        var goal = await _uow.Repository<SavingsGoal>().GetByIdAsync(id, ct);
        if (goal is null || goal.UserId != CurrentUserId)
            return NotFound();

        goal.Name = model.Name;
        goal.TargetAmount = model.TargetAmount;
        goal.CurrentAmount = model.CurrentAmount;
        goal.TargetDate = model.TargetDate;
        _uow.Repository<SavingsGoal>().Update(goal);
        await _uow.SaveChangesAsync(ct);

        TempData["Success"] = "Savings goal updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contribute(int id, decimal amount, CancellationToken ct)
    {
        var goal = await _uow.Repository<SavingsGoal>().GetByIdAsync(id, ct);
        if (goal is null || goal.UserId != CurrentUserId)
            return NotFound();

        if (amount > 0)
        {
            goal.CurrentAmount += amount;
            _uow.Repository<SavingsGoal>().Update(goal);
            await _uow.SaveChangesAsync(ct);
            TempData["Success"] = $"Added {amount.ToMyr()} to '{goal.Name}'.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var goal = await _uow.Repository<SavingsGoal>().GetByIdAsync(id, ct);
        if (goal is null || goal.UserId != CurrentUserId)
            return NotFound();

        _uow.Repository<SavingsGoal>().Delete(goal);
        await _uow.SaveChangesAsync(ct);

        TempData["Success"] = "Savings goal deleted.";
        return RedirectToAction(nameof(Index));
    }
}
