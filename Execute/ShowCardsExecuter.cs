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

            try
            {
                // Extrahiere den Benutzernamen aus dem Token
                string[] tokenParts = requestContext.Token.Split('-');
                string username = tokenParts[0];

                // Rufe alle Karten des Benutzers aus der Datenbank ab
                var userCards = db.GetCardsByUsername(username);

                // Serialisiere die Karten in JSON
                response.Payload = JsonConvert.SerializeObject(userCards);
                response.StatusCode = StatusCode.Ok;
            }
            catch (Exception ex)
            {
                response.Payload = $"An error occurred: {ex.Message}";
                response.StatusCode = StatusCode.InternalServerError;
            }

            return response;
        }
    }
}
