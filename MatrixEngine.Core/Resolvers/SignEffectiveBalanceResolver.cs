using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Models;
using MatrixEngine.Core.Models.DTOs;
using MatrixEngine.Core.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MatrixEngine.Core.Resolvers;

public interface ISignEffectiveBalanceResolver
{
    Task Resolve(RewardCycle rewardCycle, List<EffectiveBalanceModel> effectiveBalanceModels);
    Task SignData(int batchSize = Pagination.DefaultSignDataBatch);
}

/// <summary>
/// This resolver is to call the signing service to sign the calculated effective balance
/// It will check the account punishment mark to check if users have withdrawn the staking amount, they will get the punishment.
/// As the punishment requires the modification of users' backwards data,
/// this resolver will add the users modified era effective balance to db to store.
/// At last, all data will be signed with a KMS key in batch (500 each)  
/// , and each record in each batch will have the same signature and timestamp with a batch number (integer)
/// e.g. batch number can be 1714752243-1
/// </summary>
public class SignEffectiveBalanceResolver : ISignEffectiveBalanceResolver
{
    private readonly IEffectiveBalanceService _effectiveBalanceService;
    private readonly IAccountPunishmentMarkService _accountPunishmentMarkService;
    private readonly ISignEffectiveBalanceService _signEffectiveBalanceService;
    private readonly ISignatureService _signatureService;
    private readonly ILogger<SignEffectiveBalanceResolver> _logger;

    public SignEffectiveBalanceResolver(IEffectiveBalanceService effectiveBalanceService,
        IAccountPunishmentMarkService accountPunishmentMarkService,
        ISignEffectiveBalanceService signEffectiveBalanceService, ISignatureService signatureService,
        ILogger<SignEffectiveBalanceResolver> logger)
    {
        _logger = logger;
        _signatureService = signatureService;
        _signEffectiveBalanceService = signEffectiveBalanceService;
        _accountPunishmentMarkService = accountPunishmentMarkService;
        _effectiveBalanceService = effectiveBalanceService;
    }

    //TODO: refactor this method to make it more readable
    public async Task Resolve(RewardCycle rewardCycle, List<EffectiveBalanceModel> effectiveBalanceModels)
    {
        _logger.LogInformation(
            $"Start signing effective balance for cycle - Era: {rewardCycle.EndEraIndex} StartBlock: {rewardCycle.StartBlock}.");
        //sort startblock and endblock  and get the smallest startblock and largest endblock from effectiveBalanceModels 
        var startBlock = effectiveBalanceModels.Min(e => e.StartBlock);
        var endBlock = effectiveBalanceModels.Max(e => e.EndBlock);

        //load the punishment marks in this era
        _logger.LogInformation($"Loading punishment marks from block {startBlock} to {endBlock}.");
        var punishmentMarks =
            await _accountPunishmentMarkService.LoadNewPunishmentMarksByBlockRange(startBlock, endBlock);
        //Loop the effective balance accounts
        var readyToSignEffectiveBalance = new List<SignEffectiveBalanceModel>();

        //group effectiveBalanceModels by account and generate dictionary
        var effectiveBalanceModelsGroupByAccount = effectiveBalanceModels.GroupBy(e => e.Account)
            .ToDictionary(e => e.Key, e => e.ToList());

        var punishmentMarksGroupByAccount = punishmentMarks.GroupBy(p => p.Account)
            .ToDictionary(p => p.Key, p => p.ToList());

        //loop account in effectiveBalanceModels and find if it is in punishmentMarksGroupByAccount
        foreach (var eb in effectiveBalanceModelsGroupByAccount)
        {
            var account = eb.Key;
            if (account == null) continue;

            // check
            // if account IS in the punishment marked list
            if (punishmentMarksGroupByAccount.ContainsKey(account))
            {
                var toSignRecords = await WhenAccountHasPunishmentRecords(account, rewardCycle);
                readyToSignEffectiveBalance.AddRange(toSignRecords);
            }
            else // if account is NOT in the punishment marked list 
            {
                var toSignRecords =
                    await WhenAccountHasNoPunishmentRecords(rewardCycle, account, eb);
                // Add it into sign effective balance collection, and ready to sign
                readyToSignEffectiveBalance.AddRange(toSignRecords);
            }
        }

        _logger.LogInformation($"Updating {punishmentMarks.Count} punishment marks applied.");
        await _accountPunishmentMarkService.UpdatePunishmentMarksApplied(punishmentMarks);
        //save to SignEffectiveBalanceModel collection
        _logger.LogInformation($"Saving {readyToSignEffectiveBalance.Count} signed effective balances to sign.");
        await _signEffectiveBalanceService.InsertSignEffectiveBalance(readyToSignEffectiveBalance);
    }

    private async Task<IEnumerable<SignEffectiveBalanceModel>> WhenAccountHasNoPunishmentRecords(
        RewardCycle rewardCycle,
        string account, KeyValuePair<string?, List<EffectiveBalanceModel>> eb)
    {
        // query sign-effective-balance to get the latest era index
        // then find the gap between the latest era index and the end era index in effective-balance
        // only add the gap data
        // get latest era index in sign-effective-balance as start era
        var startEra = await _signEffectiveBalanceService.FindLatestEraIndex(account);
        startEra = startEra == 0 ? rewardCycle.StartEraIndex : startEra + 1;

        // get latest era index in effective-balance as end era
        var endEra = eb.Value.Max(e => e.EraIndex);

        // query effective balance 
        var loadEffectiveBalanceInBlockRange =
            await _effectiveBalanceService.LoadAccountEffectiveBalanceInEraRange(account,
                startEra,
                endEra
            );
        var mapped = loadEffectiveBalanceInBlockRange.Select(e =>
            new SignEffectiveBalanceModel()
            {
                Account = e.Account,
                EffectiveBalance = e.EffectiveBalance,
                EraIndex = e.EraIndex,
                EffectiveBlocks = e.EffectiveBlocks,
            });
        return mapped;
    }

    private async Task<IEnumerable<SignEffectiveBalanceModel>> WhenAccountHasPunishmentRecords(string account,
        RewardCycle rewardCycle)
    {
        // query effective balance 
        var loadEffectiveBalanceInEraRange =
            await _effectiveBalanceService.LoadAccountEffectiveBalanceInEraRange(account,
                rewardCycle.StartEraIndex,
                rewardCycle.EndEraIndex
            );
        var mapped = loadEffectiveBalanceInEraRange.Select(l => new SignEffectiveBalanceModel()
        {
            Account = l.Account,
            EffectiveBalance = l.EffectiveBalance,
            EraIndex = l.EraIndex,
            EffectiveBlocks = l.EffectiveBlocks,
        });
        return mapped;
    }

    public async Task SignData(int batchSize = Pagination.DefaultSignDataBatch)
    {
        _logger.LogInformation($"Signing effective balance data. Batch size: {batchSize}.");
        //load all the sign effective balance data that are not signed and timestamp is latest (max)
        //TODO: check why signed effective balance has huge number of data 
        var signEffectiveBalances = await _signEffectiveBalanceService.LoadUnsignedEffectiveBalances();

        _logger.LogInformation($"Loaded {signEffectiveBalances.Count} effective balances to sign.");

        //group by era index and convert to dictionary that key is era index
        var groupByEraIndex = signEffectiveBalances.GroupBy(s => s.EraIndex)
            .OrderBy(s => s.Key)
            .ToDictionary(s => s.Key, s => s.ToList());

        //loop the groupByEraIndex and sign them
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var tasks = new List<Task<List<SignEffectiveBalanceModel>>>();
        foreach (var eraIndex in groupByEraIndex.Keys)
        {
            var signEffectiveBalance = groupByEraIndex[eraIndex];
            tasks.Add(BatchSignEffectiveBalances(batchSize, eraIndex, signEffectiveBalance, timestamp));
        }

        var result = await Task.WhenAll(tasks);

        var allResults = result.SelectMany(x => x).ToList();
        await _signEffectiveBalanceService.UpdateSignedEffectiveBalance(allResults);
    }

    private async Task<List<SignEffectiveBalanceModel>> BatchSignEffectiveBalances(int batchSize, int eraIndex,
        List<SignEffectiveBalanceModel> signEffectiveBalances, long timestamp)
    {
        var result = new List<SignEffectiveBalanceModel>();
        //paged by 500 and sign them
        var batch = 1;
        var total = signEffectiveBalances.Count;
        var totalBatch = total / batchSize + 1;
        for (var i = 0; i < totalBatch; i++)
        {
            var batchSignEffectiveBalances = signEffectiveBalances.Skip(i * batchSize).Take(batchSize).ToList();
            // serialized the list and use base64 to encrypt the data to pass in SignMessage
            var serializedData = JsonConvert.SerializeObject(batchSignEffectiveBalances.Select(x =>
                new SignEffectiveBalanceDto()
                {
                    Account = x.Account,
                    EffectiveBalance = x.EffectiveBalance,
                    EraIndex = x.EraIndex,
                }));

            var base64Data = _signatureService.Base64Encrypt(serializedData);
            var signature = await _signatureService.SignMessage(base64Data);
            var batchNumber = $"{timestamp}-{eraIndex}-{batch}";
            batch++;
            _logger.LogInformation($"Signing batch {batchNumber} with {batchSignEffectiveBalances.Count} records.");
            foreach (var signEffectiveBalance in batchSignEffectiveBalances)
            {
                signEffectiveBalance.Signature = signature;
                signEffectiveBalance.Timestamp = timestamp;
                signEffectiveBalance.BatchNumber = batchNumber;
            }

            result.AddRange(batchSignEffectiveBalances);
        }

        return result;
    }
}