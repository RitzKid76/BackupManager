namespace Backup.BackupComponents;

public class InvalidCompressionBucketConfig() : Exception(
    "Ensure that all compression buckets are created in descending order where the first entry is the longest time"
);