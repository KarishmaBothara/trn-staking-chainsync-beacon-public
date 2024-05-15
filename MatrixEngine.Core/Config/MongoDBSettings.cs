namespace MatrixEngine.Core.Config;

public class MongoDbSettings
{
    public const string SectionName = "MongoDB";
    public string ConnectionString { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
}