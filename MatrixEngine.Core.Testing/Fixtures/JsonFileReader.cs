using Newtonsoft.Json;

namespace MatrixEngine.Core.Testing.Fixtures;

public static class JsonFileReader
{
    public static T Read<T>(string path)
    {
        var serializer = new JsonSerializer();
        using var streamReader = new StreamReader(path);
        using var textReader = new JsonTextReader(streamReader);
        return serializer.Deserialize<T>(textReader);
    }
}