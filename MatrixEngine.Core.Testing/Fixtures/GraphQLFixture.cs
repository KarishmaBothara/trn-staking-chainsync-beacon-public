using GraphQL;
using MatrixEngine.Core.GraphQL.ActiveEras;
using MatrixEngine.Core.GraphQL.Bondeds;
using MatrixEngine.Core.GraphQL.Stakers;
using MatrixEngine.Core.GraphQL.Withdrawns;

namespace MatrixEngine.Core.Testing.Fixtures;

public static class GraphQLFixture
{
    public static GraphQLResponse<GetActiveErasConnectionResponseType> LoadEras(int batchNumber = 1)
    {
        var path = $"Data/GraphQL/eras-{batchNumber}.json";
        var response = JsonFileReader.Read<GraphQLResponse<GetActiveErasConnectionResponseType>>(path);

        return response;
    }
    
    public static GraphQLResponse<GetStakersConnectionResponseType> LoadStakers(int batchNumber = 1)
    {
        var path = $"Data/GraphQL/stakers-{batchNumber}.json";
        var response = JsonFileReader.Read<GraphQLResponse<GetStakersConnectionResponseType>>(path);

        return response;
    }

    public static GraphQLResponse<GetBondedsConnectionResponseType> LoadBondeds(int batchNumber = 1)
    {
        var path = $"Data/GraphQL/bondeds-{batchNumber}.json";
        var response = JsonFileReader.Read<GraphQLResponse<GetBondedsConnectionResponseType>>(path);

        return response;
    }

    public static GraphQLResponse<GetWithdrawnsConnectionResponseType> LoadWithdrawns(int batchNumber = 1)
    {
        var path = $"Data/GraphQL/withdrawns-{batchNumber}.json";
        var response = JsonFileReader.Read<GraphQLResponse<GetWithdrawnsConnectionResponseType>>(path);

        return response; 
    }
}