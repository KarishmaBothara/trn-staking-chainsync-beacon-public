using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace MatrixEngine.Core.Services;

public interface ICronScheduleService
{
    Task<CronScheduleModel?> GetLatestSchedule();
}

public class CronScheduleService : ICronScheduleService
{
    private readonly IMongoCollection<CronScheduleModel> _collection;
    private readonly ILogger<CronScheduleService> _logger;

    public CronScheduleService(IMongoDatabase database, ILogger<CronScheduleService> logger)
    {
        _collection = database.GetCollection<CronScheduleModel>(DbCollectionName.CronSchedule);
        _logger = logger;
    }

    public async Task<CronScheduleModel?> GetLatestSchedule()
    {
        try
        {
            var filter = Builders<CronScheduleModel>.Filter.Eq(x => x.Name, "runCalculatorJob");
            var schedule = await _collection.Find(filter)
                .FirstOrDefaultAsync();
            return schedule;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching cron schedule from MongoDB");
            return null;
        }
    }
} 