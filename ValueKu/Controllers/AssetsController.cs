using Microsoft.AspNetCore.Mvc;
using ValueKu.Core.Entities;
using ValueKu.Core.Interfaces;
using ValueKu.Core.Specifications;
using ValueKu.ViewModels;

namespace ValueKu.Controllers;

public class AssetsController : AppControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly IAssetValuationService _valuation;
    private readonly IAssetValuationCalculator _calculator;
    private readonly INetWorthService _netWorth;

    public AssetsController(
        IUnitOfWork uow,
        IAssetValuationService valuation,
        IAssetValuationCalculator calculator,
        INetWorthService netWorth)
    {
        _uow = uow;
        _valuation = valuation;
        _calculator = calculator;
        _netWorth = netWorth;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var assets = await _uow.Repository<Asset>().ListAsync(new AssetsByUserSpec(CurrentUserId), ct);
        return View(assets);
    }

    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var asset = await _uow.Repository<Asset>().GetByIdAsync(id, ct);
        if (asset is null || asset.UserId != CurrentUserId)
            return NotFound();

        var history = await _uow.Repository<AssetValuationHistory>()
            .ListAsync(new ValuationHistoryByAssetSpec(id), ct);

        ViewBag.History = history;
        return View(asset);
    }

    [HttpGet]
    public IActionResult Create() => View(new AssetFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AssetFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        var asset = new Asset
        {
            UserId = CurrentUserId,
            Name = model.Name,
            Category = model.Category,
            PurchasePrice = model.PurchasePrice,
            PurchaseDate = model.PurchaseDate,
            AppreciationDepreciationRate = model.AppreciationDepreciationRate,
            CalculationType = model.CalculationType,
            Currency = "MYR"
        };
        asset.CurrentValue = _calculator.CalculateValue(asset, DateOnly.FromDateTime(DateTime.UtcNow));

        await _uow.Repository<Asset>().AddAsync(asset, ct);
        await _uow.SaveChangesAsync(ct);

        // Record today's valuation history row and refresh cached metrics.
        await _valuation.RevalueUserAsync(CurrentUserId, DateOnly.FromDateTime(DateTime.UtcNow), ct);

        TempData["Success"] = $"Asset '{asset.Name}' added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var asset = await _uow.Repository<Asset>().GetByIdAsync(id, ct);
        if (asset is null || asset.UserId != CurrentUserId)
            return NotFound();

        return View(ToForm(asset));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AssetFormViewModel model, CancellationToken ct)
    {
        if (id != model.Id)
            return BadRequest();
        if (!ModelState.IsValid)
            return View(model);

        var asset = await _uow.Repository<Asset>().GetByIdAsync(id, ct);
        if (asset is null || asset.UserId != CurrentUserId)
            return NotFound();

        asset.Name = model.Name;
        asset.Category = model.Category;
        asset.PurchasePrice = model.PurchasePrice;
        asset.PurchaseDate = model.PurchaseDate;
        asset.AppreciationDepreciationRate = model.AppreciationDepreciationRate;
        asset.CalculationType = model.CalculationType;
        asset.CurrentValue = _calculator.CalculateValue(asset, DateOnly.FromDateTime(DateTime.UtcNow));

        _uow.Repository<Asset>().Update(asset);
        await _uow.SaveChangesAsync(ct);

        await _valuation.RevalueUserAsync(CurrentUserId, DateOnly.FromDateTime(DateTime.UtcNow), ct);

        TempData["Success"] = $"Asset '{asset.Name}' updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var asset = await _uow.Repository<Asset>().GetByIdAsync(id, ct);
        if (asset is null || asset.UserId != CurrentUserId)
            return NotFound();

        return View(asset);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken ct)
    {
        var asset = await _uow.Repository<Asset>().GetByIdAsync(id, ct);
        if (asset is null || asset.UserId != CurrentUserId)
            return NotFound();

        _uow.Repository<Asset>().Delete(asset);
        await _uow.SaveChangesAsync(ct);
        await _netWorth.InvalidateAsync(CurrentUserId, ct);

        TempData["Success"] = $"Asset '{asset.Name}' deleted.";
        return RedirectToAction(nameof(Index));
    }

    private static AssetFormViewModel ToForm(Asset a) => new()
    {
        Id = a.Id,
        Name = a.Name,
        Category = a.Category,
        PurchasePrice = a.PurchasePrice,
        PurchaseDate = a.PurchaseDate,
        AppreciationDepreciationRate = a.AppreciationDepreciationRate,
        CalculationType = a.CalculationType
    };
}
