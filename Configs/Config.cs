using System.Text.Json;

namespace Backup.Configs;

public static class Config
{
    private const string CONFIG_PATH = "config.json";

    public static string BackupFolder { get; private set; } = string.Empty;
    public static string DatabaseFolder { get; private set; } = string.Empty;
    
    static Config() =>
        LoadConfigJson();

    private static void LoadConfigJson()
    {
        string json = File.ReadAllText(CONFIG_PATH);

        JsonSerializerOptions options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        ConfigModel model = JsonSerializer.Deserialize<ConfigModel>(json, options)!;

        BackupFolder = model.BackupFolder;
        DatabaseFolder = model.DatabaseFolder;
    }
}