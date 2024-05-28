using MatrixEngine.Core.Exceptions;
using MatrixEngine.Core.GraphQL.ActiveEras;
using MatrixEngine.Core.Services;
using MatrixEngine.Core.Testing.Fixtures;
using Xunit.Extensions.Ordering;

namespace MatrixEngine.Core.Testing.Services;

[CollectionDefinition("Database Collection"), Order(3)]
public class EraServiceTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    private readonly EraService _eraService;

    public EraServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _eraService = new EraService(_fixture.Database);
    }

    [Fact]
    public async Task GetEraByIndex_WhenEraExists_ReturnsEra()
    {
        // Arrange
        _fixture.BuildEraData();
        const int eraIndex = 1;

        // Act
        var era = await _eraService.GetEraByIndex(eraIndex);

        // Assert
        Assert.NotNull(era);
        Assert.Equal(era.EraIndex, eraIndex);
    }

    [Fact]
    public async Task GetEraByIndex_WhenEraDoesNotExist_ThrowException()
    {
        // Arrange
        _fixture.ClearEraData();
        const int eraIndex = 1;

        // Act
        async Task Act() => await _eraService.GetEraByIndex(eraIndex);

        // Assert
        await Assert.ThrowsAsync<EraException>(Act);
    }

    [Fact]
    public async Task GetLatestEra_WhenEraExists_ReturnsLatestEra()
    {
        // Arrange
        const int expectedEraIndex = 440;
        const int expectedStartBlock = 10937072;
        const int expectedEndBlock = 10958671;

        _fixture.BuildEraData();

        // Act
        var latestEra = await _eraService.GetLatestFinishedEra();

        // Assert
        Assert.NotNull(latestEra);
        Assert.Equal(latestEra.EraIndex, expectedEraIndex);
        Assert.Equal(latestEra.StartBlock, expectedStartBlock);
        Assert.Equal(latestEra.EndBlock, expectedEndBlock);
    }

    [Fact]
    public async Task GetLatestEra_WhenEraDoesNotExists_ThrowException()
    {
        // Arrange
        _fixture.ClearEraData();

        // Act
        async Task Act() => await _eraService.GetLatestFinishedEra();

        // Assert
        await Assert.ThrowsAsync<EraException>(Act);
    }

    [Fact]
    public async Task ResolveEraFromActiveEras_WhenActiveEraTypesExisting()
    {
        _fixture.ClearEraData();
        var activeEraTypes = new List<ActiveEraType>()
        {
            new()
            {
                EraIndex = 0,
                BlockNumber = 1120840,
                Timestamp = DateTime.Parse("2022-11-21T04:15:00.000000Z")
            },
            new()
            {
                EraIndex = 1,
                BlockNumber = 1463749,
                Timestamp = DateTime.Parse("2022-12-07T02:02:36.000000Z")
            },
            new ()
            {
                EraIndex = 2,
                BlockNumber = 1485265,
                Timestamp = DateTime.Parse("2022-12-08T02:02:36.000000Z")
            }
        };

        await _eraService.ResolveActiveErasAndSave(activeEraTypes);

        //assert
        var era0 = await _eraService.GetEraByIndex(0);
        Assert.Equal(0, era0.EraIndex);
        Assert.Equal(1120840, era0.StartBlock);
        Assert.Equal(1463748, era0.EndBlock);

        var era1 = await _eraService.GetEraByIndex(1);
        Assert.Equal(1, era1.EraIndex);
        Assert.Equal(1463749, era1.StartBlock);
        Assert.Equal(1485264, era1.EndBlock);
        
        var era2 = await _eraService.GetEraByIndex(2);
        Assert.Equal(2, era2.EraIndex);
        Assert.Equal(1485265, era2.StartBlock);
        Assert.Equal(-1, era2.EndBlock);
    }
}