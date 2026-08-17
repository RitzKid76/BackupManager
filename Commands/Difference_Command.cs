using Backup.Commands.Arguments;
using Backup.Commands.Core;
using Backup.Components;
using Backup.Components.Differences;
using Backup.ObjectDatabase;

namespace Backup.Commands;

[Command("diff")]
public class Difference_Command : ICommand
{
    public bool Execute(ArgumentSet argSet)
    {
        List<BackupEntry> backups = BackupDatabase.GetBackups();
        if (backups.Count == 0)
        {
            Logger.Log("no backups found");
            return true;
        }

        if (backups.Count < 2)
        {
            Logger.Log("need at least 2 backups to diff");
            return true;
        }

        List<string> args = argSet.GetArguments();

        BackupEntry? previous = null;
        BackupEntry? current = null;

        switch (args.Count)
        {
            case 2:
                (previous, current) = HandleBoth(args[0], args[1]);
                break;
            case 1:
                (previous, current) = HandleOne(args[0], backups);
                break;
            case 0:
                (previous, current) = (backups[1], backups[0]);
                break;
        }

        if (previous is null)
            return true;

        int previousIndex = backups.IndexOf(previous);
        int currentIndex = backups.IndexOf(current!);

        List<Difference> differences = currentIndex == previousIndex - 1
            ? current!.Differences // we store a cached diff already using the previous
            : DifferenceGenerator.FromBackup(previous, current!);

        foreach (Difference difference in differences)
            Logger.Log(difference.DiffString());

        return true;
    }

    private static (BackupEntry?, BackupEntry?) HandleBoth(string previous, string current)
    {
        if (!BackupDatabase.TryGetBackup(previous, out BackupEntry? previousBackup))
        {
            Logger.Log($"couldn't find backup '{previous}'");
            return (null, null);
        }

        if (!BackupDatabase.TryGetBackup(current, out BackupEntry? currentBackup))
        {
            Logger.Log($"couldn't find backup '{current}'");
            return (null, null);
        }

        return (previousBackup, currentBackup);
    }

    private static (BackupEntry?, BackupEntry?) HandleOne(string previous, List<BackupEntry> backups)
    {
        if (!BackupDatabase.TryGetBackup(previous, out BackupEntry? previousBackup))
        {
            Logger.Log($"couldn't find backup '{previous}'");
            return (null, null);
        }

        return (previousBackup, backups[0]);
    }

    public CommandSyntax GetSyntax(CommandSyntax syntax) => syntax
        .Description("displays the changes between 2 backup versions")
        .Parameter("previous", "the backup to compare against. default is assumed to be the second latest backup")
        .Parameter("current", "the backup to compare against the previous backup. default is the latest backup");
}