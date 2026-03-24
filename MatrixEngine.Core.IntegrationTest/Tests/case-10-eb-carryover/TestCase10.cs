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

namespace MatrixEngine.Core.IntegrationTest.Tests.case_10_eb_carryover;

[Order(10)]
public class TestCase10 : TestBed<IntegrationTestFixture>
{
    public TestCase10(ITestOutputHelper testOutputHelper, IntegrationTestFixture fixture) : base(testOutputHelper,
        fixture)
    {
    }

    [Fact]
    public async Task Test_Scenario_1()
    {
        // This test case will test whether an accounts effective balance is carried over from the previous cycle 
        // if they do not have any transactions in the current cycle

        // Arrange
        var dataLoader = _fixture.GetService<IDataLoader>(_testOutputHelper);
        await dataLoader?.LoadCase("case-10")!;

        var engineCore = _fixture.GetService<IEngineCore>(_testOutputHelper);
        var signEffectiveBalanceService = _fixture.GetService<ISignEffectiveBalanceService>(_testOutputHelper);

        // Act
        await engineCore?.Start()!;
        
        var database = _fixture.GetService<IMongoDatabase>(_testOutputHelper);
        var collection = database.GetCollection<SignedEffectiveBalanceModel>(DbCollectionName.SignEffectiveBalance);
        var signedBalances = await collection.Find(_ => true).ToListAsync();
        Assert.Equal(2, signedBalances.Count);
        
        
        var balance1 = signedBalances[0];
        Assert.Equal("0xE566475DE82dd261cA0b2a8625bD8a3D822A7546", balance1.Account);
        Assert.Equal(1, balance1.VtxDistributionId);
        Assert.Equal("1230000", balance1.TotalRewardPoints);
        Assert.Equal(0, balance1.StartBlock);
        Assert.Equal(899, balance1.EndBlock);
        Assert.False(balance1.Submitted);
        
        var balance2 = signedBalances[1];
        Assert.Equal("0xE566475DE82dd261cA0b2a8625bD8a3D822A7546", balance2.Account);
        Assert.Equal(2, balance2.VtxDistributionId);
        Assert.Equal("2460000", balance2.TotalRewardPoints);
        Assert.Equal(900, balance2.StartBlock);
        Assert.Equal(1799, balance2.EndBlock);
        Assert.False(balance2.Submitted);
        
        var effectiveBalances = database.GetCollection<EffectiveBalanceModel>(DbCollectionName.EffectiveBalance);
        var effectiveBalance = await effectiveBalances.Find(_ => true)
            .Sort(Builders<EffectiveBalanceModel>.Sort.Ascending(x => x.StartBlock))
            .ToListAsync();
        
        // Verify the effective balance is as expected
        Assert.Equal(2, effectiveBalance.Count);
        
        // Account 0xE566... (450-899)
        var entry1 = effectiveBalance[0];
        Assert.Equal("0xE566475DE82dd261cA0b2a8625bD8a3D822A7546", entry1.Account);
        Assert.Equal(450, entry1.StartBlock);
        Assert.Equal(899, entry1.EndBlock);
        Assert.Equal(450, entry1.EffectiveBlocks);
        Assert.Equal(1, entry1.VtxDistributionId);
        Assert.Equal(0.5, entry1.Percentage);
        // Bonded properties
        Assert.Equal("100000000", entry1.Bonded.Balance);
        Assert.Equal("100000000", entry1.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", entry1.Bonded.Rate.ToString());
        Assert.Equal("Staker", entry1.Bonded.StakerType);
        Assert.Equal("1230000", entry1.Bonded.RewardPoints);
        // Unlocking properties
        Assert.Equal("0", entry1.Unlocking.Balance);
        Assert.Equal("0", entry1.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry1.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry1.Unlocking.StakerType);
        Assert.Equal("0", entry1.Unlocking.RewardPoints);
        
        // Account 0xE566... (900-1799)
        var entry2 = effectiveBalance[1];
        Assert.Equal("0xE566475DE82dd261cA0b2a8625bD8a3D822A7546", entry2.Account);
        Assert.Equal(900, entry2.StartBlock);
        Assert.Equal(1799, entry2.EndBlock);
        Assert.Equal(900, entry2.EffectiveBlocks);
        Assert.Equal(2, entry2.VtxDistributionId);
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
        
        // Sum all reward points for each signed balance and verify
        var totalRewardPoints1 = effectiveBalance
            .Where(x => x.VtxDistributionId == 1)
            .Aggregate(BigInteger.Zero, (sum, x) => sum + BigInteger.Parse(x.TotalRewardPoints));
        Assert.Equal(balance1.TotalRewardPoints, totalRewardPoints1.ToString());
        
        var totalRewardPoints2 = effectiveBalance
            .Where(x => x.VtxDistributionId == 2)
            .Aggregate(BigInteger.Zero, (sum, x) => sum + BigInteger.Parse(x.TotalRewardPoints));
        Assert.Equal(balance2.TotalRewardPoints, totalRewardPoints2.ToString());
    }
} 