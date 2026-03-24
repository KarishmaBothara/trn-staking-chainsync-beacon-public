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
public class TestCase13a : TestBed<IntegrationTestFixture>
{
    public TestCase13a(ITestOutputHelper testOutputHelper, IntegrationTestFixture fixture) : base(testOutputHelper,
        fixture)
    {
    }

    [Fact]
    public async Task Test_Scenario_1()
    {
        // This test scenario will test multiple accounts with various nominator rates

        var dataLoader = _fixture.GetService<IDataLoader>(_testOutputHelper);
        await dataLoader?.LoadCase("case-13a")!;

        var engineCore = _fixture.GetService<IEngineCore>(_testOutputHelper);
        var signEffectiveBalanceService = _fixture.GetService<ISignEffectiveBalanceService>(_testOutputHelper);

        // Act
        await engineCore?.Start()!;

        // Get all signed effective balances
        var database = _fixture.GetService<IMongoDatabase>(_testOutputHelper);
        var collection = database.GetCollection<SignedEffectiveBalanceModel>(DbCollectionName.SignEffectiveBalance);
        var signedBalances = await collection.Find(_ => true)
            .Sort(Builders<SignedEffectiveBalanceModel>.Sort.Ascending(x => x.StartBlock).Ascending(x => x.Account))
            .ToListAsync();
        Assert.Equal(5, signedBalances.Count);
        
        // Verify first signed balance
        var signedBalance1 = signedBalances[0];
        Assert.Equal("0x25451A4de12dcCc2D166922fA938E900fCc4ED24", signedBalance1.Account);
        Assert.Equal(0, signedBalance1.StartBlock);
        Assert.Equal(899, signedBalance1.EndBlock);
        Assert.Equal(-1, signedBalance1.VtxDistributionId);
        Assert.Equal("4914532", signedBalance1.TotalRewardPoints);
        Assert.False(signedBalance1.Submitted);
        Assert.False(signedBalance1.Verified);
        
        // Verify second signed balance
        var signedBalance2 = signedBalances[1];
        Assert.Equal("0xE04CC55ebEE1cBCE552f250e85c57B70B2E2625b", signedBalance2.Account);
        Assert.Equal(0, signedBalance2.StartBlock);
        Assert.Equal(899, signedBalance2.EndBlock);
        Assert.Equal(-1, signedBalance2.VtxDistributionId);
        Assert.Equal("4920000", signedBalance2.TotalRewardPoints);
        Assert.False(signedBalance2.Submitted);
        Assert.False(signedBalance2.Verified);
        
        // Verify third signed balance
        var signedBalance5 = signedBalances[2];
        Assert.Equal("0x25451A4de12dcCc2D166922fA938E900fCc4ED24", signedBalance5.Account);
        Assert.Equal(900, signedBalance5.StartBlock);
        Assert.Equal(1799, signedBalance5.EndBlock);
        Assert.Equal(0, signedBalance5.VtxDistributionId);
        Assert.Equal("2459998", signedBalance5.TotalRewardPoints);
        Assert.False(signedBalance5.Submitted);
        Assert.False(signedBalance5.Verified);
        
        // Verify fourth signed balance
        var signedBalance3 = signedBalances[3];
        Assert.Equal("0xE04CC55ebEE1cBCE552f250e85c57B70B2E2625b", signedBalance3.Account);
        Assert.Equal(900, signedBalance3.StartBlock);
        Assert.Equal(1799, signedBalance3.EndBlock);
        Assert.Equal(0, signedBalance3.VtxDistributionId);
        Assert.Equal("241080000", signedBalance3.TotalRewardPoints);
        Assert.False(signedBalance3.Submitted);
        Assert.False(signedBalance3.Verified);
        
        // Verify fifth signed balance
        var signedBalance4 = signedBalances[4];
        Assert.Equal("0xf24FF3a9CF04c71Dbc94D0b566f7A27B94566cac", signedBalance4.Account);
        Assert.Equal(900, signedBalance4.StartBlock);
        Assert.Equal(1799, signedBalance4.EndBlock);
        Assert.Equal(0, signedBalance4.VtxDistributionId);
        Assert.Equal("1279200000", signedBalance4.TotalRewardPoints);
        Assert.False(signedBalance4.Submitted);
        Assert.False(signedBalance4.Verified);
        
        // Now verify the effective balances
        var effectiveBalances = database.GetCollection<EffectiveBalanceModel>(DbCollectionName.EffectiveBalance);
        var effectiveBalance = await effectiveBalances.Find(_ => true)
            .Sort(Builders<EffectiveBalanceModel>.Sort.Ascending(x => x.StartBlock))
            .ToListAsync();
        
        // There should be 6 effective balance entries from the JSON data
        Assert.Equal(6, effectiveBalance.Count);
        
        // Let's check the entries for the first cycle (VtxDistributionId = -1)
        var firstCycleEntries = effectiveBalance.Where(x => x.VtxDistributionId == -1).ToList();
        Assert.Equal(2, firstCycleEntries.Count);
        
        // First account in first cycle (0xE04CC55... 0-899)
        var entry1 = firstCycleEntries.First(x => x.Account == "0xE04CC55ebEE1cBCE552f250e85c57B70B2E2625b");
        // General properties
        Assert.Equal("0xE04CC55ebEE1cBCE552f250e85c57B70B2E2625b", entry1.Account);
        Assert.Equal(0, entry1.StartBlock);
        Assert.Equal(899, entry1.EndBlock);
        Assert.Equal(900, entry1.EffectiveBlocks);
        Assert.Equal(-1, entry1.VtxDistributionId);
        Assert.Equal(1.0, entry1.Percentage);
        // Bonded properties
        Assert.Equal("100000000", entry1.Bonded.Balance);
        Assert.Equal("100000000", entry1.Bonded.EffectiveBalance);
        Assert.Equal("0.0492", entry1.Bonded.Rate.ToString());
        Assert.Equal("Nominator", entry1.Bonded.StakerType);
        Assert.Equal("4920000", entry1.Bonded.RewardPoints);
        // Unlocking properties
        Assert.Equal("0", entry1.Unlocking.Balance);
        Assert.Equal("0", entry1.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry1.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry1.Unlocking.StakerType);
        Assert.Equal("0", entry1.Unlocking.RewardPoints);
        
        // Second account in first cycle (0x2545... 1-899)
        var entry2 = firstCycleEntries.First(x => x.Account == "0x25451A4de12dcCc2D166922fA938E900fCc4ED24");
        // General properties
        Assert.Equal("0x25451A4de12dcCc2D166922fA938E900fCc4ED24", entry2.Account);
        Assert.Equal(1, entry2.StartBlock);
        Assert.Equal(899, entry2.EndBlock);
        Assert.Equal(899, entry2.EffectiveBlocks);
        Assert.Equal(-1, entry2.VtxDistributionId);
        Assert.Equal(0.9988889, entry2.Percentage);
        // Bonded properties
        Assert.Equal("200000000", entry2.Bonded.Balance);
        Assert.Equal("200000000", entry2.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", entry2.Bonded.Rate.ToString());
        Assert.Equal("Staker", entry2.Bonded.StakerType);
        Assert.Equal("4914532", entry2.Bonded.RewardPoints);
        // Unlocking properties
        Assert.Equal("0", entry2.Unlocking.Balance);
        Assert.Equal("0", entry2.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry2.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry2.Unlocking.StakerType);
        Assert.Equal("0", entry2.Unlocking.RewardPoints);
        
        // Let's check the entries for the second cycle (VtxDistributionId = 0)
        var secondCycleEntries = effectiveBalance.Where(x => x.VtxDistributionId == 0).ToList();
        Assert.Equal(4, secondCycleEntries.Count);
        
        // First account in second cycle (0xE04CC... 900-1799)
        var entry3 = secondCycleEntries.First(x => x.Account == "0xE04CC55ebEE1cBCE552f250e85c57B70B2E2625b");
        // General properties
        Assert.Equal("0xE04CC55ebEE1cBCE552f250e85c57B70B2E2625b", entry3.Account);
        Assert.Equal(900, entry3.StartBlock);
        Assert.Equal(1799, entry3.EndBlock);
        Assert.Equal(900, entry3.EffectiveBlocks);
        Assert.Equal(0, entry3.VtxDistributionId);
        Assert.Equal(1.0, entry3.Percentage);
        // Bonded properties
        Assert.Equal("4900000000", entry3.Bonded.Balance);
        Assert.Equal("4900000000", entry3.Bonded.EffectiveBalance);
        Assert.Equal("0.0492", entry3.Bonded.Rate.ToString());
        Assert.Equal("Nominator", entry3.Bonded.StakerType);
        Assert.Equal("241080000", entry3.Bonded.RewardPoints);
        // Unlocking properties
        Assert.Equal("0", entry3.Unlocking.Balance);
        Assert.Equal("0", entry3.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry3.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry3.Unlocking.StakerType);
        Assert.Equal("0", entry3.Unlocking.RewardPoints);
        
        // Second account in second cycle, first entry (0x2545... 900-1499)
        var entry4 = secondCycleEntries.First(x => 
            x.Account == "0x25451A4de12dcCc2D166922fA938E900fCc4ED24" && 
            x.StartBlock == 900);
        // General properties
        Assert.Equal("0x25451A4de12dcCc2D166922fA938E900fCc4ED24", entry4.Account);
        Assert.Equal(900, entry4.StartBlock);
        Assert.Equal(1499, entry4.EndBlock);
        Assert.Equal(600, entry4.EffectiveBlocks);
        Assert.Equal(0, entry4.VtxDistributionId);
        Assert.Equal(0.6666667, entry4.Percentage);
        // Bonded properties
        Assert.Equal("200000000", entry4.Bonded.Balance);
        Assert.Equal("100000000", entry4.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", entry4.Bonded.Rate.ToString());
        Assert.Equal("Staker", entry4.Bonded.StakerType);
        Assert.Equal("1639999", entry4.Bonded.RewardPoints);
        // Unlocking properties
        Assert.Equal("0", entry4.Unlocking.Balance);
        Assert.Equal("0", entry4.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry4.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry4.Unlocking.StakerType);
        Assert.Equal("0", entry4.Unlocking.RewardPoints);
        
        // Second account in second cycle, second entry (0x2545... 1500-1799)
        var entry5 = secondCycleEntries.First(x => 
            x.Account == "0x25451A4de12dcCc2D166922fA938E900fCc4ED24" && 
            x.StartBlock == 1500);
        // General properties
        Assert.Equal("0x25451A4de12dcCc2D166922fA938E900fCc4ED24", entry5.Account);
        Assert.Equal(1500, entry5.StartBlock);
        Assert.Equal(1799, entry5.EndBlock);
        Assert.Equal(300, entry5.EffectiveBlocks);
        Assert.Equal(0, entry5.VtxDistributionId);
        Assert.Equal(0.3333333, entry5.Percentage);
        // Bonded properties
        Assert.Equal("100000000", entry5.Bonded.Balance);
        Assert.Equal("100000000", entry5.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", entry5.Bonded.Rate.ToString());
        Assert.Equal("Staker", entry5.Bonded.StakerType);
        Assert.Equal("819999", entry5.Bonded.RewardPoints);
        // Unlocking properties
        Assert.Equal("0", entry5.Unlocking.Balance);
        Assert.Equal("0", entry5.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry5.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry5.Unlocking.StakerType);
        Assert.Equal("0", entry5.Unlocking.RewardPoints);
        
        // Third account in second cycle (0xf24F... 900-1799)
        var entry6 = secondCycleEntries.First(x => x.Account == "0xf24FF3a9CF04c71Dbc94D0b566f7A27B94566cac");
        // General properties
        Assert.Equal("0xf24FF3a9CF04c71Dbc94D0b566f7A27B94566cac", entry6.Account);
        Assert.Equal(900, entry6.StartBlock);
        Assert.Equal(1799, entry6.EndBlock);
        Assert.Equal(900, entry6.EffectiveBlocks);
        Assert.Equal(0, entry6.VtxDistributionId);
        Assert.Equal(1.0, entry6.Percentage);
        // Bonded properties
        Assert.Equal("52000000000", entry6.Bonded.Balance);
        Assert.Equal("52000000000", entry6.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", entry6.Bonded.Rate.ToString());
        Assert.Equal("Staker", entry6.Bonded.StakerType);
        Assert.Equal("1279200000", entry6.Bonded.RewardPoints);
        // Unlocking properties
        Assert.Equal("0", entry6.Unlocking.Balance);
        Assert.Equal("0", entry6.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry6.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry6.Unlocking.StakerType);
        Assert.Equal("0", entry6.Unlocking.RewardPoints);
    }
}