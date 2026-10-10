using Backup.ObjectDatabase.Hashing;

namespace Backup.ObjectDatabase;

public record FileVersion(Hash Pointer, BackupEntry Backup)
{
    public override string ToString() =>
        $"{Pointer} - {Backup.GetCreationTimeString()}";
}