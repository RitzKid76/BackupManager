using Backup.Commands.Arguments;
using Backup.Commands.Core;
using Backup.ObjectDatabase;

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

        if (!argSet.HasFlag("no-gc"))
            GarbageCollector.Run();

        return true;
    }

    public CommandSyntax GetSyntax(CommandSyntax syntax) => syntax
        .AddParameter("backup_name", "the name of the backup to create. default is the timestamp of creation")
        .AddFlag("f", "forces the backup if one with the same name already exists")
        .AddFlag("no-gc", "skips garbage collection");
}