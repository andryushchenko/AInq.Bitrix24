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

#if NETSTANDARD
namespace AInq.Bitrix24;

internal static class ChunkHelper
{
    public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, long size)
    {
        using var enumerator = source.GetEnumerator();
        while (enumerator.MoveNext())
            yield return enumerator.Take(size);
    }

    private static IEnumerable<T> Take<T>(this IEnumerator<T> enumerator, long count)
    {
        yield return enumerator.Current;
        count--;
        while (count > 0 && enumerator.MoveNext())
        {
            count--;
            yield return enumerator.Current;
        }
    }
}
#endif
