using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace TodoApi.Tests.Controllers;

/// <summary>Minimal stub so <see cref="ControllerBase.CreatedAtAction"/> does not require routing setup in unit tests.
/// </summary>
internal sealed class FixedUrlHelper : IUrlHelper
{
    private readonly string _actionUrl;

    public FixedUrlHelper(string actionUrl = "http://localhost/api/todos/42")
    {
        _actionUrl = actionUrl;
    }

    public ActionContext ActionContext => throw new NotSupportedException();

    public string? Action(UrlActionContext actionContext) => _actionUrl;

    public string? Content(string? contentPath) => throw new NotSupportedException();

    public bool IsLocalUrl(string? url) => throw new NotSupportedException();

    public string? Link(string? routeName, object? values) => throw new NotSupportedException();

    public string? RouteUrl(UrlRouteContext routeContext) => throw new NotSupportedException();
}
