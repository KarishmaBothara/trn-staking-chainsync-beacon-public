using MatrixEngine.Core.GraphQL.Common;

namespace MatrixEngine.Core.GraphQL.Slashed;

public class GetSlashedsConnectionResponseType
{
    public SlashedsConnectionType SlashedsConnection { get; set; }
}

public class SlashedsConnectionType
{
    public List<EdgeType<SlashedNodeType>> Edges { get; set; }
    public PageInfoType PageInfo { get; set; }
    public int TotalCount { get; set; }
}

public class SlashedNodeType
{
    public string Staker { get; set; }
    public int BlockNumber { get; set; }
    public string Amount { get; set; }
} 