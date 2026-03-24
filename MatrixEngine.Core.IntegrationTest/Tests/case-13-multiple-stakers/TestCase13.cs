using MatrixEngine.Core.Engine;
using MatrixEngine.Core.IntegrationTest.Fixtures;
using Xunit.Abstractions;
using Xunit.Extensions.Ordering;
using Xunit.Microsoft.DependencyInjection.Abstracts;
using MongoDB.Driver;
using MatrixEngine.Core.Services;
using MatrixEngine.Core.Models;
using MatrixEngine.Core.Constants;

namespace MatrixEngine.Core.IntegrationTest.Tests.case_13_multiple_stakers;

[Order(3)]
public class TestCase13 : TestBed<IntegrationTestFixture>
{
    public TestCase13(ITestOutputHelper testOutputHelper, IntegrationTestFixture fixture) : base(testOutputHelper,
        fixture)
    {
    }

    [Fact]
    public async Task Test_Scenario_1()
    {
        // This test scenario tests multiple accounts, each one a staker. Used for testing with Validator
        
        var dataLoader = _fixture.GetService<IDataLoader>(_testOutputHelper);
        await dataLoader?.LoadCase("case-13")!;

        var engineCore = _fixture.GetService<IEngineCore>(_testOutputHelper);
        var signEffectiveBalanceService = _fixture.GetService<ISignEffectiveBalanceService>(_testOutputHelper);

        // Act
        await engineCore?.Start()!;
        
        // Get all signed effective balances
        var database = _fixture.GetService<IMongoDatabase>(_testOutputHelper);
        var collection = database.GetCollection<SignedEffectiveBalanceModel>(DbCollectionName.SignEffectiveBalance);
        var signedBalances = await collection.Find(_ => true).ToListAsync();
        Assert.Equal(5, signedBalances.Count);
        
        // Verify first signed balance
        var signedBalance1 = signedBalances[0];
        Assert.Equal("0xE04CC55ebEE1cBCE552f250e85c57B70B2E2625b", signedBalance1.Account);
        Assert.Equal(0, signedBalance1.StartBlock);
        Assert.Equal(899, signedBalance1.EndBlock);
        Assert.Equal(-1, signedBalance1.VtxDistributionId);
        Assert.Equal("2460000", signedBalance1.TotalRewardPoints);
        Assert.False(signedBalance1.Submitted);
        Assert.False(signedBalance1.Verified);
        
        // Verify second signed balance
        var signedBalance2 = signedBalances[1];
        Assert.Equal("0x25451A4de12dcCc2D166922fA938E900fCc4ED24", signedBalance2.Account);
        Assert.Equal(0, signedBalance2.StartBlock);
        Assert.Equal(899, signedBalance2.EndBlock);
        Assert.Equal(-1, signedBalance2.VtxDistributionId);
        Assert.Equal("4914532", signedBalance2.TotalRewardPoints);
        Assert.False(signedBalance2.Submitted);
        Assert.False(signedBalance2.Verified);
        
        // Verify third signed balance
        var signedBalance3 = signedBalances[2];
        Assert.Equal("0xE04CC55ebEE1cBCE552f250e85c57B70B2E2625b", signedBalance3.Account);
        Assert.Equal(900, signedBalance3.StartBlock);
        Assert.Equal(1799, signedBalance3.EndBlock);
        Assert.Equal(0, signedBalance3.VtxDistributionId);
        Assert.Equal("120539986", signedBalance3.TotalRewardPoints);
        Assert.False(signedBalance3.Submitted);
        Assert.False(signedBalance3.Verified);
        
        // Verify fourth signed balance
        var signedBalance5 = signedBalances[3];
        Assert.Equal("0x25451A4de12dcCc2D166922fA938E900fCc4ED24", signedBalance5.Account);
        Assert.Equal(900, signedBalance5.StartBlock);
        Assert.Equal(1799, signedBalance5.EndBlock);
        Assert.Equal(0, signedBalance5.VtxDistributionId);
        Assert.Equal("2459998", signedBalance5.TotalRewardPoints);
        Assert.False(signedBalance5.Submitted);
        Assert.False(signedBalance5.Verified);
        
        // Verify fifth signed balance
        var signedBalance4 = signedBalances[4];
        Assert.Equal("0xf24FF3a9CF04c71Dbc94D0b566f7A27B94566cac", signedBalance4.Account);
        Assert.Equal(900, signedBalance4.StartBlock);
        Assert.Equal(1799, signedBalance4.EndBlock);
        Assert.Equal(0, signedBalance4.VtxDistributionId);
        Assert.Equal("1279200000", signedBalance4.TotalRewardPoints);
        Assert.False(signedBalance4.Submitted);
        Assert.False(signedBalance4.Verified);
    }
}