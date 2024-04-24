using MatrixEngine.Core.Engine;
using MatrixEngine.Core.Resolvers;
using MatrixEngine.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Xunit.Microsoft.DependencyInjection;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace MatrixEngine.Core.IntegrationTest.Fixtures;

public class IntegrationTestFixture: TestBedFixture
{
    protected override void AddServices(IServiceCollection services, IConfiguration? configuration)
    {
        services.Configure<MongoDBSettings>(
            configuration.GetSection(MongoDBSettings.SectionName));

        services.AddSingleton<IMongoClient>(sp =>
        {
            var mongoDbSettings = configuration.GetSection(MongoDBSettings.SectionName).Get<MongoDBSettings>();
            return new MongoClient(mongoDbSettings?.ConnectionString);
        });

        services.AddScoped(sp =>
        {
            var mongoDbSettings = configuration.GetSection(MongoDBSettings.SectionName).Get<MongoDBSettings>();
            var client = sp.GetRequiredService<IMongoClient>();
            var database = mongoDbSettings?.Database;
            return client.GetDatabase(database);
        });

        services.AddScoped<IDataLoader, DataLoader>();
        services.AddScoped<IEraService, EraService>();
        services.AddScoped<IRewardCycleService, RewardCycleService>();
        services.AddScoped<IStakerService, StakerService>();
        services.AddScoped<IBalanceSnapshotService, BalanceSnapshotService>();
        services.AddScoped<IEffectiveBalanceService, EffectiveBalanceService>();
        services.AddScoped<ITransactionEventService, TransactionEventService>();
        services.AddScoped<IBalanceChangeService, BalanceChangeService>();
        
        services.AddScoped<IBalanceChangeResolver, BalanceChangeResolver>();
        services.AddScoped<IEffectiveBalanceResolver, EffectiveBalanceResolver>();
        services.AddScoped<IRewardCycleResolver, RewardCycleResolver>();
        services.AddScoped<IEngineCore, Engine.EngineCoreCore>();
    }

    protected override IEnumerable<TestAppSettings> GetTestAppSettings()
    {
        yield return new TestAppSettings { Filename = "appsettings.json", IsOptional = false };
    }

    protected override ValueTask DisposeAsyncCore()
    {
        return new ValueTask(Task.CompletedTask);
    }
}