using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RestEase;

namespace PiBox.Extensions.RestEase.Authentication
{
    internal interface IAuthApi
    {
        [Post("{url}")]
        public Task<OAuth2Model> GetToken([Path(UrlEncode = false)] string url, [Body(BodySerializationMethod.UrlEncoded)] IDictionary<string, object> formContent, CancellationToken cancellationToken = default);
    }
}
