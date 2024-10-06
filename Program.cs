using System.Diagnostics.Metrics;
using System.Net;
using Trimmel_MCTG.db;
using Trimmel_MCTG.HTTP;

Console.WriteLine("Hello world");
Database db = new Database();
db.CreateTables();

HttpServer server = new HttpServer(IPAddress.Loopback, 1000);
server.Start();