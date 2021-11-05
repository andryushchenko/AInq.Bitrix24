using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace AInq.Bitrix24
{
    public class Users
    {
        public ICollection<UserModel>? List { get; private set; }

        private readonly IBitrix24Client client;
        private const string method = "/user.get.json";

        public Users(IBitrix24Client client)
        {
            this.client = client;
            GetUsers();
        }

        private async void GetUsers()
        {
            var data = await client.GetAsync(method);
            if (data != null)
            {
                try
                {
                    List = JsonConvert.DeserializeObject<UserModel.Root>(data.Content);
                }
                catch (Exception ex)
                {
                    throw new Bitrix24CallException(method, "Error parsing response", ex);
                }
            }
        }


    }
}
