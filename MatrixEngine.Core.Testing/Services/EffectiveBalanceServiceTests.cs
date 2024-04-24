using MatrixEngine.Core.Models;
using MatrixEngine.Core.Services;
using MatrixEngine.Core.Testing.Fixtures;
using Xunit.Extensions.Ordering;

namespace MatrixEngine.Core.Testing.Services;

[CollectionDefinition("Database Collection"), Order(2)]
public class EffectiveBalanceServiceTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    private readonly EffectiveBalanceService _effectiveBalanceService;

    public EffectiveBalanceServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _effectiveBalanceService = new EffectiveBalanceService(_fixture.Database);
    }

    [Fact]
    public async Task GetEffectiveBalanceByAddress_WhenEffectiveBalanceExists_ReturnsEffectiveBalance()
    {
        // Arrange
        _fixture.BuildEffectiveBalanceData();
        const string account = "0xcb1de4FADCA68F601871f7E6E47fd43D707c779A";

        //Act
        var effectiveBalance = await _effectiveBalanceService.GetEffectiveBalancesByAccount(account);

        //Assert
        Assert.NotNull(effectiveBalance);
        Assert.Equal(183, effectiveBalance.Count);
        Assert.Equal(effectiveBalance[0].Account, account);
        _fixture.ClearEraData();
    }

    [Fact]
    public async Task UpsertEffectiveBalance_WhenEffectiveBalanceExists_UpsertRecordSuccess()
    {
        _fixture.BuildEffectiveBalanceData();
        var file = @"Data/update-effective-balances.json";
        var upsertData = JsonFileReader.Read<List<EffectiveBalanceModel>>(file);

        await _effectiveBalanceService.UpsertEffectiveBalance(upsertData);

        //Assert account 0xcb1de4FADCA68F601871f7E6E47fd43D707c779A update
        var account1 =
            await _effectiveBalanceService.GetEffectiveBalancesByAccount("0xcb1de4FADCA68F601871f7E6E47fd43D707c779A");

        var updateRecord1 = account1.FirstOrDefault(m => m.StartBlock == 7035347 && m.EndBlock == 7056944);
        Assert.NotNull(updateRecord1);
        Assert.Equal("10000000000", updateRecord1?.Balance);
        Assert.Equal("1111111111", updateRecord1?.EffectiveBalance);

        var updateRecord2= account1.FirstOrDefault(m => m.StartBlock == 7056945 && m.EndBlock == 7078543);
        Assert.NotNull(updateRecord1);
        Assert.Equal("10000000000", updateRecord1?.Balance);
        Assert.Equal("1111111111", updateRecord1?.EffectiveBalance);

        //Assert account 0xcb1de4FADCA68F601871f7E6E47fd43D707c779B insert
        var account2 =
            await _effectiveBalanceService.GetEffectiveBalancesByAccount("0xcb1de4FADCA68F601871f7E6E47fd43D707c779B");

        var insertRecord = account2.FirstOrDefault(m => m.StartBlock == 7056945 && m.EndBlock == 7078543);
        
        Assert.NotNull(insertRecord);
        Assert.Equal("10000000000", insertRecord?.Balance);
        Assert.Equal("1111111111", insertRecord?.EffectiveBalance);
        _fixture.ClearEffectiveBalanceData();
    }
}