using Backup.Components;
using Backup.Commands.Arguments;
using Backup.Commands.Core;

namespace Backup.Commands;

[Command("restore")]
public class Restore_Command : ICommand
{
    public bool Execute(ArgumentSet argSet)
    {
        List<string> args = argSet.GetArguments();
        if (args.Count == 0)
            return false;

        if (argSet.HasFlag("v"))
            Logger.EnableInfo();

        string backupName = args[0];
        args.RemoveAt(0);

        if (!BackupDatabase.Restore(backupName, args))
            Logger.Log($"couldn't find backup '{backupName}'");

        Logger.DisableInfo();
        return true;
    }

    public CommandSyntax GetSyntax(CommandSyntax syntax) => syntax
        .Description("restores the files stored in a backup to their original location\nthis will override any existing contents with the contents stored in the backup\nany changes made after the backup are undone, including new files")
        .Parameter("backup_name", "the name of the backup to restore to", true)
        .Parameter("paths...", "list of paths to restore. can be folders or files")
        .Flag("v", "logs all actions to show progress");
}