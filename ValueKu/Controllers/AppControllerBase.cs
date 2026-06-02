using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ValueKu.Common;

namespace ValueKu.Controllers;

/// <summary>Base controller for all authenticated feature areas; exposes the current user id.</summary>
[Authorize]
public abstract class AppControllerBase : Controller
{
    protected int CurrentUserId => User.GetUserId();
}
