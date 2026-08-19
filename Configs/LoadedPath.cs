namespace Backup.Configs;

public readonly record struct LoadedPath(
    string Path,
    bool Blacklisted
);