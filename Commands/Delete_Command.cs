using Backup.Components;
using Backup.Commands.Arguments;
using Backup.Commands.Core;

namespace Backup.Commands;

[Command("delete")]
public class Delete_Command : ICommand
{
    public bool Execute(ArgumentSet argSet)
    {
        if (argSet.HasFlag("v"))
            Logger.EnableInfo();

        bool force = argSet.HasFlag("f");

        if (argSet.HasFlag("wipe-all"))
        {
            if (!BackupDatabase.Wipe(force))
                Logger.Log("failed to finish wiping backups");

            Logger.DisableInfo();
            return true;
        }

        List<string> args = argSet.GetArguments();

        if (args.Count == 0)
        {
            Logger.DisableInfo();
            return false;
        }

        string backupName = args[0];

        if (!BackupDatabase.Delete(backupName, force))
        {
            Logger.Log($"couldn't find backup '{backupName}'");

            Logger.DisableInfo();
            return true;
        }

        Logger.DisableInfo();
        return true;
    }

    public CommandSyntax GetSyntax(CommandSyntax syntax) => syntax
        .Description("deletes the backup specified by the name provided permanently")
        .Parameter("backup_name", "the name of the backup to delete", true)
        .Flag("f", "skips the confirmation prompt when deleting the backup")
        .Flag("v", "logs all actions to show progress")
        .Flag("wipe-all", "deletes the entire backup list with no recovery. only use if you know what you are doing");
}