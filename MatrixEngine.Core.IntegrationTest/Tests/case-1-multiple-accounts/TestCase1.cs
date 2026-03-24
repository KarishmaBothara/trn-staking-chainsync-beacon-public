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

namespace MatrixEngine.Core.IntegrationTest.Tests.case_1_multiple_accounts;

[Order(3)]
public class TestCase1 : TestBed<IntegrationTestFixture>
{
    public TestCase1(ITestOutputHelper testOutputHelper, IntegrationTestFixture fixture) : base(testOutputHelper,
        fixture)
    {
    }

    [Fact]
    public async Task Test_Scenario_1()
    {
        // This test case will test the effective balances of multiple accounts in one cycle
        // Arrange
        var dataLoader = _fixture.GetService<IDataLoader>(_testOutputHelper);
        await dataLoader?.LoadCase("case-1")!;

        var engineCore = _fixture.GetService<IEngineCore>(_testOutputHelper);
        var signEffectiveBalanceService = _fixture.GetService<ISignEffectiveBalanceService>(_testOutputHelper);

        // Act
        await engineCore?.Start()!;
        
        // Get all signed effective balances
        var database = _fixture.GetService<IMongoDatabase>(_testOutputHelper);
        var collection = database.GetCollection<SignedEffectiveBalanceModel>(DbCollectionName.SignEffectiveBalance);
        var signedBalances = await collection.Find(_ => true).ToListAsync();
        
        // Verify signed balances
        Assert.Equal(5, signedBalances.Count);
        
        // Verify first signed balance
        var signedBalance1 = signedBalances[0];
        Assert.Equal("0x0000000000000000000000000000000000000000", signedBalance1.Account);
        Assert.Equal(0, signedBalance1.StartBlock);
        Assert.Equal(899, signedBalance1.EndBlock);
        Assert.Equal(0, signedBalance1.VtxDistributionId);
        Assert.Equal("1223165", signedBalance1.TotalRewardPoints);
        
        // Verify second signed balance
        var signedBalance2 = signedBalances[1];
        Assert.Equal("0x1111111111111111111111111111111111111111", signedBalance2.Account);
        Assert.Equal(0, signedBalance2.StartBlock);
        Assert.Equal(899, signedBalance2.EndBlock);
        Assert.Equal(0, signedBalance2.VtxDistributionId);
        Assert.Equal("2496898", signedBalance2.TotalRewardPoints);
        
        // Verify third signed balance
        var signedBalance3 = signedBalances[2];
        Assert.Equal("0x2222222222222222222222222222222222222222", signedBalance3.Account);
        Assert.Equal(0, signedBalance3.StartBlock);
        Assert.Equal(899, signedBalance3.EndBlock);
        Assert.Equal(0, signedBalance3.VtxDistributionId);
        Assert.Equal("478333", signedBalance3.TotalRewardPoints);
        
        // Verify fourth signed balance
        var signedBalance4 = signedBalances[3];
        Assert.Equal("0x3333333333333333333333333333333333333333", signedBalance4.Account);
        Assert.Equal(0, signedBalance4.StartBlock);
        Assert.Equal(899, signedBalance4.EndBlock);
        Assert.Equal(0, signedBalance4.VtxDistributionId);
        Assert.Equal("709299", signedBalance4.TotalRewardPoints);
        
        // Verify fifth signed balance
        var signedBalance5 = signedBalances[4];
        Assert.Equal("0x4444444444444444444444444444444444444444", signedBalance5.Account);
        Assert.Equal(0, signedBalance5.StartBlock);
        Assert.Equal(899, signedBalance5.EndBlock);
        Assert.Equal(0, signedBalance5.VtxDistributionId);
        Assert.Equal("934800", signedBalance5.TotalRewardPoints);
        
        var effectiveBalances = database.GetCollection<EffectiveBalanceModel>(DbCollectionName.EffectiveBalance);
        var effectiveBalance = await effectiveBalances.Find(_ => true)
            .Sort(Builders<EffectiveBalanceModel>.Sort.Ascending(x => x.StartBlock))
            .ToListAsync();
        
        // Verify the effective balance is as expected
        Assert.Equal(8, effectiveBalance.Count);
        
        // Account 0x0000 (5-79)
        var entry1 = effectiveBalance[0];
        Assert.Equal("0x0000000000000000000000000000000000000000", entry1.Account);
        Assert.Equal(5, entry1.StartBlock);
        Assert.Equal(79, entry1.EndBlock);
        Assert.Equal(75, entry1.EffectiveBlocks);
        Assert.Equal(0, entry1.VtxDistributionId);
        Assert.Equal(0.0833333, entry1.Percentage);
        // Bonded
        Assert.Equal("100000000", entry1.Bonded.Balance);
        Assert.Equal("50000000", entry1.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", entry1.Bonded.Rate.ToString());
        Assert.Equal("Staker", entry1.Bonded.StakerType);
        Assert.Equal("102499", entry1.Bonded.RewardPoints);
        
        // Account 0x1111 (15-74)
        var entry2 = effectiveBalance[1];
        Assert.Equal("0x1111111111111111111111111111111111111111", entry2.Account);
        Assert.Equal(15, entry2.StartBlock);
        Assert.Equal(74, entry2.EndBlock);
        Assert.Equal(60, entry2.EffectiveBlocks);
        Assert.Equal(0, entry2.VtxDistributionId);
        Assert.Equal(0.0666667, entry2.Percentage);
        // Bonded
        Assert.Equal("10000000", entry2.Bonded.Balance);
        Assert.Equal("10000000", entry2.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", entry2.Bonded.Rate.ToString());
        Assert.Equal("Staker", entry2.Bonded.StakerType);
        Assert.Equal("16399", entry2.Bonded.RewardPoints);
        
        // Account 0x2222 (25-899)
        var entry3 = effectiveBalance[2];
        Assert.Equal("0x2222222222222222222222222222222222222222", entry3.Account);
        Assert.Equal(25, entry3.StartBlock);
        Assert.Equal(899, entry3.EndBlock);
        Assert.Equal(875, entry3.EffectiveBlocks);
        Assert.Equal(0, entry3.VtxDistributionId);
        Assert.Equal(0.9722222, entry3.Percentage);
        // Bonded
        Assert.Equal("20000000", entry3.Bonded.Balance);
        Assert.Equal("20000000", entry3.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", entry3.Bonded.Rate.ToString());
        Assert.Equal("Staker", entry3.Bonded.StakerType);
        Assert.Equal("478333", entry3.Bonded.RewardPoints);
        
        // Account 0x3333 (35-899)
        var entry4 = effectiveBalance[3];
        Assert.Equal("0x3333333333333333333333333333333333333333", entry4.Account);
        Assert.Equal(35, entry4.StartBlock);
        Assert.Equal(899, entry4.EndBlock);
        Assert.Equal(865, entry4.EffectiveBlocks);
        Assert.Equal(0, entry4.VtxDistributionId);
        Assert.Equal(0.9611111, entry4.Percentage);
        // Bonded
        Assert.Equal("30000000", entry4.Bonded.Balance);
        Assert.Equal("30000000", entry4.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", entry4.Bonded.Rate.ToString());
        Assert.Equal("Staker", entry4.Bonded.StakerType);
        Assert.Equal("709299", entry4.Bonded.RewardPoints);
        
        // Account 0x4444 (45-899)
        var entry5 = effectiveBalance[4];
        Assert.Equal("0x4444444444444444444444444444444444444444", entry5.Account);
        Assert.Equal(45, entry5.StartBlock);
        Assert.Equal(899, entry5.EndBlock);
        Assert.Equal(855, entry5.EffectiveBlocks);
        Assert.Equal(0, entry5.VtxDistributionId);
        Assert.Equal(0.95, entry5.Percentage);
        // Bonded
        Assert.Equal("40000000", entry5.Bonded.Balance);
        Assert.Equal("40000000", entry5.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", entry5.Bonded.Rate.ToString());
        Assert.Equal("Staker", entry5.Bonded.StakerType);
        Assert.Equal("934800", entry5.Bonded.RewardPoints);
        
        // Account 0x1111 (75-899)
        var entry6 = effectiveBalance[5];
        Assert.Equal("0x1111111111111111111111111111111111111111", entry6.Account);
        Assert.Equal(75, entry6.StartBlock);
        Assert.Equal(899, entry6.EndBlock);
        Assert.Equal(825, entry6.EffectiveBlocks);
        Assert.Equal(0, entry6.VtxDistributionId);
        Assert.Equal(0.9166667, entry6.Percentage);
        // Bonded
        Assert.Equal("110000000", entry6.Bonded.Balance);
        Assert.Equal("110000000", entry6.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", entry6.Bonded.Rate.ToString());
        Assert.Equal("Staker", entry6.Bonded.StakerType);
        Assert.Equal("2480499", entry6.Bonded.RewardPoints);
        
        // Account 0x0000 (80-84) with unlocking balance
        var entry7 = effectiveBalance[6];
        Assert.Equal("0x0000000000000000000000000000000000000000", entry7.Account);
        Assert.Equal(80, entry7.StartBlock);
        Assert.Equal(84, entry7.EndBlock);
        Assert.Equal(5, entry7.EffectiveBlocks);
        Assert.Equal(0, entry7.VtxDistributionId);
        Assert.Equal(0.0055556, entry7.Percentage);
        // Bonded
        Assert.Equal("50000000", entry7.Bonded.Balance);
        Assert.Equal("50000000", entry7.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", entry7.Bonded.Rate.ToString());
        Assert.Equal("Staker", entry7.Bonded.StakerType);
        Assert.Equal("6833", entry7.Bonded.RewardPoints);
        // Unlocking
        Assert.Equal("50000000", entry7.Unlocking.Balance);
        Assert.Equal("0", entry7.Unlocking.EffectiveBalance);
        Assert.Equal("0.0246", entry7.Unlocking.Rate.ToString());
        Assert.Equal("Staker", entry7.Unlocking.StakerType);
        Assert.Equal("0", entry7.Unlocking.RewardPoints);
        
        // Account 0x0000 (85-899)
        var entry8 = effectiveBalance[7];
        Assert.Equal("0x0000000000000000000000000000000000000000", entry8.Account);
        Assert.Equal(85, entry8.StartBlock);
        Assert.Equal(899, entry8.EndBlock);
        Assert.Equal(815, entry8.EffectiveBlocks);
        Assert.Equal(0, entry8.VtxDistributionId);
        Assert.Equal(0.9055556, entry8.Percentage);
        // Bonded
        Assert.Equal("50000000", entry8.Bonded.Balance);
        Assert.Equal("50000000", entry8.Bonded.EffectiveBalance);
        Assert.Equal("0.0246", entry8.Bonded.Rate.ToString());
        Assert.Equal("Staker", entry8.Bonded.StakerType);
        Assert.Equal("1113833", entry8.Bonded.RewardPoints);
        // Unlocking
        Assert.Equal("0", entry8.Unlocking.Balance);
        Assert.Equal("0", entry8.Unlocking.EffectiveBalance);
    }
}