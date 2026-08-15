using YamlDotNet.Serialization;

namespace Backup.Configs;

public static class PathLoader
{
    private const string CONFIG = "paths.yml";

    public static List<string> Load()
    {
        var data = ReadPathConfig();

        return data
            .SelectMany(drive => FlattenPaths($"{drive.Key}:\\", drive.Value))
            .ToList();
    }

    private static Dictionary<string, object> ReadPathConfig()
    {
        var deserializer = new DeserializerBuilder()
            .Build();

        string contents = File.ReadAllText(CONFIG);

        return deserializer.Deserialize<Dictionary<string, object>>(contents);
    }

    private static IEnumerable<string> FlattenPaths(string basePath, object? node)
    {
        switch (node)
        {
            case Dictionary<object, object> map:
                foreach (KeyValuePair<object, object> entry in map)
                {
                    string childBase = Path.Combine(basePath, entry.Key.ToString()!);

                    foreach (string path in FlattenPaths(childBase, entry.Value))
                        yield return path;
                }

                break;

            case List<object> list:
                foreach (object item in list)
                {
                    foreach (string path in FlattenPaths(basePath, item))
                        yield return path;
                }

                break;

            case string leaf:
                yield return Path.Combine(basePath, leaf);
                break;
        }
    }
}