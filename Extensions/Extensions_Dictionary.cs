namespace Backup.Extensions;

public static class Extensions_Dictionary
{
    public static TValue GetValueOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, Func<TValue> getter)
        where TKey : notnull
    {
        if (dict.TryGetValue(key, out TValue? output))
            return output;

        output = getter();
        dict[key] = output;

        return output;
    }
}