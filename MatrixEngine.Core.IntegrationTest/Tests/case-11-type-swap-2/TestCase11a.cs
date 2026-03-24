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

namespace MatrixEngine.Core.IntegrationTest.Tests.case_11_type_swap_2;

[Order(3)]
public class TestCase11a : TestBed<IntegrationTestFixture>
{
    public TestCase11a(ITestOutputHelper testOutputHelper, IntegrationTestFixture fixture) : base(testOutputHelper,
        fixture)
    {
    }

    [Fact]
    public async Task Test_Scenario_1()
    {
        // This test case has different staker types at start and end.
        // At the start it's a Validator, then it swaps at era 13 to Nominator until the end of the cycle 
        // There is also some bonded/ withdrawn transactions to ensure the balances table gets split at both the balance changes
        // and staker type changes.

        var dataLoader = _fixture.GetService<IDataLoader>(_testOutputHelper);
        await dataLoader?.LoadCase("case-11a")!;

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
        Assert.Equal("12587716", signedBalance1.TotalRewardPoints);
        
        var effectiveBalances = database.GetCollection<EffectiveBalanceModel>(DbCollectionName.EffectiveBalance);
        var effectiveBalance = await effectiveBalances.Find(_ => true)
            .Sort(Builders<EffectiveBalanceModel>.Sort.Ascending(x => x.StartBlock))
            .ToListAsync();
        Assert.Equal(6, effectiveBalance.Count);
        
        // Account 0x0000... (0-44)
        // Block 45 is the first bonded event
        var entry1 = effectiveBalance[0];
        Assert.Equal("0x0000000000000000000000000000000000000000", entry1.Account);
        Assert.Equal(0, entry1.StartBlock);
        Assert.Equal(44, entry1.EndBlock);
        Assert.Equal(45, entry1.EffectiveBlocks);
        Assert.Equal(1, entry1.VtxDistributionId);
        Assert.Equal(0.05, entry1.Percentage);
        // Bonded properties
        Assert.Equal("100000000", entry1.Bonded.Balance);
        Assert.Equal("50000000", entry1.Bonded.EffectiveBalance);
        Assert.Equal("0.0739", entry1.Bonded.Rate.ToString());
        Assert.Equal("Validator", entry1.Bonded.StakerType);
        Assert.Equal("184750", entry1.Bonded.RewardPoints);
        // Unlocking properties
        Assert.Equal("0", entry1.Unlocking.Balance);
        Assert.Equal("0", entry1.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry1.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry1.Unlocking.StakerType);
        Assert.Equal("0", entry1.Unlocking.RewardPoints);
        
        // Account 0x0000... (45-129)
        // Block 129 is where the staker type changes to Nominator
        var entry2 = effectiveBalance[1];
        Assert.Equal("0x0000000000000000000000000000000000000000", entry2.Account);
        Assert.Equal(45, entry2.StartBlock);
        Assert.Equal(129, entry2.EndBlock);
        Assert.Equal(85, entry2.EffectiveBlocks);
        Assert.Equal(1, entry2.VtxDistributionId);
        Assert.Equal(0.0944444, entry2.Percentage);
        // Bonded properties
        Assert.Equal("250000000", entry2.Bonded.Balance);
        Assert.Equal("50000000", entry2.Bonded.EffectiveBalance);
        Assert.Equal("0.0739", entry2.Bonded.Rate.ToString());
        Assert.Equal("Validator", entry2.Bonded.StakerType);
        Assert.Equal("348972", entry2.Bonded.RewardPoints);
        // Unlocking properties
        Assert.Equal("0", entry2.Unlocking.Balance);
        Assert.Equal("0", entry2.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry2.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry2.Unlocking.StakerType);
        Assert.Equal("0", entry2.Unlocking.RewardPoints);
        
        // Account 0x0000... (130-144)
        // Next withdraw event is at block 145
        var entry3 = effectiveBalance[2];
        Assert.Equal("0x0000000000000000000000000000000000000000", entry3.Account);
        Assert.Equal(130, entry3.StartBlock);
        Assert.Equal(144, entry3.EndBlock);
        Assert.Equal(15, entry3.EffectiveBlocks);
        Assert.Equal(1, entry3.VtxDistributionId);
        Assert.Equal(0.0166667, entry3.Percentage);
        // Bonded properties
        Assert.Equal("250000000", entry3.Bonded.Balance);
        Assert.Equal("50000000", entry3.Bonded.EffectiveBalance);
        Assert.Equal("0.0492", entry3.Bonded.Rate.ToString());
        Assert.Equal("Nominator", entry3.Bonded.StakerType);
        Assert.Equal("40999", entry3.Bonded.RewardPoints);
        // Unlocking properties
        Assert.Equal("0", entry3.Unlocking.Balance);
        Assert.Equal("0", entry3.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry3.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry3.Unlocking.StakerType);
        Assert.Equal("0", entry3.Unlocking.RewardPoints);
        
        // Account 0x0000... (145-444)
        var entry4 = effectiveBalance[3];
        Assert.Equal("0x0000000000000000000000000000000000000000", entry4.Account);
        Assert.Equal(145, entry4.StartBlock);
        Assert.Equal(444, entry4.EndBlock);
        Assert.Equal(300, entry4.EffectiveBlocks);
        Assert.Equal(1, entry4.VtxDistributionId);
        Assert.Equal(0.3333333, entry4.Percentage);
        // Bonded properties
        Assert.Equal("50000000", entry4.Bonded.Balance);
        Assert.Equal("50000000", entry4.Bonded.EffectiveBalance);
        Assert.Equal("0.0492", entry4.Bonded.Rate.ToString());
        Assert.Equal("Nominator", entry4.Bonded.StakerType);
        Assert.Equal("819999", entry4.Bonded.RewardPoints);
        // Unlocking properties
        Assert.Equal("0", entry4.Unlocking.Balance);
        Assert.Equal("0", entry4.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry4.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry4.Unlocking.StakerType);
        Assert.Equal("0", entry4.Unlocking.RewardPoints);
        
        // Account 0x0000... (445-549)
        var entry5 = effectiveBalance[4];
        Assert.Equal("0x0000000000000000000000000000000000000000", entry5.Account);
        Assert.Equal(445, entry5.StartBlock);
        Assert.Equal(549, entry5.EndBlock);
        Assert.Equal(105, entry5.EffectiveBlocks);
        Assert.Equal(1, entry5.VtxDistributionId);
        Assert.Equal(0.1166667, entry5.Percentage);
        // Bonded properties
        Assert.Equal("550000000", entry5.Bonded.Balance);
        Assert.Equal("450000000", entry5.Bonded.EffectiveBalance);
        Assert.Equal("0.0492", entry5.Bonded.Rate.ToString());
        Assert.Equal("Nominator", entry5.Bonded.StakerType);
        Assert.Equal("2582998", entry5.Bonded.RewardPoints);
        // Unlocking properties
        Assert.Equal("0", entry5.Unlocking.Balance);
        Assert.Equal("0", entry5.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry5.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry5.Unlocking.StakerType);
        Assert.Equal("0", entry5.Unlocking.RewardPoints);
        
        // Account 0x0000... (550-899)
        var entry6 = effectiveBalance[5];
        Assert.Equal("0x0000000000000000000000000000000000000000", entry6.Account);
        Assert.Equal(550, entry6.StartBlock);
        Assert.Equal(899, entry6.EndBlock);
        Assert.Equal(350, entry6.EffectiveBlocks);
        Assert.Equal(1, entry6.VtxDistributionId);
        Assert.Equal(0.3888889, entry6.Percentage);
        // Bonded properties
        Assert.Equal("450000000", entry6.Bonded.Balance);
        Assert.Equal("450000000", entry6.Bonded.EffectiveBalance);
        Assert.Equal("0.0492", entry6.Bonded.Rate.ToString());
        Assert.Equal("Nominator", entry6.Bonded.StakerType);
        Assert.Equal("8609998", entry6.Bonded.RewardPoints);
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