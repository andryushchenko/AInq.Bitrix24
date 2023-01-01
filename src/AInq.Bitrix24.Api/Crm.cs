// Copyright 2021-2023 Anton Andryushchenko
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

/// <summary> Bitrix24 CRM Api calls </summary>
public class Crm
{
    /// <param name="client"> API Client </param>
    public Crm(IBitrix24Client client)
    {
        _ = client ?? throw new ArgumentNullException(nameof(client));
        Lead = new CrmEntity("lead", client);
        Contact = new CrmEntity("contact", client);
        Company = new CrmEntity("company", client);
        Deal = new CrmEntity("deal", client);
    }

    /// <summary> CRM Lead calls </summary>
    [PublicAPI]
    public CrmEntity Lead { get; }

    /// <summary> CRM Contact calls </summary>
    [PublicAPI]
    public CrmEntity Contact { get; }

    /// <summary> CRM Company calls </summary>
    [PublicAPI]
    public CrmEntity Company { get; }

    /// <summary> CRM Deal calls </summary>
    [PublicAPI]
    public CrmEntity Deal { get; }

    /// <summary> Remove cached data </summary>
    [PublicAPI]
    public void ResetCache()
    {
        Lead.ResetCache();
        Contact.ResetCache();
        Company.ResetCache();
        Deal.ResetCache();
    }
}
