namespace Backup.Configs;

public record ConfigModel(
    string BackupFolder,
    string DatabaseFolder
);