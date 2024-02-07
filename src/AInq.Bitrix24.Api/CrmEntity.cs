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
using System.Net;
using System.Runtime.CompilerServices;

namespace AInq.Bitrix24;

/// <summary> CRM entity base class </summary>
public class CrmEntity
{
    private const string ResultField = "result";
    private const string FilterArgument = "filter";
    private const string SelectArgument = "select";
    private const string StartArgument = "start";
    private const string IdField = "ID";
    private const string IdFilter = ">ID";
    private const string OrderArgument = "order";
    private const string IdArgument = "id";
    private const string FieldsArgument = "fields";
    private const string ParamsArgument = "params";
    private const string RegisterEventField = "REGISTER_SONET_EVENT";

    private static readonly JToken IdOrder = new JObject {{IdField, "ASC"}};
    private static readonly JToken EventTrue = new JObject {{RegisterEventField, "Y"}};
    private static readonly JToken EventFalse = new JObject {{RegisterEventField, "N"}};

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
        => Fields ??= (await Client.GetAsync($"crm.{Type}.fields", cancellation).ConfigureAwait(false))[ResultField]!;

    private ValueTask<IReadOnlyCollection<string>> GetDefaultFieldsListAsync(CancellationToken cancellation = default)
        => Fields == null
            ? new ValueTask<IReadOnlyCollection<string>>(LoadDefaultFieldsListAsync(cancellation))
            : new ValueTask<IReadOnlyCollection<string>>(ReadDefaultFields(Fields));

    private async Task<IReadOnlyCollection<string>> LoadDefaultFieldsListAsync(CancellationToken cancellation = default)
        => ReadDefaultFields(await LoadFieldsAsync(cancellation).ConfigureAwait(false));

    private static IReadOnlyCollection<string> ReadDefaultFields(JToken fields)
        => fields.Cast<JProperty>()
                 .Where(property => property.Value.TryGetBool("isMultiple").ValueOrDefault(false))
                 .Select(property => property.Name)
                 .Prepend("UF_*")
                 .Prepend("*")
                 .ToArray();

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
            return result[ResultField] switch
            {
                JObject deal => deal,
                JArray {Count: > 0} array => array.First!,
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
                {FilterArgument, new JObject {{IdField, new JArray(batch)}}},
                {SelectArgument, new JArray(await GetDefaultFieldsListAsync(cancellation).ConfigureAwait(false))},
                {StartArgument, -1}
            };
            if ((await Client.PostAsync($"crm.{Type}.list", request, cancellation).ConfigureAwait(false))[ResultField] is not JArray result
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
                                          {IdArgument, id},
                                          {FieldsArgument, (fields ?? throw new ArgumentNullException(nameof(fields))).DeepClone()},
                                          {ParamsArgument, registerSonetEvent ? EventTrue : EventFalse}
                                      },
                                      cancellation)
                                  .ConfigureAwait(false)).TryGetBool(ResultField);
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
        var result = (await Client.PostAsync($"crm.{Type}.delete", new JObject {{IdArgument, id}}, cancellation).ConfigureAwait(false))
            .TryGetBool(ResultField);
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
                                      {FieldsArgument, (fields ?? throw new ArgumentNullException(nameof(fields))).DeepClone()},
                                      {ParamsArgument, registerSonetEvent ? EventTrue : EventFalse}
                                  },
                                  cancellation)
                              .ConfigureAwait(false))
            .TryGetInt(ResultField);
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
        if (!data.ContainsKey(IdFilter))
            data.Add(IdFilter, 0);
        var request = new JObject
        {
            {OrderArgument, IdOrder},
            {FilterArgument, data},
            {SelectArgument, new JArray(new HashSet<string>((select ?? throw new ArgumentNullException(nameof(select))).Append(IdField)))},
            {StartArgument, -1}
        };
        while (true)
        {
            if ((await Client.PostAsync($"crm.{Type}.list", request, cancellation).ConfigureAwait(false))[ResultField] is not JArray result
                || result.Count == 0) yield break;
            foreach (var item in result)
                yield return item.DeepClone();
            if (result.Count < 50) yield break;
            data.Property(IdFilter)!.Value = result.Max(item => item.Value<int>(IdField));
        }
    }

    /// <summary> List entities </summary>
    /// <param name="filter"> Filter </param>
    /// <param name="cancellation"> Cancellation token </param>
    [PublicAPI]
    public async IAsyncEnumerable<JToken> ListAsync(JObject filter, [EnumeratorCancellation] CancellationToken cancellation = default)
    {
        var data = ((filter ?? throw new ArgumentNullException(nameof(filter))).DeepClone() as JObject)!;
        if (!data.ContainsKey(IdFilter))
            data.Add(IdFilter, 0);
        var request = new JObject
        {
            {OrderArgument, IdOrder},
            {FilterArgument, data},
            {SelectArgument, new JArray(await GetDefaultFieldsListAsync(cancellation).ConfigureAwait(false))},
            {StartArgument, -1}
        };
        while (true)
        {
            if ((await Client.PostAsync($"crm.{Type}.list", request, cancellation).ConfigureAwait(false))[ResultField] is not JArray result
                || result.Count == 0) yield break;
            foreach (var item in result)
                yield return item.DeepClone();
            if (result.Count < 50) yield break;
            data.Property(IdFilter)!.Value = result.Max(item => item.Value<int>(IdField));
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
