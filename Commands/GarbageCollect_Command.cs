using Backup.Commands.Arguments;
using Backup.Commands.Core;
using Backup.Components;
using Backup.ObjectDatabase;

namespace Backup.Commands;

[Command("gc")]
public class GarbageCollect_Command : ICommand
{
    public bool Execute(ArgumentSet argSet)
    {
        if (argSet.HasFlag("v"))
            Logger.EnableInfo();

        GarbageCollector.Run();

        Logger.DisableInfo();
        return true;
    }

    public CommandSyntax GetSyntax(CommandSyntax syntax) => syntax
        .Description("deletes any unreferenced objects in the database.\nthis process runs automatically when running backup commands\nthis will not delete your backups or important data")
        .Flag("v", "logs all actions to show progress");
}