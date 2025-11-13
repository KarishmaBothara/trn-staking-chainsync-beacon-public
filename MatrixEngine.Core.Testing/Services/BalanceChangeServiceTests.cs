using MatrixEngine.Core.Models;
using MatrixEngine.Core.Services;
using MatrixEngine.Core.Testing.Fixtures;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Extensions.Ordering;

namespace MatrixEngine.Core.Testing.Services;

[CollectionDefinition("Database Collection"), Order(1)]
public class BalanceChangeServiceTests: IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    private readonly Mock<ILogger<BalanceChangeService>> _logger;
    private readonly BalanceChangeService _balanceChangeService;

    public BalanceChangeServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _logger = new Mock<ILogger<BalanceChangeService>>();
        _balanceChangeService = new BalanceChangeService(_fixture.Database, _logger.Object);
    }

    [Fact]
    public async Task UpsertUserBalanceChanges_WhenBalanceChangeExisting()
    {
        // Arrange
        _fixture.BuildBalanceChangeData();
        var changes = new List<BalanceChangeModel>
        {
            new()
            {
                Account = "0xcb1de4FADCA68F601871f7E6E47fd43D707c779A",
                StartBlock = 7035347,
                EndBlock = 8851596,
                BalanceInBlockRange = 50000000000
            },
            new()
            {
                Account = "0xcb1de4FADCA68F601871f7E6E47fd43D707c779A",
                StartBlock = 8851596,
                EndBlock = 8973463,
                BalanceInBlockRange = 50000000000
            }
        };
        
        await _balanceChangeService.UpsertUserBalanceChanges(changes);
        
        //Assert by querying the data
        var balanceModel1 = await _balanceChangeService.GetBalanceChanges("0xcb1de4FADCA68F601871f7E6E47fd43D707c779A", 7035347, 8851596);
        Assert.NotNull(balanceModel1);
        Assert.Single(balanceModel1);
        Assert.Equal("50000000000", balanceModel1[0].Balance);
        
        var balanceModel2 = await _balanceChangeService.GetBalanceChanges("0xcb1de4FADCA68F601871f7E6E47fd43D707c779A", 8851596, 8973463);
        Assert.NotNull(balanceModel2);
        Assert.Single(balanceModel2);
        Assert.Equal("50000000000", balanceModel2[0].Balance);
    }
    
    [Fact]
    public async Task GetTotalStakedBalance_WhenBalanceChangeExisting()
    {
        // Arrange
        _fixture.BuildBalanceChangeData();
        var changes = new List<BalanceChangeModel>
        {
            new()
            {
                Account = "0xcb1de4FADCA68F601871f7E6E47fd43D707c779A",
                StartBlock = 7035347,
                EndBlock = 8851596,
                BalanceInBlockRange = 50000000000
            },
            new()
            {
                Account = "0xcb1de4FADCA68F601871f7E6E47fd43D707c779A",
                StartBlock = 8851596,
                EndBlock = 8973463,
                BalanceInBlockRange = 50000000000
            }
        };
        
        await _balanceChangeService.UpsertUserBalanceChanges(changes);
        
        //Act
        var totalStakedBalance = await _balanceChangeService.GetTotalStakedBalance(7035347, 8973463);
        
        //Assert
        Assert.NotNull(totalStakedBalance);
        Assert.Equal("100000000000", totalStakedBalance);
    }
}
