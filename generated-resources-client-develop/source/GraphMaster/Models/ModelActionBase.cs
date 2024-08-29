
public class ModelAction
{
    public CreateModelAction? Create { get; set; }
    public UpdateModelAction? Update { get; set; }
    public UpsertModelAction? Upsert { get; set; }
    public CreateIfNotExistsModelAction? CreateIfNotExists { get; set; }
    public CopyModelAction? CopyFrom { get; set; }
}

// Базовый абстрактный класс для ModelAction
public abstract class ModelActionBase
{
    public string NameShortRu { get; set; }
    public int? GroupId { get; set; }
    public int? ParentModelId { get; set; }
    public string Comments { get; set; }
    public string Type { get; set; }
    public List<int> TagIds { get; set; }
    public bool IsDeleted { get; set; }
}

// Класс для действия "create"
public class CreateModelAction : ModelActionBase { }

// Класс для действия "update"
public class UpdateModelAction : ModelActionBase { }

// Класс для действия "upsert"
public class UpsertModelAction : ModelActionBase { }

// Класс для действия "createIfNotExists"
public class CreateIfNotExistsModelAction : ModelActionBase { }

// Класс для действия "copyFrom"
public class CopyModelAction : ModelActionBase
{
    public int SourceModelId { get; set; }
    public bool CopyData { get; set; }
    public bool CopyTags { get; set; }
    public bool CopyAttributes { get; set; }
}