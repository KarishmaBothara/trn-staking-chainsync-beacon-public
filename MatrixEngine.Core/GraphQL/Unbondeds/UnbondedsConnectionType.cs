using MatrixEngine.Core.GraphQL.Common;

namespace MatrixEngine.Core.GraphQL.Unbondeds;

public class GetUnbondedsConnectionResponseType
{
    public UnbondedsConnectionType UnbondedsConnection { get; set; }
}

public class UnbondedsConnectionType
{
    public List<EdgeType<UnbondedNodeType>> Edges { get; set; }
    public PageInfoType PageInfo { get; set; }
    public int TotalCount { get; set; }
}

public class UnbondedNodeType
{
    public string Stash { get; set; }
    public int BlockNumber { get; set; }
    public string Amount { get; set; }
} 