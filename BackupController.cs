using Backup.Commands.Core;

namespace Backup;

public static class BackupController
{
    public static void Main(string[] args)
    {
        CommandExecutor.Execute(args);
    }
}