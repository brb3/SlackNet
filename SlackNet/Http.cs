﻿using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SlackNet;

public interface IHttp
{
    Task<T> Execute<T>(HttpRequestMessage requestMessage, CancellationToken cancellationToken = default);
}

class Http(Func<HttpClient> getHttpClient, SlackJsonSettings jsonSettings, ILogger logger) : IHttp
{
    private readonly ILogger _log = logger.ForSource<Http>();

    public async Task<T> Execute<T>(HttpRequestMessage requestMessage, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;

        var requestLog = requestMessage.Content is null
            ? _log
            : _log.WithContext("RequestBody", await requestMessage.Content.ReadAsStringAsync().ConfigureAwait(false));

        try
        {
            response = await getHttpClient().SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
            requestLog
                .WithContext("ResponseStatus", response.StatusCode)
                .WithContext("ResponseReason", response.ReasonPhrase)
                .WithContext("ResponseHeaders", response.Headers)
                .WithContext("ResponseBody", await response.Content.ReadAsStringAsync().ConfigureAwait(false))
                .Data("Sent {RequestMethod} request to {RequestUrl}", requestMessage.Method, requestMessage.RequestUri);
        }
        catch (Exception e)
        {
            requestLog.Error(e, "Error sending {RequestMethod} request to {RequestUrl}", requestMessage.Method, requestMessage.RequestUri);
            throw;
        }

        if ((int)response.StatusCode == 429) // TODO use the enum when TooManyRequests becomes available
            throw new SlackRateLimitException(response.Headers.RetryAfter.Delta);
        response.EnsureSuccessStatusCode();

        return response.Content.Headers.ContentType.MediaType == "application/json"
            ? await Deserialize<T>(response).ConfigureAwait(false)
            : default;
    }

    private async Task<T> Deserialize<T>(HttpResponseMessage response)
    {
        using var jsonTextReader = new JsonTextReader(new StreamReader(await response.Content.ReadAsStreamAsync().ConfigureAwait(false)));
        return JsonSerializer.Create(jsonSettings.SerializerSettings).Deserialize<T>(jsonTextReader);
    }
}

public class SlackRateLimitException(TimeSpan? retryAfter) : Exception
{
    public TimeSpan? RetryAfter { get; } = retryAfter;
}