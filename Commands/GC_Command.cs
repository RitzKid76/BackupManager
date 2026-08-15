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

    public CommandSyntax GetSyntax(CommandSyntax syntax) => syntax;
}