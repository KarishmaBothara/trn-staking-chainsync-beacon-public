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

namespace MatrixEngine.Core.IntegrationTest.Tests.case_13_multiple_stakers;

[Order(3)]
public class TestCase16 : TestBed<IntegrationTestFixture>
{
    public TestCase16(ITestOutputHelper testOutputHelper, IntegrationTestFixture fixture) : base(testOutputHelper,
        fixture)
    {
    }

    [Fact]
    public async Task Test_Scenario_1()
    {
        // This test case will perform multiple bonded and unbonded events, lowering the max effective balance
        // twice in one reward cycle. The effective balance should be:
        // block range    bonded eff bal
        // 0 - 9             600
        // 10 - 29           800
        // 30 ->             1100

        var dataLoader = _fixture.GetService<IDataLoader>(_testOutputHelper);
        await dataLoader?.LoadCase("case-16")!;

        var engineCore = _fixture.GetService<IEngineCore>(_testOutputHelper);
        var signEffectiveBalanceService = _fixture.GetService<ISignEffectiveBalanceService>(_testOutputHelper);

        // Act
        await engineCore?.Start()!;

        // Get all signed effective balances
        var database = _fixture.GetService<IMongoDatabase>(_testOutputHelper);
        var collection = database.GetCollection<SignedEffectiveBalanceModel>(DbCollectionName.SignEffectiveBalance);
        // var signedBalances = await collection.Find(_ => true)
        //     .Sort(Builders<SignedEffectiveBalanceModel>.Sort.Ascending(x => x.StartBlock).Ascending(x => x.VtxDistributionId))
        //     .ToListAsync();
        // Assert.Equal(2, signedBalances.Count);
        //
        // // Verify first signed balance
        // var signedBalance1 = signedBalances[0];
        // Assert.Equal("0xE04CC55ebEE1cBCE552f250e85c57B70B2E2625b", signedBalance1.Account);
        // Assert.Equal(0, signedBalance1.StartBlock);
        // Assert.Equal(899, signedBalance1.EndBlock);
        // Assert.Equal(0, signedBalance1.VtxDistributionId);
        // Assert.Equal("196", signedBalance1.TotalRewardPoints);
        // Assert.False(signedBalance1.Submitted);
        // Assert.False(signedBalance1.Verified);
        //
        // // Verify second signed balance
        // var signedBalance2 = signedBalances[1];
        // Assert.Equal("0xE04CC55ebEE1cBCE552f250e85c57B70B2E2625b", signedBalance2.Account);
        // Assert.Equal(900, signedBalance2.StartBlock);
        // Assert.Equal(1799, signedBalance2.EndBlock);
        // Assert.Equal(1, signedBalance2.VtxDistributionId);
        // Assert.Equal("442", signedBalance2.TotalRewardPoints);
        // Assert.False(signedBalance2.Submitted);
        // Assert.False(signedBalance2.Verified);
        //
        // // Now verify the balance changes, there should only be 2 entries as the changes occured on 2 blocks
        // var balanceChanges = database.GetCollection<BalanceChangeModel>(DbCollectionName.Balance);
        // var balanceChange = await balanceChanges.Find(_ => true)
        //     .Sort(Builders<BalanceChangeModel>.Sort.Ascending(x => x.StartBlock))
        //     .ToListAsync();
        //
        // // Verify the balance change records
        // Assert.Equal(2, balanceChange.Count);
        //
        // // First balance change entry (0-899)
        // // There was the following events on block 0:
        // //    Event               Bonded Bal     Unbonded Bal
        // // 1. Bonded 10000         10000          0
        // // 2. Unbonded 5000        5000           5000
        // // 3. Withdrawn 2000       5000           3000
        // // 4. ReBonded 2000        7000           1000
        // // These should all be summed together and the final values inserted in the balance entry for block 0
        // var balanceEntry1 = balanceChange[0];
        // Assert.Equal("0xE04CC55ebEE1cBCE552f250e85c57B70B2E2625b", balanceEntry1.Account);
        // Assert.Equal(0, balanceEntry1.StartBlock);
        // Assert.Equal(899, balanceEntry1.EndBlock);
        // // Bonded balances
        // Assert.Equal("0", balanceEntry1.Bonded.PreviousBalance.ToString());
        // Assert.Equal("7000", balanceEntry1.Bonded.BalanceChange.ToString());
        // Assert.Equal("7000", balanceEntry1.Bonded.BalanceInBlockRange.ToString());
        // Assert.Equal("Staker", balanceEntry1.Bonded.StakerType);
        // // Unlocking balances
        // Assert.Equal("0", balanceEntry1.Unlocking.PreviousBalance.ToString());
        // Assert.Equal("1000", balanceEntry1.Unlocking.BalanceChange.ToString());
        // Assert.Equal("1000", balanceEntry1.Unlocking.BalanceInBlockRange.ToString());
        // Assert.Equal("Staker", balanceEntry1.Unlocking.StakerType);
        //
        // // Second balance change entry (900-1799)
        // // On block 900, the new reward cycle starts. This means that the previous balance is carried over
        // // We also had the following 2 events on the first block of the cycle:
        // //   Event               Bonded Bal     Unbonded Bal
        // // 1. Bonded 10000         10000          0
        // // 2. Unbonded 1000        9000           1000
        // // This should be added onto the previous balance entry which was
        // //    Bonded 7000, Unbonded 1000
        // var balanceEntry2 = balanceChange[1];
        // Assert.Equal("0xE04CC55ebEE1cBCE552f250e85c57B70B2E2625b", balanceEntry2.Account);
        // Assert.Equal(900, balanceEntry2.StartBlock);
        // Assert.Equal(1799, balanceEntry2.EndBlock);
        // // Bonded balances
        // Assert.Equal("7000", balanceEntry2.Bonded.PreviousBalance.ToString());
        // Assert.Equal("9000", balanceEntry2.Bonded.BalanceChange.ToString());
        // Assert.Equal("16000", balanceEntry2.Bonded.BalanceInBlockRange.ToString());
        // Assert.Equal("Staker", balanceEntry2.Bonded.StakerType);
        // // Unlocking balances
        // Assert.Equal("1000", balanceEntry2.Unlocking.PreviousBalance.ToString());
        // Assert.Equal("1000", balanceEntry2.Unlocking.BalanceChange.ToString());
        // Assert.Equal("2000", balanceEntry2.Unlocking.BalanceInBlockRange.ToString());
        // Assert.Equal("Staker", balanceEntry2.Unlocking.StakerType);
        //
        // // Now verify the effective balances
        // var effectiveBalances = database.GetCollection<EffectiveBalanceModel>(DbCollectionName.EffectiveBalance);
        // var effectiveBalance = await effectiveBalances.Find(_ => true)
        //     .Sort(Builders<EffectiveBalanceModel>.Sort.Ascending(x => x.StartBlock))
        //     .ToListAsync();
        //
        // // Verify the effective balance is as expected
        // Assert.Equal(2, effectiveBalance.Count);
        //
        // // Account 0xE04CC55... (0-899)
        // var entry1 = effectiveBalance[0];
        // Assert.Equal("0xE04CC55ebEE1cBCE552f250e85c57B70B2E2625b", entry1.Account);
        // Assert.Equal(0, entry1.StartBlock);
        // Assert.Equal(899, entry1.EndBlock);
        // Assert.Equal(900, entry1.EffectiveBlocks);
        // Assert.Equal(0, entry1.VtxDistributionId);
        // Assert.Equal(1.0, entry1.Percentage);
        // // Bonded
        // Assert.Equal("7000", entry1.Bonded.Balance);
        // Assert.Equal("7000", entry1.Bonded.EffectiveBalance);
        // Assert.Equal("0.0246", entry1.Bonded.Rate.ToString());
        // Assert.Equal("Staker", entry1.Bonded.StakerType);
        // Assert.Equal("172", entry1.Bonded.RewardPoints);
        // // Unlocking
        // Assert.Equal("1000", entry1.Unlocking.Balance);
        // Assert.Equal("1000", entry1.Unlocking.EffectiveBalance);
        // Assert.Equal("0.0246", entry1.Unlocking.Rate.ToString());
        // Assert.Equal("Staker", entry1.Unlocking.StakerType);
        // Assert.Equal("24", entry1.Unlocking.RewardPoints);
        //
        // // Account 0xE04CC55... (900-1799)
        // var entry2 = effectiveBalance[1];
        // Assert.Equal("0xE04CC55ebEE1cBCE552f250e85c57B70B2E2625b", entry2.Account);
        // Assert.Equal(900, entry2.StartBlock);
        // Assert.Equal(1799, entry2.EndBlock);
        // Assert.Equal(900, entry2.EffectiveBlocks);
        // Assert.Equal(1, entry2.VtxDistributionId);
        // Assert.Equal(1.0, entry2.Percentage);
        // // Bonded
        // Assert.Equal("16000", entry2.Bonded.Balance);
        // Assert.Equal("16000", entry2.Bonded.EffectiveBalance);
        // Assert.Equal("0.0246", entry2.Bonded.Rate.ToString());
        // Assert.Equal("Staker", entry2.Bonded.StakerType);
        // Assert.Equal("393", entry2.Bonded.RewardPoints);
        // // Unlocking
        // Assert.Equal("2000", entry2.Unlocking.Balance);
        // Assert.Equal("2000", entry2.Unlocking.EffectiveBalance);
        // Assert.Equal("0.0246", entry2.Unlocking.Rate.ToString());
        // Assert.Equal("Staker", entry2.Unlocking.StakerType);
        // Assert.Equal("49", entry2.Unlocking.RewardPoints);
        //
        // // Verify the total reward points match
        // Assert.Equal(signedBalance1.TotalRewardPoints, (int.Parse(entry1.Bonded.RewardPoints) + int.Parse(entry1.Unlocking.RewardPoints)).ToString());
        // Assert.Equal(signedBalance2.TotalRewardPoints, (int.Parse(entry2.Bonded.RewardPoints) + int.Parse(entry2.Unlocking.RewardPoints)).ToString());
    }
}