using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MatrixEngine.Core;

internal class Program
{
    private static void Main(string[] args)
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
            )
            .ConfigureServices((context, services) => { services.ConfigureServices(context.Configuration); })
            .Build();
        var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        try
        {
            services.GetRequiredService<App>().Run(args);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}