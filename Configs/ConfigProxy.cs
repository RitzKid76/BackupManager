using System.Reflection;

namespace Backup.Configs;

public static class ConfigProxy
{
    private const string CONFIG = "config.json";
    private const string DEFAULT_CONFIG = "Backup.Configs.config.json";

    private const string PATHS = "paths.yml";
    private const string DEFAULT_PATHS = "Backup.Configs.paths.yml";

    public static string ReadConfig()
    {
        if (File.Exists(CONFIG))
            return File.ReadAllText(CONFIG);

        CreateAll();
        throw new DefaultStartupException();
    }

    public static string ReadPaths()
    {
        if (File.Exists(PATHS))
            return File.ReadAllText(PATHS);

        CreateAll();
        throw new DefaultStartupException();
    }

    private static void CreateAll()
    {
        TryCreate(DEFAULT_CONFIG, CONFIG);
        TryCreate(DEFAULT_PATHS, PATHS);
    }

    private static void TryCreate(string target, string destination)
    {
        if (File.Exists(destination))
            return;

        Assembly assembly = Assembly.GetExecutingAssembly();

        using Stream? stream = assembly.GetManifestResourceStream(target) ??
            throw new FileNotFoundException($"{target} was not found in the app");

        using FileStream file = File.Create(destination);
        stream.CopyTo(file);
    }
}