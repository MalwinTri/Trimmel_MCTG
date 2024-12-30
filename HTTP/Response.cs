using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCTG_Trimmel.HTTP
{
    public class Response
    {
        // Der Statuscode der Antwort, z. B. 200 (OK), 404 (Not Found), etc.
        public StatusCode StatusCode { get; set; }

        // Der Payload der Antwort - die eigentlichen Daten, die im Body der Antwort enthalten sind
        public string? Payload { get; set; }

        public Trimmel_MCTG.HTTP.IRoute IRoute
        {
            get => default;
            set
            {
            }
        }

        public Trimmel_MCTG.HTTP.HttpClient HttpClient
        {
            get => default;
            set
            {
            }
        }
    }
}
