using Ambev.DeveloperEvaluation.Common.HealthChecks;
using Ambev.DeveloperEvaluation.Common.Logging;
using Ambev.DeveloperEvaluation.WebApi.Middleware;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;

namespace Ambev.DeveloperEvaluation.WebApi.Hosting;

public static class ApplicationPipelineExtensions
{
    public static WebApplication UseWebApiPipeline(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<HttpObservabilityMiddleware>();
        app.UseDefaultLogging();
        app.UseMiddleware<ValidationExceptionMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
        }

        if (!string.Equals(
                Environment.GetEnvironmentVariable("DISABLE_HTTPS_REDIRECTION"),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            app.UseHttpsRedirection();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    public static WebApplication MapWebApiEndpoints(this WebApplication app)
    {
        app.UseBasicHealthChecks();
        app.MapHealthChecks("/health-ui-target", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });
        app.MapHealthChecksUI(options =>
        {
            options.UIPath = "/health-ui";
            options.ApiPath = "/health-ui-api";
        });

        if (app.Environment.IsDevelopment())
        {
            app.MapScalarApiReference(options =>
            {
                options.Title = "Developer Evaluation API";
                options.Theme = ScalarTheme.Saturn;
                options.WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json");
            });

            app.MapGet("/", () => Results.Redirect("/scalar/v1"))
                .ExcludeFromDescription();
        }

        app.MapControllers();
        return app;
    }
}
