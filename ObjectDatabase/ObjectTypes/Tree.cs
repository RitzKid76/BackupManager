using System.Text;

namespace Backup.ObjectDatabase.ObjectTypes;

public record Tree(string Name, List<ObjectReference> References)
{
    public Tree(string name) :
        this(name, [])
    { }

    public void AddReference(ObjectReference reference) =>
        References.Add(reference);

    public static Tree Parse(string name, string[] contents)
    {
        List<ObjectReference> references = [];
        foreach (string line in contents)
            references.Add(ObjectReference.Parse(line));

        return new(name, references);
    }

    public override string ToString()
    {
        StringBuilder output = new();

        foreach (ObjectReference reference in References)
            output.AppendLine(reference.ToString());

        return output.ToString();
    }
}
