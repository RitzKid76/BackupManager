using Backup.Commands.Arguments;
using Backup.Commands.Core;
using Backup.Components;
using Backup.ObjectDatabase;
using Backup.ObjectDatabase.ObjectTypes;

namespace Backup.Commands;

[Command("info")]
public class Info_Command : ICommand
{
    public bool Execute(ArgumentSet argSet)
    {
        if (!argSet.HasArgs(out List<string> args))
            return false;

        string pointer = args[0];

        if (Database.TryReadUnknownTree(pointer, out Tree? tree))
        {
            HandleTree(tree!);
            return true;
        }

        Logger.Log("blob");

        return true;
    }

    private static void HandleTree(Tree tree)
    {
        IEnumerable<string> lines = tree.References
            .OrderBy(r => r.Name)
            .OrderBy(r => r.Format)
            .Select(ReferenceInfoLine);

        Logger.Colorize(lines, ColorMap);
    }

    private static string ReferenceInfoLine(ObjectReference reference)
    {
        string baseLine = $" {reference.Pointer} {reference.FullName}";

        return reference.Format switch
        {
            ObjectFormat.TREE => $"[] {baseLine}",
            _ => $"|  {baseLine}"
        };
    }

    private static ConsoleColor? ColorMap(string line)
    {
        if (line.StartsWith("[]"))
            return ConsoleColor.Blue;

        return ConsoleColor.White;
    }

    public CommandSyntax GetSyntax(CommandSyntax syntax) => syntax
        .Description("gets information about a given object pointer")
        .Parameter("pointer", "the pointer to get information for")
        .SupportsVerbosity();
}