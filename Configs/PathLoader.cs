using YamlDotNet.Serialization;

namespace Backup.Configs;

public static class PathLoader
{
    private const string PATHS = "paths.yml";

    public static List<LoadedPath> Load()
    {
        Dictionary<string, object> data = ReadPathConfig();
        if (data is null)
            return [];

        return data
            .SelectMany(drive => FlattenPaths($"{drive.Key}:\\", drive.Value))
            .ToList();
    }

    private static Dictionary<string, object> ReadPathConfig()
    {
        IDeserializer deserializer = new DeserializerBuilder().Build();

        string contents = File.ReadAllText(PATHS);

        return deserializer.Deserialize<Dictionary<string, object>>(contents);
    }

    private static IEnumerable<LoadedPath> FlattenPaths(string basePath, object? node)
    {
        switch (node)
        {
            case Dictionary<object, object> map:
                foreach (KeyValuePair<object, object> entry in map)
                {
                    string path = Path.Combine(basePath, entry.Key.ToString()!);

                    if (ContainsBlacklist(entry.Value))
                        yield return new(path, false);

                    foreach (LoadedPath child in FlattenPaths(path, entry.Value))
                        yield return child;
                }

                break;

            case List<object> list:
                foreach (object item in list)
                {
                    foreach (LoadedPath path in FlattenPaths(basePath, item))
                        yield return path;
                }

                break;

            case string leaf:
                bool blacklisted = leaf.StartsWith('^');
                string leafPath = blacklisted
                    ? leaf[1..]
                    : leaf;

                yield return new(
                    Path.Combine(basePath, leafPath),
                    blacklisted
                );

                break;
        }
    }

    private static bool ContainsBlacklist(object? node) => node switch
    {
        List<object> list => list.Any(item =>
            item is string value && value.StartsWith('^')),

        Dictionary<object, object> map => map.Any(entry =>
            ContainsBlacklist(entry.Value)),

        _ => false
    };
}