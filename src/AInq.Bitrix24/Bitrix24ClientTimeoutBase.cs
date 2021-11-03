// Copyright 2021 Anton Andryushchenko
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

namespace AInq.Bitrix24;

/// <summary> Bitrix24 REST API client with basic request timeout control (NOT thread safe) </summary>
public abstract class Bitrix24ClientTimeoutBase : Bitrix24ClientBase
{
    private Task _delay = Task.CompletedTask;

    /// <inheritdoc />
    protected Bitrix24ClientTimeoutBase(string portal, string clientId, string clientSecret, TimeSpan timeout,
        int maxTransientRetry = -1, int maxTimeoutRetry = -1, ILogger<IBitrix24Client>? logger = null, LogLevel logLevel = LogLevel.Debug)
        : base(portal, clientId, clientSecret, timeout, maxTransientRetry, maxTimeoutRetry, logger, logLevel) { }

    /// <inheritdoc />
    protected override async Task<JToken> GetRequestAsync(string method, CancellationToken cancellation = default)
    {
        await _delay.WaitAsync(cancellation).ConfigureAwait(false);
        _delay = Task.Delay(Timeout, default);
        return await base.GetRequestAsync(method, cancellation).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async Task<JToken> PostRequestAsync(string method, JToken data, CancellationToken cancellation = default)
    {
        await _delay.WaitAsync(cancellation).ConfigureAwait(false);
        _delay = Task.Delay(Timeout, default);
        return await base.PostRequestAsync(method, data, cancellation).ConfigureAwait(false);
    }
}
