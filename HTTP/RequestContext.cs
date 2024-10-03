using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trimmel_MCTG.HTTP
{
    public class RequestContext
    {
        public HttpMethod Method { get; set; }
        public string ResourcePath { get; set; }
        public string HttpVersion { get; set; }
        public string token { get; set; }
        public Dictionary<string, string> Header { get; set; }
        public string? Payload { get; set; }

        public RequestContext()
        {
            this.Method = HttpMethod.Get;
            this.ResourcePath = "";
            this.token = "";
            this.HttpVersion = "HTTP/1.1";
            this.Header = new Dictionary<string, string>();
            this.Payload = null;
        }
    }
}
