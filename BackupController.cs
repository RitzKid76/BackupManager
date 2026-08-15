using Backup.Configs;
using Backup.ObjectDatabase;

namespace Backup;

public static class BackupController
{
    public static void Main()
    {
        List<string> paths = PathLoader.Load();
        List<ObjectReference> pathReferences = [];

        foreach (string path in paths)
            pathReferences.Add(DatabaseFeeder.Feed(path));

        foreach (ObjectReference reference in pathReferences)
            Console.WriteLine($"{reference.Name}, {reference.Pointer}");
    }
}