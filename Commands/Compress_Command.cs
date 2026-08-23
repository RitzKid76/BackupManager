using Backup.Commands.Arguments;
using Backup.Commands.Core;
using Backup.Components;

namespace Backup.Commands;

[Command("compress")]
public class Compress_Command : ICommand
{
    public bool Execute(ArgumentSet argSet)
    {
        if (argSet.HasFlag("v"))
            Logger.EnableInfo();

        Compressor.Run();

        Logger.DisableInfo();
        return true;
    }

    public CommandSyntax GetSyntax(CommandSyntax syntax) => syntax
        .Description("compresses backups according to the configured buckets.\nthis process runs automatically when running backup commands\nthis may delete your backups or important data")
        .Flag("v", "logs all actions to show progress");
}