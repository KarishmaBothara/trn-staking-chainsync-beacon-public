using MatrixEngine.Core.Services;

namespace MatrixEngine.Core.Resolvers;

public interface IVortexResolver
{
    Task Resolve();
}
/// <summary>
/// Vortex Resolver is the module to calculate the vortex reward for each cycle
/// if the cycle set needToCalculate to true,
/// then we need to calculate the reward for the cycle
/// step1: get the start and end era index of the cycle
/// step2: get the start and end block of the cycle
/// step3: user start block and end block to effective balance for this cycle
/// step4: for the total bootstrap reward, accumulate the reward for all effective balance and save to the cycle' bootstrapRewardInTotal
///        to get the percentage, loop each account,
///        accumulate the reward for each account
///        and divide by the total reward
///        save the percentage(bootStrapRewardPercentage) to the account in vortex-reward collection
/// step5: workpoints:
///        get the total workpoints for the cycle
///        accumulate all workpoints for all accounts to get the total workpoints and save to the cycle's workpointsRewardInTotal
///        accumulate the reward for each account (including nominator_payouts and validator_payouts)
///        and divide by the total reward
///        save the percentage(workpointsPercentage) to the account in account-vortex-reward collection
/// step 6:
///        Calculate vortex by using fee-pot module
///        get result and save to the cycle's vortex
/// step6: set needToCalculate to false 
/// </summary>
public class VortexResolver : IVortexResolver
{
    private readonly IRewardCycleService _rewardCycleService;
    private readonly IEffectiveBalanceService _effectiveBalanceService;

    public VortexResolver(IRewardCycleService rewardCycleService, IEffectiveBalanceService effectiveBalanceService)
    {
        _effectiveBalanceService = effectiveBalanceService;
        _rewardCycleService = rewardCycleService;
    }
    
    public async Task Resolve()
    {
        var currentRewardCycle = await _rewardCycleService.GetCurrentRewardCycle();     
        
        //set needToCalculate to true
        currentRewardCycle.NeedToCalculate = true;
        await _rewardCycleService.SetNeedToCalculate(currentRewardCycle);

        var (accountsRewardWithPercentage, totalReward) = await _effectiveBalanceService.GetBootstrapRewardInBlockRange(currentRewardCycle.StartBlock,
            currentRewardCycle.EndBlock);
        
        //TODO: fee pot service
        
        //TODO: workpoints service to load fetched workpoints
    }
}