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

using AInq.Background.Services;

namespace AInq.Bitrix24;

internal sealed class Bitrix24PriorityConveyorProxy : IBitrix24Client, IBitrix24PriorityService
{
    private IPriorityConveyor<(string, JToken?), JToken>? _conveyor;
    private string? _portal;

    internal IPriorityConveyor<(string, JToken?), JToken> Conveyor
    {
        set => _conveyor ??= value ?? throw new ArgumentNullException(nameof(value));
    }

    internal string Portal
    {
        set => _portal ??= value ?? throw new ArgumentNullException(nameof(value));
    }

    string IBitrix24Client.Portal => _portal ?? throw new InvalidOperationException();

    async Task<JToken> IBitrix24Client.GetAsync(string method, CancellationToken cancellation)
        => await (_conveyor?.ProcessDataAsync((method, null), cancellation: cancellation) ?? throw new InvalidOperationException())
            .ConfigureAwait(false);

    async Task<JToken> IBitrix24Client.PostAsync(string method, JToken data, CancellationToken cancellation)
        => await (_conveyor?.ProcessDataAsync((method, data), cancellation: cancellation) ?? throw new InvalidOperationException())
            .ConfigureAwait(false);

    int IBitrix24PriorityService.MaxPriority => _conveyor?.MaxPriority ?? throw new InvalidOperationException();

    IBitrix24Client IBitrix24PriorityService.GetPriorityClient(int priority)
        => new Bitrix24PriorityProxy(this, Math.Min(_conveyor?.MaxPriority ?? throw new InvalidOperationException(), Math.Max(0, priority)));

    private sealed class Bitrix24PriorityProxy : IBitrix24Client
    {
        private readonly Bitrix24PriorityConveyorProxy _owner;
        private readonly int _priority;

        internal Bitrix24PriorityProxy(Bitrix24PriorityConveyorProxy owner, int priority)
        {
            _owner = owner;
            _priority = priority;
        }

        string IBitrix24Client.Portal => _owner._portal ?? throw new InvalidOperationException();

        async Task<JToken> IBitrix24Client.GetAsync(string method, CancellationToken cancellation)
            => await (_owner._conveyor?.ProcessDataAsync((method, null), _priority, cancellation) ?? throw new InvalidOperationException())
                .ConfigureAwait(false);

        async Task<JToken> IBitrix24Client.PostAsync(string method, JToken data, CancellationToken cancellation)
            => await (_owner._conveyor?.ProcessDataAsync((method, data), _priority, cancellation) ?? throw new InvalidOperationException())
                .ConfigureAwait(false);
    }
}
