namespace Backup.Commands.Arguments;

public class ArgumentSet
{
    private readonly Dictionary<string, string?> flags;
    private readonly List<string> arguments;

    private ArgumentSet(List<string> arguments, Dictionary<string, string?> flags)
    {
        this.arguments = arguments;
        this.flags = flags;
    }

    public static ArgumentSet Create(string[] args)
    {
        (List<string> arguments, string[] remaining) = ExtractArguments(args);
        Dictionary<string, string?> extracted = ExtractFlags(remaining);

        return new(arguments, extracted);
    }

    private static (List<string>, string[]) ExtractArguments(string[] args)
    {
        List<string> arguments = [];
        int flagsStart = -1;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];

            if (arg.StartsWith('-'))
            {
                flagsStart = i;
                break;
            }

            arguments.Add(arg);
        }

        return (
            arguments,
            flagsStart == -1
                ? []
                : args[flagsStart..]
        );
    }

    private static Dictionary<string, string?> ExtractFlags(string[] flags)
    {
        if (flags.Length == 0)
            return [];

        Dictionary<string, string?> output = [];

        Queue<string> queue = new(flags);

        string? activeArg = null;
        while (queue.TryDequeue(out string? arg))
        {
            if (!arg.StartsWith('-'))
            {
                if (activeArg is null)
                    throw new InvalidFlagsException(arg);

                output[activeArg] = arg;
                continue;
            }

            bool isCluster = !arg.StartsWith("--");
            string value = isCluster
                ? arg[1..]
                : arg[2..];

            if (!isCluster)
            {
                activeArg = value;
                output[activeArg] = null;
                continue;
            }

            activeArg = null;
            foreach (char v in value)
                output[v.ToString()] = null;
        }

        return output;
    }

    public List<string> GetArguments() =>
        arguments;

    public bool HasFlag(string flag) =>
        flags.ContainsKey(flag);

    public bool TryGetFlag<T>(string flag, out T? value, bool allowDefault = false)
    {
        value = default;

        if (!flags.TryGetValue(flag, out string? v))
            return false;

        if (v is T val)
        {
            value = val;
            return true;
        }

        return allowDefault;
    }
}