using Backup.Configs;
using Backup.ObjectDatabase;
using Backup.ObjectDatabase.ObjectTypes;

namespace Backup;

public static class BackupRollback
{
    private static readonly Queue<ObjectReference> references = [];

    public static void Apply(string backupName)
    {
        foreach(ObjectReference reference in Database.ReadBackup(backupName))
            references.Enqueue(reference);

        Rollback();
    }

    private static void EnqueueTree(Tree tree)
    {
        foreach(ObjectReference reference in tree.References)
        {
            reference.PrependPath(tree.Name);
            references.Enqueue(reference);
        }
    }

    private static void Rollback()
    {   
        // sadly have to use var here since i dont like the yellow squiggle that the nullable generates
        while(references.TryDequeue(out var reference))
        {
            switch (reference.Format) {
                case ObjectFormat.TREE:
                    HandleTree(reference);
                    break;
                case ObjectFormat.BLOB:
                    HandleBlob(reference);
                    break;
            }
        }
    }

    private static void HandleTree(ObjectReference treeReference)
    {
        Tree tree = Database.ReadTree(treeReference);
        EnqueueTree(tree);
    }

    private static void HandleBlob(ObjectReference blobReference)
    {
        Database.ReadFile(blobReference);
    }
}