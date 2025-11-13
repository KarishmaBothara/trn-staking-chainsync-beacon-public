using MatrixEngine.Core.Models;
using MatrixEngine.Core.Resolvers;
using MatrixEngine.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace MatrixEngine.Core.Testing.Resolvers;

public class StakerRateResolverTests
{
    private readonly Mock<IStakerRateService> _stakerRateService;
    private readonly Mock<ISignatureService> _signatureService;
    private readonly Mock<ILogger<StakerRateResolver>> _logger;
    private StakerRateResolver _stakerRateResolver;

    public StakerRateResolverTests()
    {
        _stakerRateService = new Mock<IStakerRateService>();
        _signatureService = new Mock<ISignatureService>();
        _logger = new Mock<ILogger<StakerRateResolver>>();
        _stakerRateResolver = new StakerRateResolver(_stakerRateService.Object, _signatureService.Object, _logger.Object);
    }

    [Fact]
    public async Task ResolveStakerRateFromEffectiveBalance_WhenProvidingEffectiveBalance()
    {
        // Arrange
        var effectiveBalances = new List<EffectiveBalanceModel>
        {
            new EffectiveBalanceModel
            {
                Account = "Account1",
                EraIndex = 1,
                Type = "validator"
            },
            new EffectiveBalanceModel
            {
                Account = "Account2",
                EraIndex = 2,
                Type = "nominator"
            },
            new EffectiveBalanceModel
            {
                Account = "Account3",
                EraIndex = 3,
                Type = "staker"
            },
        };

        // Act
        await _stakerRateResolver.ResolveStakerRateFromEffectiveBalance(effectiveBalances);

        // Assert
        _stakerRateService.Verify(m => m.UpsertStakerRates(It.Is<List<StakerRateModel>>(
            x => x.Count == 3 &&
                 x[0].Account == "Account1" && x[0].EraIndex == 1 && x[0].Type == "validator" && x[0].Rate == "0.0739" &&
                 x[1].Account == "Account2" && x[1].EraIndex == 2 && x[1].Rate == "0.0492" && x[1].Type == "nominator" && 
                 x[2].Account == "Account3" && x[2].EraIndex == 3 && x[2].Rate == "0.0246" && x[2].Type == "staker" 
            )), Times.Once);
    }
    
    [Fact]
    public async Task SignStakerRate_WhenLoadingLatestUnsignedStakerRates()
    {
        // Arrange
        var stakerRates = new List<StakerRateModel>
        {
            new StakerRateModel
            {
                Account = "Account1",
                EraIndex = 1,
                Type = "validator",
                Rate = "0.0739"
            },
            new StakerRateModel
            {
                Account = "Account2",
                EraIndex = 2,
                Type = "nominator",
                Rate = "0.0492"
            },
            new StakerRateModel
            {
                Account = "Account3",
                EraIndex = 3,
                Type = "staker",
                Rate = "0.0246"
            },
        };
        _stakerRateService.Setup(m => m.LoadLatestUnsignedStakerRates()).ReturnsAsync(stakerRates);
        const string base64Encrypted = "base64Encrypted";
        _signatureService.Setup(m => m.Base64Encrypt(It.IsAny<string>())).Returns(base64Encrypted);

        const string signature = "signature";
        _signatureService.Setup(m => m.SignMessage(base64Encrypted)).ReturnsAsync(signature);
        // Act
        await _stakerRateResolver.SignStakerRate();
        
        // Assert
        _stakerRateService.Verify(m => m.UpdateSignatures(It.Is<List<StakerRateModel>>(
            x => x.Any(m => m.Signature == signature && m.BatchNumber != null) 
            )), Times.Once);
    }
}