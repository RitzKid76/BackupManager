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

        if (argSet.HasFlag("v"))
            Logger.EnableInfo();

        List<string> args = argSet.GetArguments();

        BackupEntry? previous = null;
        BackupEntry? current = null;
        List<string> fileGlobs = [];

        switch (args.Count)
        {
            case >= 3:
                previous = ExtractBackup(args[0]);
                current = ExtractBackup(args[1]);

                fileGlobs = args[2..];

                break;
            case 2:
                previous = ExtractBackup(args[0]);
                current = ExtractBackup(args[1]);

                if (current is null)
                {
                    current = BackupDatabase.GetBackup(0);
                    fileGlobs = args[1..];
                }

                break;
            case 1:
                previous = ExtractBackup(args[0]);
                current = BackupDatabase.GetBackup(0);

                if (previous is null)
                {
                    previous = BackupDatabase.GetBackup(1);
                    fileGlobs = args[0..];
                }

                break;
            case 0:
                previous = BackupDatabase.GetBackup(1);
                current = BackupDatabase.GetBackup(0);
                break;
        }

        if (previous is null)
        {
            Logger.Log($"backup not found: {args[0]}");

            Logger.DisableInfo();
            return true;
        }

        if (current is null)
        {
            Logger.Log($"backup not found: {args[1]}");

            Logger.DisableInfo();
            return true;
        }

        int previousIndex = BackupDatabase.IndexOf(previous.Name);
        int currentIndex = BackupDatabase.IndexOf(current.Name);

        List<Difference> differences = currentIndex == previousIndex - 1 // we store a cached diff already using the previous
            ? DifferenceGenerator.Filter(current.Differences, fileGlobs)
            : DifferenceGenerator.FromBackup(previous, current, fileGlobs);

        foreach (Difference difference in differences)
        {
            Logger.Log(difference.DiffString());
            if (!argSet.HasFlag("l"))
                continue;

            ObjectReference? previousObject = difference.Previous;
            ObjectReference? currentObject = difference.Current;

            string[] previousData = previousObject is not null
                ? Database.ReadFile(previousObject)
                : [];

            string[] currentData = currentObject is not null
                ? Database.ReadFile(currentObject)
                : [];

            List<string> diff = FileDifferenceGenerator.DiffContents(previousData, currentData);
            Logger.Colorize(diff, DiffColorMap);
        }

        Logger.DisableInfo();
        return true;
    }

    private static BackupEntry? ExtractBackup(string backupName)
    {
        BackupDatabase.TryGetBackup(backupName, out BackupEntry? backup);
        return backup;
    }

    private static ConsoleColor? DiffColorMap(string line) => line[0] switch
    {
        ' ' => ConsoleColor.Gray,
        '+' => ConsoleColor.Green,
        '-' => ConsoleColor.Red,
        '@' => ConsoleColor.DarkYellow,
        _ => null
    };

    public CommandSyntax GetSyntax(CommandSyntax syntax) => syntax
        .Description("displays the changes between 2 backup versions")
        .Parameter("previous", "the backup to compare against. default is assumed to be the second latest backup.")
        .Parameter("current", "the backup to compare against the previous backup. default is the latest backup")
        .Parameter("files...", "the file names to display in the diff. supports glob matching")
        .Flag("l", "shows file content differences")
        .Flag("v", "logs all actions to show progress");
}