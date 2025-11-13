using MatrixEngine.Core.Constants;
using MatrixEngine.Core.Services;
using MatrixEngine.Core.Testing.Fixtures;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Extensions.Ordering;

namespace MatrixEngine.Core.Testing.Services;

[CollectionDefinition("Database Collection"), Order(5)]
public class StakerServiceTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public StakerServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetAccountType_WhenValidatorExists_ReturnsValidator()
    {
        _fixture.BuildStakerData();
        const string account = "0xA76bFE6d90952Eb0a3b8fA31dF2580692acEf772";
        const int eraIndex = 48;

        var loggerMock = new Mock<ILogger<StakerService>>();
        var stakerService = new StakerService(_fixture.Database, loggerMock.Object);

        var stakerType = await stakerService.GetAccountType(account, eraIndex);

        Assert.NotNull(stakerType);
        Assert.Equal(StakerType.Validator, stakerType);
        _fixture.ClearStakerData();
    }

    [Fact]
    public async Task GetAccountType_WhenStashDoesNotExist_ReturnsStaker()
    {
        _fixture.BuildStakerData();
        const string account = "0xA76bFE6d90952Eb0a3b8fA31dF2580692acEf772";
        const int eraIndex = 49;

        var loggerMock = new Mock<ILogger<StakerService>>();
        var stakerService = new StakerService(_fixture.Database, loggerMock.Object);

        var stakerType = await stakerService.GetAccountType(account, eraIndex);

        Assert.NotNull(stakerType);
        Assert.Equal(StakerType.Staker, stakerType);
        _fixture.ClearStakerData();
    }

    [Fact]
    public async Task GetAccountsStakerTypesByEraIndexes_WhenStashDoesNotExist_ReturnsStaker()
    {
        _fixture.BuildStakerData();
        var accounts = new List<string>
        {
            "0xA76bFE6d90952Eb0a3b8fA31dF2580692acEf772",
            "0x541F4DfED4656C65C9F3767bEfA350910A4ae41F",
        };
        
        var eraIndexes = new List<int> { 48,49 };
        
        var loggerMock = new Mock<ILogger<StakerService>>();
        var stakerService = new StakerService(_fixture.Database, loggerMock.Object);
        
        var stakers = await stakerService.GetAccountsStakerTypesByEraIndexes(accounts, eraIndexes);
        
        Assert.NotNull(stakers);
        Assert.Equal(3, stakers.Count);
    }
}