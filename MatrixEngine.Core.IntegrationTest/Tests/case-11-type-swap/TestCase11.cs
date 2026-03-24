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

namespace MatrixEngine.Core.IntegrationTest.Tests.case_11_type_swap;

[Order(3)]
public class TestCase11 : TestBed<IntegrationTestFixture>
{
    public TestCase11(ITestOutputHelper testOutputHelper, IntegrationTestFixture fixture) : base(testOutputHelper,
        fixture)
    {
    }

    [Fact]
    public async Task Test_Scenario_1()
    {
        // This test case will test whether the balance table gets correctly split if the staker type changes mid-cycle
        // In this case, the staker is Nominator at the start and end, but is Validator for 10 eras in the middle

        var dataLoader = _fixture.GetService<IDataLoader>(_testOutputHelper);
        await dataLoader?.LoadCase("case-11")!;

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
        Assert.Equal(1, signedBalance1.VtxDistributionId);
        Assert.Equal("5194443", signedBalance1.TotalRewardPoints);
        
        var effectiveBalances = database.GetCollection<EffectiveBalanceModel>(DbCollectionName.EffectiveBalance);
        var effectiveBalance = await effectiveBalances.Find(_ => true)
            .Sort(Builders<EffectiveBalanceModel>.Sort.Ascending(x => x.StartBlock))
            .ToListAsync();
        Assert.Equal(3, effectiveBalance.Count);
        
        // Account 0x0000... (0-29)
        var entry1 = effectiveBalance[0];
        Assert.Equal("0x0000000000000000000000000000000000000000", entry1.Account);
        Assert.Equal(0, entry1.StartBlock);
        Assert.Equal(29, entry1.EndBlock);
        Assert.Equal(30, entry1.EffectiveBlocks);
        Assert.Equal(1, entry1.VtxDistributionId);
        Assert.Equal(0.0333333, entry1.Percentage);
        // Bonded properties
        Assert.Equal("100000000", entry1.Bonded.Balance);
        Assert.Equal("100000000", entry1.Bonded.EffectiveBalance);
        Assert.Equal("0.0492", entry1.Bonded.Rate.ToString());
        Assert.Equal("Nominator", entry1.Bonded.StakerType);
        Assert.Equal("163999", entry1.Bonded.RewardPoints);
        // Unlocking properties
        Assert.Equal("0", entry1.Unlocking.Balance);
        Assert.Equal("0", entry1.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry1.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry1.Unlocking.StakerType);
        Assert.Equal("0", entry1.Unlocking.RewardPoints);
        
        // Account 0x0000... (30-129)
        // Account is now a Validator in the stakers.json file for these eras
        var entry2 = effectiveBalance[1];
        Assert.Equal("0x0000000000000000000000000000000000000000", entry2.Account);
        Assert.Equal(30, entry2.StartBlock);
        Assert.Equal(129, entry2.EndBlock);
        Assert.Equal(100, entry2.EffectiveBlocks);
        Assert.Equal(1, entry2.VtxDistributionId);
        Assert.Equal(0.1111111, entry2.Percentage);
        // Bonded properties
        Assert.Equal("100000000", entry2.Bonded.Balance);
        Assert.Equal("100000000", entry2.Bonded.EffectiveBalance);
        Assert.Equal("0.0739", entry2.Bonded.Rate.ToString());
        Assert.Equal("Validator", entry2.Bonded.StakerType);
        Assert.Equal("821111", entry2.Bonded.RewardPoints);
        // Unlocking properties
        Assert.Equal("0", entry2.Unlocking.Balance);
        Assert.Equal("0", entry2.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry2.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry2.Unlocking.StakerType);
        Assert.Equal("0", entry2.Unlocking.RewardPoints);
        
        // Account 0x0000... (130-899)
        var entry3 = effectiveBalance[2];
        Assert.Equal("0x0000000000000000000000000000000000000000", entry3.Account);
        Assert.Equal(130, entry3.StartBlock);
        Assert.Equal(899, entry3.EndBlock);
        Assert.Equal(770, entry3.EffectiveBlocks);
        Assert.Equal(1, entry3.VtxDistributionId);
        Assert.Equal(0.8555556, entry3.Percentage);
        // Bonded properties
        Assert.Equal("100000000", entry3.Bonded.Balance);
        Assert.Equal("100000000", entry3.Bonded.EffectiveBalance);
        Assert.Equal("0.0492", entry3.Bonded.Rate.ToString());
        Assert.Equal("Nominator", entry3.Bonded.StakerType);
        Assert.Equal("4209333", entry3.Bonded.RewardPoints);
        // Unlocking properties
        Assert.Equal("0", entry3.Unlocking.Balance);
        Assert.Equal("0", entry3.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry3.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry3.Unlocking.StakerType);
        Assert.Equal("0", entry3.Unlocking.RewardPoints);
        
        // Sum all reward points and assert it is equal to the total reward points
        var totalRewardPoints = effectiveBalance
            .Aggregate(BigInteger.Zero, (sum, x) => sum + BigInteger.Parse(x.TotalRewardPoints));
        Assert.Equal(signedBalance1.TotalRewardPoints, totalRewardPoints.ToString());
    }
}