using System.Text;
using Backup.ObjectDatabase.Hashing;

namespace Backup.ObjectDatabase.ObjectTypes;

public class Tree
{
    public string Name { get; private set; }
    public IEnumerable<ObjectReference> References { get => references.Values; }

    private readonly Dictionary<Hash, ObjectReference> references = [];

    public Tree(string name) :
        this(name, [])
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
        references.Add(reference.Pointer, reference);

    public static Tree Parse(string name, string[] contents)
    {
        List<ObjectReference> references = [];
        foreach (string line in contents)
            references.Add(ObjectReference.Parse(line));

        return new(name, references);
    }

    public void PrependRefernces()
    {
        foreach (ObjectReference reference in References)
            reference.PrependPath(Name);
    }

    public bool TryGetReferenceByPointer(Hash hash, out ObjectReference? reference) =>
        references.TryGetValue(hash, out reference);

    public override string ToString()
    {
        StringBuilder output = new();

        foreach (ObjectReference reference in References)
            output.AppendLine(reference.ToString());

        return output.ToString();
    }
}
