using MCTG_Trimmel.HTTP;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Trimmel_MCTG.db;

namespace Trimmel_MCTG.HTTP
{
    public class HttpServer
    {
        private readonly TcpListener tcpListener;
        private readonly Route route;
        private readonly Database db; // Gemeinsame Datenbankinstanz
        private bool listener;

        public HttpServer(IPAddress address, int port, Route route)
        {
            tcpListener = new TcpListener(address, port);
            this.route = route;
            db = new Database(); // Erstellen einer einzigen gemeinsamen Datenbankverbindung
        }

        public HttpMethod HttpMethod
        {
            get => default;
            set
            {
            }
        }

        public void Start()
        {
            try
            {
                tcpListener.Start();
                listener = true;

                Console.WriteLine("Server is running and listening for requests...");

                while (listener)
                {
                    try
                    {
                        var connection = tcpListener.AcceptTcpClient();
                        Console.WriteLine("Client connected");

                        // Verwenden des ThreadPools zum Handling der Clients
                        ThreadPool.QueueUserWorkItem(HandleClient, connection);
                    }
                    catch (SocketException ex)
                    {
                        Console.WriteLine($"Socket error: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Unexpected error accepting client connection: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Critical error in server start: {ex.Message}");
            }
        }

        private void HandleClient(object state)
        {
            var connection = state as TcpClient;
            if (connection == null)
            {
                Console.WriteLine("Invalid client connection state.");
                return;
            }

            try
            {
                using (connection) // Sicherstellen, dass die Verbindung korrekt geschlossen wird
                {
                    var client = new HttpClient(connection);
                    var request = client.ReceiveRequest();

                    Response response;

                    if (request == null)
                    {
                        Console.WriteLine("Received invalid request.");
                        response = new Response
                        {
                            StatusCode = StatusCode.BadRequest,
                            Payload = "Invalid Request"
                        };
                    }
                    else
                    {
                        try
                        {

                            // dass nur ein Thread gleichzeitig darauf zugreift.
                            lock (db)
                            {
                                var command = route.Resolve(request);
                                if (command != null)
                                {
                                    command.SetDatabase(db); // Setzen der Datenbankverbindung
                                    response = command.Execute();
                                }
                                else
                                {
                                    response = new Response
                                    {
                                        StatusCode = StatusCode.NotFound,
                                        Payload = "No route found for the given request."
                                    };
                                }
                            }
                        }
                        catch (Exception commandEx)
                        {
                            Console.WriteLine($"Error executing command: {commandEx.Message}");
                            response = new Response
                            {
                                StatusCode = StatusCode.InternalServerError,
                                Payload = "Internal server error occurred while executing command."
                            };
                        }
                    }

                    client.SendResponse(response);
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"SocketException: {ex.Message}");
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("Client connection was already disposed before processing could complete.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client: {ex.Message}");
            }
        }

        public void Stop()
        {
            // Beenden des TcpListener
            listener = false;
            tcpListener.Stop();
            Console.WriteLine("Server has stopped listening for requests.");
        }
    }
}
