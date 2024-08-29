using Newtonsoft.Json;

namespace GeneratedResourceClient.GenresObject;

public abstract class BaseGenResObject : IGenResObject
{
    public BaseGenResObject(string type)
    {
        Id = Guid.NewGuid();
        this.type = type;
    }
    public BaseGenResObject(Guid id, string type)
    {
        Id = id;
        this.type = type;
    }

    [JsonProperty("id")]
    public Guid Id { get; protected set; }
    public string type { get; private set; }
    public string NameShortRu { get; set; } = default!;
    public bool? needsMatch { get; set; }
    public override string ToString() => $"{type} : {NameShortRu}";
    public override bool Equals(object? obj) => obj != null && obj.GetHashCode() == GetHashCode();
    public override int GetHashCode() => HashCode.Combine(NameShortRu, type);
    public virtual string GetKey() => type + "_" + NameShortRu;
    public string _reference => GetKey();
}