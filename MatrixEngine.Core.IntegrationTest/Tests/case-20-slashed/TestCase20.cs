using MatrixEngine.Core.Engine;
using MatrixEngine.Core.IntegrationTest.Fixtures;
using Xunit.Abstractions;
using Xunit.Extensions.Ordering;
using Xunit.Microsoft.DependencyInjection.Abstracts;
using MongoDB.Driver;
using MatrixEngine.Core.Services;
using MatrixEngine.Core.Models;
using MatrixEngine.Core.Constants;
namespace MatrixEngine.Core.IntegrationTest.Tests.case_18;

[Order(3)]
public class TestCase20 : TestBed<IntegrationTestFixture>
{
    public TestCase20(ITestOutputHelper testOutputHelper, IntegrationTestFixture fixture) : base(testOutputHelper,
        fixture)
    {
    }
    
    [Fact]
    public async Task Test_Scenario_1()
    {
        // This test case will test whether the balance is correctly reduced after a slash

        var dataLoader = _fixture.GetService<IDataLoader>(_testOutputHelper);
        await dataLoader?.LoadCase("case-20")!;

        var engineCore = _fixture.GetService<IEngineCore>(_testOutputHelper);
        var signEffectiveBalanceService = _fixture.GetService<ISignEffectiveBalanceService>(_testOutputHelper);

        // run engine core with only one era, block 0 - 9
        await engineCore?.Start()!;

        // Verify the effective balances
        var database = _fixture.GetService<IMongoDatabase>(_testOutputHelper);
        var effectiveBalances = database.GetCollection<EffectiveBalanceModel>(DbCollectionName.EffectiveBalance);
        var effectiveBalance = await effectiveBalances.Find(_ => true)
            .Sort(Builders<EffectiveBalanceModel>.Sort.Ascending(x => x.StartBlock))
            .ToListAsync();
        
        // Verify the effective balance is as expected
        Assert.Equal(3, effectiveBalance.Count);
        
        // Account 0xE04CC55... (0-24)
        // Original bonded event
        var entry1 = effectiveBalance[0];
        Assert.Equal(0, entry1.StartBlock);
        Assert.Equal(24, entry1.EndBlock);
        Assert.Equal(25, entry1.EffectiveBlocks);
        Assert.Equal(0, entry1.VtxDistributionId);
        Assert.Equal(0.0277778, entry1.Percentage);
        // Bonded
        Assert.Equal("20000", entry1.Bonded.Balance);
        Assert.Equal("15000", entry1.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", entry1.Bonded.Rate.ToString());
        Assert.Equal("Staker", entry1.Bonded.StakerType);
        Assert.Equal("10", entry1.Bonded.RewardPoints);
        // Unlocking
        Assert.Equal("0", entry1.Unlocking.Balance);
        Assert.Equal("0", entry1.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry1.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry1.Unlocking.StakerType);
        Assert.Equal("0", entry1.Unlocking.RewardPoints);
        
        // Account 0xE04CC55... (25-99)
        // slashed event, taken out of bonded
        var entry2 = effectiveBalance[1];
        Assert.Equal(25, entry2.StartBlock);
        Assert.Equal(899, entry2.EndBlock);
        Assert.Equal(875, entry2.EffectiveBlocks);
        Assert.Equal(0, entry2.VtxDistributionId);
        Assert.Equal(0.9722222, entry2.Percentage);
        // Bonded
        Assert.Equal("15000", entry2.Bonded.Balance);
        Assert.Equal("15000", entry2.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", entry2.Bonded.Rate.ToString());
        Assert.Equal("Staker", entry2.Bonded.StakerType);
        Assert.Equal("358", entry2.Bonded.RewardPoints);
        // Unlocking
        Assert.Equal("0", entry2.Unlocking.Balance);
        Assert.Equal("0", entry2.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry2.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry2.Unlocking.StakerType);
        Assert.Equal("0", entry2.Unlocking.RewardPoints);
        
        // Account 0xE04CC55... (20-29) 
        // Withdrawn event
        var entry3 = effectiveBalance[2];
        Assert.Equal(900, entry3.StartBlock);
        Assert.Equal(1799, entry3.EndBlock);
        Assert.Equal(900, entry3.EffectiveBlocks);
        Assert.Equal(1, entry3.VtxDistributionId);
        Assert.Equal(1, entry3.Percentage);
        // Bonded
        Assert.Equal("15000", entry3.Bonded.Balance);
        Assert.Equal("15000", entry3.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", entry3.Bonded.Rate.ToString());
        Assert.Equal("Staker", entry3.Bonded.StakerType);
        Assert.Equal("369", entry3.Bonded.RewardPoints);
        // Unlocking - Shows decreased unlocking balance due to withdraw
        Assert.Equal("0", entry3.Unlocking.Balance);
        Assert.Equal("0", entry3.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry3.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry3.Unlocking.StakerType);
        Assert.Equal("0", entry3.Unlocking.RewardPoints);
        

        var balanceCollection = database.GetCollection<BalanceModel>(DbCollectionName.Balance);
        var balances = await balanceCollection.Find(_ => true)
            .Sort(Builders<BalanceModel>.Sort.Ascending(x => x.StartBlock))
            .ToListAsync();
        
        // Verify the balance count
        Assert.Equal(4, balances.Count);
        
        // Balance Entry 1 (0-24) - Initial bonding
        var balanceEntry1 = balances[0];
        Assert.Equal("0xE04CC55ebEE1cBCE552f250e85c57B70B2E2625b", balanceEntry1.Account);
        Assert.Equal(0, balanceEntry1.StartBlock);
        Assert.Equal(24, balanceEntry1.EndBlock);
        // Bonded
        Assert.Equal("0", balanceEntry1.Bonded.PreviousBalance);
        Assert.Equal("20000", balanceEntry1.Bonded.BalanceChange);
        Assert.Equal("20000", balanceEntry1.Bonded.BalanceInBlockRange);
        Assert.Equal("Staker", balanceEntry1.Bonded.StakerType);
        // Unlocking
        Assert.Equal("0", balanceEntry1.Unlocking.PreviousBalance);
        Assert.Equal("0", balanceEntry1.Unlocking.BalanceChange);
        Assert.Equal("0", balanceEntry1.Unlocking.BalanceInBlockRange);
        Assert.Equal("Staker", balanceEntry1.Unlocking.StakerType);
        
        // Balance Entry 2 (25-899) - Slash event
        var balanceEntry2 = balances[1];
        Assert.Equal("0xE04CC55ebEE1cBCE552f250e85c57B70B2E2625b", balanceEntry2.Account);
        Assert.Equal(25, balanceEntry2.StartBlock);
        Assert.Equal(899, balanceEntry2.EndBlock);
        // Bonded
        Assert.Equal("20000", balanceEntry2.Bonded.PreviousBalance);
        Assert.Equal("-5000", balanceEntry2.Bonded.BalanceChange);
        Assert.Equal("15000", balanceEntry2.Bonded.BalanceInBlockRange);
        Assert.Equal("Staker", balanceEntry2.Bonded.StakerType);
        // Unlocking
        Assert.Equal("0", balanceEntry2.Unlocking.PreviousBalance);
        Assert.Equal("0", balanceEntry2.Unlocking.BalanceChange);
        Assert.Equal("0", balanceEntry2.Unlocking.BalanceInBlockRange);
        Assert.Equal("Staker", balanceEntry2.Unlocking.StakerType);
        
        // Balance Entry 3 (900-1799) - Carry over to next cycle
        var balanceEntry3 = balances[2];
        Assert.Equal("0xE04CC55ebEE1cBCE552f250e85c57B70B2E2625b", balanceEntry3.Account);
        Assert.Equal(900, balanceEntry3.StartBlock);
        Assert.Equal(1799, balanceEntry3.EndBlock);
        // Bonded
        Assert.Equal("15000", balanceEntry3.Bonded.PreviousBalance);
        Assert.Equal("0", balanceEntry3.Bonded.BalanceChange);
        Assert.Equal("15000", balanceEntry3.Bonded.BalanceInBlockRange);
        Assert.Equal("Staker", balanceEntry3.Bonded.StakerType);
        // Unlocking
        Assert.Equal("0", balanceEntry3.Unlocking.PreviousBalance);
        Assert.Equal("0", balanceEntry3.Unlocking.BalanceChange);
        Assert.Equal("0", balanceEntry3.Unlocking.BalanceInBlockRange);
        Assert.Equal("Staker", balanceEntry3.Unlocking.StakerType);
        
        // Balance Entry 4 (1800-1819) - Continued into next cycle
        var balanceEntry4 = balances[3];
        Assert.Equal("0xE04CC55ebEE1cBCE552f250e85c57B70B2E2625b", balanceEntry4.Account);
        Assert.Equal(1800, balanceEntry4.StartBlock);
        Assert.Equal(1819, balanceEntry4.EndBlock);
        // Bonded
        Assert.Equal("15000", balanceEntry4.Bonded.PreviousBalance);
        Assert.Equal("0", balanceEntry4.Bonded.BalanceChange);
        Assert.Equal("15000", balanceEntry4.Bonded.BalanceInBlockRange);
        Assert.Equal("Staker", balanceEntry4.Bonded.StakerType);
        // Unlocking
        Assert.Equal("0", balanceEntry4.Unlocking.PreviousBalance);
        Assert.Equal("0", balanceEntry4.Unlocking.BalanceChange);
        Assert.Equal("0", balanceEntry4.Unlocking.BalanceInBlockRange);
        Assert.Equal("Staker", balanceEntry4.Unlocking.StakerType);
    }
}