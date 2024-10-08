using System.Diagnostics.Metrics;
using System.Net;
using Trimmel_MCTG.db;
using Trimmel_MCTG.HTTP;

Console.WriteLine("Server starting");
Database db = new Database();
db.CreateTables();

Route route = new Route();
HttpServer server = new HttpServer(IPAddress.Loopback, 10001, route);
server.Start();