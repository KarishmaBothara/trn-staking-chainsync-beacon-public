using Cronos;
using MatrixEngine.Core;
using Microsoft.Extensions.Options;

namespace MatrixEngine.WorkerService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly CronExpression _cronExpression;
    private readonly IServiceScopeFactory _serviceProviderFactory;

    public Worker(IServiceScopeFactory serviceProviderFactory, IOptions<CronScheduleOptions> options,
        ILogger<Worker> logger)
    {
        _serviceProviderFactory = serviceProviderFactory;
        _logger = logger;
        _cronExpression = CronExpression.Parse(options.Value.Schedule);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await Task.Delay(1000, stoppingToken);
            var utcNow = DateTime.UtcNow;
             _logger.LogInformation("UTC now: {time}", utcNow);
            var nextUtc = _cronExpression.GetNextOccurrence(utcNow);

            if (nextUtc == null) continue;

            using IServiceScope scope = _serviceProviderFactory.CreateScope();
            var app = scope.ServiceProvider.GetRequiredService<IApp>();

            await Task.Delay(nextUtc.Value - utcNow, stoppingToken);
            await app.Run();
        }
    }
}
