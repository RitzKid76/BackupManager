using Backup.BackupComponents;
using Backup.Commands.Arguments;
using Backup.Commands.Core;

namespace Backup.Commands;

[Command("start")]
public class Start_Command : ICommand
{
    public bool Execute(ArgumentSet argSet)
    {
        List<string> arguments = argSet.GetArguments();
        string? backupName = arguments.Count > 0
            ? arguments[0]
            : null;

        BackupDatabase.Generate(backupName, argSet.HasFlag("f"));

        return true;
    }

    public CommandSyntax GetSyntax(CommandSyntax syntax) => syntax
        .Description("starts the backup process using the path file")
        .Parameter("backup_name", "the name of the backup to create. default is the date of creation")
        .Flag("f", "forces the backup if one with the same name already exists");
}