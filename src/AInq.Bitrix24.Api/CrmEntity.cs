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

using AInq.Helpers.Linq;
using System.Net;
using System.Runtime.CompilerServices;

namespace AInq.Bitrix24;

/// <summary> CRM entity base class </summary>
public class CrmEntity
{
    /// <summary> CRM client </summary>
    [PublicAPI]
    protected readonly IBitrix24Client Client;

    /// <summary> Entity type </summary>
    [PublicAPI]
    protected readonly string Type;

    /// <summary> Entity fields info </summary>
    [PublicAPI]
    protected JToken? Fields;

    internal CrmEntity(string type, IBitrix24Client client)
    {
        Type = type;
        Client = client;
    }

    internal void ResetCache()
        => Fields = null;

    /// <summary> Get entity fields info </summary>
    /// <param name="cancellation"> Cancellation token </param>
    [PublicAPI]
    public ValueTask<JToken> GetFieldsAsync(CancellationToken cancellation = default)
        => Fields == null ? new ValueTask<JToken>(LoadFieldsAsync(cancellation)) : new ValueTask<JToken>(Fields);

    /// <summary> Create link for entity </summary>
    /// <param name="id"> Entity ID </param>
    [PublicAPI]
    public string CreateLink(int id)
        => $"https://{Client.Portal}/crm/{Type}/details/{id}/";

    private async Task<JToken> LoadFieldsAsync(CancellationToken cancellation = default)
        => Fields ??= (await Client.GetAsync($"crm.{Type}.fields", cancellation).ConfigureAwait(false))["result"]!;

    private ValueTask<IReadOnlyCollection<string>> GetDefaultFieldsListAsync(CancellationToken cancellation = default)
        => Fields == null
            ? new ValueTask<IReadOnlyCollection<string>>(LoadDefaultFieldsListAsync(cancellation))
            : new ValueTask<IReadOnlyCollection<string>>(Fields.Cast<JProperty>()
                                                               .Where(property => property.Value.TryGetBool("isMultiple").ValueOrDefault(false))
                                                               .Select(property => property.Name)
                                                               .Prepend("UF_*")
                                                               .Prepend("*")
                                                               .ToList());

    private async Task<IReadOnlyCollection<string>> LoadDefaultFieldsListAsync(CancellationToken cancellation = default)
        => (await LoadFieldsAsync(cancellation).ConfigureAwait(false)).Cast<JProperty>()
                                                                      .Where(property
                                                                          => property.Value.TryGetBool("isMultiple").ValueOrDefault(false))
                                                                      .Select(property => property.Name)
                                                                      .Prepend("UF_*")
                                                                      .Prepend("*")
                                                                      .ToList();

    /// <summary> Get entity by Id </summary>
    /// <param name="id"> Id </param>
    /// <param name="cancellation"> Cancellation token </param>
    [PublicAPI]
    public async Task<Maybe<JToken>> GetAsync(int id, CancellationToken cancellation = default)
    {
        if (id < 1) throw new ArgumentOutOfRangeException(nameof(id));
        try
        {
            var result = await Client.GetAsync($"crm.{Type}.get?id={id}", cancellation).ConfigureAwait(false);
            return result["result"] switch
            {
                JObject deal => Maybe.Value<JToken>(deal),
                JArray {Count: > 0} array => Maybe.Value(array.First!),
                _ => Maybe.None<JToken>()
            };
        }
        catch (Bitrix24CallException ex)
            when (ex.Data.Contains("Status") && ex.Data["Status"] is HttpStatusCode.BadRequest or HttpStatusCode.NotFound)
        {
            return Maybe.None<JToken>();
        }
    }

    /// <summary> Get entities by Id </summary>
    /// <param name="ids"> Id collection </param>
    /// <param name="cancellation"> Cancellation token </param>
    [PublicAPI]
    public async IAsyncEnumerable<JToken> GetAsync(IEnumerable<int> ids, [EnumeratorCancellation] CancellationToken cancellation = default)
    {
        _ = ids ?? throw new ArgumentNullException(nameof(ids));
        foreach (var batch in ids.Where(id => id > 0).Batch(50))
        {
            var request = new JObject
            {
                {"filter", new JObject {{"ID", new JArray(batch)}}},
                {"select", new JArray(await GetDefaultFieldsListAsync(cancellation).ConfigureAwait(false))},
                {"start", -1}
            };
            if ((await Client.PostAsync($"crm.{Type}.list", request, cancellation).ConfigureAwait(false))["result"] is not JArray result
                || result.Count == 0) continue;
            foreach (var item in result)
                yield return item.DeepClone();
        }
    }

    /// <summary> Update entity </summary>
    /// <param name="id"> Entity Id </param>
    /// <param name="fields"> Fields data </param>
    /// <param name="registerSonetEvent"> Register update event </param>
    /// <param name="cancellation"> Cancellation token </param>
    [PublicAPI]
    public async Task<bool> UpdateAsync(int id, JToken fields, bool registerSonetEvent = false, CancellationToken cancellation = default)
    {
        if (id < 1) throw new ArgumentOutOfRangeException(nameof(id));
        var result = (await Client.PostAsync($"crm.{Type}.update",
                                      new JObject
                                      {
                                          {"id", id},
                                          {"fields", (fields ?? throw new ArgumentNullException(nameof(fields))).DeepClone()},
                                          {"params", new JObject {{"REGISTER_SONET_EVENT", registerSonetEvent ? "Y" : "N"}}}
                                      },
                                      cancellation)
                                  .ConfigureAwait(false)).TryGetBool("result");
        if (result.HasValue) return result.Value;
        throw new Bitrix24CallException($"crm.{Type}.update", "Element update failed") {Data = {["ElementId"] = id}};
    }

    /// <summary> Delete entity </summary>
    /// <param name="id"> Entity Id </param>
    /// <param name="cancellation"> Cancellation token </param>
    [PublicAPI]
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellation = default)
    {
        if (id < 1) throw new ArgumentOutOfRangeException(nameof(id));
        var result = (await Client.PostAsync($"crm.{Type}.delete", new JObject {{"id", id}}, cancellation).ConfigureAwait(false))
            .TryGetBool("result");
        if (result.HasValue) return result.Value;
        throw new Bitrix24CallException($"crm.{Type}.delete", "Element delete failed") {Data = {["ElementId"] = id}};
    }

    /// <summary> Add new entity </summary>
    /// <param name="fields"> Fields data </param>
    /// <param name="registerSonetEvent"> Register update event </param>
    /// <param name="cancellation"> Cancellation token </param>
    /// <returns> New entity Id </returns>
    [PublicAPI]
    public async Task<int> AddAsync(JObject fields, bool registerSonetEvent = false, CancellationToken cancellation = default)
    {
        var id = (await Client.PostAsync($"crm.{Type}.add",
                                  new JObject
                                  {
                                      {"fields", (fields ?? throw new ArgumentNullException(nameof(fields))).DeepClone()},
                                      {"params", new JObject {{"REGISTER_SONET_EVENT", registerSonetEvent ? "Y" : "N"}}}
                                  },
                                  cancellation)
                              .ConfigureAwait(false))
            .TryGetInt("result");
        return id.HasValue ? id.Value : throw new Bitrix24CallException($"crm.{Type}.add", "Element add failed");
    }

    /// <summary> List entities </summary>
    /// <param name="filter"> Filter </param>
    /// <param name="select"> Requested fields </param>
    /// <param name="cancellation"> Cancellation token </param>
    [PublicAPI]
    public async IAsyncEnumerable<JToken> ListAsync(JObject filter, [InstantHandle] IEnumerable<string> select,
        [EnumeratorCancellation] CancellationToken cancellation = default)
    {
        var data = ((filter ?? throw new ArgumentNullException(nameof(filter))).DeepClone() as JObject)!;
        if (!data.ContainsKey(">ID"))
            data.Add(">ID", 0);
        var request = new JObject
        {
            {"order", new JObject {{"ID", "ASC"}}},
            {"filter", data},
            {"select", new JArray(new HashSet<string>((select ?? throw new ArgumentNullException(nameof(select))).Append("ID")))},
            {"start", -1}
        };
        while (true)
        {
            if ((await Client.PostAsync($"crm.{Type}.list", request, cancellation).ConfigureAwait(false))["result"] is not JArray result
                || result.Count == 0) yield break;
            foreach (var item in result)
                yield return item.DeepClone();
            if (result.Count < 50) yield break;
            data.Property(">ID")!.Value = result.Max(item => item.Value<int>("ID"));
        }
    }

    /// <summary> List entities </summary>
    /// <param name="filter"> Filter </param>
    /// <param name="cancellation"> Cancellation token </param>
    [PublicAPI]
    public async IAsyncEnumerable<JToken> ListAsync(JObject filter, [EnumeratorCancellation] CancellationToken cancellation = default)
    {
        var data = ((filter ?? throw new ArgumentNullException(nameof(filter))).DeepClone() as JObject)!;
        if (!data.ContainsKey(">ID"))
            data.Add(">ID", 0);
        var request = new JObject
        {
            {"order", new JObject {{"ID", "ASC"}}},
            {"filter", data},
            {"select", new JArray(await GetDefaultFieldsListAsync(cancellation).ConfigureAwait(false))},
            {"start", -1}
        };
        while (true)
        {
            if ((await Client.PostAsync($"crm.{Type}.list", request, cancellation).ConfigureAwait(false))["result"] is not JArray result
                || result.Count == 0) yield break;
            foreach (var item in result)
                yield return item.DeepClone();
            if (result.Count < 50) yield break;
            data.Property(">ID")!.Value = result.Max(item => item.Value<int>("ID"));
        }
    }

    /// <summary> List entities </summary>
    /// <param name="select"> Requested fields </param>
    /// <param name="cancellation"> Cancellation token </param>
    [PublicAPI]
    public IAsyncEnumerable<JToken> ListAsync([InstantHandle] IEnumerable<string> select, CancellationToken cancellation = default)
        => ListAsync(new JObject(), select, cancellation);

    /// <summary> List entities </summary>
    /// <param name="cancellation"> Cancellation token </param>
    [PublicAPI]
    public IAsyncEnumerable<JToken> ListAsync(CancellationToken cancellation = default)
        => ListAsync(new JObject(), cancellation);
}
