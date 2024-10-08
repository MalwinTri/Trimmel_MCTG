using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trimmel_MCTG.HTTP
{
    public class Route : IRoute
    {
        public IRouteCommand? Resolve(RequestContext request)
        {
            IRouteCommand? command = request switch
            {
                { Method: HttpMethod.Post, ResourcePath: "/users" } => new RegisterExecuter(request),
                // { Method: HttpMethod.Post, ResourcePath: "/sessions" } => new LoginExecuter(request),
                _ => null
            };

            return command;
        }

        private string EnsureBody(string? body)
        {
            if (body == null)
            {
                throw new InvalidDataException();
            }
            return body;
        }

        private T Deserialize<T>(string? body) where T : class
        {
            var data = body != null ? JsonConvert.DeserializeObject<T>(body) : null;
            if (data == null)
            {
                throw new InvalidDataException();
            }
            return data;
        }
    }
}
