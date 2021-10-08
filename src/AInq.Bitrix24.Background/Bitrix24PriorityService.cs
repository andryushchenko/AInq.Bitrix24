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

using AInq.Background.Services;

namespace AInq.Bitrix24;

/// <summary> Bitrix24 client service with prioritization </summary>
internal sealed class Bitrix24PriorityService : IBitrix24Client, IBitrix24PriorityService
{
    private readonly IPriorityConveyor<(string, JToken?), JToken> _conveyor;

    internal Bitrix24PriorityService(IPriorityConveyor<(string, JToken?), JToken> conveyor)
        => _conveyor = conveyor ?? throw new ArgumentNullException(nameof(conveyor));

    async Task<JToken> IBitrix24Client.GetAsync(string method, CancellationToken cancellation)
        => await _conveyor.ProcessDataAsync((method, null), cancellation);

    async Task<JToken> IBitrix24Client.PostAsync(string method, JToken data, CancellationToken cancellation)
        => await _conveyor.ProcessDataAsync((method, data), cancellation);

    int IBitrix24PriorityService.MaxPriority => _conveyor.MaxPriority;

    IBitrix24Client IBitrix24PriorityService.GetPriorityClient(int priority)
        => new Bitrix24PriorityProxy(this, Math.Min(_conveyor.MaxPriority, Math.Max(0, priority)));

    private class Bitrix24PriorityProxy : IBitrix24Client
    {
        private readonly Bitrix24PriorityService _owner;
        private readonly int _priority;

        public Bitrix24PriorityProxy(Bitrix24PriorityService owner, int priority)
        {
            _owner = owner;
            _priority = priority;
        }

        async Task<JToken> IBitrix24Client.GetAsync(string method, CancellationToken cancellation)
            => await _owner._conveyor.ProcessDataAsync((method, null), _priority, cancellation);

        async Task<JToken> IBitrix24Client.PostAsync(string method, JToken data, CancellationToken cancellation)
            => await _owner._conveyor.ProcessDataAsync((method, data), _priority, cancellation);
    }
}
