namespace GeneratedResourceClient.GenresObject;

public interface IGenResObject
{
    public Guid Id { get; }
    public string NameShortRu { get; set; }
    public string type { get; }
    public string GetKey();
    public string _reference { get; }
}
