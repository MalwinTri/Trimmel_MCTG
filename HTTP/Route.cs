using MCTG_Trimmel.HTTP;
using Newtonsoft.Json;
using Trimmel_MCTG.Execute;

namespace Trimmel_MCTG.HTTP
{
    public class Route : IRoute
    {
        // Dictionary zur Routenverwaltung
        private readonly Dictionary<(HttpMethod method, string resourcePath), Func<RequestContext, IRouteCommand>> routes;

        Route IRoute.Route { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Route()
        {
            // Initialisiere alle bekannten Routen
            routes = new Dictionary<(HttpMethod, string), Func<RequestContext, IRouteCommand>>
            {
                { (HttpMethod.Post, "/users"), request => new RegisterExecuter(request) },
                { (HttpMethod.Post, "/sessions"), request => new LoginExecuter(request) },
                { (HttpMethod.Post, "/packages"), request => new CreatePackageExecuter(request) },
                { (HttpMethod.Post, "/transactions/packages"), request => new AcquirePackage(request) },
                { (HttpMethod.Get, "/cards"), request => new ShowCardsExecuter(request) }

                // Weitere Routen können hier hinzugefügt werden
            };
        }

        public IRouteCommand? Resolve(RequestContext request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), "Request cannot be null.");
            }

            // Überprüfe, ob die Route existiert
            if (routes.TryGetValue((request.Method, request.ResourcePath), out var routeHandler))
            {
                // Token-Validierung nur für geschützte Routen
                if (request.ResourcePath != "/users" && request.ResourcePath != "/sessions")
                {
                    if (string.IsNullOrEmpty(request.Token))
                    {
                        Console.WriteLine("Token is missing or invalid.");
                        return new ErrorExecuter("Token is missing or invalid.", StatusCode.Unauthorized);
                    }
                }

                // Routen-Handler ausführen
                return routeHandler(request);
            }

            Console.WriteLine($"No matching route found for Method: {request.Method} and Path: {request.ResourcePath}");
            return null;
        }

        // Diese Methode stellt sicher, dass der Payload-Body nicht null ist
        private string EnsureBody(string? body)
        {
            if (body == null)
            {
                throw new InvalidDataException("Request body cannot be null.");
            }
            return body;
        }

        // Deserialisiert den JSON-Body in das angegebene Typ-Objekt
        private T Deserialize<T>(string? body) where T : class
        {
            if (string.IsNullOrEmpty(body))
            {
                throw new InvalidDataException("Request body is empty or null.");
            }

            var data = JsonConvert.DeserializeObject<T>(body);
            if (data == null)
            {
                throw new InvalidDataException("Failed to deserialize request body.");
            }
            return data;
        }

        IRouteCommand? IRoute.Resolve(RequestContext request)
        {
            throw new NotImplementedException();
        }
    }
}
