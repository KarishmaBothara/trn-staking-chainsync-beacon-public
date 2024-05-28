using MatrixEngine.Core.Engine;
using MatrixEngine.Core.IntegrationTest.Fixtures;
using Xunit.Abstractions;
using Xunit.Extensions.Ordering;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace MatrixEngine.Core.IntegrationTest.Tests.case_1;

[Order(1)]
public class TestCase1 : TestBed<IntegrationTestFixture>
{
    public TestCase1(ITestOutputHelper testOutputHelper, IntegrationTestFixture fixture) : base(testOutputHelper,
        fixture)
    {
    }

    [Fact]
    public async Task Test_SingleRewardCycle_WithExistingEvents()
    {
        // Arrange
        var dataLoader = _fixture.GetService<IDataLoader>(_testOutputHelper);
        await dataLoader?.LoadCase("case-1-one-cycle")!;

        var engineCore = _fixture.GetService<IEngineCore>(_testOutputHelper);

        // Act
        await engineCore?.Start()!;
    }
}