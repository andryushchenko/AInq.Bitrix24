using AInq.Background.Tasks;
using AInq.Helpers.Polly;
using AInq.Optional;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Polly;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AInq.Bitrix24
{

public abstract class B24Client : IB24Client, IThrottling, IConveyorMachine<(string Method, JToken? Data), JToken>, IDisposable
{
    private const string AuthPath = "https://oauth.bitrix.info/oauth/token/";
    private readonly HttpClient _client;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly ILogger<IB24Client> _logger;
    private readonly IAsyncPolicy<HttpResponseMessage> _policy;
    private readonly string _portal;
    private readonly TimeSpan _timeout;

    protected B24Client(string portal, string clientId, string clientSecret, ILogger<IB24Client> logger, TimeSpan timeout)
    {
        _clientId = string.IsNullOrWhiteSpace(clientId) ? throw new ArgumentOutOfRangeException(nameof(clientId)) : clientId;
        _clientSecret = string.IsNullOrWhiteSpace(clientSecret) ? throw new ArgumentOutOfRangeException(nameof(clientSecret)) : clientSecret;
        _portal = string.IsNullOrWhiteSpace(portal) ? throw new ArgumentOutOfRangeException(nameof(portal)) : portal;
        _timeout = timeout > TimeSpan.Zero ? timeout : throw new ArgumentOutOfRangeException(nameof(timeout));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _client = new HttpClient {BaseAddress = new Uri($"https://{_portal}/rest/")};
        _policy = Policy.WrapAsync(HttpRetryPolicies.TransientRetryAsyncPolicy(),
            HttpRetryPolicies.TimeoutRetryAsyncPolicy(_timeout),
            Policy.HandleResult<HttpResponseMessage>(response => response.StatusCode == HttpStatusCode.Unauthorized)
                  .RetryAsync(async (_, _, ctx) => await AuthAsync(ctx.GetCancellationToken()).ConfigureAwait(false)));
    }

    Task<JToken> IB24Client.GetAsync(string method, CancellationToken cancellation)
        => GetRequestAsync(method, cancellation);

    Task<JToken> IB24Client.PostAsync(string method, JToken data, CancellationToken cancellation)
        => PostRequestAsync(method, data, cancellation);

    Task<JToken> IConveyorMachine<(string Method, JToken? Data), JToken>.ProcessDataAsync((string Method, JToken? Data) data,
        IServiceProvider provider, CancellationToken cancellation)
        => data.Data == null
            ? GetRequestAsync(data.Method, cancellation)
            : PostRequestAsync(data.Method, data.Data, cancellation);

    void IDisposable.Dispose()
        => _client.Dispose();

    TimeSpan IThrottling.Timeout => _timeout;

    private async Task<JToken> GetRequestAsync(string method, CancellationToken cancellation = default)
    {
        await InitAsync(cancellation).ConfigureAwait(false);
        using var result = await _policy.GetAsync(_client, method, _logger, cancellation).ConfigureAwait(false);
        return JToken.Parse(await result.Content.ReadAsStringAsync().ConfigureAwait(false));
    }

    private async Task<JToken> PostRequestAsync(string method, JToken data, CancellationToken cancellation = default)
    {
        await InitAsync(cancellation).ConfigureAwait(false);
        using var content = new StringContent(data.ToString(), Encoding.UTF8, "application/json");
        using var result = await _policy.PostAsync(_client, method, content, _logger, cancellation).ConfigureAwait(false);
        return JToken.Parse(await result.Content.ReadAsStringAsync().ConfigureAwait(false));
    }

    private async Task AuthAsync(CancellationToken cancellation)
    {
        var currentRefreshToken = await GetValue("b24_refresh").ConfigureAwait(false);
        if (currentRefreshToken.HasValue && await RefreshAsync(currentRefreshToken.Value, cancellation).ConfigureAwait(false)) return;
        await RemoveValue("b24_refresh").ConfigureAwait(false);
        while (!await AuthorizeAsync(cancellation).ConfigureAwait(false)) { }
    }

    private async Task<bool> AuthorizeAsync(CancellationToken cancellation)
    {
        var code = await GetAuthorizationCode(_portal, _clientId).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(code)) return false;
        using var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string?, string?>("grant_type", "authorization_code"),
            new KeyValuePair<string?, string?>("client_id", _clientId),
            new KeyValuePair<string?, string?>("client_secret", _clientSecret),
            new KeyValuePair<string?, string?>("code", code)
        });
        using var result = await HttpRetryPolicies.TransientRetryAsyncPolicy()
                                                  .PostAsync(AuthPath, content, _logger, cancellation)
                                                  .ConfigureAwait(false);
        if (!result.IsSuccessStatusCode) return false;
        await SetTokenAsync(await result.Content.ReadAsStringAsync().ConfigureAwait(false)).ConfigureAwait(false);
        return true;
    }

    private async Task<bool> RefreshAsync(string refreshToken, CancellationToken cancellation)
    {
        using var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string?, string?>("grant_type", "refresh_token"),
            new KeyValuePair<string?, string?>("client_id", _clientId),
            new KeyValuePair<string?, string?>("client_secret", _clientSecret),
            new KeyValuePair<string?, string?>("refresh_token", refreshToken)
        });
        using var result = await HttpRetryPolicies.TransientRetryAsyncPolicy()
                                                  .PostAsync(AuthPath, content, _logger, cancellation)
                                                  .ConfigureAwait(false);
        if (!result.IsSuccessStatusCode) return false;
        await SetTokenAsync(await result.Content.ReadAsStringAsync().ConfigureAwait(false)).ConfigureAwait(false);
        return true;
    }

    private async ValueTask SetTokenAsync(string httpData)
    {
        var data = JToken.Parse(httpData);
        var refresh = data.Value<string>("refresh_token")!;
        await StoreValue("b24_refresh", refresh).ConfigureAwait(false);
        var access = data.Value<string>("access_token")!;
        await StoreValue("b24_access", access).ConfigureAwait(false);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access);
    }

    private async ValueTask InitAsync(CancellationToken cancellation)
    {
        if (_client.DefaultRequestHeaders.Authorization == null)
        {
            var accessToken = await GetValue("b24_access").ConfigureAwait(false);
            if (accessToken.HasValue)
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Value);
            else await AuthAsync(cancellation).ConfigureAwait(false);
        }
    }

    protected abstract Task<string> GetAuthorizationCode(string portal, string clientId);
    protected abstract ValueTask<Maybe<string>> GetValue(string key);
    protected abstract ValueTask StoreValue(string key, string value);
    protected abstract ValueTask RemoveValue(string key);
}

}
