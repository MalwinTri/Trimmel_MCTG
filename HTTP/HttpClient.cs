using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using MCTG_Trimmel.HTTP;

namespace Trimmel_MCTG.HTTP
{
    public class HttpClient
    {
        private readonly TcpClient connection;

        public HttpClient(TcpClient connection)
        {
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public HttpMethod HttpMethod
        {
            get => default;
            set
            {
            }
        }

        public RequestContext? ReceiveRequest()
        {
            var buffer = new byte[1024];
            var stream = connection.GetStream();
            int bytesRead = stream.Read(buffer, 0, buffer.Length);

            if (bytesRead <= 0)
            {
                return null;
            }

            var requestString = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            return ParseRequest(requestString);
        }

        public void SendResponse(Response response)
        {
            var stream = connection.GetStream();

            string responseString;
            if (response.StatusCode == StatusCode.Created)
            {
                responseString = $"{GetHttpStatusCode(response.StatusCode)}\r\n" +
                                 "User created successfully";
            }
            else
            {
                responseString = $"{GetHttpStatusCode(response.StatusCode)}\r\n" +
                                 $"{response.Payload}";
            }

            var responseBytes = Encoding.UTF8.GetBytes(responseString);

            stream.Write(responseBytes, 0, responseBytes.Length);
            stream.Flush();
        }


        private string GetHttpStatusCode(StatusCode statusCode)
        {
            return statusCode switch
            {
                StatusCode.Ok => "200 OK",
                StatusCode.Created => "201 Created",
                StatusCode.Accepted => "202 Accepted",
                StatusCode.NoContent => "204 No Content",
                StatusCode.BadRequest => "400 Bad Request",
                StatusCode.Unauthorized => "401 Unauthorized",
                StatusCode.Forbidden => "403 Forbidden",
                StatusCode.NotFound => "404 Not Found",
                StatusCode.Conflict => "409 Conflict",
                StatusCode.InternalServerError => "500 Internal Server Error",
                StatusCode.NotImplemented => "501 Not Implemented",
                _ => "500 Internal Server Error"
            };
        }

        private RequestContext? ParseRequest(string requestString)
        {
            var lines = requestString.Split("\r\n");
            if (lines.Length > 0)
            {
                var parts = lines[0].Split(' ');
                if (parts.Length >= 2)
                {
                    var methodString = parts[0];
                    var path = parts[1];

                    var headers = new Dictionary<string, string>();
                    int i = 1;
                    while (!string.IsNullOrWhiteSpace(lines[i]))
                    {
                        var headerParts = lines[i].Split(':', 2);
                        if (headerParts.Length == 2)
                        {
                            headers[headerParts[0].Trim()] = headerParts[1].Trim();
                        }
                        i++;
                    }

                    if (Enum.TryParse<HttpMethod>(methodString, true, out var method))
                    {
                        var body = lines.Skip(i + 1).FirstOrDefault();

                        return new RequestContext(headers, body, method, path);
                    }
                }
            }
            return null;
        }
    }
}
