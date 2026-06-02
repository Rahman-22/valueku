using Microsoft.AspNetCore.Mvc;
using ValueKu.Core.Interfaces;
using ValueKu.ViewModels;

namespace ValueKu.Controllers;

public class ReportsController : AppControllerBase
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports) => _reports = reports;

    [HttpGet]
    public IActionResult Index()
    {
        var today = DateTime.Today;
        return View(new ReportRequestViewModel { Year = today.Year, Month = today.Month });
    }

    [HttpGet]
    public async Task<IActionResult> Download(int year, int month, CancellationToken ct)
    {
        if (month is < 1 or > 12)
            return BadRequest("Invalid month.");

        var bytes = await _reports.GenerateMonthlyStatementAsync(CurrentUserId, year, month, ct);
        var fileName = $"ValueKu-Statement-{year:D4}-{month:D2}.pdf";
        return File(bytes, "application/pdf", fileName);
    }
}
