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

namespace MatrixEngine.Core.IntegrationTest.Tests.case_10_eb_carryover_2;

[Order(10)]
public class TestCase10a : TestBed<IntegrationTestFixture>
{
    public TestCase10a(ITestOutputHelper testOutputHelper, IntegrationTestFixture fixture) : base(testOutputHelper,
        fixture)
    {
    }

    [Fact]
    public async Task Test_Scenario_1()
    {
        // This test case will test whether an accounts effective balance is correctly calculated if the reward cycle
        // starts after their transaction occured. 
        // In this case. the user has 3 transactions before block 900, and the reward cycle starts at block 900.

        // Arrange
        var dataLoader = _fixture.GetService<IDataLoader>(_testOutputHelper);
        await dataLoader?.LoadCase("case-10a")!;

        var engineCore = _fixture.GetService<IEngineCore>(_testOutputHelper);

        // Act
        await engineCore?.Start()!;
        
        var database = _fixture.GetService<IMongoDatabase>(_testOutputHelper);
        var collection = database.GetCollection<SignedEffectiveBalanceModel>(DbCollectionName.SignEffectiveBalance);
        var signedBalances = await collection.Find(_ => true).ToListAsync();
        Assert.Equal(2, signedBalances.Count);
        
        // First Signed balance is for the Genesis Cycle, which will have a vtxDistributionId of -1
        var balance1 = signedBalances[0];
        Assert.Equal("0xE566475DE82dd261cA0b2a8625bD8a3D822A7546", balance1.Account);
        Assert.Equal(-1, balance1.VtxDistributionId);
        Assert.Equal("615544", balance1.TotalRewardPoints);
        Assert.Equal(0, balance1.StartBlock);
        Assert.Equal(899, balance1.EndBlock);
        Assert.False(balance1.Submitted);
        
        // Second Signed balance is for the first official Cycle, which will have a vtxDistributionId of 0
        var balance2 = signedBalances[1];
        Assert.Equal("0xE566475DE82dd261cA0b2a8625bD8a3D822A7546", balance2.Account);
        Assert.Equal(0, balance2.VtxDistributionId);
        Assert.Equal("1722000", balance2.TotalRewardPoints);
        Assert.Equal(900, balance2.StartBlock);
        Assert.Equal(1799, balance2.EndBlock);
        Assert.False(balance2.Submitted);
        
        // Verify the Reward Cycles were correctly created and added to the DB
        var rewardCyclesCollection = database.GetCollection<RewardCycleModel>(DbCollectionName.RewardCycle);
        var rewardCycles = await rewardCyclesCollection.Find(_ => true)
            .Sort(Builders<RewardCycleModel>.Sort.Ascending(x => x.StartBlock))
            .ToListAsync();
        
        // Should have 3 reward cycles, genesis, first official complete reward cycle, and the next unfinished one
        Assert.Equal(3, rewardCycles.Count);
        
        // Verify Genesis Cycle (0-899)
        var genesisCycle = rewardCycles[0];
        Assert.Equal(0, genesisCycle.StartBlock);
        Assert.Equal(899, genesisCycle.EndBlock);
        Assert.Equal(0, genesisCycle.StartEraIndex);
        Assert.Equal(89, genesisCycle.EndEraIndex);
        Assert.Equal(-1, genesisCycle.VtxDistributionId);
        Assert.True(genesisCycle.CalculationComplete);
        
        // Verify First Official Cycle (900-1799)
        var firstCycle = rewardCycles[1];
        Assert.Equal(900, firstCycle.StartBlock);
        Assert.Equal(1799, firstCycle.EndBlock);
        Assert.Equal(90, firstCycle.StartEraIndex);
        Assert.Equal(179, firstCycle.EndEraIndex);
        Assert.Equal(0, firstCycle.VtxDistributionId);
        Assert.True(firstCycle.CalculationComplete);
        
        // Verify Second cycle (1800-infinity and beyond)
        var secondCycle = rewardCycles[2];
        Assert.Equal(1800, secondCycle.StartBlock);
        Assert.Equal(-1, secondCycle.EndBlock); // Not set yet
        Assert.Equal(180, secondCycle.StartEraIndex);
        Assert.Equal(-1, secondCycle.EndEraIndex); // Not set yet
        Assert.Equal(1, secondCycle.VtxDistributionId);
        Assert.False(secondCycle.CalculationComplete);
        
        var effectiveBalances = database.GetCollection<EffectiveBalanceModel>(DbCollectionName.EffectiveBalance);
        var effectiveBalance = await effectiveBalances.Find(_ => true)
            .Sort(Builders<EffectiveBalanceModel>.Sort.Ascending(x => x.StartBlock))
            .ToListAsync();
        
        // Verify the effective balance is as expected
        // It should have the effective balance for the Genesis Cycle, and the first official cycle.
        Assert.Equal(5, effectiveBalance.Count);
        
        // Account 0xE566... (450-474)
        var entry1 = effectiveBalance[0];
        Assert.Equal("0xE566475DE82dd261cA0b2a8625bD8a3D822A7546", entry1.Account);
        Assert.Equal(450, entry1.StartBlock);
        Assert.Equal(474, entry1.EndBlock);
        Assert.Equal(25, entry1.EffectiveBlocks);
        Assert.Equal(-1, entry1.VtxDistributionId);
        Assert.Equal(0.0277778, entry1.Percentage);
        // Bonded properties
        Assert.Equal("100000000", entry1.Bonded.Balance);
        Assert.Equal("50000000", entry1.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", entry1.Bonded.Rate.ToString());
        Assert.Equal("Staker", entry1.Bonded.StakerType);
        Assert.Equal("34166", entry1.Bonded.RewardPoints);
        // Unlocking properties
        Assert.Equal("0", entry1.Unlocking.Balance);
        Assert.Equal("0", entry1.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry1.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry1.Unlocking.StakerType);
        Assert.Equal("0", entry1.Unlocking.RewardPoints);
        
        // Account 0xE566... (475-499)
        var entry2 = effectiveBalance[1];
        Assert.Equal("0xE566475DE82dd261cA0b2a8625bD8a3D822A7546", entry2.Account);
        Assert.Equal(475, entry2.StartBlock);
        Assert.Equal(499, entry2.EndBlock);
        Assert.Equal(25, entry2.EffectiveBlocks);
        Assert.Equal(-1, entry2.VtxDistributionId);
        Assert.Equal(0.0277778, entry2.Percentage);
        // Bonded properties
        Assert.Equal("50000000", entry2.Bonded.Balance);
        Assert.Equal("50000000", entry2.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", entry2.Bonded.Rate.ToString());
        Assert.Equal("Staker", entry2.Bonded.StakerType);
        Assert.Equal("34166", entry2.Bonded.RewardPoints);
        // Unlocking properties
        Assert.Equal("50000000", entry2.Unlocking.Balance);
        Assert.Equal("0", entry2.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry2.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry2.Unlocking.StakerType);
        Assert.Equal("0", entry2.Unlocking.RewardPoints);
        
        // Account 0xE566... (500-898)
        var entry3 = effectiveBalance[2];
        Assert.Equal("0xE566475DE82dd261cA0b2a8625bD8a3D822A7546", entry3.Account);
        Assert.Equal(500, entry3.StartBlock);
        Assert.Equal(898, entry3.EndBlock);
        Assert.Equal(399, entry3.EffectiveBlocks);
        Assert.Equal(-1, entry3.VtxDistributionId);
        Assert.Equal(0.4433333, entry3.Percentage);
        // Bonded properties
        Assert.Equal("50000000", entry3.Bonded.Balance);
        Assert.Equal("50000000", entry3.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", entry3.Bonded.Rate.ToString());
        Assert.Equal("Staker", entry3.Bonded.StakerType);
        Assert.Equal("545299", entry3.Bonded.RewardPoints);
        // Unlocking properties
        Assert.Equal("0", entry3.Unlocking.Balance);
        Assert.Equal("0", entry3.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry3.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry3.Unlocking.StakerType);
        Assert.Equal("0", entry3.Unlocking.RewardPoints);
        
        // Account 0xE566... (899-899)
        var entry4 = effectiveBalance[3];
        Assert.Equal("0xE566475DE82dd261cA0b2a8625bD8a3D822A7546", entry4.Account);
        Assert.Equal(899, entry4.StartBlock);
        Assert.Equal(899, entry4.EndBlock);
        Assert.Equal(1, entry4.EffectiveBlocks);
        Assert.Equal(-1, entry4.VtxDistributionId);
        Assert.Equal(0.0011111, entry4.Percentage);
        // Bonded properties
        Assert.Equal("70000000", entry4.Bonded.Balance);
        Assert.Equal("70000000", entry4.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", entry4.Bonded.Rate.ToString());
        Assert.Equal("Staker", entry4.Bonded.StakerType);
        Assert.Equal("1913", entry4.Bonded.RewardPoints);
        // Unlocking properties
        Assert.Equal("0", entry4.Unlocking.Balance);
        Assert.Equal("0", entry4.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry4.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry4.Unlocking.StakerType);
        Assert.Equal("0", entry4.Unlocking.RewardPoints);
        
        // Account 0xE566... (900-1799)
        // This is the first official cycle, so the vtxDistributionId is 0
        var entry5 = effectiveBalance[4];
        Assert.Equal("0xE566475DE82dd261cA0b2a8625bD8a3D822A7546", entry5.Account);
        Assert.Equal(900, entry5.StartBlock);
        Assert.Equal(1799, entry5.EndBlock);
        Assert.Equal(900, entry5.EffectiveBlocks);
        Assert.Equal(0, entry5.VtxDistributionId);
        Assert.Equal(1.0, entry5.Percentage);
        // Bonded properties
        Assert.Equal("70000000", entry5.Bonded.Balance);
        Assert.Equal("70000000", entry5.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", entry5.Bonded.Rate.ToString());
        Assert.Equal("Staker", entry5.Bonded.StakerType);
        Assert.Equal("1722000", entry5.Bonded.RewardPoints);
        // Unlocking properties
        Assert.Equal("0", entry5.Unlocking.Balance);
        Assert.Equal("0", entry5.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry5.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry5.Unlocking.StakerType);
        Assert.Equal("0", entry5.Unlocking.RewardPoints);
        
        // Sum all reward points for each signed balance and verify
        var totalRewardPoints1 = effectiveBalance
            .Where(x => x.VtxDistributionId == -1)
            .Aggregate(BigInteger.Zero, (sum, x) => sum + BigInteger.Parse(x.TotalRewardPoints));
        Assert.Equal(balance1.TotalRewardPoints, totalRewardPoints1.ToString());
        
        var totalRewardPoints2 = effectiveBalance
            .Where(x => x.VtxDistributionId == 0)
            .Aggregate(BigInteger.Zero, (sum, x) => sum + BigInteger.Parse(x.TotalRewardPoints));
        Assert.Equal(balance2.TotalRewardPoints, totalRewardPoints2.ToString());
    }
} 