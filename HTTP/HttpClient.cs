using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Trimmel_MCTG.HTTP
{
    public class HttpClient
    {
        private readonly TcpClient ?connection;

        public HttpClient(TcpClient? connection)
        {
            this.connection = connection;
        }

    }
}
