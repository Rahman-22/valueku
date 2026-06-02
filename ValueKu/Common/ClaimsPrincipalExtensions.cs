using System.Security.Claims;

namespace ValueKu.Common;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Reads the authenticated user's id from the NameIdentifier claim (0 if absent).</summary>
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : 0;
    }
}
