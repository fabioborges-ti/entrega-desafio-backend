using Ambev.DeveloperEvaluation.Common.Logging;
using Ambev.DeveloperEvaluation.WebApi.Configuration;
using Ambev.DeveloperEvaluation.WebApi.Hosting;
using Serilog;

namespace Ambev.DeveloperEvaluation.WebApi;

/// <summary>
/// Entry point for the Ambev Developer Evaluation Web API application.
/// </summary>
public class Program
{
    /// <summary>
    /// Main entry point for the application.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    public static async Task Main(string[] args)
    {
        try
        {
            Log.Information("Starting web application");

            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            builder.AddDefaultLogging();
            builder.AddWebApiServiceRegistrations();

            var app = builder.Build();
            await app.RunStartupTasksAsync();
            app.UseWebApiPipeline();
            app.MapWebApiEndpoints();

            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            Environment.ExitCode = 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
