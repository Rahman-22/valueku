using System.ComponentModel.DataAnnotations;

namespace ValueKu.ViewModels;

public class ReportRequestViewModel
{
    [Range(2000, 2100)]
    public int Year { get; set; }

    [Range(1, 12)]
    public int Month { get; set; }
}
