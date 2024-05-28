using MatrixEngine.Core.Models;
using MatrixEngine.Core.Models.DTOs;
using MatrixEngine.Core.Resolvers;
using MatrixEngine.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace MatrixEngine.Core.Testing.Resolvers;

public class SignEffectiveBalanceResolverTests
{
    private readonly Mock<IEffectiveBalanceService> _effectiveBalanceService;
    private readonly Mock<IAccountPunishmentMarkService> _accountPunishmentMarkService;
    private readonly Mock<ISignEffectiveBalanceService> _signEffectiveBalanceService;
    private SignEffectiveBalanceResolver _signEffectiveBalanceResolver;
    private Mock<ISignatureService> _signatureService;
    private Mock<ILogger<SignEffectiveBalanceResolver>> _logger;

    public SignEffectiveBalanceResolverTests()
    {
        _effectiveBalanceService = new Mock<IEffectiveBalanceService>();
        _accountPunishmentMarkService = new Mock<IAccountPunishmentMarkService>();
        _signEffectiveBalanceService = new Mock<ISignEffectiveBalanceService>();
        _signatureService = new Mock<ISignatureService>();
        _logger = new Mock<ILogger<SignEffectiveBalanceResolver>>();
        _signEffectiveBalanceResolver = new SignEffectiveBalanceResolver(
            _effectiveBalanceService.Object,
            _accountPunishmentMarkService.Object,
            _signEffectiveBalanceService.Object,
            _signatureService.Object,
            _logger.Object);
    }

    [Fact]
    public async Task Resolve_WhenRewardCycleAndEffectiveBalance_WithPunishmentMark_ExpectSignEffectiveBalance()
    {
        // Arrange
        var rewardCycle = new RewardCycle()
        {
            StartBlock = 0,
            EndBlock = 100,
            StartEraIndex = 0,
            EndEraIndex = 2,
        };
        var accountEffectiveBalances1 = new List<EffectiveBalanceModel>()
        {
            new EffectiveBalanceModel()
            {
                Account = "0x123",
                StartBlock = 0,
                EndBlock = 29,
                EffectiveBalance = "100",
                EraIndex = 0
            },
            new EffectiveBalanceModel()
            {
                Account = "0x123",
                StartBlock = 30,
                EndBlock = 59,
                EffectiveBalance = "100",
                EraIndex = 1
            },
            new EffectiveBalanceModel()
            {
                Account = "0x123",
                StartBlock = 60,
                EndBlock = 89,
                EffectiveBalance = "100",
                EraIndex = 2
            },
        };
        var accountEffectiveBalances2 = new List<EffectiveBalanceModel>()
        {
            new EffectiveBalanceModel()
            {
                Account = "0x234",
                StartBlock = 60,
                EndBlock = 89,
                EffectiveBalance = "100",
                EraIndex = 2
            },
        };
        var effectiveBalanceModels = new List<EffectiveBalanceModel>()
        {
            new EffectiveBalanceModel()
            {
                Account = "0x123",
                StartBlock = 60,
                EndBlock = 89,
                EffectiveBalance = "100",
                EraIndex = 2
            },
            new EffectiveBalanceModel()
            {
                Account = "0x234",
                StartBlock = 60,
                EndBlock = 89,
                EffectiveBalance = "100",
                EraIndex = 2
            }
        };
        var punishmentMarks = new List<AccountPunishmentMarkModel>()
        {
            new AccountPunishmentMarkModel()
            {
                Account = "0x123",
                BlockNumber = 80,
                Amount = "100",
                Applied = false,
                Type = "withdrawn"
            },
        };

        _effectiveBalanceService.Setup(m =>
                m.LoadAccountEffectiveBalanceInEraRange("0x123", rewardCycle.StartEraIndex, rewardCycle.EndEraIndex))
            .ReturnsAsync(accountEffectiveBalances1);
        _effectiveBalanceService.Setup(m =>
                m.LoadAccountEffectiveBalanceInEraRange("0x234", 2, rewardCycle.EndEraIndex))
            .ReturnsAsync(accountEffectiveBalances2);
        _accountPunishmentMarkService
            .Setup(m => m.LoadNewPunishmentMarksByBlockRange(60, 89))
            .ReturnsAsync(punishmentMarks);
        _signEffectiveBalanceService.Setup(x => x.FindLatestEraIndex("0x234")).ReturnsAsync(1); 
        // Act
        await _signEffectiveBalanceResolver.Resolve(rewardCycle, effectiveBalanceModels);

        // Assert
        _accountPunishmentMarkService.Verify(m => m.UpdatePunishmentMarksApplied(
                It.Is<List<AccountPunishmentMarkModel>>(l => l.Count == 1)),
            Times.Once()
        );

        _signEffectiveBalanceService.Verify(
            m => m.InsertSignEffectiveBalance(
                It.Is<List<SignEffectiveBalanceModel>>(
                    l => l.Count == 4
                    )), Times.Once);
    }
    
    [Fact]
    public async Task SignData_WhenSignEffectiveBalances_ExpectSignData()
    {
        // Arrange
        var signEffectiveBalances = new List<SignEffectiveBalanceModel>()
        {
            new SignEffectiveBalanceModel()
            {
                Account = "0x123",
                EffectiveBalance = "100",
                EraIndex = 1,
                EffectiveBlocks = 30
            },
            new SignEffectiveBalanceModel()
            {
                Account = "0x234",
                EffectiveBalance = "100",
                EraIndex = 2,
                EffectiveBlocks = 30
            },
            new SignEffectiveBalanceModel()
            {
                Account = "0x123",
                EffectiveBalance = "100",
                EraIndex = 3,
                EffectiveBlocks = 30
            },
            new SignEffectiveBalanceModel()
            {
                Account = "0x234",
                EffectiveBalance = "100",
                EraIndex = 4,
                EffectiveBlocks = 30
            }
        };

        _signEffectiveBalanceService.Setup(m => m.LoadUnsignedEffectiveBalances())
            .ReturnsAsync(signEffectiveBalances);

        const string base64Encrypted = "base64Encrypted";
        _signatureService.Setup(m => m.Base64Encrypt(It.IsAny<string>())).Returns(base64Encrypted);

        const string signature = "signature";
        _signatureService.Setup(m => m.SignMessage(base64Encrypted)).ReturnsAsync(signature);
        
        // Act
        await _signEffectiveBalanceResolver.SignData(10);

        // Assert
        _signEffectiveBalanceService.Verify(m =>
            m.UpdateSignedEffectiveBalance(It.Is<List<SignEffectiveBalanceModel>>(
                l=>l.Any(s => 
                    s.Signature == signature 
                    && s.BatchNumber != null && s.Timestamp > 0)
                )), Times.Exactly(4));
    }
}