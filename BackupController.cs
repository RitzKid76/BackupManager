namespace Backup;

public static class BackupController
{
    public static void Main()
    {
        BackupDatabase.Generate();
        GarbageCollector.Run();
    }
}