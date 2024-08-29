namespace GeneratedResourceClient.Graph;

public class Relation : IEquatable<Relation>
{
    public Relation(Guid from, Guid to)
    {
        From = from;
        To = to;
    }

    public Guid From { get; set; }
    public Guid To { get; set; }

    public bool Equals(Relation? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return From.Equals(other.From) && To.Equals(other.To);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Relation)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(From, To);
    }
}