using Cronos;
using MatrixEngine.Core;
using MatrixEngine.Core.Services;
using Microsoft.Extensions.Options;

namespace MatrixEngine.WorkerService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly string _defaultSchedule;
    private readonly CronExpression _cronExpression;
    private readonly IServiceScopeFactory _serviceProviderFactory;
    private DateTime _lastCheckTime = DateTime.MinValue;
    private DateTime? _nextScheduledTime;

    public Worker(
        IServiceScopeFactory serviceProviderFactory, 
        IOptions<CronScheduleOptions> options,
        ILogger<Worker> logger)
    {
        _serviceProviderFactory = serviceProviderFactory;
        _logger = logger;
        _cronExpression = CronExpression.Parse(options.Value.Schedule);
        _defaultSchedule = options.Value.Schedule;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var nextUtc = await GetNextExecutionTime();
                
                if (nextUtc == null)
                {
                    _logger.LogInformation("No time found, trying again in 10 seconds");
                    await Task.Delay(10000, stoppingToken); // Wait 10 seconds before retrying
                    continue;
                }

                var utcNow = DateTime.UtcNow;

                // If the next execution time is in the past or present, execute immediately
                if (nextUtc <= utcNow)
                {
                    using (IServiceScope scope = _serviceProviderFactory.CreateScope())
                    {
                        var app = scope.ServiceProvider.GetRequiredService<IApp>();
                        await app.Run();
                    }
                }
                else
                {
                    // Wait until the next execution time or 10 seconds, whichever is shorter
                    var delayTime = Math.Min((nextUtc.Value - utcNow).TotalMilliseconds, 10000);
                    await Task.Delay(TimeSpan.FromMilliseconds(delayTime), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in worker execution");
                await Task.Delay(10000, stoppingToken); // Wait 10 seconds before retrying
            }
        }
    }
    
    // Get the next execution time from the MongoDB database. If no entry found, use the default specified in config
    public async Task<DateTime?> GetNextExecutionTime()
    {
        var utcNow = DateTime.UtcNow;
        
        // If we haven't checked in the last 10 seconds, get the latest schedule
        if ((utcNow - _lastCheckTime).TotalSeconds < 10 && _nextScheduledTime != null)
        {
            return _nextScheduledTime;
        }

        using (var scope = _serviceProviderFactory.CreateScope())
        {
            var cronScheduleService = scope.ServiceProvider.GetRequiredService<ICronScheduleService>();
            var newSchedule = await cronScheduleService.GetLatestSchedule();
            if (newSchedule == null)
            {
                _logger.LogWarning("No schedule found, using default schedule");
                _nextScheduledTime = _cronExpression.GetNextOccurrence(utcNow);
                return _nextScheduledTime;
            }

            var cronExpression = CronExpression.Parse(newSchedule.CronTime);
            var nextOccurrence = cronExpression.GetNextOccurrence(utcNow);
            _lastCheckTime = utcNow;
            if (_nextScheduledTime != nextOccurrence)
            {
                _logger.LogInformation("Next execution time updated: {nextOccurrence}", nextOccurrence);
            }
            _nextScheduledTime = nextOccurrence;
            return nextOccurrence;
        }
    }
}