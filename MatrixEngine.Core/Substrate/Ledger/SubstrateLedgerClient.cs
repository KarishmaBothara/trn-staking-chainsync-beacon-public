using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using Isopoh.Cryptography.Blake2b;
using MatrixEngine.Core.Models.DTOs;
using MatrixEngine.Core.Models.Events;


namespace MatrixEngine.Core.Substrate.Ledger
{
    // Interface for the Substrate ledger client
    public interface ISubstrateLedgerClient
    {
        // Gets the staking ledger for an account at a specific block
        Task<string> GetBondedTypeAsync(string accountId, int blockNumber, string bondedAmount);
    }
    
    // Client for fetching staking ledger information from a Substrate node
    public class SubstrateLedgerClient : ISubstrateLedgerClient
    {
        private readonly ILogger<SubstrateLedgerClient> _logger;
        // private readonly SubstrateClient _client;
        private readonly string _nodeEndpoint;
        
        // Initializes a new instance of the <see cref="SubstrateLedgerClient"/> class.
        public SubstrateLedgerClient(
            IOptions<SubstrateSettings> options,
            ILogger<SubstrateLedgerClient> logger)
        {
            _logger = logger;
            _nodeEndpoint = options.Value.NodeApiEndpoint;
        }
        
        // Gets the block hash for a specific block number
        private async Task<string?> GetBlockHashAsync(int blockNumber)
        {
            var requestBody = new
            {
                jsonrpc = "2.0",
                method = "chain_getBlockHash",
                @params = new object[] { blockNumber },
                id = 1
            };
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            using var client = new HttpClient();
            var response = await client.PostAsync(_nodeEndpoint, content);
            var responseString = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<JsonElement>(responseString);
            var blockHash = result.GetProperty("result").GetString();
            if (string.IsNullOrEmpty(blockHash))
            {
                _logger.LogWarning("No block hash found for block {BlockNumber}", blockNumber);
                return null;
            }

            return blockHash;
        }
        
        private async Task<string?> GetStorageDataAsync(string at, string accountId)
        {
            // Hardcoded fixed prefix for Staking.Ledger storage map
            var fixedPrefix = "0x5f3e4907f716ac89b6347d15ececedca422adb579f1dbf4f3886c5cfa3bb8cc4";
            var key = Hasher.ComputeStorageValue(fixedPrefix, accountId);
            var requestBody = new
            {
                jsonrpc = "2.0",
                method = "state_getStorage",
                @params = new object[] { Hasher.ToHex(key), at },
                id = 1
            };
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            using var client = new HttpClient();
            var response = await client.PostAsync(_nodeEndpoint, content);
            var responseString = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<JsonElement>(responseString);
            if (result.GetProperty("result").ValueKind == JsonValueKind.Null)
            {
                return null;
            }  
            var storageData = result.GetProperty("result").GetString();
            return storageData;
        }

        // Get the staking ledger data and parse it
        // Returns null if the ledger is not found
        private async Task<StakingLedgerModel?> GetStakingLedgerAsync(string accountId, int blockNumber)
        {
            _logger.LogInformation("Getting staking ledger for account {AccountId} at block {BlockNumber}", accountId, blockNumber);
            
            const int maxRetries = 10;
            const int retryDelayMs = 20000; // 20 seconds
            int retryCount = 0;
            
            while (true)
            {
                try
                {
                    // Get ledger info for the current block
                    var blockHash = await GetBlockHashAsync(blockNumber);
                    _logger.LogInformation($"Block hash for block {blockNumber} is {blockHash}");
                    if (string.IsNullOrEmpty(blockHash))
                    {
                        _logger.LogWarning($"Could not find block hash for block: {blockNumber}");
                        return null;
                    }
                    var storageValue = await GetStorageDataAsync(blockHash, accountId);
                    if (string.IsNullOrEmpty(storageValue)) return null;
                    
                    return StakingLedgerParser.Parse(storageValue, _logger);
                }
                catch (Exception ex)
                {
                    retryCount++;
                    
                    if (retryCount >= maxRetries)
                    {
                        _logger.LogError(ex, "Error getting staking ledger for account {AccountId} at block {BlockNumber} after {RetryCount} retries. Giving up.", 
                            accountId, blockNumber, retryCount);
                        throw;
                    }
                    
                    _logger.LogWarning(ex, "Error getting staking ledger for account {AccountId} at block {BlockNumber}. Retry {RetryCount}/{MaxRetries} in {RetryDelayMs}ms", 
                        accountId, blockNumber, retryCount, maxRetries, retryDelayMs);
                    
                    await Task.Delay(retryDelayMs);
                }
            }
        }
        
        // Gets the bonded type for an account at a specific block
        // This should be called at the block number for all bonded events
        // It will compare the on-chain ledger value for the account at the block number for the bonded event
        // as well as the block number prior. 
        // If the ledger total value changes, that means the type is bonded
        // if the total does not change (i.e. value comes from the unlocking) that is a rebond
        public async Task<string> GetBondedTypeAsync(string accountId, int blockNumber, string bondedAmount)
        {
            // All genesis block events are bonded
            if (blockNumber == 0) return TransactionType.Bonded;
            try
            {
                var ledgerModel = await GetStakingLedgerAsync(accountId, blockNumber);
                if (ledgerModel == null)
                {
                    _logger.LogWarning("No ledger found for account {AccountId} at block {BlockNumber}", accountId, blockNumber);
                    return TransactionType.Bonded;
                }

                var previousLedgerModel = await GetStakingLedgerAsync(accountId, blockNumber - 1);
                if (previousLedgerModel == null)
                {
                    // This is a likely scenario where the account is being bonded for the first time
                    _logger.LogWarning("No previous ledger found for account {AccountId} at block {BlockNumber}", accountId, blockNumber - 1);
                    return TransactionType.Bonded;
                }
                
                // Perform some basic verification. The active amount should be the same as the bonded amount
                var activeDiff = ledgerModel.Active - previousLedgerModel.Active;
                if (activeDiff.ToString() != bondedAmount)
                {
                    _logger.LogError("Active amount mismatch for account {AccountId} at block {BlockNumber}: expected {Expected}, got {Got}", 
                        accountId, blockNumber, bondedAmount, activeDiff);
                }
                
                // compare this block with previous block
                var totalDiff = ledgerModel.Total - previousLedgerModel.Total;
                if (totalDiff == 0)
                {
                    // If the total has not changed, that means the bond event is a rebonding event
                    return TransactionType.ReBonded;
                }
                return TransactionType.Bonded;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting staking ledger for account {AccountId} at block {BlockNumber}", accountId, blockNumber);
                throw;
            }
        }
    }
} 