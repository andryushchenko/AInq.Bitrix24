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

using Newtonsoft.Json.Linq;

namespace AInq.Bitrix24;

/// <summary> Bitrix24 REST API client interface </summary>
public interface IBitrix24Client
{
    /// <summary> Bitrix24 portal address </summary>
    string Portal { get; }

    /// <summary> Call GET method </summary>
    /// <param name="method"> REST method name </param>
    /// <param name="cancellation"> Cancellation token </param>
    Task<JToken> GetAsync(string method, CancellationToken cancellation = default);

    /// <summary> Call POST method </summary>
    /// <param name="method"> REST method name </param>
    /// <param name="data"> Call data </param>
    /// <param name="cancellation"> Cancellation token </param>
    Task<JToken> PostAsync(string method, JToken data, CancellationToken cancellation = default);
}
