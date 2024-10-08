using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using MCTG_Trimmel.HTTP;
using Newtonsoft.Json;

namespace Trimmel_MCTG.HTTP
{
    public class HttpClient : IDisposable
    {
        private readonly TcpClient connection;

        public HttpClient(TcpClient? connection)
        {
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection), "TcpClient cannot be null");
        }

        public RequestContext? ReceiveRequest()
        {
            using var reader = new StreamReader(connection.GetStream(), Encoding.UTF8);
            var isFirstLine = true;

            HttpMethod method = HttpMethod.Get;
            string? path = null;
            string? version = null;
            string? userToken = null;
            var headers = new Dictionary<string, string>();
            int contentLength = 0;
            string? payload = null;

            try
            {
                string? line;
                while (!string.IsNullOrWhiteSpace(line = reader.ReadLine()?.Trim()))
                {
                    if (isFirstLine)
                    {
                        (method, path, version) = ParseRequestLine(line);
                        isFirstLine = false;
                    }
                    else
                    {
                        (string headerKey, string headerValue) = ParseHeaderLine(line);
                        headers[headerKey] = headerValue;

                        if (headerKey.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                        {
                            userToken = headerValue;
                        }

                        if (headerKey.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!int.TryParse(headerValue, out contentLength))
                            {
                                throw new InvalidDataException("Invalid Content-Length value.");
                            }
                        }
                    }
                }

                if (path == null || version == null)
                {
                    throw new InvalidDataException("Invalid HTTP request. Path or version is missing.");
                }

                payload = ReadPayload(reader, contentLength);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"I/O error while reading request: {ex.Message}");
                return null;
            }
            catch (InvalidDataException ex)
            {
                Console.WriteLine($"Invalid HTTP request: {ex.Message}");
                return null;
            }

            return new RequestContext
            {
                Method = method,
                ResourcePath = path,
                Token = userToken,
                HttpVersion = version,
                Header = headers,
                Payload = payload
            };
        }

        public void SendResponse(Response response)
        {
            using var writer = new StreamWriter(connection.GetStream(), Encoding.UTF8) { AutoFlush = true };
            writer.Write($"HTTP/1.1 {(int)response.StatusCode} {response.StatusCode}\r\n");
            if (!string.IsNullOrEmpty(response.Payload))
            {
                var payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response.Payload));
                writer.Write($"Content-Length: {payload.Length}\r\n");
                writer.Write("\r\n");
                writer.Write(Encoding.UTF8.GetString(payload));
            }
            else
            {
                writer.Write("\r\n");
            }
        }

        public void Dispose()
        {
            connection?.Close();
            connection?.Dispose();
        }

        private (HttpMethod, string?, string?) ParseRequestLine(string line)
        {
            var parts = line.Split(' ');
            if (parts.Length != 3)
            {
                throw new InvalidDataException("Invalid HTTP request line format.");
            }

            var method = MethodUtilities.GetMethod(parts[0].Trim());
            var path = parts[1].Trim();
            var version = parts[2].Trim();

            return (method, path, version);
        }

        private (string, string) ParseHeaderLine(string line)
        {
            var headerParts = line.Split(':', 2);
            if (headerParts.Length == 2)
            {
                var headerKey = headerParts[0].Trim();
                var headerValue = headerParts[1].Trim();
                return (headerKey, headerValue);
            }
            else
            {
                throw new InvalidDataException("Invalid HTTP header format.");
            }
        }

        private string? ReadPayload(StreamReader reader, int contentLength)
        {
            if (contentLength <= 0)
            {
                return null;
            }

            char[] buffer = new char[contentLength];
            int totalBytesRead = 0;
            var data = new StringBuilder();

            while (totalBytesRead < contentLength)
            {
                int bytesRead = reader.Read(buffer, 0, Math.Min(buffer.Length, contentLength - totalBytesRead));
                if (bytesRead == 0)
                {
                    break;
                }
                totalBytesRead += bytesRead;
                data.Append(buffer, 0, bytesRead);
            }

            return data.ToString();
        }
    }
}
