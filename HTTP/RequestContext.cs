using System;
using System.Collections.Generic;

namespace Trimmel_MCTG.HTTP
{
    public class RequestContext
    {
        public HttpMethod Method { get; set; } = HttpMethod.Get;

        public string ResourcePath { get; set; } = string.Empty;

        public string HttpVersion { get; set; } = "HTTP/1.1";

        public string? Payload { get; set; }

        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        private int? userId;
        public int? UserId
        {
            get
            {
                if (userId.HasValue)
                {
                    return userId;
                }

                if (!string.IsNullOrEmpty(Token))
                {
                    userId = GetUserIdFromToken(Token);
                }

                return userId;
            }
        }

        public string? Token
        {
            get
            {
                if (Headers.ContainsKey("Authorization"))
                {
                    var authHeader = Headers["Authorization"];
                    if (authHeader.StartsWith("Bearer "))
                    {
                        return authHeader.Substring(7); 
                    }
                }
                return null;
            }
        }

        public Dictionary<string, string> QueryParameters
        {
            get
            {
                var queryParams = new Dictionary<string, string>();
                if (ResourcePath.Contains("?"))
                {
                    var query = ResourcePath.Split('?')[1]; 
                    foreach (var param in query.Split('&'))
                    {
                        var keyValue = param.Split('=');
                        if (keyValue.Length == 2)
                        {
                            queryParams[keyValue[0]] = keyValue[1];
                        }
                    }
                }
                return queryParams;
            }
        }

        public RequestContext(Dictionary<string, string> headers, string? payload, HttpMethod method, string resourcePath)
        {
            Headers = headers;
            Payload = payload;
            Method = method;
            ResourcePath = resourcePath;
        }

        private int? GetUserIdFromToken(string token)
        {

            if (int.TryParse(token, out int id))
            {
                return id; 
            }

            Console.WriteLine($"Token '{token}' konnte keiner UserId zugeordnet werden.");
            return null;
        }
    }
}
