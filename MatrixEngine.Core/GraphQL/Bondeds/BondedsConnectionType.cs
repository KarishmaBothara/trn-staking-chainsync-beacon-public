using MatrixEngine.Core.GraphQL.Common;

namespace MatrixEngine.Core.GraphQL.Bondeds;

public class GetBondedsConnectionResponseType
{
    public BondedsConnectionType BondedsConnection { get; set; }
}

public class BondedsConnectionType
{
    public List<EdgeType<BondedNodeType>> Edges { get; set; }
    public PageInfoType PageInfo { get; set; }
    public int TotalCount { get; set; }
}

public class BondedNodeType
{
    public string Stash { get; set; }
    public int BlockNumber { get; set; }
    public string Amount { get; set; }
}