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

namespace MatrixEngine.Core.IntegrationTest.Tests.case_18;

[Order(3)]
public class TestCase19 : TestBed<IntegrationTestFixture>
{
    public TestCase19(ITestOutputHelper testOutputHelper, IntegrationTestFixture fixture) : base(testOutputHelper,
        fixture)
    {
    }

    // insert an era into the eras db
    private async Task InsertTestEra(int eraIndex)
    {
        var eraService = _fixture.GetService<IEraService>(_testOutputHelper);
        var newEra = new EraModel
        {
            EraIndex = eraIndex,
            StartBlock = eraIndex * 10,
            EndBlock = eraIndex * 10 + 9,
        };
        await eraService?.InsertEra(newEra)!;
    }

    [Fact]
    public async Task Test_Scenario_1()
    {
        // This test will test the reward cycle behaviour and ensure it does the following:
        // 1. Inserts -1 vtxDistributionId if the first era does not start at 0
        // 2. Updates the CurrentEraId to the latest era when processing a new era
        // 3. Sets the Reward Cycle parameters correctly when an era is completed and creates the next era
        
        var dataLoader = _fixture.GetService<IDataLoader>(_testOutputHelper);
        await dataLoader?.LoadCase("case-19")!;

        var engineCore = _fixture.GetService<IEngineCore>(_testOutputHelper);
        var signEffectiveBalanceService = _fixture.GetService<ISignEffectiveBalanceService>(_testOutputHelper);

        // run engine core with 10 eras, block 0 - 99
        await engineCore?.Start()!;

        // Get all signed effective balances
        var database = _fixture.GetService<IMongoDatabase>(_testOutputHelper);
        
        // Get reward cycle and ensure -1 index was inserted
        var collection = database.GetCollection<RewardCycleModel>(DbCollectionName.RewardCycle);
        var rewardCycles = await collection.Find(_ => true)
            .Sort(Builders<RewardCycleModel>.Sort.Ascending(x => x.StartBlock))
            .ToListAsync();
        Assert.Equal(2, rewardCycles.Count);
        
        // Verify the -1 cycle was correctly added up until the first era
        var rewardCycle1 = rewardCycles[0];
        Assert.Equal(-1, rewardCycle1.VtxDistributionId);
        Assert.Equal(0, rewardCycle1.StartBlock);
        Assert.Equal(99, rewardCycle1.EndBlock);
        Assert.Equal(0, rewardCycle1.StartEraIndex);
        Assert.Equal(9, rewardCycle1.EndEraIndex);
        
        // Verify second reward cycle
        var rewardCycle2 = rewardCycles[1];
        Assert.Equal(0, rewardCycle2.VtxDistributionId);
        Assert.False(rewardCycle2.CalculationComplete);
        Assert.Equal(100, rewardCycle2.StartBlock);
        Assert.Equal(-1, rewardCycle2.EndBlock); // end block not finished yet, set to -1
        Assert.Equal(10, rewardCycle2.StartEraIndex);
        Assert.Equal(-1, rewardCycle2.EndEraIndex); // end era not finished yet, set to -1
        Assert.Equal(10, rewardCycle2.CurrentEraIndex);
        
        // Insert one era and re-run the engine core
        await InsertTestEra(11);
        await engineCore?.Start()!;
        
        // Fetch reward cycles again to verify
        rewardCycles = await collection.Find(_ => true)
            .Sort(Builders<RewardCycleModel>.Sort.Ascending(x => x.StartBlock))
            .ToListAsync();
        Assert.Equal(2, rewardCycles.Count);
        
        // Verify the -1 cycle was unchanged
        rewardCycle1 = rewardCycles[0];
        Assert.Equal(-1, rewardCycle1.VtxDistributionId);
        Assert.Equal(0, rewardCycle1.StartBlock);
        Assert.Equal(99, rewardCycle1.EndBlock);
        Assert.Equal(0, rewardCycle1.StartEraIndex);
        Assert.Equal(9, rewardCycle1.EndEraIndex);
        
        // Verify second reward cycle, CurrentEraIndex should be updated to 11
        rewardCycle2 = rewardCycles[1];
        Assert.Equal(0, rewardCycle2.VtxDistributionId);
        Assert.False(rewardCycle2.CalculationComplete);
        Assert.Equal(100, rewardCycle2.StartBlock);
        Assert.Equal(-1, rewardCycle2.EndBlock); // end block not finished yet, set to -1
        Assert.Equal(10, rewardCycle2.StartEraIndex);
        Assert.Equal(-1, rewardCycle2.EndEraIndex); // end era not finished yet, set to -1
        Assert.Equal(11, rewardCycle2.CurrentEraIndex);
        
        // Add eras up until the last one, but not including the last one
        for (var i = 12; i < 99; i++)
        {
            await InsertTestEra(i);
        }
        await engineCore?.Start()!;
        
        // Fetch reward cycles again to verify
        rewardCycles = await collection.Find(_ => true)
            .Sort(Builders<RewardCycleModel>.Sort.Ascending(x => x.StartBlock))
            .ToListAsync();
        Assert.Equal(2, rewardCycles.Count);
        
        // Verify the first cycle currentEraIndex was updated
        rewardCycle2 = rewardCycles[1];
        Assert.Equal(0, rewardCycle2.VtxDistributionId);
        Assert.False(rewardCycle2.CalculationComplete);
        Assert.Equal(100, rewardCycle2.StartBlock);
        Assert.Equal(-1, rewardCycle2.EndBlock);
        Assert.Equal(10, rewardCycle2.StartEraIndex);
        Assert.Equal(-1, rewardCycle2.EndEraIndex);
        Assert.Equal(98, rewardCycle2.CurrentEraIndex);
        
        // Add the last era and check it was complete
        await InsertTestEra(99);
        await engineCore?.Start()!;
        // Fetch reward cycles again to verify
        rewardCycles = await collection.Find(_ => true)
            .Sort(Builders<RewardCycleModel>.Sort.Ascending(x => x.StartBlock))
            .ToListAsync();
        
        // Next one should have been created
        Assert.Equal(3, rewardCycles.Count);
        
        // Verify the first cycle currentEraIndex was updated
        rewardCycle2 = rewardCycles[1];
        Assert.Equal(0, rewardCycle2.VtxDistributionId);        
        Assert.True(rewardCycle2.CalculationComplete);
        Assert.Equal(100, rewardCycle2.StartBlock);
        Assert.Equal(999, rewardCycle2.EndBlock); // end block should be set to 999
        Assert.Equal(10, rewardCycle2.StartEraIndex);
        Assert.Equal(99, rewardCycle2.EndEraIndex); // end era should be set to 99
        Assert.Equal(99, rewardCycle2.CurrentEraIndex); // current era should be set to 99
        
        // Verify the new cycle was created
        var rewardCycle3 = rewardCycles[2];
        Assert.Equal(1, rewardCycle3.VtxDistributionId);
        Assert.False(rewardCycle3.CalculationComplete);
        Assert.Equal(1000, rewardCycle3.StartBlock);
        Assert.Equal(-1, rewardCycle3.EndBlock); // end block not finished yet, set to -1
        Assert.Equal(100, rewardCycle3.StartEraIndex);
        Assert.Equal(-1, rewardCycle3.EndEraIndex); // end era not finished yet, set to -1
        Assert.Equal(100, rewardCycle3.CurrentEraIndex); // current era should be set to 100
        
        // Add another 90 eras and check
        for (var i = 100; i < 190; i++)
        {
            await InsertTestEra(i);
        }
        await engineCore?.Start()!;
        
        // Fetch reward cycles again to verify
        rewardCycles = await collection.Find(_ => true)
            .Sort(Builders<RewardCycleModel>.Sort.Ascending(x => x.StartBlock))
            .ToListAsync();
        
        // Verify the new cycle was created
        Assert.Equal(4, rewardCycles.Count);
        
        // Verify the new cycle was completed
        rewardCycle3 = rewardCycles[2];
        Assert.Equal(1, rewardCycle3.VtxDistributionId);
        Assert.True(rewardCycle3.CalculationComplete);
        Assert.Equal(1000, rewardCycle3.StartBlock);
        Assert.Equal(1899, rewardCycle3.EndBlock); // end block not finished yet, set to -1
        Assert.Equal(100, rewardCycle3.StartEraIndex);
        Assert.Equal(189, rewardCycle3.EndEraIndex); // end era not finished yet, set to -1
        Assert.Equal(189, rewardCycle3.CurrentEraIndex); // current era should be set to 100
        
    }
}