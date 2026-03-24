using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MatrixEngine.Core;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        var builder = Host.CreateDefaultBuilder();
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
        });
        
        var host = builder
            .ConfigureAppConfiguration(app =>
                app.AddJsonFile("appsettings.json", false, true)
                    .AddJsonFile($"appsettings.{environment}.json", true)
                    .AddEnvironmentVariables()
            )
            .ConfigureServices((context, services) => { services.ConfigureConsoleApplicationServices(context.Configuration); })
            .Build();
        
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        logger.LogTrace("Running App.");
        
        var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        try
        {
            var app = services.GetRequiredService<App>();
            await app.Run();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}