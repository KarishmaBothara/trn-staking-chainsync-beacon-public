using MatrixEngine.Core.Services;
using MatrixEngine.Core.Testing.Fixtures;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Extensions.Ordering;

namespace MatrixEngine.Core.Testing.Services;
[CollectionDefinition("Database Collection"), Order(7)]

public class SignEffectiveBalanceServiceTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    private Mock<ILogger<SignEffectiveBalanceService>> _logger;
    private SignEffectiveBalanceService _signEffectiveBalanceService;

    public SignEffectiveBalanceServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _logger = new Mock<ILogger<SignEffectiveBalanceService>>();
        _signEffectiveBalanceService = new SignEffectiveBalanceService(_fixture.Database, _logger.Object);
    }

    [Fact]
    public async Task LoadUnsignedEffectiveBalances_WhenDataExists()
    {
        // Arrange
        _fixture.BuildSignEffectiveBalanceData();

        // Act
        var result = await _signEffectiveBalanceService.LoadUnsignedEffectiveBalances();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        Assert.Contains(result, model => 
            (model.Account == "0xcb1de4FADCA68F601871f7E6E47fd43D707c7791" && model.EraIndex == 1)
        || (model.Account == "0xcb1de4FADCA68F601871f7E6E47fd43D707c7792" && model.EraIndex == 1));
    }
}