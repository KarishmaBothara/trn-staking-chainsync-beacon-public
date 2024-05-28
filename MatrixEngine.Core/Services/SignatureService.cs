using System.Security.Cryptography;
using System.Text;
using Amazon;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using MatrixEngine.Core.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MatrixEngine.Core.Services;

public interface ISignatureService
{
    Task<string> SignMessage(string message);
    string Base64Encrypt(string message);
}

public class SignatureService : ISignatureService
{
    private readonly KmsSettings _kmsSettings;
    private readonly ILogger<SignatureService> _logger;

    public SignatureService(IOptions<KmsSettings> options, ILogger<SignatureService> logger)
    {
        _logger = logger;
        _kmsSettings = options.Value;
    }

    public async Task<string> SignMessage(string message)
    {
        _logger.LogInformation($"Signing message");
        try
        {
            var awsCredentials = new Amazon.Runtime.EnvironmentVariablesAWSCredentials();
            var endpoint = RegionEndpoint.GetBySystemName(_kmsSettings.Region);
            var kmsClient = new AmazonKeyManagementServiceClient(awsCredentials, endpoint);
            var keyArn = $"arn:aws:kms:{_kmsSettings.Region}:{_kmsSettings.AccountId}:key/{_kmsSettings.KeyId}";

            var messageByte = Encoding.UTF8.GetBytes(message);

            using SHA256 sha256 = SHA256.Create();
            // Compute the hash of the JSON bytes
            byte[] hashBytes = sha256.ComputeHash(messageByte);
            // // Convert the hash bytes to a hexadecimal string
            // string hashHex = BitConverter.ToString(hashBytes).Replace("-", "");
            // Console.WriteLine($"SHA-256 Hash: {hashHex}");
            var signRequest = new SignRequest()
            {
                KeyId = keyArn,
                Message = new MemoryStream(hashBytes),
                MessageType = MessageType.DIGEST,
                SigningAlgorithm = SigningAlgorithmSpec.RSASSA_PKCS1_V1_5_SHA_256,
            };

            _logger.LogInformation($"Signing message with key");

            var signResponse = await kmsClient.SignAsync(signRequest);

            _logger.LogInformation($"Got signature");

            var signature = signResponse.Signature.ToArray();
            return Convert.ToBase64String(signature);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            return null;
        }
    }

    public string Base64Encrypt(string message)
    {
        var plainTextBytes = Encoding.UTF8.GetBytes(message);
        return Convert.ToBase64String(plainTextBytes);
    }
}