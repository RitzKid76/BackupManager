using YamlDotNet.Serialization;

namespace Backup.Configs;

public static class PathLoader
{
    private const string PATHS = "paths.yml";
    private const string BLACKLIST = "^";

    public static (IEnumerable<string>, IEnumerable<BlacklistEntry>) Load()
    {
        Dictionary<string, object> data = ReadPathConfig();

        HashSet<string> paths = new(StringComparer.OrdinalIgnoreCase);
        HashSet<BlacklistEntry> blacklist = [];

        foreach (KeyValuePair<string, object> entry in data)
        {
            if (entry.Key == BLACKLIST)
            {
                LoadBlacklist(blacklist, string.Empty, entry.Value);
                continue;
            }

            string basePath = $"{entry.Key}:\\";
            FlattenPaths(basePath, entry.Value, paths, blacklist);
        }

        return (paths, blacklist);
    }

    private static Dictionary<string, object> ReadPathConfig()
    {
        IDeserializer deserializer = new DeserializerBuilder().Build();
        string contents = File.ReadAllText(PATHS);

        return deserializer.Deserialize<Dictionary<string, object>>(contents);
    }

    private static void FlattenPaths(string basePath, object? node, HashSet<string> paths, HashSet<BlacklistEntry> blacklist)
    {
        if (node is null)
        {
            paths.Add(basePath);
            return;
        }

        if (node is not Dictionary<object, object> map)
            return;

        if (map.TryGetValue(BLACKLIST, out object? blacklistNode))
        {
            LoadBlacklist(blacklist, basePath, blacklistNode);
            paths.Add(basePath);
        }

        foreach (KeyValuePair<object, object> entry in map)
        {
            string key = entry.Key.ToString()!;

            if (key == BLACKLIST)
                continue;

            string path = Path.Combine(basePath, key);
            FlattenPaths(path, entry.Value, paths, blacklist);
        }
    }

    private static void LoadBlacklist(HashSet<BlacklistEntry> blacklist, string scope, object? node)
    {
        if (node is not List<object> list)
            return;

        foreach (object item in list)
            if (item is string pattern)
                blacklist.Add(new(scope, pattern));
    }
}