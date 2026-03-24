using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using MatrixEngine.Core;
using MatrixEngine.Core.Config;
using MatrixEngine.Core.Engine;
using MatrixEngine.Core.GraphQL.ActiveEras;
using MatrixEngine.Core.GraphQL.Bondeds;
using MatrixEngine.Core.GraphQL.Chilled;
using MatrixEngine.Core.GraphQL.Slashed;
using MatrixEngine.Core.GraphQL.Stakers;
using MatrixEngine.Core.GraphQL.Unbondeds;
using MatrixEngine.Core.GraphQL.Withdrawns;
using MatrixEngine.Core.Resolvers;
using MatrixEngine.Core.Services;
using MatrixEngine.Core.Substrate;
using MatrixEngine.Core.Substrate.Ledger;
using MongoDB.Driver;

namespace MatrixEngine.WorkerService;

public static class DependencyInjectionHelper
{
    public static void ConfigureWorkerServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient();
        services.AddSingleton<IGraphQLClient>(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(GraphQLHttpClient));
            var options = configuration.GetSection(GraphqlApiOptions.SectionName).Get<GraphqlApiOptions>();
            var endPoint = new Uri(options.BaseUrl);
            httpClient.BaseAddress = endPoint;
            httpClient.Timeout = TimeSpan.FromMinutes(2);
            var graphqlOptions = new GraphQLHttpClientOptions
            {
                EndPoint = endPoint
            };
            return new GraphQLHttpClient(graphqlOptions, new NewtonsoftJsonSerializer(), httpClient);
        });

        services.Configure<KmsSettings>(
            configuration.GetSection(KmsSettings.Kms));
        
        // Configure Substrate services
        services.Configure<SubstrateSettings>(
            configuration.GetSection("Substrate"));
        services.AddSingleton<ISubstrateLedgerClient, SubstrateLedgerClient>();
        
        services.Configure<MongoDbSettings>(
            configuration.GetSection(MongoDbSettings.SectionName));

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

        //DB services
        services.AddScoped<ICronScheduleService, CronScheduleService>();
        services.AddScoped<IEraService, EraService>();
        services.AddScoped<IRewardCycleService, RewardCycleService>();
        services.AddScoped<IStakerService, StakerService>();
        services.AddScoped<IEffectiveBalanceService, EffectiveBalanceService>();
        services.AddScoped<ITransactionEventService, TransactionEventService>();
        services.AddScoped<IBalanceChangeService, BalanceChangeService>();
        services.AddScoped<IChilledService, ChilledService>();
        services.AddScoped<ISignEffectiveBalanceService, SignEffectiveBalanceService>();

        //AWS
        services.AddScoped<ISignatureService, SignatureService>();
        
        //GraphQL
        services.AddScoped<IGetActiveErasConnection, GetActiveErasConnection>();
        services.AddScoped<IGetStakersConnection, GetStakersConnection>();
        services.AddScoped<IGetBondedsConnection, GetBondedsCnnection>();
        services.AddScoped<IGetUnbondedsConnection, GetUnbondedsConnection>();
        services.AddScoped<IGetWithdrawnsConnection, GetWithdrawnsConnection>();
        services.AddScoped<IGetChilledConnection, GetChilledConnection>();
        services.AddScoped<IGetSlashedsConnection, GetSlashedsConnection>();

        //Resolver
        services.AddScoped<IErasResolver, ErasResolver>();
        services.AddScoped<IStakersResolver, StakersResolver>();
        services.AddScoped<ITransactionEventsResolver, TransactionEventsResolver>();
        services.AddScoped<IBalanceChangeResolver, BalanceChangeResolver>();
        services.AddScoped<IEffectiveBalanceResolver, EffectiveBalanceResolver>();
        services.AddScoped<IRewardCycleResolver, RewardCycleResolver>();
        services.AddScoped<IChilledResolver, ChilledResolver>();
        
        //Engine
        services.AddScoped<IDataCore, DataCore>();
        services.AddScoped<IComputingCore, ComputingCore>();
        services.AddScoped<IEngineCore, EngineCoreCore>();
        services.AddScoped<IApp, App>();
        
        services.Configure<CronScheduleOptions>(
            configuration.GetSection(CronScheduleOptions.Cron));
        services.AddHostedService<Worker>();
    }
}