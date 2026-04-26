using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.TestHelpers;

/// <summary>
/// Auxiliares para configurar <see cref="ControllerContext"/> em testes unitários:
/// usuário autenticado, query string e contexto HTTP padrão.
/// </summary>
internal static class ControllerTestContext
{
    public static T WithAuthenticatedUser<T>(this T controller, int userId, string? role = null)
        where T : ControllerBase
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };
        if (!string.IsNullOrEmpty(role))
            claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext ??= new ControllerContext();
        controller.ControllerContext.HttpContext ??= new DefaultHttpContext();
        controller.ControllerContext.HttpContext.User = principal;
        return controller;
    }

    public static T WithUnauthenticatedUser<T>(this T controller, string? badNameIdentifier = null)
        where T : ControllerBase
    {
        var claims = new List<Claim>();
        if (badNameIdentifier is not null)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, badNameIdentifier));

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext ??= new ControllerContext();
        controller.ControllerContext.HttpContext ??= new DefaultHttpContext();
        controller.ControllerContext.HttpContext.User = principal;
        return controller;
    }

    public static T WithQueryString<T>(this T controller, string queryString)
        where T : ControllerBase
    {
        controller.ControllerContext ??= new ControllerContext();
        controller.ControllerContext.HttpContext ??= new DefaultHttpContext();
        controller.ControllerContext.HttpContext.Request.QueryString =
            new QueryString(queryString.StartsWith('?') ? queryString : "?" + queryString);
        return controller;
    }

    public static T WithEmptyContext<T>(this T controller)
        where T : ControllerBase
    {
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }
}

