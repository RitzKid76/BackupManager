namespace Backup.Configs;

public readonly record struct BlacklistEntry(
    string Scope,
    string Pattern
);