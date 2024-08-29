namespace GeneratedResourceClient.GraphMaster.Preprocessor;

public interface IResourceUploadPreprocessor
{
    /// <summary>
    /// Конверитирует обьекты для загрузки в ГР
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    IDictionary<string, object> Process(IDictionary<string, object> source);
}