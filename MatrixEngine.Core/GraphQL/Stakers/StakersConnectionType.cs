using MatrixEngine.Core.GraphQL.Common;

namespace MatrixEngine.Core.GraphQL.Stakers;

public class GetStakersConnectionResponseType
{
    public StakersConnectionType StakersConnection { get; set; }
} 


public class StakersConnectionType
{
   //properties: edges, pageInfo, totalCount
    public List<EdgeType<StakerNodeType>> Edges { get; set; }
    public PageInfoType PageInfo { get; set; }
    public int TotalCount { get; set; } 
   
}

public class StakerNodeType
{
    //properties: stash, eraIndex, stakerType, totalStake, parentStash
    public string Stash { get; set; }
    public int EraIndex { get; set; }
    public string StakerType { get; set; }
    public string TotalStake { get; set; }
    public string ParentStash { get; set; }
    
}