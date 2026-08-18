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
        int backupCount = BackupDatabase.Count();
        if (backupCount == 0)
        {
            Logger.Log("no backups found");
            return true;
        }

        if (backupCount < 2)
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
                (previous, current) = (ExtractBackup(args[0]), ExtractBackup(args[1]));
                break;
            case 1:
                (previous, current) = (ExtractBackup(args[0]), BackupDatabase.GetBackup(0));
                break;
            case 0:
                (previous, current) = (BackupDatabase.GetBackup(1), BackupDatabase.GetBackup(0));
                break;
        }

        if (previous is null)
            return true;

        int previousIndex = BackupDatabase.IndexOf(previous.Name);
        int currentIndex = BackupDatabase.IndexOf(current!.Name);

        List<Difference> differences = currentIndex == previousIndex - 1
            ? current!.Differences // we store a cached diff already using the previous
            : DifferenceGenerator.FromBackup(previous, current!);

        foreach (Difference difference in differences)
            Logger.Log(difference.DiffString());

        return true;
    }

    private static BackupEntry? ExtractBackup(string backupName)
    {
        if (!BackupDatabase.TryGetBackup(backupName, out BackupEntry? backup))
        {
            Logger.Log($"couldn't find backup '{backupName}'");
            return null;
        }

        return backup;
    }

    public CommandSyntax GetSyntax(CommandSyntax syntax) => syntax
        .Description("displays the changes between 2 backup versions")
        .Parameter("previous", "the backup to compare against. default is assumed to be the second latest backup")
        .Parameter("current", "the backup to compare against the previous backup. default is the latest backup");
}