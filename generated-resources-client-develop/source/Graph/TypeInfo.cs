using Nntc.ObjectModel;

namespace GeneratedResourceClient.Graph;

public enum Multiplicity
{
    One,
    Many,
    None
}

public record TypeInfo((Multiplicity, Multiplicity) Quantity, RelationshipDirection Direction, (string From, string To) TypeNames);