namespace MatrixEngine.Core.Config;

public class KmsSettings
{
    public const string Kms = "Kms";

    public string? Region { get; set; }

    public string? AccountId { get; set; }

    public string? KeyId { get; set; }
}