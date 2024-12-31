using MCTG_Trimmel.HTTP;
using Newtonsoft.Json;
using Npgsql;
using Trimmel_MCTG.db;
using Trimmel_MCTG.HTTP;

namespace Trimmel_MCTG.Execute
{
    public class ShowCardsExecuter : IRouteCommand
    {
        private readonly RequestContext requestContext;
        private Database db; // Datenbank-Instanz

        public ShowCardsExecuter(RequestContext requestContext)
        {
            this.requestContext = requestContext;
        }

        // Implementierung der Methode SetDatabase
        public void SetDatabase(Database database)
        {
            db = database;
        }

        public Response Execute()
        {
            Response response = new Response();

            // Prüfen, ob das Token bereitgestellt wurde
            if (string.IsNullOrEmpty(requestContext.Token))
            {
                response.StatusCode = StatusCode.Unauthorized; // Setze 401 Unauthorized
                response.Payload = "Authorization token is missing.";
                return response;
            }

            try
            {
                // Extrahiere den Benutzernamen aus dem Token
                string[] tokenParts = requestContext.Token.Split('-');
                if (tokenParts.Length == 0 || string.IsNullOrEmpty(tokenParts[0]))
                {
                    response.StatusCode = StatusCode.BadRequest;
                    response.Payload = "Invalid authorization token format.";
                    return response;
                }

                string username = tokenParts[0];

                // Rufe alle Karten des Benutzers aus der Datenbank ab
                var userCards = db.GetCardsByUsername(username);
                response.Payload = JsonConvert.SerializeObject(userCards);
                response.StatusCode = StatusCode.Ok; // Setze 200 OK
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                response.StatusCode = StatusCode.InternalServerError; // Setze 500 Internal Server Error
                response.Payload = $"An error occurred: {ex.Message}";
            }

            return response;
        }


    }
}
