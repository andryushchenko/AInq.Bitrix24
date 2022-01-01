// Copyright 2021-2022 Anton Andryushchenko
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

using AInq.Background.Tasks;

namespace AInq.Bitrix24;

internal sealed class Bitrix24ConveyorMachine : IConveyorMachine<(string, JToken?), JToken>, IThrottling
{
    private readonly IBitrix24Client _client;
    private readonly TimeSpan _timeout;
    private DateTime _nextCall = DateTime.MinValue;

    public Bitrix24ConveyorMachine(IBitrix24Client client, TimeSpan timeout)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _timeout = timeout >= TimeSpan.Zero
            ? timeout
            : throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Must be greater than or equal to 00:00:00.000");
    }

    async Task<JToken> IConveyorMachine<(string, JToken?), JToken>.ProcessDataAsync((string, JToken?) data, IServiceProvider provider,
        CancellationToken cancellation)
    {
        _nextCall = DateTime.UtcNow.Add(_timeout);
        var (method, postData) = data;
        return postData == null
            ? await _client.GetAsync(method, cancellation).ConfigureAwait(false)
            : await _client.PostAsync(method, postData, cancellation).ConfigureAwait(false);
    }

    TimeSpan IThrottling.Timeout
    {
        get
        {
            if (_timeout == TimeSpan.Zero) return TimeSpan.Zero;
            var now = DateTime.UtcNow;
            return now < _nextCall ? _nextCall.Subtract(now) : TimeSpan.Zero;
        }
    }
}
