using Backup.Components;
using Backup.Commands.Arguments;
using Backup.Commands.Core;
using Backup.ObjectDatabase;
using Backup.Components.Differences;

namespace Backup.Commands;

[Command("list")]
public class List_Command : ICommand
{
    public bool Execute(ArgumentSet argSet)
    {
        if (argSet.HasArgs(out List<string> args))
        {
            string backupName = args[0];

            if (!BackupDatabase.TryGetBackup(backupName, out BackupEntry? backup))
            {
                Logger.Colorize($"couldn't find backup '{backupName}'", ConsoleColor.Red);
                return true;
            }

            PrintAll(backup!, true);
        }

        foreach (BackupEntry backup in BackupDatabase.GetBackups())
            if (argSet.HasFlag("l"))
                PrintAll(backup);
            else
                Logger.Colorize(backup.Name, ConsoleColor.Blue);

        return true;
    }

    private static void PrintAll(BackupEntry backup, bool showDiff = false)
    {
        List<string> output = [];

        output.Add(new string('-', 80));

        string creationTimeString = backup.GetCreationTimeString();
        output.Add($"{creationTimeString,-48}{backup.Name}");

        if (showDiff)
        {
            output.Add(new string('-', 80));

            output.Add(backup.GetDiffString());
        }

        Logger.Colorize(output, ColorMap);
    }

    private static ConsoleColor? ColorMap(string line)
    {
        if (line.StartsWith("--"))
            return ConsoleColor.DarkBlue;

        ConsoleColor? output = FileDifferenceGenerator.ColorMap(line);
        if (output is not null)
            return output;

        return ConsoleColor.Cyan;
    }

    public CommandSyntax GetSyntax(CommandSyntax syntax) => syntax
        .Description("lists the backups currently stored\ncan list details on the backup like creation time and diffs")
        .Parameter("backup_name", "the backup to inspect details on")
        .Flag("l", "show details for every backup");
}