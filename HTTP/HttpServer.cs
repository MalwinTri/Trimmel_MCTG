using MCTG_Trimmel.HTTP;
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
                    try
                    {
                        var client = new HttpClient(connection);
                        var request = client.ReceiveRequest();

                        Response response;
                        if (request == null)
                        {
                            response = new Response
                            {
                                StatusCode = StatusCode.BadRequest,
                                Payload = "Invalid Request"
                            };
                        }
                        else
                        {
                            response = new Response
                            {
                                StatusCode = StatusCode.Ok,
                                Payload = "Request received successfully"
                            };
                        }

                        client.SendResponse(response);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error handling client: {ex.Message}");
                    }
                    finally
                    {
                        connection.Close();
                    }
                });

                clientThread.Start();
            }
        }

        public void Stop()
        {
            listener = false;
            tcpListener.Stop();
        }
    }
}
