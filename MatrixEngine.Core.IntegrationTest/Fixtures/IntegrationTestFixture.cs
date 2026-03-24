using MatrixEngine.Core.Config;
using MatrixEngine.Core.Engine;
using MatrixEngine.Core.Resolvers;
using MatrixEngine.Core.Services;
using MatrixEngine.Core.Substrate;
using MatrixEngine.Core.Substrate.Ledger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using Xunit.Microsoft.DependencyInjection;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace MatrixEngine.Core.IntegrationTest.Fixtures;

public class IntegrationTestFixture: TestBedFixture
{
    protected override void AddServices(IServiceCollection services, IConfiguration? configuration)
    {
        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Trace);
        });

        services.Configure<KmsSettings>(
            configuration.GetSection(KmsSettings.Kms));

        services.Configure<MongoDbSettings>(
            configuration.GetSection(MongoDbSettings.SectionName));

        // Configure Substrate services
        services.Configure<SubstrateSettings>(
            configuration.GetSection("Substrate"));
        services.AddSingleton<ISubstrateLedgerClient, SubstrateLedgerClient>();
        
        services.AddSingleton<IMongoClient>(sp =>
        {
            var mongoDbSettings = configuration.GetSection(MongoDbSettings.SectionName).Get<MongoDbSettings>();
            return new MongoClient(mongoDbSettings?.ConnectionString);
        });

        services.AddScoped(sp =>
        {
            var mongoDbSettings = configuration.GetSection(MongoDbSettings.SectionName).Get<MongoDbSettings>();
            var client = sp.GetRequiredService<IMongoClient>();
            var database = mongoDbSettings?.Database;
            return client.GetDatabase(database);
        });

        services.AddScoped<IDataLoader, DataLoader>();
        services.AddScoped<IEraService, EraService>();
        services.AddScoped<IRewardCycleService, RewardCycleService>();
        services.AddScoped<IStakerService, StakerService>();
        services.AddScoped<IEffectiveBalanceService, EffectiveBalanceService>();
        services.AddScoped<ITransactionEventService, TransactionEventService>();
        services.AddScoped<IBalanceChangeService, BalanceChangeService>();
        services.AddScoped<ISignatureService, SignatureService>();
        services.AddScoped<IChilledService, ChilledService>();
        services.AddScoped<ISignEffectiveBalanceService, SignEffectiveBalanceService>();

        services.AddScoped<IErasResolver, ErasResolver>();
        services.AddScoped<IStakersResolver, StakersResolver>();
        services.AddScoped<ITransactionEventsResolver, TransactionEventsResolver>();
        services.AddScoped<IBalanceChangeResolver, BalanceChangeResolver>();
        services.AddScoped<IEffectiveBalanceResolver, EffectiveBalanceResolver>();
        services.AddScoped<IRewardCycleResolver, RewardCycleResolver>();
        services.AddScoped<IChilledResolver, ChilledResolver>();
        
        services.AddScoped<IDataCore>(x => new Mock<IDataCore>().Object);
        services.AddScoped<IComputingCore, ComputingCore>();
        services.AddScoped<IEngineCore, EngineCoreCore>();
    }

    protected override IEnumerable<TestAppSettings> GetTestAppSettings()
    {
        yield return new TestAppSettings { Filename = "./appsettings.json", IsOptional = false };
    }

    protected override ValueTask DisposeAsyncCore()
    {
        return new ValueTask(Task.CompletedTask);
    }
}