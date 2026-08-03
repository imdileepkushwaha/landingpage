using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SoftflipSolutions.Services;

namespace SoftflipSolutions.Filters;

/// <summary>Loads allotted admin menus into ViewBag and blocks unauthorized actions.</summary>
public class AdminMenuAccessFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.Controller is not Controller controller)
        {
            await next();
            return;
        }

        var action = context.RouteData.Values["action"]?.ToString() ?? "";
        if (action.Equals("Login", StringComparison.OrdinalIgnoreCase) ||
            action.Equals("Logout", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        var isAnonymous = context.ActionDescriptor.EndpointMetadata
            .OfType<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>().Any();
        var authenticated = context.HttpContext.User?.Identity?.IsAuthenticated == true;

        if (isAnonymous && !authenticated)
        {
            await next();
            return;
        }

        var access = context.HttpContext.RequestServices.GetRequiredService<IAdminAccessService>();
        var user = context.HttpContext.User;
        var keys = await access.GetMenuKeysForPrincipalAsync(user);
        controller.ViewBag.AdminMenuKeys = keys;
        controller.ViewBag.IsSuperAdmin = access.IsSuperAdmin(user);

        if (action.Equals("AccessDenied", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        var menuKey = AdminMenuCatalog.KeyForAction(action);
        if (menuKey != null && !keys.Contains(menuKey) && !access.IsSuperAdmin(user))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Admin", null);
            return;
        }

        await next();
    }
}
