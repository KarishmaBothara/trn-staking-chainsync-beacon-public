using MatrixEngine.Core.IntegrationTest.Fixtures;
using MatrixEngine.Core.Models.DTOs;
using MatrixEngine.Core.Services;
using Newtonsoft.Json;
using Xunit.Abstractions;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace MatrixEngine.Core.IntegrationTest.Tests.kms;

public class TestKms : TestBed<IntegrationTestFixture>
{
    public TestKms(ITestOutputHelper testOutputHelper, IntegrationTestFixture fixture) : base(testOutputHelper,
        fixture)
    {
    }

    [Fact]
    public async Task SignMessage_IfConfigurationLoaded()
    {
        var signService = _fixture.GetService<ISignatureService>(_testOutputHelper);
        
        var data =  new List<SignEffectiveBalanceDto>
        {
            new()
            {
                Account = "0xcb1de4FADCA68F601871f7E6E47fd43D707c779A",
                EffectiveBalance = "100000000",
                EraIndex = 10,
                StartBlock = 1,
                EndBlock = 1,
            },
            new()
            {
                Account = "0xcb1de4FADCA68F601871f7E6E47fd43D707c779A",
                EffectiveBalance = "100000000",
                StartBlock = 11,
                EndBlock = 20,
                EraIndex = 2,
            }
        };

        var serializedData = JsonConvert.SerializeObject(data);

        var signedMessage = await signService.SignMessage(serializedData);
        
        Assert.NotNull(signedMessage);
        Assert.Equal(@"K1qIxYivMhO4BfmCcqcWRgsj589z9nlNQhQl2Wa3c52ZzFi9vKFgsezDuNbWry7Zi1xP2Jt5zzkT3OM9z1AOcYXtr9LsEs7fGgrN+V4uRwxfgGjtLNvrPLghP0jzy516UFt18BJJM4vUjXSnjF0yl2bfP6H7S28UQYCufcxJ675Wc1+eg9TTLhzUQfCacyAMZLVGBSvCw5dLIEJKcyof5J63tLOheABXQldWQk3q6aYHwqAFZrdbNgpLUFeInpJtykR+QL9iPaeBcgx3W5Crv5FDDUHh8vpwOVD52PR8G74FV0yUk47ZQd9MTPZXgsm3hHlX2Q7+jZqswqmSkubGuw==", signedMessage);
    }
}