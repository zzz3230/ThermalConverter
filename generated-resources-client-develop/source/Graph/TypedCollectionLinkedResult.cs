using GeneratedResourceClient.GraphMaster.Tools;

namespace GeneratedResourceClient.Graph;

public class TypedCollectionLinkedResult
{
    public TypedCollectionLinkedResult(IDictionary<(string, string), HashSet<Relation>> relations, IGeneratedResourcesCollection generatedResourcesCollection)
    {
        Relations = relations;
        GeneratedResourcesCollection = generatedResourcesCollection;
    }

    public IDictionary<(string From, string To), HashSet<Relation>> Relations { get; }
    public IGeneratedResourcesCollection GeneratedResourcesCollection { get; }
}