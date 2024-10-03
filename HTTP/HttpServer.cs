using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Trimmel_MCTG.HTTP
{
    public class HttpServer
    {
        private readonly TcpListener tcpListener;

        private bool listener;

        public HttpServer(IPAddress address, int port)
        {
            tcpListener = new TcpListener(address, port);
        }

        public void Start()
        {
            tcpListener.Start();
            listener = true;

            while (listener)
            {
                var connection = tcpListener.AcceptTcpClient();
                Console.WriteLine("Client connected");

                var clientThread = new Thread(() =>
                {
                    var client = new Trimmel_MCTG.HTTP.HttpClient(connection);

                    var request = client.ReciveRequest();
                });
            }
        }
    }
}
