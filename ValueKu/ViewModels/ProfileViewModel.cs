using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ValueKu.ViewModels;

public class ProfileViewModel
{
    [Required]
    [StringLength(64)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(64)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Country Code")]
    [StringLength(8)]
    public string? PhoneCountryCode { get; set; } = "+60";

    [StringLength(32)]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }

    /// <summary>Uploaded profile picture (not persisted directly; handled by the controller).</summary>
    public IFormFile? AvatarFile { get; set; }
}
