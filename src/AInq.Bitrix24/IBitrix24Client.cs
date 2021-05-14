using Newtonsoft.Json.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AInq.Bitrix24
{

public interface IBitrix24Client
{
    Task<JToken> GetAsync(string method, CancellationToken cancellation = default);
    Task<JToken> PostAsync(string method, JToken data, CancellationToken cancellation = default);
}

}
