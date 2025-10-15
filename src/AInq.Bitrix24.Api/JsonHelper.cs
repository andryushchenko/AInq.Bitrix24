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

using System.Globalization;

namespace AInq.Bitrix24;

/// <summary> JSON data reading methods to handle some strange Bitrix24 behaviour </summary>
public static class JsonHelper
{
    /// <summary> Try get <see cref="float" /> value </summary>
    /// <param name="token"> JSON source </param>
    [PublicAPI]
    public static Maybe<float> TryGetFloat(this JToken? token)
        => token?.Type switch
        {
            JTokenType.Float or JTokenType.Integer => token.Value<float>(),
#if NETSTANDARD
            JTokenType.String => float.TryParse(token.Value<string>(), NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
                                 || float.TryParse(token.Value<string>(), out result)
#else
            JTokenType.String => float.TryParse(token.Value<string>(), CultureInfo.InvariantCulture, out var result)
                                 || float.TryParse(token.Value<string>(), out result)
#endif
                ? result
                : Maybe.None<float>(),
            _ => Maybe.None<float>()
        };

    /// <summary> Try get <see cref="float" /> value </summary>
    /// <param name="token"> JSON source </param>
    /// <param name="paramName"> Property name </param>
    [PublicAPI]
    public static Maybe<float> TryGetFloat(this JToken? token, string paramName)
        => (token?[paramName]).TryGetFloat();

    /// <summary> Try get <see cref="int" /> value </summary>
    /// <param name="token"> JSON source </param>
    [PublicAPI]
    public static Maybe<int> TryGetInt(this JToken? token)
        => token?.Type switch
        {
            JTokenType.Integer => token.Value<int>(),
            JTokenType.String => int.TryParse(token.Value<string>(), out var result)
                ? result
                : Maybe.None<int>(),
            _ => Maybe.None<int>()
        };

    /// <summary> Try get <see cref="int" /> value </summary>
    /// <param name="token"> JSON source </param>
    /// <param name="paramName"> Property name </param>
    [PublicAPI]
    public static Maybe<int> TryGetInt(this JToken? token, string paramName)
        => (token?[paramName]).TryGetInt();

    /// <summary> Try get <see cref="long" /> value </summary>
    /// <param name="token"> JSON source </param>
    [PublicAPI]
    public static Maybe<long> TryGetLong(this JToken? token)
        => token?.Type switch
        {
            JTokenType.Integer => token.Value<long>(),
            JTokenType.String => long.TryParse(token.Value<string>(), out var result)
                ? result
                : Maybe.None<long>(),
            _ => Maybe.None<long>()
        };

    /// <summary> Try get <see cref="long" /> value </summary>
    /// <param name="token"> JSON source </param>
    /// <param name="paramName"> Property name </param>
    [PublicAPI]
    public static Maybe<long> TryGetLong(this JToken? token, string paramName)
        => (token?[paramName]).TryGetLong();

    /// <summary> Try get <see cref="int" /> values array </summary>
    /// <param name="token"> JSON source </param>
    /// <param name="ignoreTypeMismatch"> Skip values that can't be parsed, otherwise return empty array </param>
    [PublicAPI]
    public static int[] TryGetIntArray(this JToken? token, bool ignoreTypeMismatch = true)
        => token?.Type switch
        {
            JTokenType.Array => !ignoreTypeMismatch
                                && token.Any(item => item.Type is not (JTokenType.Integer or JTokenType.Float or JTokenType.String))
                ? []
                : token.Select(item => item.TryGetInt()).Values().ToArray(),
            JTokenType.Integer => [token.Value<int>()],
            JTokenType.String => int.TryParse(token.Value<string>(), out var result)
                ? new[] {result}
                : Array.Empty<int>(),
            JTokenType.Float => [(int) token.Value<float>()],
            _ => []
        };

    /// <summary> Try get <see cref="int" /> values array </summary>
    /// <param name="token"> JSON source </param>
    /// <param name="paramName"> Property name </param>
    /// <param name="ignoreTypeMismatch"> Skip values that can't be parsed, otherwise return empty array </param>
    [PublicAPI]
    public static int[] TryGetIntArray(this JToken? token, string paramName, bool ignoreTypeMismatch = true)
        => (token?[paramName]).TryGetIntArray(ignoreTypeMismatch);

    /// <summary> Try get <see cref="bool" /> value </summary>
    /// <param name="token"> JSON source </param>
    [PublicAPI]
    public static Maybe<bool> TryGetBool(this JToken? token)
        => token?.Type switch
        {
            JTokenType.Boolean => token.Value<bool>(),
            JTokenType.Integer => token.Value<int>() switch
            {
                0 => false,
                1 => true,
                _ => Maybe.None<bool>()
            },
            JTokenType.String => bool.TryParse(token.Value<string>(), out var boolResult)
                ? boolResult
                : token.Value<string>()?.ToUpperInvariant().Trim() switch
                {
                    "Y" or "1" => true,
                    "N" or "0" => false,
                    _ => Maybe.None<bool>()
                },
            _ => Maybe.None<bool>()
        };

    /// <summary> Try get <see cref="bool" /> value </summary>
    /// <param name="token"> JSON source </param>
    /// <param name="paramName"> Property name </param>
    [PublicAPI]
    public static Maybe<bool> TryGetBool(this JToken? token, string paramName)
        => (token?[paramName]).TryGetBool();

    /// <summary> Try get <see cref="bool" /> value </summary>
    /// <param name="token"> JSON source </param>
    [PublicAPI]
    public static bool GetBoolOrFalse(this JToken? token)
        => token.TryGetBool().ValueOrDefault(false);

    /// <summary> Try get <see cref="bool" /> value </summary>
    /// <param name="token"> JSON source </param>
    /// <param name="paramName"> Property name </param>
    [PublicAPI]
    public static bool GetBoolOrFalse(this JToken? token, string paramName)
        => (token?[paramName]).GetBoolOrFalse();

    /// <summary> Try get <see cref="DateTime" /> value </summary>
    /// <param name="token"> JSON source </param>
    [PublicAPI]
    public static Maybe<DateTime> TryGetDateTime(this JToken? token)
    {
        try
        {
            return (token?.Value<DateTime?>()).AsMaybeIfNotNull();
        }
        catch (Exception)
        {
            return Maybe.None<DateTime>();
        }
    }

    /// <summary> Try get <see cref="DateTime" /> value </summary>
    /// <param name="token"> JSON source </param>
    /// <param name="paramName"> Property name </param>
    [PublicAPI]
    public static Maybe<DateTime> TryGetDateTime(this JToken? token, string paramName)
        => (token?[paramName]).TryGetDateTime();
}
