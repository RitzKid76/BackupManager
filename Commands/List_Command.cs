using Backup.Commands.Arguments;
using Backup.Commands.Core;
using Backup.ObjectDatabase;

namespace Backup.Commands;

[Command("list")]
public class List_Command : ICommand
{
    public bool Execute(ArgumentSet argSet)
    {
        List<BackupEntry> backups = BackupDatabase.GetBackups();

        foreach (BackupEntry backup in backups)
            Console.WriteLine(backup.Name);

        return true;
    }

    public CommandSyntax GetSyntax(CommandSyntax syntax) => syntax
        .Description("lists the backups currently stored\ncan list details on the backup such as creation date and stored files")
        .Parameter("backup_name", "the backup to inspect details on")
        .Flag("a", "show all details for every backup");
}