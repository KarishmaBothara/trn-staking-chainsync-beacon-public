using MatrixEngine.Core.Engine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MatrixEngine.Core;

public interface IApp
{
    Task Run();
}

public class App : IApp
{
    private readonly ILogger<App> _logger;
    private readonly IServiceScopeFactory _serviceProviderFactory;

    public App(IServiceScopeFactory serviceProviderFactory, ILogger<App> logger)
    {
        _serviceProviderFactory = serviceProviderFactory;
        _logger = logger;
    }

    public async Task Run()
    {
        using (var serviceProvider = _serviceProviderFactory.CreateScope())
        {
            var services = serviceProvider.ServiceProvider;
            var engineCore = services.GetRequiredService<IEngineCore>();

            var startTime = DateTime.UtcNow;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            await engineCore.Start();
            watch.Stop();
            var endTime = DateTime.UtcNow;
            _logger.LogInformation(
                $"Started engine at {startTime.ToShortDateString()} {startTime.ToLongTimeString()} UTC");
            _logger.LogInformation($"Engine ended at {endTime.ToShortDateString()} {endTime.ToLongTimeString()} UTC");
            _logger.LogInformation($"Execution timespan: {watch.ElapsedMilliseconds / 1000} seconds");
        }
    }
}