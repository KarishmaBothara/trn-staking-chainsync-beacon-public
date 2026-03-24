using Newtonsoft.Json;

namespace MatrixEngine.Core.IntegrationTest.Fixtures;

public static class JsonFileReader
{
    private static readonly string BasePath = Path.GetDirectoryName(typeof(JsonFileReader).Assembly.Location);
    
    public static T Read<T>(string path)
    {
        var fullPath = Path.Combine(BasePath, path);
        var jsonContent = File.ReadAllText(fullPath);
        var serializer = new JsonSerializer();
        using var streamReader = new StreamReader(fullPath);
        using var textReader = new JsonTextReader(streamReader);
        return serializer.Deserialize<T>(textReader);
    }
}