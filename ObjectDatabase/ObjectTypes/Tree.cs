using System.Text;

namespace Backup.ObjectDatabase.ObjectTypes;

public class Tree
{
    public string FullName { get; private set; }

    public string Name { get; private set; }
    public IEnumerable<ObjectReference> References => references;

    private readonly List<ObjectReference> references = [];

    public Tree(string fullName) :
        this(fullName, fullName, [])
    { }

    public Tree(List<ObjectReference> references) :
        this(string.Empty, string.Empty, references)
    { }

    private Tree(string fullName, string name, List<ObjectReference> references)
    {
        FullName = fullName;
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

    public static Tree Parse(string fullName, string name, string[] contents)
    {
        List<ObjectReference> references = [];
        foreach (string line in contents)
        {
            ObjectReference.TryParse(line, out ObjectReference? reference);
            references.Add(reference!);
        }

        Tree output = new(fullName, name, references);
        output.PrependReferences();

        return output;
    }

    private void PrependReferences()
    {
        if (string.IsNullOrWhiteSpace(FullName))
            return;

        foreach (ObjectReference reference in References)
            reference.SetFullName(FullName);
    }

    public override string ToString()
    {
        StringBuilder output = new();

        foreach (ObjectReference reference in References.OrderBy(r => r.Pointer.ToString()))
            output.AppendLine(reference.ToString());

        return output.ToString();
    }
}
