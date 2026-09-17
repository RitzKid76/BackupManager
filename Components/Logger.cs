namespace Backup.Components;

public static class Logger
{
    private static bool enabled = false;

    public static void DisableInfo() =>
        enabled = false;

    public static void EnableInfo() =>
        enabled = true;

    public static void Info(object input)
    {
        if (enabled)
            Log(input);
    }

    public static void LogPartial(object input) =>
        Console.Write(input);

    public static void Log(object input) =>
        Console.WriteLine(input);

    public static void Colorize(string input, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(input);

        Console.ResetColor();
    }

    public static void Colorize(IEnumerable<string> inputs, Func<string, ConsoleColor?> mapping)
    {
        foreach (string input in inputs)
        {
            ConsoleColor? color = mapping(input);
            if (color is null)
                Console.ResetColor();
            else
                Console.ForegroundColor = color.Value;

            Console.WriteLine(input);
        }

        Console.ResetColor();
    }

    public static string? Prompt(string prompt)
    {
        LogPartial($"{prompt} ");
        return Console.ReadLine();
    }

    public static bool Confirm(string prompt)
    {
        string? answer = Prompt($"{prompt} (y/N):");
        return answer == "y" || answer == "yes";
    }
}