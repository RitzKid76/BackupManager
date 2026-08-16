using Backup.Commands.Arguments;
using Backup.Commands.Core;

namespace Backup.Commands;

[Command("delete")]
public class Delete_Command : ICommand
{
    public bool Execute(ArgumentSet argSet)
    {
        List<string> args = argSet.GetArguments();

        if (args.Count == 0)
            return false;

        string backupName = args[0];
        if (!BackupDatabase.Delete(backupName))
            Console.WriteLine($"couldn't find backup '{backupName}'");

        return true;
    }

    public CommandSyntax GetSyntax(CommandSyntax syntax) => syntax
        .Description("deletes the backup specified by the name provided permanently")
        .Parameter("backup_name", "the name of the backup to delete", true);
}