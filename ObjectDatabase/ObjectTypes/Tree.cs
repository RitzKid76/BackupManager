using System.Text;

namespace Backup.ObjectDatabase.ObjectTypes;

public record Tree(string Name, List<ObjectReference> References)
{
    public Tree(string name) :
        this(name, [])
    { }

    public string GetData()
    {
        StringBuilder output = new();

        foreach (ObjectReference reference in References)
            output.AppendLine(reference.ToString());

        return output.ToString();
    }

    public void AddReference(ObjectReference reference) =>
        References.Add(reference);
}
