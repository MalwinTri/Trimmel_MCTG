using System;
using System.Collections.Generic;

namespace Trimmel_MCTG.HTTP
{
    public class RequestContext
    {
        // Die HTTP-Methode der Anfrage
        public HttpMethod Method { get; set; } = HttpMethod.Get;

        // Der Pfad der Ressource
        public string ResourcePath { get; set; } = string.Empty;

        // Die HTTP-Version
        public string HttpVersion { get; set; } = "HTTP/1.1";

        // Der Payload der Anfrage
        public string? Payload { get; set; }

        // Header der Anfrage
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        // Token (extrahiert aus den Headers)
        public string? Token
        {
            get
            {
                if (Headers.ContainsKey("Authorization"))
                {
                    var authHeader = Headers["Authorization"];
                    if (authHeader.StartsWith("Bearer "))
                    {
                        return authHeader.Substring(7); // Entferne "Bearer "
                    }
                }
                return null;
            }
        }

        // Query-Parameter als Dictionary
        public Dictionary<string, string> QueryParameters
        {
            get
            {
                var queryParams = new Dictionary<string, string>();
                if (ResourcePath.Contains("?"))
                {
                    var query = ResourcePath.Split('?')[1]; // Alles nach dem '?' extrahieren
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
    }
}
