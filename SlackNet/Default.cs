﻿using System;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SlackNet.SocketMode;

namespace SlackNet;

public static class Default
{
    public static IHttp Http(SlackJsonSettings jsonSettings = null, Func<HttpClient> getHttpClient = null, ILogger logger = null) =>
        new Http(getHttpClient ?? HttpClientFactory(), jsonSettings ?? JsonSettings(), logger ?? Logger);

    private static Func<HttpClient> HttpClientFactory()
    {
        var httpClient = new HttpClient();
        return () => httpClient;
    }

    public static ISlackUrlBuilder UrlBuilder(SlackJsonSettings jsonSettings = null) => new SlackUrlBuilder(jsonSettings ?? JsonSettings());

    public static SlackJsonSettings JsonSettings(ISlackTypeResolver slackTypeResolver = null, ILogger logger = null) => 
        new(SerializerSettings(slackTypeResolver ?? SlackTypeResolver(), logger ?? Logger));

    private static JsonSerializerSettings SerializerSettings(ISlackTypeResolver slackTypeResolver, ILogger logger)
    {
        var namingStrategy = new SnakeCaseNamingStrategy();
        return new JsonSerializerSettings
            {
                TraceWriter = new SerializationLogger(logger),
                NullValueHandling = NullValueHandling.Ignore,
                DateFormatString = "yyyy-MM-dd",
                ContractResolver = new SlackNetContractResolver
                    {
                        NamingStrategy = namingStrategy,
                    },
                Converters =
                    {
                        new EnumNameConverter(namingStrategy),
                        new TimeSpanConverter(),
                        new SlackTypeConverter(slackTypeResolver, logger)
                    }
            };
    }

    public static ISlackTypeResolver SlackTypeResolver() => new SlackTypeResolver(AssembliesContainingSlackTypes);

    public static ISlackTypeResolver SlackTypeResolver(params Assembly[] assembliesContainingSlackTypes) => new SlackTypeResolver(assembliesContainingSlackTypes);

    public static Assembly[] AssembliesContainingSlackTypes => [typeof(Default).GetTypeInfo().Assembly];

    public static IWebSocketFactory WebSocketFactory => new WebSocketFactory();

    public static IScheduler Scheduler => System.Reactive.Concurrency.Scheduler.Default;

    public static SocketModeConnectionOptions SocketModeConnectionOptions => new();

    public static ILogger Logger => new NullLogger();
}