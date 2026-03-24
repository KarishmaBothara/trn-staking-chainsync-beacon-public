using System.Numerics;
using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Models;
using MatrixEngine.Core.Models.DTOs;
using MatrixEngine.Core.Services;
using MatrixEngine.Core.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Linq;

namespace MatrixEngine.Core.Resolvers;

public interface IEffectiveBalanceResolver
{
    Task<Dictionary<string, List<EffectiveBalanceModel>>> CalculateEffectiveBalances(
        Dictionary<string, List<BalanceChangeModel>> balanceChanges,
        RewardCycle rewardCycle
    );
    
    // Get the effective balances for the previous cycle
    // rewardCycleStartBlock is the start block of the current cycle
    Task<List<EffectiveBalanceModel>> GetPreviousCycleEffectiveBalances(
        int rewardCycleStartBlock
    );
    
    Task SaveEffectiveBalances(List<EffectiveBalanceModel> effectiveBalances);
}

public class EffectiveBalanceResolver : IEffectiveBalanceResolver
{
    private readonly IEffectiveBalanceService _effectiveBalanceService;
    private readonly ISignatureService _signatureService;
    private readonly ISignEffectiveBalanceService _signedEffectiveBalanceService;
    private readonly ILogger<EffectiveBalanceResolver> _logger;

    public EffectiveBalanceResolver(
        IEffectiveBalanceService effectiveBalanceService,
        ISignatureService signatureService,
        ISignEffectiveBalanceService signedEffectiveBalanceService,
        ILogger<EffectiveBalanceResolver> logger
    )
    {
        _logger = logger;
        _effectiveBalanceService = effectiveBalanceService;
        _signedEffectiveBalanceService = signedEffectiveBalanceService;
        _signatureService = signatureService;
    }

    public Task SaveEffectiveBalances(List<EffectiveBalanceModel> effectiveBalances)
    {
        _logger.LogInformation($"Saving {effectiveBalances?.Count ?? 0} effective balances.");
        return _effectiveBalanceService.UpsertEffectiveBalance(effectiveBalances);
    }

    public async Task<Dictionary<string, List<EffectiveBalanceModel>>> CalculateEffectiveBalances(
        Dictionary<string, List<BalanceChangeModel>> balanceChanges,
        RewardCycle rewardCycle
    )
    {
        _logger.LogInformation($"Calculating effective balances for {balanceChanges.Keys.Count} accounts.");
        _logger.LogInformation($"VtxDistributionId: {rewardCycle.VtxDistributionId}, " +
                               $"StartBlock: {rewardCycle.StartBlock}, " +
                               $"EndBlock: {rewardCycle.EndBlock}");
        
        var effectiveBalances = new Dictionary<string, List<EffectiveBalanceModel>>();
        var signedEffectiveBalances = new List<SignedEffectiveBalanceModel>();

        foreach (var account in balanceChanges.Keys)
        {
            var balanceChangesForAccount = balanceChanges[account];
            (BigInteger totalRewardPoints, var effectiveBalanceModel) = 
                CalculateOneAccountEffectiveBalance(account, balanceChangesForAccount, rewardCycle);
            
            // Sign and add to list of signed effective balances
            signedEffectiveBalances.Add(await SignEffectiveBalance(account, totalRewardPoints, rewardCycle));
            // Console.WriteLine("\n\n Signature: " + effectiveBalanceModel.Signature + "\n\n");
            effectiveBalances.Add(account, effectiveBalanceModel);
        }

        // Insert signed balances into DB
        if (signedEffectiveBalances.Count > 0)
        {
            _signedEffectiveBalanceService.InsertSignEffectiveBalance(signedEffectiveBalances);
        }

        return effectiveBalances;
    }

    // Sign the effective balance for an account using the signature service. 
    private async Task<SignedEffectiveBalanceModel> SignEffectiveBalance(
        string account,
        BigInteger totalRewardPoints,
        RewardCycle rewardCycle
    )
    {
        // Construct the payload with just the total reward points over the whole cycle per account
        var payload = JsonConvert.SerializeObject(new SignEffectiveBalanceDto(
            account, 
            rewardCycle.VtxDistributionId, 
            totalRewardPoints.ToString()
        ));
        var signature = await _signatureService.SignMessage(payload);

        SignedEffectiveBalanceModel signedModel = new SignedEffectiveBalanceModel()
        {
            Account = account,
            TotalRewardPoints = totalRewardPoints.ToString(),
            Signature = signature,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            VtxDistributionId = rewardCycle.VtxDistributionId,
            StartBlock = rewardCycle.StartBlock,
            EndBlock = rewardCycle.EndBlock
        };
        
        // _logger.LogTrace("Signing effective balance for account: " + account +
        //                  " Total Effective Balance: " + totalRewardPoints + "\n" +
        //                  "Payload: " + payload + "\n" +
        //                  "Signature: " + signature);

        return signedModel;
    }

    // Fetch all stored Effective balances from the previous reward cycle, this will be used to calculate the effective balance for the current cycle
    public async Task<List<EffectiveBalanceModel>> GetPreviousCycleEffectiveBalances(
        int rewardCycleStartBlock
    )
    {
        return await _effectiveBalanceService.GetPreviousCycleEffectiveBalances(rewardCycleStartBlock);
    }

    // Calculate the effective balance of one account for all events over the reward cycle.
    public (BigInteger, List<EffectiveBalanceModel>) CalculateOneAccountEffectiveBalance(
        string account,
        List<BalanceChangeModel> balanceChangesForAccount,
        RewardCycle rewardCycle
    )
    {
        _logger.LogTrace($"Calculating effective balance for account {account} in cycle: {rewardCycle.VtxDistributionId}. num balance changes: {balanceChangesForAccount.Count}");
        // reverse balanceChangesForAccount so we can calculate from the end of the cycle
        balanceChangesForAccount.Reverse();
        var rewardCycleLength = rewardCycle.EndBlock - rewardCycle.StartBlock + 1;
        var firstBalanceChange = balanceChangesForAccount.First();
        BigInteger currentBondedMax = firstBalanceChange.Bonded.BalanceInBlockRange;
        BigInteger currentUnlockingMax = firstBalanceChange.Unlocking.BalanceInBlockRange;
        BigInteger totalRewardPoints = 0;
        
        // // decimal totalEffectiveBalance = 0;
        List<EffectiveBalanceModel> effectiveBalances = new List<EffectiveBalanceModel>(balanceChangesForAccount.Count);
        
        foreach (var balance in balanceChangesForAccount)
        {
            var blockRange = balance.EndBlock - balance.StartBlock + 1;
            var blockPercentage = (decimal)blockRange / rewardCycleLength;
        
            // Calculate Bonded Effective Balance Portion
            var newBondedBalance = balance.Bonded.BalanceInBlockRange;
            if (newBondedBalance < currentBondedMax)
            {
                currentBondedMax = newBondedBalance;
            }

            var (bondedReward, bondedBalanceDetail) = CalculateEffectiveBalanceDetail(
                balance.Bonded,
                currentBondedMax,
                blockPercentage
            );
            
            // Calculate Unlocking Effective Balance Portion
            var newUnlockingBalance = balance.Unlocking.BalanceInBlockRange;
            if (newUnlockingBalance < currentUnlockingMax)
            {
                currentUnlockingMax = newUnlockingBalance;
            }

            var (unlockingReward, unlockingBalanceDetail) = CalculateEffectiveBalanceDetail(
                balance.Unlocking,
                currentUnlockingMax,
                blockPercentage
            );
            var totalRewardPointsForBlock = unlockingReward + bondedReward;
            totalRewardPoints += totalRewardPointsForBlock;
            
            var percentage = Math.Round((double)blockPercentage, 7);
            effectiveBalances.Add(new EffectiveBalanceModel
            {
                Account = account,
                VtxDistributionId = rewardCycle.VtxDistributionId,
                EffectiveBlocks = blockRange,
                Percentage = percentage, // Rounded to 7 DP to match the calculation
                StartBlock = balance.StartBlock,
                EndBlock = balance.EndBlock,
                Bonded = bondedBalanceDetail,
                Unlocking = unlockingBalanceDetail,
                TotalRewardPoints = totalRewardPointsForBlock.ToString(),
            });
            
            _logger.LogTrace($"Block {balance.StartBlock}-{balance.EndBlock}: " +
                             $"Range={blockRange}, +" +
                             $"Percentage={percentage}, " +
                             $"TotalRewardPoints={totalRewardPointsForBlock}");
        }

        return (totalRewardPoints, effectiveBalances);
    }

    // Calculate the individual reward points and effective balance for a given balance detail
    private (BigInteger, EffectiveBalanceDetail) CalculateEffectiveBalanceDetail(BalanceChangeDetail balanceDetail, BigInteger currentMax, decimal blockPercentage)
    {
        // Calculate contribution based on percentage of max balance
        // We convert to BigInteger with 7 decimal places of precision
        var contribution = (BigInteger)(blockPercentage * 10000000) * currentMax / 10000000;
        string stakerType = balanceDetail.StakerType ?? StakerType.Staker;
        var rate = StakerUtils.GetStakerRate(stakerType);
          
        // We have 5 decimal places of precision for staker rate
        // 0.0739 -> 7390
        var reward = (BigInteger)(rate * 100000) * contribution / 100000;
            
        var effectiveBalanceDetail = new EffectiveBalanceDetail(
            balance: balanceDetail.BalanceInBlockRange,
            effectiveBalance: currentMax,
            rate,
            rewardPoints: reward,
            stakerType
        );
        return (reward, effectiveBalanceDetail);
    }
}