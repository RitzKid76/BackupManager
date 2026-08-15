namespace Backup;

public static class BackupController
{
    public static void Main()
    {
        BackupGenerator.Generate();
        BackupRollback.Apply("2026-08-15_01-55-51");
    }
}