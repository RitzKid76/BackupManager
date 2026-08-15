using Backup.Configs;
using Backup.ObjectDatabase;

namespace Backup;

public static class BackupController
{
    public static void Main()
    {
        WriteTest();
    }

    private static void WriteTest()
    {
        ObjectReference backup = DatabaseFeeder.Feed("T:\\Backup");
        Console.WriteLine($"{backup.Name}, {backup.Pointer}");

        foreach (string path in PathLoader.Load())
            Console.WriteLine(path);
    }
}