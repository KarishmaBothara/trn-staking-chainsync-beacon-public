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
using MatrixEngine.Core.Utils;
using System.Linq;

namespace MatrixEngine.Core.IntegrationTest.Tests.case_12_chilled;

[Order(3)]
public class TestCase12 : TestBed<IntegrationTestFixture>
{
    public TestCase12(ITestOutputHelper testOutputHelper, IntegrationTestFixture fixture) : base(testOutputHelper,
        fixture)
    {
    }

    [Fact]
    public async Task Test_Scenario_1()
    {
        // This test scenario will test whether a chilled entry correctly converts the users staker rate to "Staker"
        // In this test, the account is a validator for every era, but for the "Chilled" eras, they will become a staker
        // It should split the effective balances table to represent this change
        // The chilled json file has one event for the start, end and middle of an era 
        // These are blocks 15, 150 and 899

        var dataLoader = _fixture.GetService<IDataLoader>(_testOutputHelper);
        await dataLoader?.LoadCase("case-12")!;

        var engineCore = _fixture.GetService<IEngineCore>(_testOutputHelper);
        var signEffectiveBalanceService = _fixture.GetService<ISignEffectiveBalanceService>(_testOutputHelper);

        // Act
        await engineCore?.Start()!;

        // Get all signed effective balances
        var database = _fixture.GetService<IMongoDatabase>(_testOutputHelper);
        var collection = database.GetCollection<SignedEffectiveBalanceModel>(DbCollectionName.SignEffectiveBalance);
        var signedBalances = await collection.Find(_ => true).ToListAsync();
        Assert.Single(signedBalances);
        
        // Verify first signed balance
        var signedBalance1 = signedBalances[0];
        Assert.Equal("0x0000000000000000000000000000000000000000", signedBalance1.Account);
        Assert.Equal(0, signedBalance1.StartBlock);
        Assert.Equal(899, signedBalance1.EndBlock);
        Assert.Equal(0, signedBalance1.VtxDistributionId);
        Assert.Equal("7225665", signedBalance1.TotalRewardPoints);
        
        var effectiveBalances = database.GetCollection<EffectiveBalanceModel>(DbCollectionName.EffectiveBalance);
        var effectiveBalance = await effectiveBalances.Find(_ => true)
            .Sort(Builders<EffectiveBalanceModel>.Sort.Ascending(x => x.StartBlock))
            .ToListAsync();
        Assert.Equal(6, effectiveBalance.Count);
        
        // Account 0x0000... (0-9)
        var entry1 = effectiveBalance[0];
        Assert.Equal("0x0000000000000000000000000000000000000000", entry1.Account);
        Assert.Equal(0, entry1.StartBlock);
        Assert.Equal(9, entry1.EndBlock);
        Assert.Equal(10, entry1.EffectiveBlocks);
        Assert.Equal(0, entry1.VtxDistributionId);
        Assert.Equal(0.0111111, entry1.Percentage);
        // Bonded properties
        Assert.Equal("100000000", entry1.Bonded.Balance);
        Assert.Equal("100000000", entry1.Bonded.EffectiveBalance);
        Assert.Equal("0.0739", entry1.Bonded.Rate.ToString());
        Assert.Equal("Validator", entry1.Bonded.StakerType);
        Assert.Equal("82111", entry1.Bonded.RewardPoints);
        // Unlocking properties
        Assert.Equal("0", entry1.Unlocking.Balance);
        Assert.Equal("0", entry1.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry1.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry1.Unlocking.StakerType);
        Assert.Equal("0", entry1.Unlocking.RewardPoints);
        
        // Account 0x0000... (10-19)
        // Chilled on block 15
        var entry2 = effectiveBalance[1];
        Assert.Equal("0x0000000000000000000000000000000000000000", entry2.Account);
        Assert.Equal(10, entry2.StartBlock);
        Assert.Equal(19, entry2.EndBlock);
        Assert.Equal(10, entry2.EffectiveBlocks);
        Assert.Equal(0, entry2.VtxDistributionId);
        Assert.Equal(0.0111111, entry2.Percentage);
        // Bonded properties
        Assert.Equal("100000000", entry2.Bonded.Balance);
        Assert.Equal("100000000", entry2.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", entry2.Bonded.Rate.ToString());
        Assert.Equal("Staker", entry2.Bonded.StakerType);
        Assert.Equal("27333", entry2.Bonded.RewardPoints);
        // Unlocking properties
        Assert.Equal("0", entry2.Unlocking.Balance);
        Assert.Equal("0", entry2.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry2.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry2.Unlocking.StakerType);
        Assert.Equal("0", entry2.Unlocking.RewardPoints);
        
        // Account 0x0000... (20-149)
        var entry3 = effectiveBalance[2];
        Assert.Equal("0x0000000000000000000000000000000000000000", entry3.Account);
        Assert.Equal(20, entry3.StartBlock);
        Assert.Equal(149, entry3.EndBlock);
        Assert.Equal(130, entry3.EffectiveBlocks);
        Assert.Equal(0, entry3.VtxDistributionId);
        Assert.Equal(0.1444444, entry3.Percentage);
        // Bonded properties
        Assert.Equal("100000000", entry3.Bonded.Balance);
        Assert.Equal("100000000", entry3.Bonded.EffectiveBalance);
        Assert.Equal("0.0739", entry3.Bonded.Rate.ToString());
        Assert.Equal("Validator", entry3.Bonded.StakerType);
        Assert.Equal("1067444", entry3.Bonded.RewardPoints);
        // Unlocking properties
        Assert.Equal("0", entry3.Unlocking.Balance);
        Assert.Equal("0", entry3.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry3.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry3.Unlocking.StakerType);
        Assert.Equal("0", entry3.Unlocking.RewardPoints);
        
        // Account 0x0000... (150-159)
        // Chilled on block 150
        var entry4 = effectiveBalance[3];
        Assert.Equal("0x0000000000000000000000000000000000000000", entry4.Account);
        Assert.Equal(150, entry4.StartBlock);
        Assert.Equal(159, entry4.EndBlock);
        Assert.Equal(10, entry4.EffectiveBlocks);
        Assert.Equal(0, entry4.VtxDistributionId);
        Assert.Equal(0.0111111, entry4.Percentage);
        // Bonded properties
        Assert.Equal("100000000", entry4.Bonded.Balance);
        Assert.Equal("100000000", entry4.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", entry4.Bonded.Rate.ToString());
        Assert.Equal("Staker", entry4.Bonded.StakerType);
        Assert.Equal("27333", entry4.Bonded.RewardPoints);
        // Unlocking properties
        Assert.Equal("0", entry4.Unlocking.Balance);
        Assert.Equal("0", entry4.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry4.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry4.Unlocking.StakerType);
        Assert.Equal("0", entry4.Unlocking.RewardPoints);
        
        // Account 0x0000... (160-889)
        var entry5 = effectiveBalance[4];
        Assert.Equal("0x0000000000000000000000000000000000000000", entry5.Account);
        Assert.Equal(160, entry5.StartBlock);
        Assert.Equal(889, entry5.EndBlock);
        Assert.Equal(730, entry5.EffectiveBlocks);
        Assert.Equal(0, entry5.VtxDistributionId);
        Assert.Equal(0.8111111, entry5.Percentage);
        // Bonded properties
        Assert.Equal("100000000", entry5.Bonded.Balance);
        Assert.Equal("100000000", entry5.Bonded.EffectiveBalance);
        Assert.Equal("0.0739", entry5.Bonded.Rate.ToString());
        Assert.Equal("Validator", entry5.Bonded.StakerType);
        Assert.Equal("5994111", entry5.Bonded.RewardPoints);
        // Unlocking properties
        Assert.Equal("0", entry5.Unlocking.Balance);
        Assert.Equal("0", entry5.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry5.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry5.Unlocking.StakerType);
        Assert.Equal("0", entry5.Unlocking.RewardPoints);
        
        // Account 0x0000... (890-899)
        // Chilled on block 899
        var entry6 = effectiveBalance[5];
        Assert.Equal("0x0000000000000000000000000000000000000000", entry6.Account);
        Assert.Equal(890, entry6.StartBlock);
        Assert.Equal(899, entry6.EndBlock);
        Assert.Equal(10, entry6.EffectiveBlocks);
        Assert.Equal(0, entry6.VtxDistributionId);
        Assert.Equal(0.0111111, entry6.Percentage);
        // Bonded properties
        Assert.Equal("100000000", entry6.Bonded.Balance);
        Assert.Equal("100000000", entry6.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", entry6.Bonded.Rate.ToString());
        Assert.Equal("Staker", entry6.Bonded.StakerType);
        Assert.Equal("27333", entry6.Bonded.RewardPoints);
        // Unlocking properties
        Assert.Equal("0", entry6.Unlocking.Balance);
        Assert.Equal("0", entry6.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry6.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry6.Unlocking.StakerType);
        Assert.Equal("0", entry6.Unlocking.RewardPoints);
        
        // Sum all reward points and assert it is equal to the total reward points
        var totalRewardPoints = effectiveBalance
            .Aggregate(BigInteger.Zero, (sum, x) => sum + BigInteger.Parse(x.TotalRewardPoints));
        Assert.Equal(signedBalance1.TotalRewardPoints, totalRewardPoints.ToString());
    }
}