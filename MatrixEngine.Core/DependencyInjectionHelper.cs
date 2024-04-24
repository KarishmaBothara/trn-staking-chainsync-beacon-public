using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using MatrixEngine.Core.Engine;
using MatrixEngine.Core.GraphQL.ActiveEras;
using MatrixEngine.Core.GraphQL.Bondeds;
using MatrixEngine.Core.GraphQL.Stakers;
using MatrixEngine.Core.GraphQL.Withdrawns;
using MatrixEngine.Core.Resolvers;
using MatrixEngine.Core.Services;
using MatrixEngine.GraphQL.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace MatrixEngine.Core;

public static class DependencyInjectionHelper
{
    public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IGraphQLClient>(sp =>
        {
            var options = configuration.GetSection(GraphqlApiOptions.SectionName).Get<GraphqlApiOptions>();
            return new GraphQLHttpClient(options.BaseUrl, new NewtonsoftJsonSerializer());
        });

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

        //DB services
        services.AddScoped<IEraService, EraService>();
        services.AddScoped<IRewardCycleService, RewardCycleService>();
        services.AddScoped<IStakerService, StakerService>();
        services.AddScoped<IBalanceSnapshotService, BalanceSnapshotService>();
        services.AddScoped<IEffectiveBalanceService, EffectiveBalanceService>();
        services.AddScoped<ITransactionEventService, TransactionEventService>();
        services.AddScoped<IBalanceChangeService, BalanceChangeService>();
        services.AddScoped<IGenesisValidatorService, GenesisValidatorService>();

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
        
        //Engine
        services.AddScoped<IDataCore, DataCore>();
        services.AddScoped<IEngineCore, Engine.EngineCoreCore>();
        services.AddSingleton<App>();
    }
}