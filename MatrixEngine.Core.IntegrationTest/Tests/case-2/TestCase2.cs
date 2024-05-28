using MatrixEngine.Core.Engine;
using MatrixEngine.Core.IntegrationTest.Fixtures;
using Xunit.Abstractions;
using Xunit.Extensions.Ordering;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace MatrixEngine.Core.IntegrationTest.Tests.case_2;

[Order(2)]
public class TestCase2 : TestBed<IntegrationTestFixture>
{
    public TestCase2(ITestOutputHelper testOutputHelper, IntegrationTestFixture fixture) : base(testOutputHelper,
        fixture)
    {
    }

    [Fact]
    public async Task Test_TwoRewardCycles_WithExistingEvents()
    {
        // Arrange
        var dataLoader = _fixture.GetService<IDataLoader>(_testOutputHelper);
        await dataLoader?.LoadCase("case-2-two-cycles")!;

        var engineCore = _fixture.GetService<IEngineCore>(_testOutputHelper);

        // Act
        await engineCore?.Start()!;
    }
}