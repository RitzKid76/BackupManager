using System.Reflection;
using System.Text;
using Backup.Commands.Arguments;

namespace Backup.Commands.Core;

public static class CommandExecutor
{
    private static readonly Dictionary<string, Func<ICommand>> commands = [];

    static CommandExecutor() =>
        LoadCommands();

    private static void LoadCommands()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        foreach (Type type in assembly.GetTypes())
            if (type.GetCustomAttribute<CommandAttribute>() is CommandAttribute commandAttribute)
                RegisterCommand(commandAttribute, type);
    }

    private static void RegisterCommand(CommandAttribute commandAttribute, Type type)
    {
        string token = commandAttribute.Token;

        ICommand constructor() =>
            (ICommand)Activator.CreateInstance(type)!;

        commands.Add(token, constructor);
    }

    public static void Execute(string[] args)
    {
        int argCount = args.Length;

        if (argCount == 0)
        {
            SendHelp();
            return;
        }

        string token = args[0];
        if (!TryGetCommand(token, out ICommand? command))
        {
            SendHelp();
            return;
        }

        ArgumentSet argSet = ArgumentSet.Create(args[1..]);

        bool result = command!.Execute(argSet);
        if (!result)
            SendHelp(token);
    }

    public static void SendHelp(string? token = null)
    {

        if (
            token is not null &&
            TryGetCommand(token, out ICommand? command)
        )
        {
            CommandSyntax syntax = new(token);

            Console.WriteLine(command!.GetSyntax(syntax).GenerateSyntax(true));
            return;
        }

        StringBuilder allHelp = new();

        foreach (KeyValuePair<string, Func<ICommand>> pair in commands)
            allHelp.AppendLine(pair.Value.Invoke().GetSyntax(new(pair.Key)).GenerateSyntax());

        Console.WriteLine(allHelp.ToString());
    }

    private static bool TryGetCommand(string token, out ICommand? command)
    {
        command = null;

        if (!commands.TryGetValue(token, out Func<ICommand>? constructor))
            return false;

        command = constructor.Invoke();
        return true;
    }
}