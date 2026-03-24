using MatrixEngine.Core.GraphQL.Common;

namespace MatrixEngine.Core.GraphQL.Chilled;

public class GetChilledConnectionResponseType
{
    // Named "Chilleds" in the GraphQL schema
    public ChilledConnectionType ChilledsConnection { get; set; }
}

public class ChilledConnectionType
{
    public int TotalCount { get; set; }
    public PageInfoType PageInfo { get; set; }
    public List<EdgeType<ChilledType>> Edges { get; set; }
}

public class ChilledType
{
    public int BlockNumber { get; set; }
    public string Stash { get; set; }
} 