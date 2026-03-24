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

namespace MatrixEngine.Core.IntegrationTest.Tests.case_13_multiple_stakers;

[Order(3)]
public class TestCase14 : TestBed<IntegrationTestFixture>
{
    public TestCase14(ITestOutputHelper testOutputHelper, IntegrationTestFixture fixture) : base(testOutputHelper,
        fixture)
    {
    }

    [Fact]
    public async Task Test_Scenario_1()
    {
        // This test case will test rebonded events vs bonded events. Rebonded should reduce the unlocking balance
        // and increase the bonded balance without affecting the total

        var dataLoader = _fixture.GetService<IDataLoader>(_testOutputHelper);
        await dataLoader?.LoadCase("case-14")!;

        var engineCore = _fixture.GetService<IEngineCore>(_testOutputHelper);
        var signEffectiveBalanceService = _fixture.GetService<ISignEffectiveBalanceService>(_testOutputHelper);

        // Act
        await engineCore?.Start()!;

        // Get all signed effective balances
        var database = _fixture.GetService<IMongoDatabase>(_testOutputHelper);
        var collection = database.GetCollection<SignedEffectiveBalanceModel>(DbCollectionName.SignEffectiveBalance);
        var signedBalances = await collection.Find(_ => true)
            .Sort(Builders<SignedEffectiveBalanceModel>.Sort.Ascending(x => x.StartBlock).Ascending(x => x.VtxDistributionId))
            .ToListAsync();
        Assert.Equal(2, signedBalances.Count);
        
        // Verify first signed balance
        var signedBalance1 = signedBalances[0];
        Assert.Equal("0xE04CC55ebEE1cBCE552f250e85c57B70B2E2625b", signedBalance1.Account);
        Assert.Equal(0, signedBalance1.StartBlock);
        Assert.Equal(899, signedBalance1.EndBlock);
        Assert.Equal(0, signedBalance1.VtxDistributionId);
        Assert.Equal("193", signedBalance1.TotalRewardPoints);
        Assert.False(signedBalance1.Submitted);
        Assert.False(signedBalance1.Verified);
        
        // Verify second signed balance
        var signedBalance2 = signedBalances[1];
        Assert.Equal("0xE04CC55ebEE1cBCE552f250e85c57B70B2E2625b", signedBalance2.Account);
        Assert.Equal(900, signedBalance2.StartBlock);
        Assert.Equal(1799, signedBalance2.EndBlock);
        Assert.Equal(1, signedBalance2.VtxDistributionId);
        Assert.Equal("196", signedBalance2.TotalRewardPoints);
        Assert.False(signedBalance2.Submitted);
        Assert.False(signedBalance2.Verified);
        
        // Now verify the effective balances
        var effectiveBalances = database.GetCollection<EffectiveBalanceModel>(DbCollectionName.EffectiveBalance);
        var effectiveBalance = await effectiveBalances.Find(_ => true)
            .Sort(Builders<EffectiveBalanceModel>.Sort.Ascending(x => x.StartBlock))
            .ToListAsync();
        
        // Verify the effective balance is as expected
        Assert.Equal(5, effectiveBalance.Count);
        
        // Account 0xE04CC55... (0-9)
        // Original bonded event
        var entry1 = effectiveBalance[0];
        Assert.Equal("0xE04CC55ebEE1cBCE552f250e85c57B70B2E2625b", entry1.Account);
        Assert.Equal(0, entry1.StartBlock);
        Assert.Equal(9, entry1.EndBlock);
        Assert.Equal(10, entry1.EffectiveBlocks);
        Assert.Equal(0, entry1.VtxDistributionId);
        Assert.Equal(0.0111111, entry1.Percentage);
        // Bonded
        Assert.Equal("10000", entry1.Bonded.Balance);
        Assert.Equal("5000", entry1.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", entry1.Bonded.Rate.ToString());
        Assert.Equal("Staker", entry1.Bonded.StakerType);
        Assert.Equal("1", entry1.Bonded.RewardPoints);
        // Unlocking
        Assert.Equal("0", entry1.Unlocking.Balance);
        Assert.Equal("0", entry1.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry1.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry1.Unlocking.StakerType);
        Assert.Equal("0", entry1.Unlocking.RewardPoints);
        
        // Account 0xE04CC55... (10-19)
        // Unbonded event
        var entry2 = effectiveBalance[1];
        Assert.Equal("0xE04CC55ebEE1cBCE552f250e85c57B70B2E2625b", entry2.Account);
        Assert.Equal(10, entry2.StartBlock);
        Assert.Equal(19, entry2.EndBlock);
        Assert.Equal(10, entry2.EffectiveBlocks);
        Assert.Equal(0, entry2.VtxDistributionId);
        Assert.Equal(0.0111111, entry2.Percentage);
        // Bonded
        Assert.Equal("5000", entry2.Bonded.Balance);
        Assert.Equal("5000", entry2.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", entry2.Bonded.Rate.ToString());
        Assert.Equal("Staker", entry2.Bonded.StakerType);
        Assert.Equal("1", entry2.Bonded.RewardPoints);
        // Unlocking
        Assert.Equal("5000", entry2.Unlocking.Balance);
        Assert.Equal("0", entry2.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry2.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry2.Unlocking.StakerType);
        Assert.Equal("0", entry2.Unlocking.RewardPoints);
        
        // Account 0xE04CC55... (20-29) 
        // Withdrawn event
        var entry3 = effectiveBalance[2];
        Assert.Equal("0xE04CC55ebEE1cBCE552f250e85c57B70B2E2625b", entry3.Account);
        Assert.Equal(20, entry3.StartBlock);
        Assert.Equal(29, entry3.EndBlock);
        Assert.Equal(10, entry3.EffectiveBlocks);
        Assert.Equal(0, entry3.VtxDistributionId);
        Assert.Equal(0.0111111, entry3.Percentage);
        // Bonded
        Assert.Equal("5000", entry3.Bonded.Balance);
        Assert.Equal("5000", entry3.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", entry3.Bonded.Rate.ToString());
        Assert.Equal("Staker", entry3.Bonded.StakerType);
        Assert.Equal("1", entry3.Bonded.RewardPoints);
        // Unlocking - Shows decreased unlocking balance due to withdraw
        Assert.Equal("3000", entry3.Unlocking.Balance);
        Assert.Equal("0", entry3.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry3.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry3.Unlocking.StakerType);
        Assert.Equal("0", entry3.Unlocking.RewardPoints);
        
        // Account 0xE04CC55... (30-899)
        // Rebond event, takes out of unlocking and increases bonded
        var entry4 = effectiveBalance[3];
        Assert.Equal("0xE04CC55ebEE1cBCE552f250e85c57B70B2E2625b", entry4.Account);
        Assert.Equal(30, entry4.StartBlock);
        Assert.Equal(899, entry4.EndBlock);
        Assert.Equal(870, entry4.EffectiveBlocks);
        Assert.Equal(0, entry4.VtxDistributionId);
        Assert.Equal(0.9666667, entry4.Percentage);
        // Bonded
        Assert.Equal("8000", entry4.Bonded.Balance);
        Assert.Equal("8000", entry4.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", entry4.Bonded.Rate.ToString());
        Assert.Equal("Staker", entry4.Bonded.StakerType);
        Assert.Equal("190", entry4.Bonded.RewardPoints);
        // Unlocking
        Assert.Equal("0", entry4.Unlocking.Balance);
        Assert.Equal("0", entry4.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry4.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry4.Unlocking.StakerType);
        Assert.Equal("0", entry4.Unlocking.RewardPoints);
        
        // Account 0xE04CC55... (900-1799)
        // Carry over to next cycle
        var entry5 = effectiveBalance[4];
        Assert.Equal("0xE04CC55ebEE1cBCE552f250e85c57B70B2E2625b", entry5.Account);
        Assert.Equal(900, entry5.StartBlock);
        Assert.Equal(1799, entry5.EndBlock);
        Assert.Equal(900, entry5.EffectiveBlocks);
        Assert.Equal(1, entry5.VtxDistributionId);
        Assert.Equal(1.0, entry5.Percentage);
        // Bonded
        Assert.Equal("8000", entry5.Bonded.Balance);
        Assert.Equal("8000", entry5.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", entry5.Bonded.Rate.ToString());
        Assert.Equal("Staker", entry5.Bonded.StakerType);
        Assert.Equal("196", entry5.Bonded.RewardPoints);
        // Unlocking
        Assert.Equal("0", entry5.Unlocking.Balance);
        Assert.Equal("0", entry5.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry5.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry5.Unlocking.StakerType);
        Assert.Equal("0", entry5.Unlocking.RewardPoints);
    }
}