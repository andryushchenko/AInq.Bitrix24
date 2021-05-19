﻿// Copyright 2021 Anton Andryushchenko
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using AInq.Helpers.Polly;
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

/// <summary> Bitrix24 REST API client base </summary>
public abstract class Bitrix24ClientBase : IBitrix24Client, IDisposable
{
    private const string AuthPath = "https://oauth.bitrix.info/oauth/token/";
    private readonly IAsyncPolicy<HttpResponseMessage> _authPolicy;
    private readonly HttpClient _client;
    private readonly string _clientSecret;
    private readonly int _maxRetry;
    private readonly IAsyncPolicy<HttpResponseMessage> _requestPolicy;

    /// <summary> OAuth application Client ID </summary>
    protected readonly string ClientId;

    /// <summary> Logger instance </summary>
    protected readonly ILogger<IBitrix24Client> Logger;

    /// <summary> Bitrix24 portal </summary>
    protected readonly string Portal;

    /// <summary> Request timeout </summary>
    protected readonly TimeSpan Timeout;

    /// <param name="portal"> Bitrix24 portal </param>
    /// <param name="clientId"> OAuth application Client ID </param>
    /// <param name="clientSecret"> OAuth application Client Secret</param>
    /// <param name="logger"> Logger instance </param>
    /// <param name="timeout"> Request timeout </param>
    /// <param name="maxTransientRetry"> Maximum retry count on transient HTTP errors (-1 for retry forever) </param>
    /// <param name="maxTimeoutRetry"> Maximum retry count on HTTP 429 (-1 for retry forever) </param>
    protected Bitrix24ClientBase(string portal, string clientId, string clientSecret, ILogger<IBitrix24Client> logger, TimeSpan timeout,
        int maxTransientRetry = -1, int maxTimeoutRetry = -1)
    {
        ClientId = string.IsNullOrWhiteSpace(clientId) ? throw new ArgumentOutOfRangeException(nameof(clientId)) : clientId;
        _clientSecret = string.IsNullOrWhiteSpace(clientSecret) ? throw new ArgumentOutOfRangeException(nameof(clientSecret)) : clientSecret;
        Portal = string.IsNullOrWhiteSpace(portal) ? throw new ArgumentOutOfRangeException(nameof(portal)) : portal;
        Timeout = timeout > TimeSpan.Zero ? timeout : throw new ArgumentOutOfRangeException(nameof(timeout));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _client = new HttpClient {BaseAddress = new Uri($"https://{Portal}/rest/")};
        _requestPolicy = Policy.WrapAsync(maxTransientRetry switch
            {
                -1 => HttpRetryPolicies.TransientRetryAsyncPolicy(),
                >0 => HttpRetryPolicies.TransientRetryAsyncPolicy(maxTransientRetry),
                _ => throw new ArgumentOutOfRangeException(nameof(maxTransientRetry))
            },
            maxTimeoutRetry switch
            {
                -1 => HttpRetryPolicies.TimeoutRetryAsyncPolicy(Timeout),
                >0 => HttpRetryPolicies.TimeoutRetryAsyncPolicy(Timeout, maxTimeoutRetry),
                _ => throw new ArgumentOutOfRangeException(nameof(maxTimeoutRetry))
            },
            Policy.HandleResult<HttpResponseMessage>(response => response.StatusCode == HttpStatusCode.Unauthorized)
                  .RetryAsync(async (_, _, ctx) => await AuthAsync(ctx.GetCancellationToken()).ConfigureAwait(false)));
        _authPolicy = maxTransientRetry > 0
            ? HttpRetryPolicies.TransientRetryAsyncPolicy(_maxRetry)
            : HttpRetryPolicies.TransientRetryAsyncPolicy();
        _maxRetry = maxTransientRetry;
    }

    Task<JToken> IBitrix24Client.GetAsync(string method, CancellationToken cancellation)
        => GetRequestAsync(method, cancellation);

    Task<JToken> IBitrix24Client.PostAsync(string method, JToken data, CancellationToken cancellation)
        => PostRequestAsync(method, data, cancellation);

    void IDisposable.Dispose()
        => _client.Dispose();

    /// <inheritdoc cref="IBitrix24Client.GetAsync" />
    protected virtual async Task<JToken> GetRequestAsync(string method, CancellationToken cancellation = default)
    {
        await InitAsync(cancellation).ConfigureAwait(false);
        string result;
        HttpStatusCode status;
        try
        {
            using var response = await _requestPolicy.GetAsync(_client, method, Logger, cancellation).ConfigureAwait(false);
            status = response.StatusCode;
#if NET5_0
            result = await response.Content.ReadAsStringAsync(cancellation).ConfigureAwait(false);
#else
            result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
        }
        catch (Exception ex)
        {
            throw new Bitrix24CallException(method, ex);
        }
        if (status == HttpStatusCode.OK)
            try
            {
                return JToken.Parse(result);
            }
            catch (Exception ex)
            {
                throw new Bitrix24CallException(method, "Error parsing response", ex);
            }
        var exception = new Bitrix24CallException(method);
        exception.Data["Status"] = status;
        exception.Data["Response"] = result;
        throw exception;
    }

    /// <inheritdoc cref="IBitrix24Client.PostAsync" />
    protected virtual async Task<JToken> PostRequestAsync(string method, JToken data, CancellationToken cancellation = default)
    {
        await InitAsync(cancellation).ConfigureAwait(false);
        string result;
        HttpStatusCode status;
        try
        {
            using var content = new StringContent(data.ToString(), Encoding.UTF8, "application/json");
            using var response = await _requestPolicy.PostAsync(_client, method, content, Logger, cancellation).ConfigureAwait(false);
            status = response.StatusCode;
#if NET5_0
            result = await response.Content.ReadAsStringAsync(cancellation).ConfigureAwait(false);
#else
            result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
        }
        catch (Exception ex)
        {
            throw new Bitrix24CallException(method, ex);
        }
        if (status == HttpStatusCode.OK)
            try
            {
                return JToken.Parse(result);
            }
            catch (Exception ex)
            {
                throw new Bitrix24CallException(method, "Error parsing response", ex);
            }
        var exception = new Bitrix24CallException(method);
        exception.Data["Status"] = status;
        exception.Data["Response"] = result;
        throw exception;
    }

    private async Task AuthAsync(CancellationToken cancellation)
    {
        await RemoveAccessToken().ConfigureAwait(false);
        var currentRefreshToken = await GetRefreshToken().ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(currentRefreshToken) && await RefreshAsync(currentRefreshToken, cancellation).ConfigureAwait(false)) return;
        await RemoveRefreshToken().ConfigureAwait(false);
        while (!await AuthorizeAsync(cancellation).ConfigureAwait(false)) { }
    }

    private async Task<bool> AuthorizeAsync(CancellationToken cancellation)
    {
        var code = await GetAuthorizationCode(cancellation).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(code)) return false;
        using var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string?, string?>("grant_type", "authorization_code"),
            new KeyValuePair<string?, string?>("client_id", ClientId),
            new KeyValuePair<string?, string?>("client_secret", _clientSecret),
            new KeyValuePair<string?, string?>("code", code)
        });
        using var result = await _authPolicy.PostAsync(AuthPath, content, Logger, cancellation).ConfigureAwait(false);
        if (!result.IsSuccessStatusCode) return false;
#if NET5_0
        var data = JToken.Parse(await result.Content.ReadAsStringAsync(cancellation).ConfigureAwait(false));
#else
        var data = JToken.Parse(await result.Content.ReadAsStringAsync().ConfigureAwait(false));
#endif
        await SetTokenAsync(data).ConfigureAwait(false);
        return true;
    }

    private async Task<bool> RefreshAsync(string refreshToken, CancellationToken cancellation)
    {
        using var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string?, string?>("grant_type", "refresh_token"),
            new KeyValuePair<string?, string?>("client_id", ClientId),
            new KeyValuePair<string?, string?>("client_secret", _clientSecret),
            new KeyValuePair<string?, string?>("refresh_token", refreshToken)
        });
        using var result = await _authPolicy.PostAsync(AuthPath, content, Logger, cancellation).ConfigureAwait(false);
        if (!result.IsSuccessStatusCode) return false;
#if NET5_0
        var data = JToken.Parse(await result.Content.ReadAsStringAsync(cancellation).ConfigureAwait(false));
#else
        var data = JToken.Parse(await result.Content.ReadAsStringAsync().ConfigureAwait(false));
#endif
        await SetTokenAsync(data).ConfigureAwait(false);
        return true;
    }

    private async ValueTask SetTokenAsync(JToken data)
    {
        var refresh = data.Value<string>("refresh_token")!;
        await StoreRefreshToken(refresh).ConfigureAwait(false);
        var access = data.Value<string>("access_token")!;
        await StoreAccessToken(access).ConfigureAwait(false);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access);
    }

    private async ValueTask InitAsync(CancellationToken cancellation)
    {
        if (_client.DefaultRequestHeaders.Authorization == null)
        {
            var accessToken = await GetAccessToken().ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            else await AuthAsync(cancellation).ConfigureAwait(false);
        }
    }

    /// <summary> Obtain OAuth Authorization Code </summary>
    /// <param name="cancellation"> Cancellation token </param>
    protected abstract ValueTask<string> GetAuthorizationCode(CancellationToken cancellation);

    /// <summary> Get Access token from persistent storage </summary>
    protected abstract ValueTask<string> GetAccessToken();

    /// <summary> Save Access token to persistent storage </summary>
    /// <param name="token"> Access token</param>
    protected abstract ValueTask StoreAccessToken(string token);

    /// <summary> Remove Access token from persistent storage </summary>
    protected abstract ValueTask RemoveAccessToken();

    /// <summary> Get Refresh token from persistent storage </summary>
    protected abstract ValueTask<string> GetRefreshToken();

    /// <summary> Save Refresh token to persistent storage </summary>
    /// <param name="token"> Refresh token</param>
    protected abstract ValueTask StoreRefreshToken(string token);

    /// <summary> Remove Refresh token from persistent storage </summary>
    protected abstract ValueTask RemoveRefreshToken();
}

}
