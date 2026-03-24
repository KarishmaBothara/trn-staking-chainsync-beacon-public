using MatrixEngine.Core.Engine;
using MatrixEngine.Core.IntegrationTest.Fixtures;
using Xunit.Abstractions;
using Xunit.Extensions.Ordering;
using Xunit.Microsoft.DependencyInjection.Abstracts;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using MatrixEngine.Core.Services;
using MatrixEngine.Core.Models;
using MatrixEngine.Core.Constants;
using System.Numerics;
using System.Linq;

namespace MatrixEngine.Core.IntegrationTest.Tests.case_10_eb_carryover_3;

[Order(10)]
public class TestCase10b : TestBed<IntegrationTestFixture>
{
    public TestCase10b(ITestOutputHelper testOutputHelper, IntegrationTestFixture fixture) : base(testOutputHelper,
        fixture)
    {
    }

    [Fact]
    public async Task Test_Scenario_1()
    {
        // This test case will test whether an accounts effective balance is correctly calculated if the reward cycle
        // starts after their transaction occured. Part 2,
        // This time we will test an unfinished reward cycle passed in, with enough eras to complete this cycle and one more
        // it should create 3 new reward cycles after it completes this one, and properly load all into the DB
        // The eras go up to 285

        // Arrange
        var dataLoader = _fixture.GetService<IDataLoader>(_testOutputHelper);
        await dataLoader?.LoadCase("case-10b")!;

        var engineCore = _fixture.GetService<IEngineCore>(_testOutputHelper);

        // Act
        await engineCore?.Start()!;
        
        var database = _fixture.GetService<IMongoDatabase>(_testOutputHelper);
        var collection = database.GetCollection<SignedEffectiveBalanceModel>(DbCollectionName.SignEffectiveBalance);
        var signedBalances = await collection.Find(_ => true).ToListAsync();
        Assert.Equal(3, signedBalances.Count);
        
        // First Signed balance is for the Genesis Cycle, which will have a vtxDistributionId of -1
        var balance1 = signedBalances[0];
        Assert.Equal("0xE566475DE82dd261cA0b2a8625bD8a3D822A7546", balance1.Account);
        Assert.Equal(-1, balance1.VtxDistributionId);
        Assert.Equal("2460000", balance1.TotalRewardPoints);
        Assert.Equal(0, balance1.StartBlock);
        Assert.Equal(999, balance1.EndBlock);
        Assert.False(balance1.Submitted);
        
        // Second Signed balance is for the first official Cycle, which will have a vtxDistributionId of 0
        var balance2 = signedBalances[1];
        Assert.Equal("0xE566475DE82dd261cA0b2a8625bD8a3D822A7546", balance2.Account);
        Assert.Equal(0, balance2.VtxDistributionId);
        Assert.Equal("2460000", balance2.TotalRewardPoints);
        Assert.Equal(1000, balance2.StartBlock);
        Assert.Equal(1899, balance2.EndBlock);
        Assert.False(balance2.Submitted);
        
        // Third Signed balance is for the second official Cycle, which will have a vtxDistributionId of 1
        var balance3 = signedBalances[2];
        Assert.Equal("0xE566475DE82dd261cA0b2a8625bD8a3D822A7546", balance3.Account);
        Assert.Equal(1, balance3.VtxDistributionId);
        Assert.Equal("2460000", balance3.TotalRewardPoints);
        Assert.Equal(1900, balance3.StartBlock);
        Assert.Equal(2799, balance3.EndBlock);
        Assert.False(balance3.Submitted);
        
        // Verify the Reward Cycles were correctly created and added to the DB
        var rewardCyclesCollection = database.GetCollection<RewardCycleModel>(DbCollectionName.RewardCycle);
        var rewardCycles = await rewardCyclesCollection.Find(_ => true)
            .Sort(Builders<RewardCycleModel>.Sort.Ascending(x => x.StartBlock))
            .ToListAsync();
        
        // Should have 4 reward cycles: genesis, first and second official complete cycles, and the next unfinished one
        Assert.Equal(4, rewardCycles.Count);
        
        // Verify Genesis Cycle (0-999)
        var genesisCycle = rewardCycles[0];
        Assert.Equal(0, genesisCycle.StartBlock);
        Assert.Equal(999, genesisCycle.EndBlock);
        Assert.Equal(0, genesisCycle.StartEraIndex);
        Assert.Equal(99, genesisCycle.EndEraIndex);
        Assert.Equal(99, genesisCycle.CurrentEraIndex);
        Assert.Equal(-1, genesisCycle.VtxDistributionId);
        Assert.True(genesisCycle.CalculationComplete);
        
        // Verify First Official Cycle (1000-1899)
        var firstCycle = rewardCycles[1];
        Assert.Equal(1000, firstCycle.StartBlock);
        Assert.Equal(1899, firstCycle.EndBlock);
        Assert.Equal(100, firstCycle.StartEraIndex);
        Assert.Equal(189, firstCycle.EndEraIndex);
        Assert.Equal(189, firstCycle.CurrentEraIndex);
        Assert.Equal(0, firstCycle.VtxDistributionId);
        Assert.True(firstCycle.CalculationComplete);
        
        // Verify Second cycle (1900-2799)
        var secondCycle = rewardCycles[2];
        Assert.Equal(1900, secondCycle.StartBlock);
        Assert.Equal(2799, secondCycle.EndBlock);
        Assert.Equal(190, secondCycle.StartEraIndex);
        Assert.Equal(279, secondCycle.EndEraIndex);
        Assert.Equal(279, secondCycle.CurrentEraIndex);
        Assert.Equal(1, secondCycle.VtxDistributionId);
        Assert.True(secondCycle.CalculationComplete);
        
        // Verify Third cycle (2800-onwards) - unfinished
        var thirdCycle = rewardCycles[3];
        Assert.Equal(2800, thirdCycle.StartBlock);
        Assert.Equal(-1, thirdCycle.EndBlock); // Not set yet for unfinished cycle
        Assert.Equal(280, thirdCycle.StartEraIndex);
        Assert.Equal(-1, thirdCycle.EndEraIndex); // Not set yet for unfinished cycle
        Assert.Equal(365, thirdCycle.CurrentEraIndex); // Set to last completed era index
        Assert.Equal(2, thirdCycle.VtxDistributionId);
        Assert.False(thirdCycle.CalculationComplete);
        
        var effectiveBalances = database.GetCollection<EffectiveBalanceModel>(DbCollectionName.EffectiveBalance);
        var effectiveBalance = await effectiveBalances.Find(_ => true)
            .Sort(Builders<EffectiveBalanceModel>.Sort.Ascending(x => x.StartBlock))
            .ToListAsync();
        
        // Verify the effective balance is as expected
        Assert.Equal(3, effectiveBalance.Count);
        
        // Account 0xE566... (0-999) - Genesis cycle
        var entry1 = effectiveBalance[0];
        Assert.Equal("0xE566475DE82dd261cA0b2a8625bD8a3D822A7546", entry1.Account);
        Assert.Equal(0, entry1.StartBlock);
        Assert.Equal(999, entry1.EndBlock);
        Assert.Equal(1000, entry1.EffectiveBlocks);
        Assert.Equal(-1, entry1.VtxDistributionId);
        Assert.Equal(1.0, entry1.Percentage);
        // Bonded properties
        Assert.Equal("100000000", entry1.Bonded.Balance);
        Assert.Equal("100000000", entry1.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", entry1.Bonded.Rate.ToString());
        Assert.Equal("Staker", entry1.Bonded.StakerType);
        Assert.Equal("2460000", entry1.Bonded.RewardPoints);
        // Unlocking properties
        Assert.Equal("0", entry1.Unlocking.Balance);
        Assert.Equal("0", entry1.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry1.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry1.Unlocking.StakerType);
        Assert.Equal("0", entry1.Unlocking.RewardPoints);
        
        // Account 0xE566... (1000-1899) - First official cycle
        var entry2 = effectiveBalance[1];
        Assert.Equal("0xE566475DE82dd261cA0b2a8625bD8a3D822A7546", entry2.Account);
        Assert.Equal(1000, entry2.StartBlock);
        Assert.Equal(1899, entry2.EndBlock);
        Assert.Equal(900, entry2.EffectiveBlocks);
        Assert.Equal(0, entry2.VtxDistributionId);
        Assert.Equal(1.0, entry2.Percentage);
        // Bonded properties
        Assert.Equal("100000000", entry2.Bonded.Balance);
        Assert.Equal("100000000", entry2.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", entry2.Bonded.Rate.ToString());
        Assert.Equal("Staker", entry2.Bonded.StakerType);
        Assert.Equal("2460000", entry2.Bonded.RewardPoints);
        // Unlocking properties
        Assert.Equal("0", entry2.Unlocking.Balance);
        Assert.Equal("0", entry2.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry2.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry2.Unlocking.StakerType);
        Assert.Equal("0", entry2.Unlocking.RewardPoints);
        
        // Account 0xE566... (1900-2799) - Second official cycle
        var entry3 = effectiveBalance[2];
        Assert.Equal("0xE566475DE82dd261cA0b2a8625bD8a3D822A7546", entry3.Account);
        Assert.Equal(1900, entry3.StartBlock);
        Assert.Equal(2799, entry3.EndBlock);
        Assert.Equal(900, entry3.EffectiveBlocks);
        Assert.Equal(1, entry3.VtxDistributionId);
        Assert.Equal(1.0, entry3.Percentage);
        // Bonded properties
        Assert.Equal("100000000", entry3.Bonded.Balance);
        Assert.Equal("100000000", entry3.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", entry3.Bonded.Rate.ToString());
        Assert.Equal("Staker", entry3.Bonded.StakerType);
        Assert.Equal("2460000", entry3.Bonded.RewardPoints);
        // Unlocking properties
        Assert.Equal("0", entry3.Unlocking.Balance);
        Assert.Equal("0", entry3.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry3.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry3.Unlocking.StakerType);
        Assert.Equal("0", entry3.Unlocking.RewardPoints);
        
        // Sum all reward points for each signed balance and verify
        var totalRewardPoints1 = effectiveBalance
            .Where(x => x.VtxDistributionId == -1)
            .Aggregate(BigInteger.Zero, (sum, x) => sum + BigInteger.Parse(x.TotalRewardPoints));
        Assert.Equal(balance1.TotalRewardPoints, totalRewardPoints1.ToString());
        
        var totalRewardPoints2 = effectiveBalance
            .Where(x => x.VtxDistributionId == 0)
            .Aggregate(BigInteger.Zero, (sum, x) => sum + BigInteger.Parse(x.TotalRewardPoints));
        Assert.Equal(balance2.TotalRewardPoints, totalRewardPoints2.ToString());
        
        var totalRewardPoints3 = effectiveBalance
            .Where(x => x.VtxDistributionId == 1)
            .Aggregate(BigInteger.Zero, (sum, x) => sum + BigInteger.Parse(x.TotalRewardPoints));
        Assert.Equal(balance3.TotalRewardPoints, totalRewardPoints3.ToString());
    }
} 