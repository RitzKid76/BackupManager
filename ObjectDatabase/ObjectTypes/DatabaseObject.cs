namespace Backup.ObjectDatabase.ObjectTypes;

public abstract record DatabaseObject
{
    public abstract string GetData();
}