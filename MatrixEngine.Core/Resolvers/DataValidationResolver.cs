using MatrixEngine.Core.Services;
using Microsoft.Extensions.Logging;

namespace MatrixEngine.Core.Resolvers;

public interface IDataValidationResolver
{
    Task ValidateEffectiveBalanceRange();
}

public class DataValidationResolver : IDataValidationResolver
{
    private readonly IEffectiveBalanceService _effectiveBalanceService;
    private readonly ILogger<DataValidationResolver> _logger;
    private readonly IBalanceChangeService _balanceChangeService;
    private readonly IRewardCycleService _rewardCycleService;
    private readonly IEraService _eraService;

    public DataValidationResolver(
        IEraService eraService,
        IRewardCycleService rewardCycleService,
        IEffectiveBalanceService effectiveBalanceService,
        IBalanceChangeService balanceChangeService, ILogger<DataValidationResolver> logger)
    {
        _eraService = eraService;
        _rewardCycleService = rewardCycleService;
        _balanceChangeService = balanceChangeService;
        _logger = logger;
        _effectiveBalanceService = effectiveBalanceService;
    }

    public async Task ValidateEffectiveBalanceRange()
    {
        var rewardCycle = await _rewardCycleService.GetCurrentRewardCycle();
        var startBlock = rewardCycle.StartBlock;
        var endBlock = rewardCycle.Finished
            ? rewardCycle.EndBlock
            : (await _eraService.GetEraByIndex(rewardCycle.CurrentEraIndex)).EndBlock;

        // get accumulated boostrap reward amount
        var bootstrapReward = await _effectiveBalanceService.GetBootstrapRewardInBlockRange(startBlock, endBlock);

        // get total staked amount * days range
        var totalStakedBalance = await _balanceChangeService.GetTotalStakedBalance(startBlock, endBlock);

        var daysRange = rewardCycle.CurrentEraIndex - rewardCycle.StartEraIndex + 1;

        //try parse total staked balance
        if (!double.TryParse(totalStakedBalance, out var totalStakedBalanceDouble))
        {
            _logger.LogError($"Failed to parse total staked balance: {totalStakedBalance}");
            return;
        }

        var range = (bootstrapReward.totalReward * 365.25) / totalStakedBalanceDouble * daysRange;

        if (range < 0.3 || range > 0.1)
        {
            _logger.LogError($"Effective balance range is not valid: {range}. Expected range is between 0.1 and 0.3.");
        }
        else
        {
            _logger.LogInformation($"Effective balance valid range: {range}");
        }
    }
}