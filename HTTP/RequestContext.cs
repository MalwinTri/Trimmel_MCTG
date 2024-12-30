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

        public RequestContext(Dictionary<string, string> headers, string? payload, HttpMethod method, string resourcePath)
        {
            Headers = headers;
            Payload = payload;
            Method = method;
            ResourcePath = resourcePath;
        }
    }
}
    