using System.Security.Claims;
using Ambev.DeveloperEvaluation.Unit.WebApi.TestHelpers;
using Ambev.DeveloperEvaluation.WebApi.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Common;

public class BaseControllerTests
{
    /// <summary>Subclasse pública para acessar membros protected do <see cref="BaseController"/>.</summary>
    private sealed class TestController : BaseController
    {
        public int CallGetCurrentUserId() => GetCurrentUserId();
        public string CallGetCurrentUserEmail() => GetCurrentUserEmail();
        public IActionResult CallOk<T>(T data) => Ok(data);
        public IActionResult CallCreated<T>(string route, object values, T data) => Created(route, values, data);
        public IActionResult CallBadRequest(string m) => BadRequest(m);
        public IActionResult CallNotFound(string m = "Resource not found") => NotFound(m);
        public IActionResult CallOkPaginated<T>(PaginatedList<T> p) => OkPaginated(p);
    }

    private static TestController WithAuthenticated(int userId, string? email = null)
    {
        var ctrl = new TestController();
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };
        if (email != null) claims.Add(new Claim(ClaimTypes.Email, email));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
        return ctrl;
    }

    [Fact(DisplayName = "GetCurrentUserId retorna o id do claim NameIdentifier")]
    public void GetCurrentUserId_ReturnsClaimId()
    {
        var ctrl = WithAuthenticated(42);
        ctrl.CallGetCurrentUserId().Should().Be(42);
    }

    [Fact(DisplayName = "GetCurrentUserId sem claim lança InvalidOperationException")]
    public void GetCurrentUserId_NoClaim_Throws()
    {
        var ctrl = new TestController().WithUnauthenticatedUser();
        var act = () => ctrl.CallGetCurrentUserId();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact(DisplayName = "GetCurrentUserEmail retorna o e-mail do claim Email")]
    public void GetCurrentUserEmail_ReturnsEmail()
    {
        var ctrl = WithAuthenticated(1, "user@host.com");
        ctrl.CallGetCurrentUserEmail().Should().Be("user@host.com");
    }

    [Fact(DisplayName = "GetCurrentUserEmail sem claim lança NullReferenceException")]
    public void GetCurrentUserEmail_NoClaim_Throws()
    {
        var ctrl = new TestController().WithUnauthenticatedUser();
        var act = () => ctrl.CallGetCurrentUserEmail();
        act.Should().Throw<NullReferenceException>();
    }

    [Fact(DisplayName = "Ok<T> embala em ApiResponseWithData<T> com Success=true")]
    public void Ok_EncapsulatesInApiResponse()
    {
        var ctrl = WithAuthenticated(1);
        var result = ctrl.CallOk(new { Foo = "bar" });

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<ApiResponse>();
        ((ApiResponse)ok.Value!).Success.Should().BeTrue();
    }

    [Fact(DisplayName = "Created<T> retorna CreatedAtRouteResult com payload encapsulado")]
    public void Created_ReturnsCreatedAtRoute()
    {
        var ctrl = WithAuthenticated(1);
        var result = ctrl.CallCreated("Route", new { id = 1 }, new { Foo = "bar" });

        var created = result.Should().BeOfType<CreatedAtRouteResult>().Subject;
        created.RouteName.Should().Be("Route");
        created.Value.Should().BeAssignableTo<ApiResponse>();
        ((ApiResponse)created.Value!).Success.Should().BeTrue();
    }

    [Fact(DisplayName = "BadRequest retorna ApiResponse com Success=false e mensagem")]
    public void BadRequest_ReturnsApiResponse()
    {
        var ctrl = WithAuthenticated(1);
        var result = ctrl.CallBadRequest("invalid");

        var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var body = bad.Value.Should().BeAssignableTo<ApiResponse>().Subject;
        body.Message.Should().Be("invalid");
        body.Success.Should().BeFalse();
    }

    [Fact(DisplayName = "NotFound retorna ApiResponse com mensagem default")]
    public void NotFound_ReturnsApiResponse()
    {
        var ctrl = WithAuthenticated(1);
        var result = ctrl.CallNotFound();

        var nf = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var body = nf.Value.Should().BeAssignableTo<ApiResponse>().Subject;
        body.Message.Should().Be("Resource not found");
        body.Success.Should().BeFalse();
    }

    [Fact(DisplayName = "OkPaginated retorna PaginatedResponse com paginação preservada")]
    public void OkPaginated_ReturnsPaginatedResponse()
    {
        var ctrl = WithAuthenticated(1);
        var paged = new PaginatedList<int>(new List<int> { 1, 2, 3 }, count: 30, pageNumber: 2, pageSize: 10);

        var result = ctrl.CallOkPaginated(paged);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var inner = ok.Value.Should().BeAssignableTo<ApiResponseWithData<PaginatedResponse<int>>>().Subject;
        inner.Data!.CurrentPage.Should().Be(2);
        inner.Data.TotalPages.Should().Be(3);
        inner.Data.TotalCount.Should().Be(30);
        inner.Data.Success.Should().BeTrue();
    }
}

