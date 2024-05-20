using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using MatrixEngine.Core.Config;
using MatrixEngine.Core.Engine;
using MatrixEngine.Core.GraphQL.ActiveEras;
using MatrixEngine.Core.GraphQL.Bondeds;
using MatrixEngine.Core.GraphQL.Stakers;
using MatrixEngine.Core.GraphQL.Withdrawns;
using MatrixEngine.Core.Resolvers;
using MatrixEngine.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace MatrixEngine.Core;

public static class DependencyInjectionHelper
{
    public static void ConfigureConsoleApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient();
        services.AddSingleton<IGraphQLClient>(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(GraphQLHttpClient));
            var options = configuration.GetSection(GraphqlApiOptions.SectionName).Get<GraphqlApiOptions>();
            var endPoint = new Uri(options.BaseUrl);
            httpClient.BaseAddress = endPoint;
            var graphqlOptions = new GraphQLHttpClientOptions
            {
                EndPoint = endPoint
            };
            return new GraphQLHttpClient(graphqlOptions, new NewtonsoftJsonSerializer(), httpClient);
        });

        services.Configure<KmsSettings>(
            configuration.GetSection(KmsSettings.Kms));
        
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
        services.AddScoped<IEraService, EraService>();
        services.AddScoped<IRewardCycleService, RewardCycleService>();
        services.AddScoped<IStakerService, StakerService>();
        services.AddScoped<IBalanceSnapshotService, BalanceSnapshotService>();
        services.AddScoped<IEffectiveBalanceService, EffectiveBalanceService>();
        services.AddScoped<ITransactionEventService, TransactionEventService>();
        services.AddScoped<IBalanceChangeService, BalanceChangeService>();
        services.AddScoped<IGenesisValidatorService, GenesisValidatorService>();
        services.AddScoped<IAccountPunishmentMarkService, AccountPunishmentMarkService>();
        services.AddScoped<ISignEffectiveBalanceService, SignEffectiveBalanceService>();
        services.AddScoped<IStakerRateService, StakerRateService>();

        //AWS
        services.AddScoped<ISignatureService, SignatureService>();
        
        //GraphQL
        services.AddScoped<IGetActiveErasConnection, GetActiveErasConnection>();
        services.AddScoped<IGetStakersConnection, GetStakersConnection>();
        services.AddScoped<IGetBondedsCnnection, GetBondedsCnnection>();
        services.AddScoped<IGetWithdrawnsConnection, GetWithdrawnsConnection>();

        //Resolver
        services.AddScoped<IErasResolver, ErasResolver>();
        services.AddScoped<IStakersResolver, StakersResolver>();
        services.AddScoped<ITransactionEventsResolver, TransactionEventsResolver>();
        services.AddScoped<IBalanceChangeResolver, BalanceChangeResolver>();
        services.AddScoped<IEffectiveBalanceResolver, EffectiveBalanceResolver>();
        services.AddScoped<IRewardCycleResolver, RewardCycleResolver>();
        services.AddScoped<IBalanceSnapshotResolver, BalanceSnapshotResolver>();
        services.AddScoped<ISignEffectiveBalanceResolver, SignEffectiveBalanceResolver>();
        services.AddScoped<IStakerRateResolver, StakerRateResolver>();
        services.AddScoped<IDataValidationResolver, DataValidationResolver>();
        
        //Engine
        services.AddScoped<IDataCore, DataCore>();
        services.AddScoped<IComputingCore, ComputingCore>();
        services.AddScoped<IEngineCore, EngineCoreCore>();
        services.AddSingleton<App>();
    }
}