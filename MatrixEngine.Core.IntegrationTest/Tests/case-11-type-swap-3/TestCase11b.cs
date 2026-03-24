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

namespace MatrixEngine.Core.IntegrationTest.Tests.case_11_type_swap_3;

[Order(3)]
public class TestCase11b : TestBed<IntegrationTestFixture>
{
    public TestCase11b(ITestOutputHelper testOutputHelper, IntegrationTestFixture fixture) : base(testOutputHelper,
        fixture)
    {
    }

    [Fact]
    public async Task Test_Scenario_1()
    {
        // This test case will alternate staker types between Validator and Nominator every era
        // It should end up with 90 entries in the effective balance table, 45 of each

        var dataLoader = _fixture.GetService<IDataLoader>(_testOutputHelper);
        await dataLoader?.LoadCase("case-11b")!;

        var engineCore = _fixture.GetService<IEngineCore>(_testOutputHelper);
        var signEffectiveBalanceService = _fixture.GetService<ISignEffectiveBalanceService>(_testOutputHelper);

        // Act
        await engineCore?.Start()!;

        // Get all signed effective balances
        var database = _fixture.GetService<IMongoDatabase>(_testOutputHelper);
        var collection = database.GetCollection<SignedEffectiveBalanceModel>(DbCollectionName.SignEffectiveBalance);
        var signedBalances = await collection.Find(_ => true).ToListAsync();
        Assert.Single(signedBalances);
        
        // Manually calculate to ensure the correct rate is being applied to the total for each section.
        var precision = 10000000; // 7 zeros to match calculation precision
        var nominatorRate = (BigInteger)(StakerUtils.GetStakerRate(StakerType.Nominator) * precision);
        var validatorRate = (BigInteger)(StakerUtils.GetStakerRate(StakerType.Validator) * precision);
        var totalBalance = BigInteger.Parse("100000000"); // From transactions table at block 0
        var percent = (BigInteger)(precision / 90); // 1/90 per era
        var rewardPointSum = BigInteger.Zero;
        for (var i = 0; i < 45; i++)
        {
            rewardPointSum += totalBalance * nominatorRate * percent / precision / precision;
        }
        for (var i = 0; i < 45; i++)
        {
            rewardPointSum += totalBalance * validatorRate * percent / precision / precision;
        }
        
        // Verify first signed balance
        var signedBalance1 = signedBalances[0];
        Assert.Equal("0x0000000000000000000000000000000000000000", signedBalance1.Account);
        Assert.Equal(0, signedBalance1.StartBlock);
        Assert.Equal(899, signedBalance1.EndBlock);
        Assert.Equal(1, signedBalance1.VtxDistributionId);
        Assert.Equal(rewardPointSum.ToString(), signedBalance1.TotalRewardPoints);
        
        var effectiveBalances = database.GetCollection<EffectiveBalanceModel>(DbCollectionName.EffectiveBalance);
        var effectiveBalance = await effectiveBalances.Find(_ => true)
            .Sort(Builders<EffectiveBalanceModel>.Sort.Ascending(x => x.StartBlock))
            .ToListAsync();
        Assert.Equal(90, effectiveBalance.Count);
        
        // Count and verify the number of Validator and Nominator entries
        var validatorCount = effectiveBalance.Count(x => x.Bonded.StakerType == "Validator");
        var nominatorCount = effectiveBalance.Count(x => x.Bonded.StakerType == "Nominator");
        Assert.Equal(45, validatorCount);
        Assert.Equal(45, nominatorCount);
        
        // Check some sample entries to ensure they have the correct structure
        // First Nominator entry (assuming first entry is a Nominator)
        var nominatorEntry = effectiveBalance.First(x => x.Bonded.StakerType == "Nominator");
        Assert.Equal("0x0000000000000000000000000000000000000000", nominatorEntry.Account);
        Assert.Equal(1, nominatorEntry.VtxDistributionId);
        Assert.Equal(10, nominatorEntry.EffectiveBlocks); // Each era is 10 blocks
        Assert.Equal(0.0111111, nominatorEntry.Percentage);
        // Bonded properties
        Assert.Equal("100000000", nominatorEntry.Bonded.Balance);
        Assert.Equal("100000000", nominatorEntry.Bonded.EffectiveBalance);
        Assert.Equal("0.0492", nominatorEntry.Bonded.Rate.ToString());
        Assert.Equal("Nominator", nominatorEntry.Bonded.StakerType);
        // Unlocking properties
        Assert.Equal("0", nominatorEntry.Unlocking.Balance);
        Assert.Equal("0", nominatorEntry.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", nominatorEntry.Unlocking.Rate.ToString());
        Assert.Equal("Staker", nominatorEntry.Unlocking.StakerType);
        Assert.Equal("0", nominatorEntry.Unlocking.RewardPoints);
        
        // First Validator entry (assuming second entry is a Validator)
        var validatorEntry = effectiveBalance.First(x => x.Bonded.StakerType == "Validator");
        Assert.Equal("0x0000000000000000000000000000000000000000", validatorEntry.Account);
        Assert.Equal(1, validatorEntry.VtxDistributionId);
        Assert.Equal(10, validatorEntry.EffectiveBlocks); // Each era is 10 blocks
        Assert.Equal(0.0111111, validatorEntry.Percentage);
        // Bonded properties
        Assert.Equal("100000000", validatorEntry.Bonded.Balance);
        Assert.Equal("100000000", validatorEntry.Bonded.EffectiveBalance);
        Assert.Equal("0.0739", validatorEntry.Bonded.Rate.ToString());
        Assert.Equal("Validator", validatorEntry.Bonded.StakerType);
        // Unlocking properties
        Assert.Equal("0", validatorEntry.Unlocking.Balance);
        Assert.Equal("0", validatorEntry.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", validatorEntry.Unlocking.Rate.ToString());
        Assert.Equal("Staker", validatorEntry.Unlocking.StakerType);
        Assert.Equal("0", validatorEntry.Unlocking.RewardPoints);
        
        // Verify reward points sum matches the signed balance total
        var totalRewardPoints = effectiveBalance
            .Aggregate(BigInteger.Zero, (sum, x) => sum + BigInteger.Parse(x.TotalRewardPoints));
        Assert.Equal(signedBalance1.TotalRewardPoints, totalRewardPoints.ToString());
    }
}