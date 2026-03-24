using MatrixEngine.Core.Engine;
using MatrixEngine.Core.IntegrationTest.Fixtures;
using Xunit.Abstractions;
using Xunit.Extensions.Ordering;
using Xunit.Microsoft.DependencyInjection.Abstracts;
using MongoDB.Driver;
using MatrixEngine.Core.Services;
using MatrixEngine.Core.Models;
using MatrixEngine.Core.Constants;
using System.Numerics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MatrixEngine.Core.IntegrationTest.Tests.case_18;

[Order(3)]
public class TestCase18 : TestBed<IntegrationTestFixture>
{
    public TestCase18(ITestOutputHelper testOutputHelper, IntegrationTestFixture fixture) : base(testOutputHelper,
        fixture)
    {
    }

    // insert an era into the eras db
    private async Task InsertTestEra(int eraIndex)
    {
        var eraService = _fixture.GetService<IEraService>(_testOutputHelper);
        var newEra = new EraModel
        {
            EraIndex = eraIndex,
            StartBlock = eraIndex * 10,
            EndBlock = eraIndex * 10 + 9,
        };
        await eraService?.InsertEra(newEra)!;
    }

    [Fact]
    public async Task Test_Scenario_1()
    {
        // This test case will test that the balances are calculated correctly for each era as they come. 
        // It will have a bunch of transactions, but the eras will be manually added, then the engine core re-run to ensure
        // the data is updated correctly

        var dataLoader = _fixture.GetService<IDataLoader>(_testOutputHelper);
        await dataLoader?.LoadCase("case-18")!;

        var engineCore = _fixture.GetService<IEngineCore>(_testOutputHelper);
        var signEffectiveBalanceService = _fixture.GetService<ISignEffectiveBalanceService>(_testOutputHelper);

        // run engine core with only one era, block 0 - 9
        await engineCore?.Start()!;

        // Get all signed effective balances
        var database = _fixture.GetService<IMongoDatabase>(_testOutputHelper);
        var balanceCollection = database.GetCollection<BalanceModel>(DbCollectionName.Balance);
        var balances = await balanceCollection.Find(_ => true)
            .Sort(Builders<BalanceModel>.Sort.Ascending(x => x.StartBlock))
            .ToListAsync();
        
        // should only be one at this point
        Assert.Equal(1, balances.Count);
        // Verify first balance from block 0 - 9
        var balance1 = balances[0];
        Assert.Equal("0xE04CC55ebEE1cBCE552f250e85c57B70B2E2625b", balance1.Account);
        Assert.Equal(0, balance1.StartBlock);
        Assert.Equal(9, balance1.EndBlock);
        // bonded
        Assert.Equal("0", balance1.Bonded.PreviousBalance);
        Assert.Equal("3000", balance1.Bonded.BalanceChange);
        Assert.Equal("3000", balance1.Bonded.BalanceInBlockRange);
        // Unlocking
        Assert.Equal("0", balance1.Unlocking.PreviousBalance);
        Assert.Equal("0", balance1.Unlocking.BalanceChange);
        Assert.Equal("0", balance1.Unlocking.BalanceInBlockRange);
        
        // insert another era into the eras db and run engine core again
        await InsertTestEra(1);
        await engineCore?.Start()!;

        balances = await balanceCollection.Find(_ => true)
            .Sort(Builders<BalanceModel>.Sort.Ascending(x => x.StartBlock))
            .ToListAsync();
        
        // should STILL only be one at this point, the next transaction is at block 20
        // All values should remain the same due to no new transactions, however the 
        // end block should now reflect the end of the new era
        Assert.Equal(1, balances.Count);
        balance1 = balances[0];
        Assert.Equal(0, balance1.StartBlock);
        Assert.Equal(19, balance1.EndBlock); // updated to new endBlock
        // bonded
        Assert.Equal("0", balance1.Bonded.PreviousBalance);
        Assert.Equal("3000", balance1.Bonded.BalanceChange);
        Assert.Equal("3000", balance1.Bonded.BalanceInBlockRange);
        // Unlocking
        Assert.Equal("0", balance1.Unlocking.PreviousBalance);
        Assert.Equal("0", balance1.Unlocking.BalanceChange);
        Assert.Equal("0", balance1.Unlocking.BalanceInBlockRange);
        
        // insert era 2 into the eras db and run engine core again
        await InsertTestEra(2);
        await engineCore?.Start()!;
        balances = await balanceCollection.Find(_ => true)
            .Sort(Builders<BalanceModel>.Sort.Ascending(x => x.StartBlock))
            .ToListAsync();
        // Should now have 2 transactions, as we had an unbonded event at block 25
        Assert.Equal(2, balances.Count);
        
        // Verify first balance from block 0 - 24
        balance1 = balances[0];
        Assert.Equal(0, balance1.StartBlock);
        Assert.Equal(24, balance1.EndBlock); // updated to new endBlock
        // bonded
        Assert.Equal("0", balance1.Bonded.PreviousBalance);
        Assert.Equal("3000", balance1.Bonded.BalanceChange);
        Assert.Equal("3000", balance1.Bonded.BalanceInBlockRange);
        // Unlocking
        Assert.Equal("0", balance1.Unlocking.PreviousBalance);
        Assert.Equal("0", balance1.Unlocking.BalanceChange);
        Assert.Equal("0", balance1.Unlocking.BalanceInBlockRange);
        
        // Verify second balance from block 25 - 29
        var balance2 = balances[1];
        Assert.Equal("0xE04CC55ebEE1cBCE552f250e85c57B70B2E2625b", balance2.Account);
        Assert.Equal(25, balance2.StartBlock);
        Assert.Equal(29, balance2.EndBlock);
        // bonded
        Assert.Equal("3000", balance2.Bonded.PreviousBalance);
        Assert.Equal("-2400", balance2.Bonded.BalanceChange);
        Assert.Equal("600", balance2.Bonded.BalanceInBlockRange);
        // unlocking
        Assert.Equal("0", balance2.Unlocking.PreviousBalance);
        Assert.Equal("2400", balance2.Unlocking.BalanceChange);
        Assert.Equal("2400", balance2.Unlocking.BalanceInBlockRange);
        
        // insert era 3 into the eras db and run engine core again
        await InsertTestEra(3);
        await engineCore?.Start()!;
        balances = await balanceCollection.Find(_ => true)
            .Sort(Builders<BalanceModel>.Sort.Ascending(x => x.StartBlock))
            .ToListAsync();
        // Should now have 3 transactions, as we had a rebonded event in block 30
        Assert.Equal(3, balances.Count);
        
        // Verify first balance from block 0 - 24
        // Should be unchanged
        balance1 = balances[0];
        Assert.Equal(0, balance1.StartBlock);
        Assert.Equal(24, balance1.EndBlock);
        // bonded
        Assert.Equal("0", balance1.Bonded.PreviousBalance);
        Assert.Equal("3000", balance1.Bonded.BalanceChange);
        Assert.Equal("3000", balance1.Bonded.BalanceInBlockRange);
        // Unlocking
        Assert.Equal("0", balance1.Unlocking.PreviousBalance);
        Assert.Equal("0", balance1.Unlocking.BalanceChange);
        Assert.Equal("0", balance1.Unlocking.BalanceInBlockRange);
        
        
        // Verify second balance from block 25 - 29
        // Should be unchanged as the new tx was at block 30
        balance2 = balances[1];
        Assert.Equal(25, balance2.StartBlock);
        Assert.Equal(29, balance2.EndBlock);
        // bonded
        Assert.Equal("3000", balance2.Bonded.PreviousBalance);
        Assert.Equal("-2400", balance2.Bonded.BalanceChange);
        Assert.Equal("600", balance2.Bonded.BalanceInBlockRange);
        // unlocking
        Assert.Equal("0", balance2.Unlocking.PreviousBalance);
        Assert.Equal("2400", balance2.Unlocking.BalanceChange);
        Assert.Equal("2400", balance2.Unlocking.BalanceInBlockRange);
        
        // Verify new balance from block 30 - 39
        // THis was the rebond of 700 at block 30
        var balance3 = balances[2];
        Assert.Equal("0xE04CC55ebEE1cBCE552f250e85c57B70B2E2625b", balance3.Account);
        Assert.Equal(30, balance3.StartBlock);
        Assert.Equal(39, balance3.EndBlock);
        // bonded
        Assert.Equal("600", balance3.Bonded.PreviousBalance);
        Assert.Equal("700", balance3.Bonded.BalanceChange);
        Assert.Equal("1300", balance3.Bonded.BalanceInBlockRange);
        // unlocking
        Assert.Equal("2400", balance3.Unlocking.PreviousBalance);
        Assert.Equal("-700", balance3.Unlocking.BalanceChange);
        Assert.Equal("1700", balance3.Unlocking.BalanceInBlockRange);
        
        
        // insert era 4 into the eras db and run engine core again
        await InsertTestEra(4);
        await engineCore?.Start()!;
        balances = await balanceCollection.Find(_ => true)
            .Sort(Builders<BalanceModel>.Sort.Ascending(x => x.StartBlock))
            .ToListAsync();
        // Should now have 4 transactions, as we had a withdrawn event in block 49
        Assert.Equal(4, balances.Count);
        
        // Skipping asserting first 2, they will be asserted again at the end of the reward cycle
        
        // Verify third balance from block 30 - 48
        balance3 = balances[2];
        Assert.Equal(30, balance3.StartBlock);
        Assert.Equal(48, balance3.EndBlock); // Updated to reflect new range up to withdrawn transaction
        // bonded
        Assert.Equal("600", balance3.Bonded.PreviousBalance);
        Assert.Equal("700", balance3.Bonded.BalanceChange);
        Assert.Equal("1300", balance3.Bonded.BalanceInBlockRange);
        // unlocking
        Assert.Equal("2400", balance3.Unlocking.PreviousBalance);
        Assert.Equal("-700", balance3.Unlocking.BalanceChange);
        Assert.Equal("1700", balance3.Unlocking.BalanceInBlockRange);
        
        // Verify fourth balance from block 49 - 49
        // This was the withdrawn of 1000 at block 49
        var balance4 = balances[3];
        Assert.Equal(49, balance4.StartBlock);
        Assert.Equal(49, balance4.EndBlock);
        // bonded
        Assert.Equal("1300", balance4.Bonded.PreviousBalance);
        Assert.Equal("0", balance4.Bonded.BalanceChange);
        Assert.Equal("1300", balance4.Bonded.BalanceInBlockRange);
        // unlocking
        Assert.Equal("1700", balance4.Unlocking.PreviousBalance);
        Assert.Equal("-1000", balance4.Unlocking.BalanceChange);
        Assert.Equal("700", balance4.Unlocking.BalanceInBlockRange);
        
        // insert era 5 AND era 6 into the eras db and run engine core again
        await InsertTestEra(5);
        await InsertTestEra(6);
        await engineCore?.Start()!;
        balances = await balanceCollection.Find(_ => true)
            .Sort(Builders<BalanceModel>.Sort.Ascending(x => x.StartBlock))
            .ToListAsync();
        // Should now have 5 transactions,
        // as we had an unbonded event in block 50 AND a bonded event in block 50 (Count as one balance change)
        Assert.Equal(5, balances.Count);
        
        // Skipping first 3, they will be asserted again at the end of the reward cycle
        // Verify fourth balance from block 49 - 49
        // Should be unchanged as the next transaction was at block 50
        balance4 = balances[3];
        Assert.Equal(49, balance4.StartBlock);
        Assert.Equal(49, balance4.EndBlock);
        // bonded
        Assert.Equal("1300", balance4.Bonded.PreviousBalance);
        Assert.Equal("0", balance4.Bonded.BalanceChange);
        Assert.Equal("1300", balance4.Bonded.BalanceInBlockRange);
        // unlocking
        Assert.Equal("1700", balance4.Unlocking.PreviousBalance);
        Assert.Equal("-1000", balance4.Unlocking.BalanceChange);
        Assert.Equal("700", balance4.Unlocking.BalanceInBlockRange);
        
        // Verify fifth balance from block 50 - 69
        var balance5 = balances[4];
        Assert.Equal(50, balance5.StartBlock);
        Assert.Equal(69, balance5.EndBlock);
        // bonded
        Assert.Equal("1300", balance5.Bonded.PreviousBalance);
        Assert.Equal("-100", balance5.Bonded.BalanceChange);
        Assert.Equal("1200", balance5.Bonded.BalanceInBlockRange);
        // unlocking
        Assert.Equal("700", balance5.Unlocking.PreviousBalance);
        Assert.Equal("300", balance5.Unlocking.BalanceChange);
        Assert.Equal("1000", balance5.Unlocking.BalanceInBlockRange);
        
        // Add 20 eras and check that the endBlock updates for the last balance
        for (var i = 7; i < 27; i++)
        {
            await InsertTestEra(i);
        }
        await engineCore?.Start()!;
        balances = await balanceCollection.Find(_ => true)
            .Sort(Builders<BalanceModel>.Sort.Ascending(x => x.StartBlock))
            .ToListAsync();
        // Should still have 5 transactions, but the last one should now be updated to the end of the last era
        Assert.Equal(5, balances.Count);
        // Verify last balance
        balance5 = balances[4];
        Assert.Equal(50, balance5.StartBlock);
        Assert.Equal(269, balance5.EndBlock); // Updated to reflect new range up to last era
        // bonded
        Assert.Equal("1300", balance5.Bonded.PreviousBalance);
        Assert.Equal("-100", balance5.Bonded.BalanceChange);
        Assert.Equal("1200", balance5.Bonded.BalanceInBlockRange);
        // unlocking
        Assert.Equal("700", balance5.Unlocking.PreviousBalance);
        Assert.Equal("300", balance5.Unlocking.BalanceChange);
        Assert.Equal("1000", balance5.Unlocking.BalanceInBlockRange);
        
        await InsertTestEra(27);
        await InsertTestEra(28);
        await engineCore?.Start()!;
        // Add the remaining eras until era 89, this should trigger the reward cycle calculations
        for (var i = 27; i < 90; i++)
        {
            await InsertTestEra(i);
        }
        await engineCore?.Start()!;
        balances = await balanceCollection.Find(_ => true)
            .Sort(Builders<BalanceModel>.Sort.Ascending(x => x.StartBlock))
            .ToListAsync();
        // Should still have 5 transactions, but the last one should now be updated to the end of the last era
        Assert.Equal(5, balances.Count);
        
        // Verify ALL balances to ensure block ranges are correct
        
        // Verify first balance from block 0 - 24
        // Should be unchanged
        balance1 = balances[0];
        Assert.Equal(0, balance1.StartBlock);
        Assert.Equal(24, balance1.EndBlock);
        // bonded
        Assert.Equal("0", balance1.Bonded.PreviousBalance);
        Assert.Equal("3000", balance1.Bonded.BalanceChange);
        Assert.Equal("3000", balance1.Bonded.BalanceInBlockRange);
        // Unlocking
        Assert.Equal("0", balance1.Unlocking.PreviousBalance);
        Assert.Equal("0", balance1.Unlocking.BalanceChange);
        Assert.Equal("0", balance1.Unlocking.BalanceInBlockRange);
        
        // Verify second balance from block 25 - 29
        // Should be unchanged as the new tx was at block 30
        balance2 = balances[1];
        Assert.Equal(25, balance2.StartBlock);
        Assert.Equal(29, balance2.EndBlock);
        // bonded
        Assert.Equal("3000", balance2.Bonded.PreviousBalance);
        Assert.Equal("-2400", balance2.Bonded.BalanceChange);
        Assert.Equal("600", balance2.Bonded.BalanceInBlockRange);
        // unlocking
        Assert.Equal("0", balance2.Unlocking.PreviousBalance);
        Assert.Equal("2400", balance2.Unlocking.BalanceChange);
        Assert.Equal("2400", balance2.Unlocking.BalanceInBlockRange);
        
        // Verify third balance from block 30 - 48
        balance3 = balances[2];
        Assert.Equal(30, balance3.StartBlock);
        Assert.Equal(48, balance3.EndBlock); 
        // bonded
        Assert.Equal("600", balance3.Bonded.PreviousBalance);
        Assert.Equal("700", balance3.Bonded.BalanceChange);
        Assert.Equal("1300", balance3.Bonded.BalanceInBlockRange);
        // unlocking
        Assert.Equal("2400", balance3.Unlocking.PreviousBalance);
        Assert.Equal("-700", balance3.Unlocking.BalanceChange);
        Assert.Equal("1700", balance3.Unlocking.BalanceInBlockRange);
        
        // Verify fourth balance from block 49 - 49
        balance4 = balances[3];
        Assert.Equal(49, balance4.StartBlock);
        Assert.Equal(49, balance4.EndBlock);
        // bonded
        Assert.Equal("1300", balance4.Bonded.PreviousBalance);
        Assert.Equal("0", balance4.Bonded.BalanceChange);
        Assert.Equal("1300", balance4.Bonded.BalanceInBlockRange);
        // unlocking
        Assert.Equal("1700", balance4.Unlocking.PreviousBalance);
        Assert.Equal("-1000", balance4.Unlocking.BalanceChange);
        Assert.Equal("700", balance4.Unlocking.BalanceInBlockRange);
        
        // Verify fifth balance from block 50 - 899
        balance5 = balances[4];
        Assert.Equal(50, balance5.StartBlock);
        Assert.Equal(899, balance5.EndBlock); // Updated to reflect new range up to last era
        // bonded
        Assert.Equal("1300", balance5.Bonded.PreviousBalance);
        Assert.Equal("-100", balance5.Bonded.BalanceChange);
        Assert.Equal("1200", balance5.Bonded.BalanceInBlockRange);
        // unlocking
        Assert.Equal("700", balance5.Unlocking.PreviousBalance);
        Assert.Equal("300", balance5.Unlocking.BalanceChange);
        Assert.Equal("1000", balance5.Unlocking.BalanceInBlockRange);
        
        // Check that the reward cycle was finished, and a new one created:
        var rewardCycleCollection = database.GetCollection<RewardCycleModel>(DbCollectionName.RewardCycle);
        var rewardCycles = await rewardCycleCollection.Find(_ => true)
            .Sort(Builders<RewardCycleModel>.Sort.Ascending(x => x.StartBlock))
            .ToListAsync();
        Assert.Equal(2, rewardCycles.Count);
        // Verify first reward cycle
        var rewardCycle1 = rewardCycles[0];
        Assert.Equal(0, rewardCycle1.VtxDistributionId);
        Assert.Equal(0, rewardCycle1.StartBlock);
        Assert.Equal(899, rewardCycle1.EndBlock);
        Assert.Equal(0, rewardCycle1.StartEraIndex);
        Assert.Equal(89, rewardCycle1.EndEraIndex);
        Assert.True(rewardCycle1.CalculationComplete);
        
        // Verify second reward cycle
        var rewardCycle2 = rewardCycles[1];
        Assert.Equal(1, rewardCycle2.VtxDistributionId);
        Assert.Equal(900, rewardCycle2.StartBlock);
        Assert.Equal(-1, rewardCycle2.EndBlock);
        Assert.Equal(90, rewardCycle2.StartEraIndex);
        Assert.Equal(-1, rewardCycle2.EndEraIndex);
        Assert.False(rewardCycle2.CalculationComplete); 
        
        
        // Now check the signed effective balance for this one account
        var signedEffectiveBalanceCollection = database.GetCollection<SignedEffectiveBalanceModel>(DbCollectionName.SignEffectiveBalance);
        var signedBalances = await signedEffectiveBalanceCollection.Find(_ => true)
            .Sort(Builders<SignedEffectiveBalanceModel>.Sort.Ascending(x => x.StartBlock).Ascending(x => x.VtxDistributionId))
            .ToListAsync();
        Assert.Equal(1, signedBalances.Count);
        // Verify first signed balance
        var signedBalance1 = signedBalances[0];
        Assert.Equal("0xE04CC55ebEE1cBCE552f250e85c57B70B2E2625b", signedBalance1.Account);
        Assert.Equal(0, signedBalance1.StartBlock);
        Assert.Equal(899, signedBalance1.EndBlock);
        Assert.Equal(0, signedBalance1.VtxDistributionId);
        Assert.Equal("50", signedBalance1.TotalRewardPoints);
        Assert.False(signedBalance1.Submitted);
        Assert.False(signedBalance1.Verified);
        
        
        // Now check the effective balance for this one account
        var effectiveBalanceCollection = database.GetCollection<EffectiveBalanceModel>(DbCollectionName.EffectiveBalance);
        var effectiveBalances = await effectiveBalanceCollection.Find(_ => true)
            .Sort(Builders<EffectiveBalanceModel>.Sort.Ascending(x => x.StartBlock))
            .ToListAsync();
        Assert.Equal(5, effectiveBalances.Count);
        
        // Verify first effective balance
        var effectiveBalance1 = effectiveBalances[0];
        Assert.Equal("0xE04CC55ebEE1cBCE552f250e85c57B70B2E2625b", effectiveBalance1.Account);
        Assert.Equal(0, effectiveBalance1.StartBlock);
        Assert.Equal(24, effectiveBalance1.EndBlock);
        Assert.Equal(25, effectiveBalance1.EffectiveBlocks);
        Assert.Equal(0, effectiveBalance1.VtxDistributionId);
        Assert.Equal(0.0277778, effectiveBalance1.Percentage);
        // Bonded
        Assert.Equal("3000", effectiveBalance1.Bonded.Balance);
        Assert.Equal("600", effectiveBalance1.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", effectiveBalance1.Bonded.Rate.ToString());
        Assert.Equal("Staker", effectiveBalance1.Bonded.StakerType);
        Assert.Equal("0", effectiveBalance1.Bonded.RewardPoints);
        // Unlocking
        Assert.Equal("0", effectiveBalance1.Unlocking.Balance);
        Assert.Equal("0", effectiveBalance1.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", effectiveBalance1.Unlocking.Rate.ToString());
        Assert.Equal("Staker", effectiveBalance1.Unlocking.StakerType);
        Assert.Equal("0", effectiveBalance1.Unlocking.RewardPoints);
        
        // Verify second effective balance
        var effectiveBalance2 = effectiveBalances[1];
        Assert.Equal("0xE04CC55ebEE1cBCE552f250e85c57B70B2E2625b", effectiveBalance2.Account);
        Assert.Equal(25, effectiveBalance2.StartBlock);
        Assert.Equal(29, effectiveBalance2.EndBlock);
        Assert.Equal(5, effectiveBalance2.EffectiveBlocks);
        Assert.Equal(0, effectiveBalance2.VtxDistributionId);
        Assert.Equal(0.0055556, effectiveBalance2.Percentage);
        // Bonded
        Assert.Equal("600", effectiveBalance2.Bonded.Balance);
        Assert.Equal("600", effectiveBalance2.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", effectiveBalance2.Bonded.Rate.ToString());
        Assert.Equal("Staker", effectiveBalance2.Bonded.StakerType);
        Assert.Equal("0", effectiveBalance2.Bonded.RewardPoints);
        // Unlocking
        Assert.Equal("2400", effectiveBalance2.Unlocking.Balance);
        Assert.Equal("700", effectiveBalance2.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", effectiveBalance2.Unlocking.Rate.ToString());
        Assert.Equal("Staker", effectiveBalance2.Unlocking.StakerType);
        Assert.Equal("0", effectiveBalance2.Unlocking.RewardPoints);
        
        // Verify third effective balance
        var effectiveBalance3 = effectiveBalances[2];
        Assert.Equal("0xE04CC55ebEE1cBCE552f250e85c57B70B2E2625b", effectiveBalance3.Account);
        Assert.Equal(30, effectiveBalance3.StartBlock);
        Assert.Equal(48, effectiveBalance3.EndBlock);
        Assert.Equal(19, effectiveBalance3.EffectiveBlocks);
        Assert.Equal(0, effectiveBalance3.VtxDistributionId);
        Assert.Equal(0.0211111, effectiveBalance3.Percentage);
        // Bonded
        Assert.Equal("1300", effectiveBalance3.Bonded.Balance);
        Assert.Equal("1200", effectiveBalance3.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", effectiveBalance3.Bonded.Rate.ToString());
        Assert.Equal("Staker", effectiveBalance3.Bonded.StakerType);
        Assert.Equal("0", effectiveBalance3.Bonded.RewardPoints);
        // Unlocking
        Assert.Equal("1700", effectiveBalance3.Unlocking.Balance);
        Assert.Equal("700", effectiveBalance3.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", effectiveBalance3.Unlocking.Rate.ToString());
        Assert.Equal("Staker", effectiveBalance3.Unlocking.StakerType);
        Assert.Equal("0", effectiveBalance3.Unlocking.RewardPoints);
        
        // Verify fourth effective balance
        var effectiveBalance4 = effectiveBalances[3];
        Assert.Equal("0xE04CC55ebEE1cBCE552f250e85c57B70B2E2625b", effectiveBalance4.Account);
        Assert.Equal(49, effectiveBalance4.StartBlock);
        Assert.Equal(49, effectiveBalance4.EndBlock);
        Assert.Equal(1, effectiveBalance4.EffectiveBlocks);
        Assert.Equal(0, effectiveBalance4.VtxDistributionId);
        Assert.Equal(0.0011111, effectiveBalance4.Percentage);
        // Bonded
        Assert.Equal("1300", effectiveBalance4.Bonded.Balance);
        Assert.Equal("1200", effectiveBalance4.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", effectiveBalance4.Bonded.Rate.ToString());
        Assert.Equal("Staker", effectiveBalance4.Bonded.StakerType);
        Assert.Equal("0", effectiveBalance4.Bonded.RewardPoints);
        // Unlocking
        Assert.Equal("700", effectiveBalance4.Unlocking.Balance);
        Assert.Equal("700", effectiveBalance4.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", effectiveBalance4.Unlocking.Rate.ToString());
        Assert.Equal("Staker", effectiveBalance4.Unlocking.StakerType);
        Assert.Equal("0", effectiveBalance4.Unlocking.RewardPoints);
        
        // Verify fifth effective balance
        var effectiveBalance5 = effectiveBalances[4];
        Assert.Equal("0xE04CC55ebEE1cBCE552f250e85c57B70B2E2625b", effectiveBalance5.Account);
        Assert.Equal(50, effectiveBalance5.StartBlock);
        Assert.Equal(899, effectiveBalance5.EndBlock);
        Assert.Equal(850, effectiveBalance5.EffectiveBlocks);
        Assert.Equal(0, effectiveBalance5.VtxDistributionId);
        Assert.Equal(0.9444444, effectiveBalance5.Percentage);
        // Bonded
        Assert.Equal("1200", effectiveBalance5.Bonded.Balance);
        Assert.Equal("1200", effectiveBalance5.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", effectiveBalance5.Bonded.Rate.ToString());
        Assert.Equal("Staker", effectiveBalance5.Bonded.StakerType);
        Assert.Equal("27", effectiveBalance5.Bonded.RewardPoints);
        // Unlocking
        Assert.Equal("1000", effectiveBalance5.Unlocking.Balance);
        Assert.Equal("1000", effectiveBalance5.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", effectiveBalance5.Unlocking.Rate.ToString());
        Assert.Equal("Staker", effectiveBalance5.Unlocking.StakerType);
        Assert.Equal("23", effectiveBalance5.Unlocking.RewardPoints);
        
    }
}