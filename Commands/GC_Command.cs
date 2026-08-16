using Backup.Commands.Arguments;
using Backup.Commands.Core;
using Backup.ObjectDatabase;

namespace Backup.Commands;

[Command("gc")]
public class GC_Command : ICommand
{
    public bool Execute(ArgumentSet argSet)
    {
        GarbageCollector.Run();
        return true;
    }

    public CommandSyntax GetSyntax(CommandSyntax syntax) => syntax
        .Description("deletes any unreferenced objects in the database.\nthis process runs automatically when running backup commands\nthis will not delete your backups or important data");
}