namespace MatrixEngine.Core;

public class MongoDBSettings
{
    public static string SectionName { get; } = "MongoDB";
    public string ConnectionString { get; set; }
    public string Database { get; set; }
}