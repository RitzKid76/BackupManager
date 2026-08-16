using System.Text;
using Backup.BackupComponents;
using Backup.Commands.Arguments;
using Backup.Commands.Core;
using Backup.Extensions;
using Backup.ObjectDatabase;

namespace Backup.Commands;

[Command("list")]
public class List_Command : ICommand
{
    public bool Execute(ArgumentSet argSet)
    {
        List<string> args = argSet.GetArguments();
        if (args.Count > 0)
        {
            string backupName = args[0];

            if (!BackupDatabase.TryGetBackup(backupName, out BackupEntry? backup))
            {
                BackupLogger.Log($"couldn't find backup '{backupName}'");
                return true;
            }

            PrintAll(backup!, true);
        }

        List<BackupEntry> backups = BackupDatabase.GetBackups();

        foreach (BackupEntry backup in backups)
            if (argSet.HasFlag("l"))
                PrintAll(backup);
            else
                BackupLogger.Log(backup.Name);

        return true;
    }

    private static void PrintAll(BackupEntry backup, bool showDiff = false)
    {
        StringBuilder output = new();

        output.AppendLine(new string('-', 80));

        string creationTimeString = DateTimeOffset.FromUnixTimeMilliseconds(backup.CreationTime).LocalDateTime.ToString("f");
        output.Append(creationTimeString.PadString(48));

        output.Append(backup.Name);

        if (showDiff)
        {
            output.AppendLine();
            output.AppendLine(new string('-', 80));

            output.AppendLine($"|   + sampleAddition.txt");
            output.AppendLine($"|   ~ sampleChange.txt");
            output.AppendLine($"|   X sampleRemoval.txt");
        }

        BackupLogger.Log(output.ToString());
    }

    public CommandSyntax GetSyntax(CommandSyntax syntax) => syntax
        .Description("lists the backups currently stored\ncan list details on the backup like creation time and diffs")
        .Parameter("backup_name", "the backup to inspect details on")
        .Flag("l", "show details for every backup");
}