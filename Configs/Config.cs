using System.Text.Json;

namespace Backup.Configs;

public static class Config
{
    public static string BackupFolder { get; private set; } = string.Empty;
    public static string DatabaseFolder { get; private set; } = string.Empty;
    public static string[] CompressionBuckets { get; private set; } = [];
    public static bool GarbageCollect { get; private set; } = true;
    public static long WriteTimeOptimizationThreshold { get; private set; } = 262144;
    public static int Threads { get; private set; } = 4;

    static Config() =>
        LoadConfigJson();

    private static void LoadConfigJson()
    {
        string json = ConfigProxy.ReadConfig();

        JsonSerializerOptions options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        ConfigModel model = JsonSerializer.Deserialize<ConfigModel>(json, options)!;

        BackupFolder = model.BackupFolder;
        DatabaseFolder = model.DatabaseFolder;
        CompressionBuckets = model.CompressionBuckets;
        GarbageCollect = model.GarbageCollect;
        WriteTimeOptimizationThreshold = model.WriteTimeOptimizationThreshold;
        Threads = model.Threads;
    }
}