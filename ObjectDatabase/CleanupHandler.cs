using Backup.BackupComponents;
using Backup.Configs;

namespace Backup.ObjectDatabase;

public static class CleanupHandler
{
    private static bool working = false;
    private static bool scheduleRun = false;
    private static bool running = false;

    public static void Run()
    {
        if (running)
            return;

        if (working)
        {
            scheduleRun = true;
            return;
        }

        scheduleRun = false;

        StartCleanup();
    }

    public static void BeginWork() =>
        working = true;

    public static void EndWork()
    {
        working = false;
        if (scheduleRun)
            Run();
    }

    private static void StartCleanup()
    {
        running = true;

        BackupCompressor.Run();

        if (Config.GarbageCollect)
            GarbageCollector.Run();

        running = false;
    }
}