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

namespace MatrixEngine.Core.IntegrationTest.Tests.case_2_validator;

[Order(3)]
public class TestCase2b : TestBed<IntegrationTestFixture>
{
    public TestCase2b(ITestOutputHelper testOutputHelper, IntegrationTestFixture fixture) : base(testOutputHelper,
        fixture)
    {
    }

    [Fact]
    public async Task Test_Scenario_1()
    {
        // This test case will test whether the validator rate gets applied for Validators
        // Note that we need a transaction event for genesis validators as this testing framework does not involve the 
        // DataCore. The DataCore is responsible for fetching the transaction events from the indexer and will populate the
        // bonded events with the genesis validator balance.

        var dataLoader = _fixture.GetService<IDataLoader>(_testOutputHelper);
        await dataLoader?.LoadCase("case-2-validator")!;

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
        Assert.Equal("7390000", signedBalance1.TotalRewardPoints);
        
        var effectiveBalances = database.GetCollection<EffectiveBalanceModel>(DbCollectionName.EffectiveBalance);
        var effectiveBalance = await effectiveBalances.Find(_ => true)
            .Sort(Builders<EffectiveBalanceModel>.Sort.Ascending(x => x.StartBlock))
            .ToListAsync();
        Assert.Single(effectiveBalance);
        
        // Verify entry (5 - 84)
        var entry1 = effectiveBalance[0];
        Assert.Equal("0x0000000000000000000000000000000000000000", entry1.Account);
        Assert.Equal(0, entry1.StartBlock);
        Assert.Equal(899, entry1.EndBlock);
        Assert.Equal(900, entry1.EffectiveBlocks);
        Assert.Equal(1, entry1.VtxDistributionId);
        Assert.Equal(1, entry1.Percentage);
        // Bonded
        Assert.Equal("100000000", entry1.Bonded.Balance);
        Assert.Equal("100000000", entry1.Bonded.EffectiveBalance);
        Assert.Equal("0.0739", entry1.Bonded.Rate.ToString());
        Assert.Equal("Validator", entry1.Bonded.StakerType);
        Assert.Equal("7390000", entry1.Bonded.RewardPoints);
    }
}