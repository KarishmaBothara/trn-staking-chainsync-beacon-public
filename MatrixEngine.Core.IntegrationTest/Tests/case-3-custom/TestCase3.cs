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

namespace MatrixEngine.Core.IntegrationTest.Tests.case_3_custom;

[Order(3)]
public class TestCase3 : TestBed<IntegrationTestFixture>
{
    public TestCase3(ITestOutputHelper testOutputHelper, IntegrationTestFixture fixture) : base(testOutputHelper,
        fixture)
    {
    }

    [Fact]
    public async Task Test_Scenario_1()
    {
        // Arrange
        var dataLoader = _fixture.GetService<IDataLoader>(_testOutputHelper);
        await dataLoader?.LoadCase("case-3")!;

        var engineCore = _fixture.GetService<IEngineCore>(_testOutputHelper);
        var signEffectiveBalanceService = _fixture.GetService<ISignEffectiveBalanceService>(_testOutputHelper);

        // Act
        await engineCore?.Start()!;
        
        // Get all signed effective balances
        var database = _fixture.GetService<IMongoDatabase>(_testOutputHelper);
        var collection = database.GetCollection<SignedEffectiveBalanceModel>(DbCollectionName.SignEffectiveBalance);
        var signedBalances = await collection.Find(_ => true).ToListAsync();
        var balance = signedBalances[0];
        
        // Verify the signed balance is as expected
        Assert.Single(signedBalances);
        Assert.Equal("0xE566475DE82dd261cA0b2a8625bD8a3D822A7546", balance.Account);
        Assert.Equal(1, balance.VtxDistributionId);
        Assert.Equal("775581", balance.TotalRewardPoints);
        Assert.False(balance.Submitted);
        
        var effectiveBalances = database.GetCollection<EffectiveBalanceModel>(DbCollectionName.EffectiveBalance);
        var effectiveBalance = await effectiveBalances.Find(_ => true)
            .Sort(Builders<EffectiveBalanceModel>.Sort.Ascending(x => x.StartBlock))
            .ToListAsync();
        
        // Verify the effective balance is as expected
        Assert.Equal(6, effectiveBalance.Count);
        
        // Account 0xE566... (0-9)
        var entry1 = effectiveBalance[0];
        Assert.Equal("0xE566475DE82dd261cA0b2a8625bD8a3D822A7546", entry1.Account);
        Assert.Equal(0, entry1.StartBlock);
        Assert.Equal(9, entry1.EndBlock);
        Assert.Equal(10, entry1.EffectiveBlocks);
        Assert.Equal(1, entry1.VtxDistributionId);
        Assert.Equal(0.0111111, entry1.Percentage);
        // Bonded properties
        Assert.Equal("100000000", entry1.Bonded.Balance);
        Assert.Equal("25000000", entry1.Bonded.EffectiveBalance);
        Assert.Equal("0.0492", entry1.Bonded.Rate.ToString());
        Assert.Equal("Nominator", entry1.Bonded.StakerType);
        Assert.Equal("13666", entry1.Bonded.RewardPoints);
        // Unlocking properties
        Assert.Equal("0", entry1.Unlocking.Balance);
        Assert.Equal("0", entry1.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry1.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry1.Unlocking.StakerType);
        Assert.Equal("0", entry1.Unlocking.RewardPoints);
        
        // Account 0xE566... (10-209)
        var entry2 = effectiveBalance[1];
        Assert.Equal("0xE566475DE82dd261cA0b2a8625bD8a3D822A7546", entry2.Account);
        Assert.Equal(10, entry2.StartBlock);
        Assert.Equal(209, entry2.EndBlock);
        Assert.Equal(200, entry2.EffectiveBlocks);
        Assert.Equal(1, entry2.VtxDistributionId);
        Assert.Equal(0.2222222, entry2.Percentage);
        // Bonded properties
        Assert.Equal("100000000", entry2.Bonded.Balance);
        Assert.Equal("25000000", entry2.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", entry2.Bonded.Rate.ToString());
        Assert.Equal("Staker", entry2.Bonded.StakerType);
        Assert.Equal("136666", entry2.Bonded.RewardPoints);
        // Unlocking properties
        Assert.Equal("0", entry2.Unlocking.Balance);
        Assert.Equal("0", entry2.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry2.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry2.Unlocking.StakerType);
        Assert.Equal("0", entry2.Unlocking.RewardPoints);
        
        // Account 0xE566... (210-224)
        var entry3 = effectiveBalance[2];
        Assert.Equal("0xE566475DE82dd261cA0b2a8625bD8a3D822A7546", entry3.Account);
        Assert.Equal(210, entry3.StartBlock);
        Assert.Equal(224, entry3.EndBlock);
        Assert.Equal(15, entry3.EffectiveBlocks);
        Assert.Equal(1, entry3.VtxDistributionId);
        Assert.Equal(0.0166667, entry3.Percentage);
        // Bonded properties
        Assert.Equal("25000000", entry3.Bonded.Balance);
        Assert.Equal("25000000", entry3.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", entry3.Bonded.Rate.ToString());
        Assert.Equal("Staker", entry3.Bonded.StakerType);
        Assert.Equal("10249", entry3.Bonded.RewardPoints);
        // Unlocking properties
        Assert.Equal("75000000", entry3.Unlocking.Balance);
        Assert.Equal("0", entry3.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry3.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry3.Unlocking.StakerType);
        Assert.Equal("0", entry3.Unlocking.RewardPoints);
        
        // Account 0xE566... (225-449)
        var entry4 = effectiveBalance[3];
        Assert.Equal("0xE566475DE82dd261cA0b2a8625bD8a3D822A7546", entry4.Account);
        Assert.Equal(225, entry4.StartBlock);
        Assert.Equal(449, entry4.EndBlock);
        Assert.Equal(225, entry4.EffectiveBlocks);
        Assert.Equal(1, entry4.VtxDistributionId);
        Assert.Equal(0.25, entry4.Percentage);
        // Bonded properties
        Assert.Equal("25000000", entry4.Bonded.Balance);
        Assert.Equal("25000000", entry4.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", entry4.Bonded.Rate.ToString());
        Assert.Equal("Staker", entry4.Bonded.StakerType);
        Assert.Equal("153750", entry4.Bonded.RewardPoints);
        // Unlocking properties
        Assert.Equal("50000000", entry4.Unlocking.Balance);
        Assert.Equal("0", entry4.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry4.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry4.Unlocking.StakerType);
        Assert.Equal("0", entry4.Unlocking.RewardPoints);
        
        // Account 0xE566... (450-674)
        var entry5 = effectiveBalance[4];
        Assert.Equal("0xE566475DE82dd261cA0b2a8625bD8a3D822A7546", entry5.Account);
        Assert.Equal(450, entry5.StartBlock);
        Assert.Equal(674, entry5.EndBlock);
        Assert.Equal(225, entry5.EffectiveBlocks);
        Assert.Equal(1, entry5.VtxDistributionId);
        Assert.Equal(0.25, entry5.Percentage);
        // Bonded properties
        Assert.Equal("25000000", entry5.Bonded.Balance);
        Assert.Equal("25000000", entry5.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", entry5.Bonded.Rate.ToString());
        Assert.Equal("Staker", entry5.Bonded.StakerType);
        Assert.Equal("153750", entry5.Bonded.RewardPoints);
        // Unlocking properties
        Assert.Equal("0", entry5.Unlocking.Balance);
        Assert.Equal("0", entry5.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry5.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry5.Unlocking.StakerType);
        Assert.Equal("0", entry5.Unlocking.RewardPoints);
        
        // Account 0xE566... (675-899)
        var entry6 = effectiveBalance[5];
        Assert.Equal("0xE566475DE82dd261cA0b2a8625bD8a3D822A7546", entry6.Account);
        Assert.Equal(675, entry6.StartBlock);
        Assert.Equal(899, entry6.EndBlock);
        Assert.Equal(225, entry6.EffectiveBlocks);
        Assert.Equal(1, entry6.VtxDistributionId);
        Assert.Equal(0.25, entry6.Percentage);
        // Bonded properties
        Assert.Equal("50000000", entry6.Bonded.Balance);
        Assert.Equal("50000000", entry6.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", entry6.Bonded.Rate.ToString());
        Assert.Equal("Staker", entry6.Bonded.StakerType);
        Assert.Equal("307500", entry6.Bonded.RewardPoints);
        // Unlocking properties
        Assert.Equal("0", entry6.Unlocking.Balance);
        Assert.Equal("0", entry6.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry6.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry6.Unlocking.StakerType);
        Assert.Equal("0", entry6.Unlocking.RewardPoints);
        
        // Sum all reward points and assert it is equal to the total reward points
        var totalRewardPoints = effectiveBalance.Aggregate(BigInteger.Zero, (sum, x) => sum + BigInteger.Parse(x.TotalRewardPoints));
        Assert.Equal(balance.TotalRewardPoints, totalRewardPoints.ToString());
    }
}