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

namespace MatrixEngine.Core.IntegrationTest.Tests.case_9_not_ended;

[Order(8)]
public class TestCase9 : TestBed<IntegrationTestFixture>
{
    public TestCase9(ITestOutputHelper testOutputHelper, IntegrationTestFixture fixture) : base(testOutputHelper,
        fixture)
    {
    }

    [Fact]
    public async Task Test_Scenario_1()
    {
        // This test case will test when the cycle has not ended, 
        // in this case no calculations should be done for effective balance
        // the balances table should have some values though (as it now calculates per block)
        // Signed balances should be empty

        // Arrange
        var dataLoader = _fixture.GetService<IDataLoader>(_testOutputHelper);
        await dataLoader?.LoadCase("case-9")!;

        var engineCore = _fixture.GetService<IEngineCore>(_testOutputHelper);
        var signEffectiveBalanceService = _fixture.GetService<ISignEffectiveBalanceService>(_testOutputHelper);

        // Act
        await engineCore?.Start()!;
        
        // Verify Signed Balances is empty
        var database = _fixture.GetService<IMongoDatabase>(_testOutputHelper);
        var collection = database.GetCollection<SignedEffectiveBalanceModel>(DbCollectionName.SignEffectiveBalance);
        var signedBalances = await collection.Find(_ => true).ToListAsync();
        Assert.Equal(0, signedBalances.Count);
        
        var effectiveBalances = database.GetCollection<EffectiveBalanceModel>(DbCollectionName.EffectiveBalance);
        var effectiveBalance = await effectiveBalances.Find(_ => true)
            .Sort(Builders<EffectiveBalanceModel>.Sort.Ascending(x => x.StartBlock))
            .ToListAsync();
        
        // Verify the effective balance is empty
        Assert.Equal(0, effectiveBalance.Count);
        
        // Verify the reward cycle is empty
        var rewardCycle = await database.GetCollection<RewardCycleModel>(DbCollectionName.RewardCycle).Find(_ => true).FirstOrDefaultAsync();
        Assert.Equal(1, rewardCycle.VtxDistributionId);
        Assert.Equal(88, rewardCycle.CurrentEraIndex);
        Assert.False(rewardCycle.CalculationComplete);
        
        // Verify Balances has a value
        var balances = await database.GetCollection<BalanceModel>(DbCollectionName.Balance).Find(_ => true).ToListAsync();
        Assert.Equal(1, balances.Count);
    }
} 