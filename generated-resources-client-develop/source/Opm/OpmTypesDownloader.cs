using AutoMapper;
using CSharpCustomExtensions;
using CSharpCustomExtensions.Flow;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Nntc.ObjectModel;
using Nntc.ObjectModel.Dto;
using Nntc.ObjectModel.Dto.Mapping;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;

namespace GeneratedResourceClient.Opm;

public class OpmTypesDownloader
{
    private readonly AsyncRetryPolicy<HttpResponseMessage> _policy;
    private readonly HttpClient _client;
    private readonly ILogger _logger;

    public OpmTypesDownloader(Uri opmMetadataAddress, ILogger logger, int retryCount = 7)
    {
        _logger = logger;

        _policy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .RetryAsync(retryCount, onRetry: async (outcome, retryAttempt) => await LogRetryAndWait(retryCount, outcome, retryAttempt));

        _client = new HttpClient() { BaseAddress = opmMetadataAddress };
    }

    private async Task LogRetryAndWait(int retryCount, DelegateResult<HttpResponseMessage> outcome, int retryAttempt)
    {
        if (outcome.Exception != null)
        {
            _logger.LogError($"Попытка {retryAttempt} из за ошибки: {outcome.Exception.Message}");
        }
        else
        {
            _logger.LogInformation($"Попытка {retryAttempt}");
        }

        int remainingRetries = retryCount - retryAttempt;
        _logger.LogInformation($"Осталось попыток: {remainingRetries}");

        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    public async Task<Metadata> GetMetadata()
    {
        var mapper = new Mapper(new MapperConfiguration(x => { x.AddProfile<MetadataMappingProfile>(); }));

        //var url = new Uri("/api/export", UriKind.Relative);
        var ct = CancellationToken.None;

        var resp = await _policy.ExecuteAndCaptureAsync(x => _client.GetAsync("", ct), ct);
        if (resp.FinalException != default)
            throw resp.FinalException;

        var metadata = await resp.Result
            .Pipe(x => x.ReadContentAsync())
            .PipeAsync(JsonConvert.DeserializeObject<MetadataDto>)
            .PipeAsync(mapper.Map<Metadata>);

        _logger.LogInformation("Метаданные успешно выгружены");

        return metadata;
    }
}