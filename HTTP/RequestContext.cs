using System;
using System.Collections.Generic;

namespace Trimmel_MCTG.HTTP
{
    public class RequestContext
    {
        public HttpMethod Method { get; set; } = HttpMethod.Get;
        public string ResourcePath { get; set; } = string.Empty;
        public string HttpVersion { get; set; } = "HTTP/1.1";
        public string Token { get; set; } = string.Empty;
        public Dictionary<string, string> Header { get; set; } = new Dictionary<string, string>();
        public string? Payload { get; set; }
    }
}