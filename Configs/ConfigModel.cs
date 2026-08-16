namespace Backup.Configs;

public record ConfigModel(
    string BackupFolder,
    string DatabaseFolder,
    string[] CompressionBuckets,
    bool GarbageCollect
);