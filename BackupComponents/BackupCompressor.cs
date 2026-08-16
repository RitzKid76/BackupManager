using System.Text.RegularExpressions;
using Backup.Configs;
using Backup.Extensions;
using Backup.ObjectDatabase;

namespace Backup.BackupComponents;

public static class BackupCompressor
{
    private static readonly Dictionary<char, long> timeMap = new() {
        {'s', 1_000},
        {'m', 60_000},
        {'h', 3_600_000},
        {'d', 86_400_000},
        {'w', 604_800_000},
        {'M', 2_678_400_000},
        {'y', 31_536_000_000}
    };

    private static readonly Dictionary<long, List<string>> buckets = [];
    private static readonly List<long> bucketTimes = [];

    static BackupCompressor() =>
        StashBucketTimes();

    private static void StashBucketTimes()
    {
        foreach (string bucketString in Config.CompressionBuckets)
            bucketTimes.Add(ParseBucket(bucketString));

        if (bucketTimes.Count <= 1)
            return;

        long previousTime = bucketTimes[0];
        foreach (long time in bucketTimes[1..])
        {
            if (previousTime <= time)
                throw new InvalidCompressionBucketConfig();

            previousTime = time;
        }
    }

    private static long ParseBucket(string bucketString)
    {
        const string pattern = @"([0-9]+)(s|m|h|d|w|M|y)";
        Match match = Regex.Match(bucketString, pattern);

        int count = int.Parse(match.Groups[1].Value);
        char timeType = match.Groups[2].Value[0];

        return count * timeMap[timeType];
    }

    public static void Compress()
    {
        PopulateBuckets();
        TrimBuckets();
    }

    private static void PopulateBuckets()
    {
        long timeOfCompression = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        foreach (BackupEntry entry in BackupDatabase.GetBackups())
            AddBackupToBucket(timeOfCompression, entry);
    }

    private static void AddBackupToBucket(long timeOfCompression, BackupEntry backup)
    {
        long timePassed = timeOfCompression - backup.CreationTime;

        foreach (long bucket in bucketTimes)
            if (timePassed >= bucket)
            {
                buckets.GetValueOrAdd(bucket, () => []).Add(backup.Name);
                return;
            }
    }

    private static void TrimBuckets()
    {
        foreach (List<string> bucket in buckets.Values)
            if (bucket.Count > 1)
                foreach (string extraBackup in bucket[1..])
                    BackupDatabase.Delete(extraBackup, true);
    }
}