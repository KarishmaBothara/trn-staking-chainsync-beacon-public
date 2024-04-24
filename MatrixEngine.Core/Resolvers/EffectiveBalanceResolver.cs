using System.Numerics;
using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Models;
using MatrixEngine.Core.Services;
using Microsoft.Extensions.Logging;

namespace MatrixEngine.Core.Resolvers;

public interface IEffectiveBalanceResolver
{
    List<EffectiveBalanceModel> CalculateOneAccountEffectiveBalance(
        string account,
        List<BalanceChangeModel> balanceChangesForAccount);

    decimal CalculateEffectiveBalanceWithPrecisions(decimal balanceEffectiveEras, BigInteger balanceTotalBalance,
        int decimalPlaces = 2);

    Dictionary<string, List<EffectiveBalanceModel>> CalculateEffectiveBalances(
        Dictionary<string, List<BalanceChangeModel>> balanceChanges);

    Task SaveEffectiveBalances(List<EffectiveBalanceModel> effectiveBalances);
}

public class EffectiveBalanceResolver : IEffectiveBalanceResolver
{
    private readonly IEffectiveBalanceService _effectiveBalanceService;
    private ILogger<EffectiveBalanceResolver> _logger;
    private const int ErasInCycle = 90;

    public EffectiveBalanceResolver(IEffectiveBalanceService effectiveBalanceService,
        ILogger<EffectiveBalanceResolver> logger)
    {
        _logger = logger;
        _effectiveBalanceService = effectiveBalanceService;
    }

    public Task SaveEffectiveBalances(List<EffectiveBalanceModel> effectiveBalances)
    {
        _logger.LogInformation($"Saving {effectiveBalances?.Count ?? 0} effective balances.");
        return _effectiveBalanceService.UpsertEffectiveBalance(effectiveBalances);
    }

    public Dictionary<string, List<EffectiveBalanceModel>> CalculateEffectiveBalances(
        Dictionary<string, List<BalanceChangeModel>> balanceChanges)
    {
        _logger.LogInformation($"Calculating effective balances for {balanceChanges.Keys.Count} accounts.");
        var effectiveBalances = new Dictionary<string, List<EffectiveBalanceModel>>();

        foreach (var account in balanceChanges.Keys)
        {
            var balanceChangesForAccount = balanceChanges[account];
            var effectiveBalanceModels = CalculateOneAccountEffectiveBalance(account, balanceChangesForAccount);
            effectiveBalances.Add(account, effectiveBalanceModels);
        }

        return effectiveBalances;
    }

    public List<EffectiveBalanceModel> CalculateOneAccountEffectiveBalance(
        string account,
        List<BalanceChangeModel> balanceChangesForAccount)
    {
        var effectiveBalanceModels = new List<EffectiveBalanceModel>();
        foreach (var balance in balanceChangesForAccount)
        {
            var effectiveBalance =
                CalculateEffectiveBalanceWithPrecisions(balance.EffectiveEras, balance.BalanceInBlockRange);
            var stakerType = balance.StakerType ?? StakerType.Staker;
            var rate = GetStakerRate(stakerType);
            var reward = effectiveBalance * GetStakerRate(stakerType);

            var effectiveBalanceModel = new EffectiveBalanceModel
            {
                Account = account,
                Type = stakerType,
                Rate = rate,
                EraIndex = balance.EraIndex,
                EffectiveBlocks = balance.EffectiveBlocks,
                EffectiveEras = balance.EffectiveEras,
                EffectiveBalance = effectiveBalance.ToString(),
                Reward = reward.ToString(),
                StartBlock = balance.StartBlock,
                EndBlock = balance.EndBlock,
                Balance = balance.BalanceInBlockRange.ToString(),
            };

            effectiveBalanceModels.Add(effectiveBalanceModel);
        }

        return effectiveBalanceModels;
    }

    public decimal CalculateEffectiveBalanceWithPrecisions(decimal balanceEffectiveEras, BigInteger balanceTotalBalance,
        int decimalPlaces = 2)
    {
        const long baseFactorDecimalPlaces = 10;

        var baseFactorWithDecimalPlaces = (long)Math.Pow(10, baseFactorDecimalPlaces);

        var denominator = (long)Math.Pow(10, baseFactorDecimalPlaces);

        var effectiveEraPortionInCycleInMillion =
            new BigInteger(balanceEffectiveEras / ErasInCycle * baseFactorWithDecimalPlaces);

        var effectiveBalanceInMillion = balanceTotalBalance * effectiveEraPortionInCycleInMillion;

        var effectiveBalance = decimal.Parse((effectiveBalanceInMillion / denominator).ToString());

        return effectiveBalance;
    }

    private decimal GetStakerRate(string type)
    {
        return type switch
        {
            StakerType.Validator => 0.0739m,
            StakerType.Nominator => 0.0492m,
            _ => 0.0246m
        };
    }
}