using Backup.Commands.Arguments;
using Backup.Commands.Core;
using Backup.ObjectDatabase;

namespace Backup.Commands;

[Command("restore")]
public class Restore_Command : ICommand
{
    public bool Execute(ArgumentSet argSet)
    {
        List<BackupEntry> backups = BackupDatabase.GetBackups();
        if (backups.Count == 0)
        {
            Console.WriteLine("no backups found");
            return true;
        }

        BackupEntry? toRestore = null;

        List<string> args = argSet.GetArguments();
        if (args.Count > 0)
        {
            string backupName = args[0];
            toRestore = backups.First(b => b.Name == backupName);

            if (toRestore is null)
            {
                Console.WriteLine($"couldn't find backup '{backupName}'");
                return true;
            }
        }
        else
            toRestore = backups[0];

        BackupDatabase.Restore(toRestore);
        return true;
    }

    public CommandSyntax GetSyntax(CommandSyntax syntax) => syntax
        .Description("restores the files stored in a backup to their original location\nthis will override any existing contents with the contents stored in the backup")
        .Parameter("backup_name", "the name of the backup to restore to. defaults to the latest backup");
}