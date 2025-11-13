using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Models;
using MatrixEngine.Core.Models.DTOs;
using MatrixEngine.Core.Services;
using MatrixEngine.Core.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MatrixEngine.Core.Resolvers;

public interface IStakerRateResolver
{
    Task ResolveStakerRateFromEffectiveBalance(List<EffectiveBalanceModel> effectiveBalances);
    Task SignStakerRate(int batchSize = Pagination.DefaultSignDataBatch);
}

public class StakerRateResolver : IStakerRateResolver
{
    private readonly IStakerRateService _stakerRate;
    private readonly ISignatureService _signatureService;
    private readonly ILogger<StakerRateResolver> _logger;

    public StakerRateResolver(IStakerRateService stakerRate, ISignatureService signatureService,
        ILogger<StakerRateResolver> logger)
    {
        _logger = logger;
        _signatureService = signatureService;
        _stakerRate = stakerRate;
    }

    public async Task ResolveStakerRateFromEffectiveBalance(List<EffectiveBalanceModel> effectiveBalances)
    {
        _logger.LogInformation("Start resolving staker rates from effective balance.");
        //effective balance contains the type that can resolve the rate
        var stakerRates = new List<StakerRateModel>();
        foreach (var effectiveBalance in effectiveBalances)
        {
            var stakerRate = new StakerRateModel
            {
                Account = effectiveBalance.Account,
                EraIndex = effectiveBalance.EraIndex,
                Rate = StakerUtils.GetStakerRate(effectiveBalance.Type).ToString(), //TODO check if this is correct
                Type = effectiveBalance.Type,
            };
            stakerRates.Add(stakerRate);
        }

        await _stakerRate.UpsertStakerRates(stakerRates);
    }

    public async Task SignStakerRate(int batchSize = Pagination.DefaultSignDataBatch)
    {
        _logger.LogInformation("Start signing staker rates.");
        //load latest unsigned staker rates
        var stakerRates = await _stakerRate.LoadLatestUnsignedStakerRates();

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        //paged by 500 and sign them
        var batch = 1;
        var total = stakerRates.Count;
        var totalBatch = total / batchSize + 1;
        var tasks = new List<Task<List<StakerRateModel>>>();
        for (var i = 0; i < totalBatch; i++)
        {
            var batchSignEffectiveBalances = stakerRates.Skip(i * batchSize).Take(batchSize).ToList();
            var batchNumber = $"{timestamp}-{batch}";
            tasks.Add(SignBatch(batchSignEffectiveBalances, timestamp, batchNumber));
            batch++;
        }

        //run all the tasks in parallel
        var result = await Task.WhenAll(tasks);
        var allResults = result.SelectMany(x => x).ToList();
        await _stakerRate.UpdateSignatures(allResults);
    }

    private async Task<List<StakerRateModel>> SignBatch(List<StakerRateModel> batchSignEffectiveBalances,
        long timestamp, string batchNumber)
    {
        var result = new List<StakerRateModel>();
        // serialized the list and use base64 to encrypt the data to pass in SignMessage
        var serializedData = JsonConvert.SerializeObject(batchSignEffectiveBalances.Select(x =>
            new StakerRateDto()
            {
                Account = x.Account,
                Rate = x.Rate,
                EraIndex = x.EraIndex,
            }));

        var base64Data = _signatureService.Base64Encrypt(serializedData);
        var signature = await _signatureService.SignMessage(base64Data);
        foreach (var signEffectiveBalance in batchSignEffectiveBalances)
        {
            signEffectiveBalance.Signature = signature;
            signEffectiveBalance.Timestamp = timestamp;
            signEffectiveBalance.BatchNumber = batchNumber;
        }

        _logger.LogInformation($"Signing batch {batchNumber} with {batchSignEffectiveBalances.Count} records.");
        result.AddRange(batchSignEffectiveBalances);
        return result;
    }
}