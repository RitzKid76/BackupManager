using System.Text;

namespace Backup.ObjectDatabase.ObjectTypes;

public class Tree
{
    public string Name { get; private set; }
    public IEnumerable<ObjectReference> References => references;

    private readonly List<ObjectReference> references = [];

    public Tree(string name) :
        this(name, [])
    { }

    public Tree(List<ObjectReference> references) :
        this(string.Empty, references)
    { }

    private Tree(string name, List<ObjectReference> references)
    {
        Name = name;
        AddReferences(references);
    }

    private void AddReferences(List<ObjectReference> references)
    {
        foreach (ObjectReference reference in references)
            AddReference(reference);
    }

    public void AddReference(ObjectReference reference) =>
        references.Add(reference);

    public static Tree Parse(string name, string[] contents)
    {
        List<ObjectReference> references = [];
        foreach (string line in contents)
        {
            ObjectReference.TryParse(line, out ObjectReference? reference);
            references.Add(reference!);
        }

        return new(name, references);
    }

    public void PrependRefernces()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return;

        foreach (ObjectReference reference in References)
            reference.PrependPath(Name);
    }

    public override string ToString()
    {
        StringBuilder output = new();

        foreach (ObjectReference reference in References)
            output.AppendLine(reference.ToString());

        return output.ToString();
    }
}
