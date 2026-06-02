using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ValueKu.Common;

public static partial class FormattingExtensions
{
    private static readonly CultureInfo Myr = CultureInfo.GetCultureInfo("ms-MY");

    /// <summary>Formats a decimal as Malaysian Ringgit, e.g. RM1,234.56.</summary>
    public static string ToMyr(this decimal value) => value.ToString("C", Myr);

    /// <summary>Turns a PascalCase enum value into spaced words, e.g. RealEstate -> "Real Estate".</summary>
    public static string Humanize(this Enum value) => SplitPascalCase().Replace(value.ToString(), " $1").Trim();

    /// <summary>Display name for an enum: uses [Display(Name)] when present, otherwise Humanize().</summary>
    public static string DisplayName(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        var display = member?.GetCustomAttribute<DisplayAttribute>();
        return display?.Name ?? value.Humanize();
    }

    [GeneratedRegex(@"(\B[A-Z])")]
    private static partial Regex SplitPascalCase();
}
