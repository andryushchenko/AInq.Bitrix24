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

/// <summary> JSON data reading methods to handle some strange Bitrix24 behaviour </summary>
public static class JsonHelper
{
    /// <summary> Try get <see cref="float" /> value </summary>
    /// <param name="token"> JSON source </param>
    public static Maybe<float> TryGetFloat(this JToken token)
        => token?.Type switch
        {
            JTokenType.Float or JTokenType.Integer => Maybe.Value(token.Value<float>()),
            JTokenType.String => float.TryParse(token.Value<string>(), out var result)
                ? Maybe.Value(result)
                : Maybe.None<float>(),
            _ => Maybe.None<float>()
        };

    /// <summary> Try get <see cref="float" /> value </summary>
    /// <param name="token"> JSON source </param>
    /// <param name="paramName"> Property name </param>
    public static Maybe<float> TryGetFloat(this JToken token, string paramName)
        => token[paramName]?.TryGetFloat() ?? Maybe.None<float>();

    /// <summary> Try get <see cref="int" /> value </summary>
    /// <param name="token"> JSON source </param>
    public static Maybe<int> TryGetInt(this JToken token)
        => token?.Type switch
        {
            JTokenType.Integer => Maybe.Value(token.Value<int>()),
            JTokenType.String => int.TryParse(token.Value<string>(), out var result)
                ? Maybe.Value(result)
                : Maybe.None<int>(),
            JTokenType.Float => Maybe.Value((int) token.Value<float>()),
            _ => Maybe.None<int>()
        };

    /// <summary> Try get <see cref="int" /> value </summary>
    /// <param name="token"> JSON source </param>
    /// <param name="paramName"> Property name </param>
    public static Maybe<int> TryGetInt(this JToken token, string paramName)
        => token[paramName]?.TryGetInt() ?? Maybe.None<int>();

    /// <summary> Try get <see cref="int" /> values array </summary>
    /// <param name="token"> JSON source </param>
    /// <param name="ignoreTypeMismatch"> Skip values that can't be parsed, otherwise return empty array </param>
    public static int[] TryGetIntArray(this JToken token, bool ignoreTypeMismatch = true)
        => token?.Type switch
        {
            JTokenType.Array => !ignoreTypeMismatch
                                && token.Any(item => item.Type is not (JTokenType.Integer or JTokenType.Float or JTokenType.String))
                ? Array.Empty<int>()
                : token.Select(item => item.TryGetInt()).Values().ToArray(),
            JTokenType.Integer => new[] {token.Value<int>()},
            JTokenType.String => int.TryParse(token.Value<string>(), out var result)
                ? new[] {result}
                : Array.Empty<int>(),
            JTokenType.Float => new[] {(int) token.Value<float>()},
            _ => Array.Empty<int>()
        };

    /// <summary> Try get <see cref="int" /> values array </summary>
    /// <param name="token"> JSON source </param>
    /// <param name="paramName"> Property name </param>
    /// <param name="ignoreTypeMismatch"> Skip values that can't be parsed, otherwise return empty array </param>
    public static int[] TryGetIntArray(this JToken token, string paramName, bool ignoreTypeMismatch = true)
        => token[paramName]?.TryGetIntArray(ignoreTypeMismatch) ?? Array.Empty<int>();

    /// <summary> Try get <see cref="bool" /> value </summary>
    /// <param name="token"> JSON source </param>
    public static Maybe<bool> TryGetBool(this JToken token)
        => token?.Type switch
        {
            JTokenType.Boolean => Maybe.Value(token.Value<bool>()),
            JTokenType.Integer or JTokenType.Float => Maybe.Value(token.Value<float>() > 0),
            JTokenType.String => bool.TryParse(token.Value<string>(), out var boolResult)
                ? Maybe.Value(boolResult)
                : float.TryParse(token.Value<string>(), out var floatResult)
                    ? Maybe.Value(floatResult > 0)
                    : Maybe.Value(string.Equals(token.Value<string>(), "Y", StringComparison.InvariantCultureIgnoreCase)),
            _ => Maybe.None<bool>()
        };

    /// <summary> Try get <see cref="bool" /> value </summary>
    /// <param name="token"> JSON source </param>
    /// <param name="paramName"> Property name </param>
    public static Maybe<bool> TryGetBool(this JToken token, string paramName)
        => token[paramName]?.TryGetBool() ?? Maybe.None<bool>();

    /// <summary> Try get <see cref="bool" /> value </summary>
    /// <param name="token"> JSON source </param>
    public static bool GetBoolOrFalse(this JToken token)
        => token.TryGetBool().ValueOrDefault(false);

    /// <summary> Try get <see cref="bool" /> value </summary>
    /// <param name="token"> JSON source </param>
    /// <param name="paramName"> Property name </param>
    public static bool GetBoolOrFalse(this JToken token, string paramName)
        => token[paramName]?.GetBoolOrFalse() ?? false;

    /// <summary> Try get <see cref="DateTime" /> value </summary>
    /// <param name="token"> JSON source </param>
    public static Maybe<DateTime> TryGetDateTime(this JToken token)
    {
        try
        {
            return Maybe.Value(token.Value<DateTime>());
        }
        catch (Exception)
        {
            return Maybe.None<DateTime>();
        }
    }

    /// <summary> Try get <see cref="DateTime" /> value </summary>
    /// <param name="token"> JSON source </param>
    /// <param name="paramName"> Property name </param>
    public static Maybe<DateTime> TryGetDateTime(this JToken token, string paramName)
        => token[paramName]?.TryGetDateTime() ?? Maybe.None<DateTime>();
}
