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

internal sealed class Bitrix24ConveyorProxy : IBitrix24Client
{
    private IConveyor<(string, JToken?), JToken>? _conveyor;
    private string? _portal;

    internal IConveyor<(string, JToken?), JToken> Conveyor
    {
        set => _conveyor ??= value ?? throw new ArgumentNullException();
    }

    internal string Portal
    {
        set => _portal ??= value ?? throw new ArgumentNullException();
    }

    string IBitrix24Client.Portal => _portal ?? throw new InvalidOperationException();

    async Task<JToken> IBitrix24Client.GetAsync(string method, CancellationToken cancellation)
        => await (_conveyor?.ProcessDataAsync((method, null), cancellation) ?? throw new InvalidOperationException());

    async Task<JToken> IBitrix24Client.PostAsync(string method, JToken data, CancellationToken cancellation)
        => await (_conveyor?.ProcessDataAsync((method, data), cancellation) ?? throw new InvalidOperationException());
}
