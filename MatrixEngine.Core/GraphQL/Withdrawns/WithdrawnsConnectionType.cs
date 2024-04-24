using MatrixEngine.Core.GraphQL.Common;

namespace MatrixEngine.Core.GraphQL.Withdrawns;

public class GetWithdrawnsConnectionResponseType
{
    public WithdrawnsConnectionType WithdrawnsConnection { get; set; }
}

public class WithdrawnsConnectionType
{
    public List<EdgeType<WithdrawnNodeType>> Edges { get; set; }
    public PageInfoType PageInfo { get; set; }
    public int TotalCount { get; set; }
}

public class WithdrawnNodeType
{
    public string Stash { get; set; }
    public int BlockNumber { get; set; }
    public string Amount { get; set; }
}