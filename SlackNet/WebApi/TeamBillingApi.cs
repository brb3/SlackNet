﻿using System.Threading;
using System.Threading.Tasks;
using Args = System.Collections.Generic.Dictionary<string, object>;

namespace SlackNet.WebApi;

public interface ITeamBillingApi
{
    /// <summary>
    /// Reads a workspace's billing plan information.
    /// </summary>
    /// <remarks>See the <a href="https://api.slack.com/methods/team.billing.info">Slack documentation</a> for more information.</remarks>
    /// <param name="cancellationToken"></param>
    Task<BillingInfo> Info(CancellationToken cancellationToken = default);
}

public class TeamBillingApi : ITeamBillingApi
{
    private readonly ISlackApiClient _client;
    public TeamBillingApi(ISlackApiClient client) => _client = client;

    public Task<BillingInfo> Info(CancellationToken cancellationToken = default) => 
        _client.Get<BillingInfo>("team.billing.info", new Args(), cancellationToken);
}