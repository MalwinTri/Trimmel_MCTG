using System;
using System.Collections.Generic;

namespace Trimmel_MCTG.HTTP
{
    public class RequestContext
    {
        // Die HTTP-Methode der Anfrage (z. B. GET, POST, PUT, DELETE, PATCH)
        public HttpMethod Method { get; set; } = HttpMethod.Get;

        // Der Pfad der angeforderten Ressource (z. B. "/users")
        public string ResourcePath { get; set; } = string.Empty;

        // Die HTTP-Version der Anfrage, standardmäßig "HTTP/1.1"
        public string HttpVersion { get; set; } = "HTTP/1.1";

        // Das Token zur Authentifizierung, falls vorhanden
        public string Token { get; set; } = string.Empty;

        // Die Header der Anfrage als Dictionary, wobei der Schlüssel der Header-Name ist und der Wert der entsprechende Header-Wert
        public Dictionary<string, string> Header { get; set; } = new Dictionary<string, string>();

        // Der Payload der Anfrage, der optionale Daten enthält, z. B. bei POST- oder PUT-Anfragen
        public string? Payload { get; set; }
    }
}
