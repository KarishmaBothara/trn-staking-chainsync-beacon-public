namespace MatrixEngine.Core.Models.DTOs;

public class SignEffectiveBalanceDto
{
    public string? Account { get; private set; }
    public int VtxDistributionId { get; private set; }
    public string? TotalRewardPoints { get; private set; }
    
    public SignEffectiveBalanceDto(string? account, int vtxDistributionId, string? totalRewardPoints)
    {
        Account = account;
        VtxDistributionId = vtxDistributionId;
        TotalRewardPoints = totalRewardPoints;
    }
}