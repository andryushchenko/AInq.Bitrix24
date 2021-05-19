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

using System;
using System.Runtime.Serialization;

namespace AInq.Bitrix24
{

/// <summary> Bitrix24 REST API call exception  </summary>
[Serializable]
public class Bitrix24CallException : Exception
{
    private const string DefaultMessage = "Error calling Bitrix24 REST API";

    /// <param name="method"> REST API method name </param>
    public Bitrix24CallException(string method) : base(DefaultMessage)
        => Method = method;

    /// <param name="method"> REST API method name </param>
    /// <param name="message"> Error message </param>
    public Bitrix24CallException(string method, string message) : base(message)
        => Method = method;

    /// <param name="method"> REST API method name </param>
    /// <param name="innerException"> Inner exception </param>
    public Bitrix24CallException(string method, Exception innerException) : base(DefaultMessage, innerException)
        => Method = method;

    /// <param name="method"> REST API method name </param>
    /// ///
    /// <param name="message"> Error message </param>
    /// ///
    /// <param name="innerException"> Inner exception </param>
    public Bitrix24CallException(string method, string message, Exception innerException) : base(message, innerException)
        => Method = method;

    /// <inheritdoc />
    protected Bitrix24CallException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    /// <summary>  REST API method name </summary>
    public string Method { get; } = string.Empty;
}

}
