using MCTG_Trimmel.HTTP;
using Newtonsoft.Json;
using System.Text;
using Trimmel_MCTG.db;
using Trimmel_MCTG.DB;
using Trimmel_MCTG.HTTP;
using CustomHttpMethod = Trimmel_MCTG.HTTP.HttpMethod;

namespace Trimmel_MCTG.Execute
{
    public class ShowDecksExecuter : IRouteCommand
    {
        private readonly RequestContext requestContext;
        private Database db;

        public ShowDecksExecuter(RequestContext requestContext)
        {
            this.requestContext = requestContext;
        }

        public void SetDatabase(Database database)
        {
            db = database;
        }

        public Response Execute()
        {
            var response = new Response();

            try
            {
                // Überprüfen, ob ein Token vorhanden ist
                if (string.IsNullOrEmpty(requestContext?.Token))
                {
                    response.Payload = "Token is missing or invalid.";
                    response.StatusCode = StatusCode.Unauthorized;
                    return response;
                }

                // Benutzername aus Token extrahieren
                string[] tokenParts = requestContext.Token.Split('-');
                string username = tokenParts[0];

                // Query-Parameter auslesen
                var format = requestContext.QueryParameters.ContainsKey("format") ? requestContext.QueryParameters["format"] : "json";

                if (requestContext.Method == CustomHttpMethod.Get)
                {
                    return ShowConfiguredDeck(username, format); // Methode ShowConfiguredDeck aufrufen
                }
                else if (requestContext.Method == CustomHttpMethod.Put)
                {
                    return ConfigureDeck(username); // Methode ConfigureDeck aufrufen
                }
                else
                {
                    response.Payload = "Unsupported HTTP method.";
                    response.StatusCode = StatusCode.NotFound;
                }
            }
            catch (Exception ex)
            {
                response.Payload = $"An error occurred: {ex.Message}";
                response.StatusCode = StatusCode.InternalServerError;
            }

            return response;
        }




        // Methode zum Anzeigen des Decks (GET)
        private Response ShowDeck(string username)
        {
            var response = new Response();
            try
            {
                var deckCards = db.ShowUnconfiguredDeck(username);

                if (deckCards == null || deckCards.Count == 0)
                {
                    response.Payload = JsonConvert.SerializeObject(new List<object>());
                    response.StatusCode = StatusCode.Ok;
                }
                else
                {
                    response.Payload = JsonConvert.SerializeObject(deckCards);
                    response.StatusCode = StatusCode.Ok;
                }
            }
            catch (Exception ex)
            {
                response.Payload = $"An error occurred: {ex.Message}";
                response.StatusCode = StatusCode.InternalServerError;
            }

            return response;
        }

        // Methode zum Konfigurieren des Decks (PUT)
        private Response ConfigureDeck(string username)
        {
            var response = new Response();

            try
            {
                // Prüfen, ob ein Body vorhanden ist
                if (string.IsNullOrEmpty(requestContext.Payload))
                {
                    response.Payload = "Request body is missing.";
                    response.StatusCode = StatusCode.BadRequest;
                    return response;
                }

                // JSON-Array von Card-IDs auslesen
                var cardIds = JsonConvert.DeserializeObject<List<string>>(requestContext.Payload);

                // Überprüfen, ob genau 4 Karten ausgewählt wurden
                if (cardIds == null || cardIds.Count != 4)
                {
                    response.Payload = "You must provide exactly 4 card IDs.";
                    response.StatusCode = StatusCode.BadRequest;
                    return response;
                }

                // Deck aktualisieren
                db.UpdateDeck(username, cardIds[0], cardIds[1], cardIds[2], cardIds[3]);

                response.Payload = "Deck configured successfully.";
                response.StatusCode = StatusCode.Ok;
            }
            catch (Exception ex)
            {
                response.Payload = $"An error occurred: {ex.Message}";
                response.StatusCode = StatusCode.InternalServerError;
            }

            return response;
        }

        public Response ShowConfiguredDeck(string username, string format = "json")
        {
            var response = new Response();
            try
            {
                var deckCards = db.GetConfiguredDeck(username); // Methode, die das Deck aus der DB abruft

                if (deckCards == null || deckCards.Count == 0)
                {
                    response.Payload = (format == "plain") ? "Deck is empty." : JsonConvert.SerializeObject(new List<object>());
                    response.StatusCode = StatusCode.Ok;
                    return response;
                }

                if (format == "plain")
                {
                    var plainText = FormatDeckAsPlainText(deckCards, username);
                    response.Payload = plainText;
                }
                else
                {
                    response.Payload = JsonConvert.SerializeObject(deckCards);
                }

                response.StatusCode = StatusCode.Ok;
            }
            catch (Exception ex)
            {
                response.Payload = $"An error occurred: {ex.Message}";
                response.StatusCode = StatusCode.InternalServerError;
            }
            return response;
        }

        private string FormatDeckAsPlainText(List<Cards> deckCards, string username)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Deck for {username}:");
            int count = 1;
            foreach (var card in deckCards)
            {
                sb.AppendLine($"{count}. {card.Name} ({card.Damage} Damage, {card.ElementType}, {card.CardType})");
                count++;
            }
            return sb.ToString();
        }

    }
}
