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

using AInq.Helpers.Linq;
using AInq.Optional;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace AInq.Bitrix24
{

/// <summary> CRM entity base class </summary>
public class CrmEntity
{
    /// <summary> CRM client </summary>
    protected readonly IBitrix24Client Client;

    /// <summary> Entity type </summary>
    protected readonly string Type;

    /// <summary> Entity fields info </summary>
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
    public async Task<JToken> FieldsAsync(CancellationToken cancellation = default)
        => Fields ??= await Client.GetAsync($"crm.{Type}.fields", cancellation).ConfigureAwait(false);

    /// <summary> Get entity by Id </summary>
    /// <param name="id"> Id </param>
    /// <param name="cancellation"> Cancellation token </param>
    public async Task<Maybe<JToken>> GetAsync(int id, CancellationToken cancellation = default)
    {
        if (id < 1) throw new ArgumentOutOfRangeException(nameof(id));
        try
        {
            var result = await Client.GetAsync($"crm.{Type}.get?id={id}", cancellation).ConfigureAwait(false);
            return result["result"] switch
            {
                JObject deal => Maybe.Value<JToken>(deal),
                JArray { Count: > 0 } array => Maybe.Value(array.First!),
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
    public async IAsyncEnumerable<JToken> GetAsync(IEnumerable<int> ids, [EnumeratorCancellation] CancellationToken cancellation = default)
    {
        _ = ids ?? throw new ArgumentNullException(nameof(ids));
        var select = (await FieldsAsync(cancellation).ConfigureAwait(false))
                     .Cast<JProperty>()
                     .Where(property => property.Value.TryGetBool("isMultiple").ValueOrDefault(false))
                     .Select(property => property.Name)
                     .Prepend("UF_*")
                     .Prepend("*")
                     .ToList();
        foreach (var batch in ids.Where(id => id > 0).Batch(50))
        {
            var request = new JObject
            {
                { "filter", new JObject { { "ID", new JArray(batch) } } }, { "select", new JArray(select) }, { "start", -1 }
            };
            if ((await Client.PostAsync($"crm.{Type}.list", request, cancellation))["result"] is not JArray result || result.Count == 0) continue;
            foreach (var item in result)
                yield return item.DeepClone();
        }
    }

    /// <summary> Update entity </summary>
    /// <param name="id"> Entity Id </param>
    /// <param name="fields"> Fields data </param>
    /// <param name="registerSonetEvent"> Register update event </param>
    /// <param name="cancellation"> Cancellation token </param>
    public async Task<bool> UpdateAsync(int id, JToken fields, bool registerSonetEvent = false, CancellationToken cancellation = default)
    {
        if (id < 1) throw new ArgumentOutOfRangeException(nameof(id));
        var result = (await Client.PostAsync($"crm.{Type}.update",
                                      new JObject
                                      {
                                          { "id", id },
                                          { "fields", (fields ?? throw new ArgumentNullException(nameof(fields))).DeepClone() },
                                          { "params", new JObject { { "REGISTER_SONET_EVENT", registerSonetEvent ? "Y" : "N" } } }
                                      },
                                      cancellation)
                                  .ConfigureAwait(false))
            .TryGetBool("result");
        if (result.HasValue) return result.Value;
        var ex = new Bitrix24CallException($"crm.{Type}.update", "Element update failed");
        ex.Data["ElementId"] = id;
        throw ex;
    }

    /// <summary> Delete entity </summary>
    /// <param name="id"> Entity Id </param>
    /// <param name="cancellation"> Cancellation token </param>
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellation = default)
    {
        if (id < 1) throw new ArgumentOutOfRangeException(nameof(id));
        var result = (await Client.PostAsync($"crm.{Type}.delete", new JObject { { "id", id } }, cancellation).ConfigureAwait(false))
            .TryGetBool("result");
        if (result.HasValue) return result.Value;
        var ex = new Bitrix24CallException($"crm.{Type}.delete", "Element delete failed");
        ex.Data["ElementId"] = id;
        throw ex;
    }

    /// <summary> Add new entity </summary>
    /// <param name="fields"> Fields data </param>
    /// <param name="registerSonetEvent"> Register update event </param>
    /// <param name="cancellation"> Cancellation token </param>
    /// <returns> New entity Id </returns>
    public async Task<int> AddAsync(JObject fields, bool registerSonetEvent = false, CancellationToken cancellation = default)
    {
        var id = (await Client.PostAsync($"crm.{Type}.add",
                                  new JObject
                                  {
                                      { "fields", (fields ?? throw new ArgumentNullException(nameof(fields))).DeepClone() },
                                      { "params", new JObject { { "REGISTER_SONET_EVENT", registerSonetEvent ? "Y" : "N" } } }
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
    public async IAsyncEnumerable<JToken> ListAsync(JObject filter, IEnumerable<string> select,
        [EnumeratorCancellation] CancellationToken cancellation = default)
    {
        var data = ((filter ?? throw new ArgumentNullException(nameof(filter))).DeepClone() as JObject)!;
        if (!data.ContainsKey(">ID"))
            data.Add(">ID", 0);
        var request = new JObject
        {
            { "order", new JObject { { "ID", "ASC" } } },
            { "filter", data },
            { "select", new JArray(new HashSet<string>(select ?? throw new ArgumentNullException(nameof(select))).Append("ID")) },
            { "start", -1 }
        };
        while (true)
        {
            if ((await Client.PostAsync($"crm.{Type}.list", request, cancellation))["result"] is not JArray result
                || result.Count == 0) yield break;
            foreach (var item in result)
                yield return item.DeepClone();
            if (result.Count < 50) yield break;
            data.Property(">ID")!.Value = result.Max(item => item.Value<int>("ID"));
        }
    }
}

}
