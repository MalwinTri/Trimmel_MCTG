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
                // Token-Validierung
                if (string.IsNullOrEmpty(requestContext?.Token))
                {
                    response.Payload = "Token is missing or invalid.";
                    response.StatusCode = StatusCode.Unauthorized;
                    return response;
                }

                // Benutzername aus Token extrahieren
                string username = requestContext.Token.Split('-')[0];
                Console.WriteLine($"Authentifizierter Benutzer: {username}");

                // Routen-Handling
                Console.WriteLine($"Received ResourcePath: {requestContext.ResourcePath}");
                if (requestContext.ResourcePath == "/deck/unconfigured")
                {
                    Console.WriteLine("Aufruf: /deck/unconfigured");
                    return ShowUnconfiguredDeck(username);
                }

                // Konfigurierte Decks oder Deck-Konfiguration
                var format = requestContext.QueryParameters?.ContainsKey("format") ?? false
                    ? requestContext.QueryParameters["format"]
                    : "json";

                Console.WriteLine($"HTTP-Methode: {requestContext.Method}, Format: {format}");

                return requestContext.Method switch
                {
                    CustomHttpMethod.Get => ShowConfiguredDeck(username, format),
                    CustomHttpMethod.Put => ConfigureDeckFromPayload(username),
                    _ => new Response
                    {
                        StatusCode = StatusCode.Forbidden,
                        Payload = "Unsupported HTTP method."
                    }
                };
            }
            catch (Exception ex)
            {
                response.Payload = $"An error occurred: {ex.Message}";
                response.StatusCode = StatusCode.InternalServerError;
                Console.WriteLine($"Fehler in Execute: {ex.Message}");
            }

            return response;
        }

        // Methode zum Anzeigen des konfigurierten Decks (GET /deck)
        private Response ShowConfiguredDeck(string username, string format)
        {
            var response = new Response();
            try
            {
                Console.WriteLine($"Abrufen des konfigurierten Decks für Benutzer: {username}");
                var deckCards = db.GetConfiguredDeck(username);
                Console.WriteLine($"Anzahl der Karten im Deck: {deckCards?.Count ?? 0}");

                if (deckCards == null || deckCards.Count == 0)
                {
                    Console.WriteLine("Deck ist leer.");
                    response.Payload = format == "plain" ? "Deck is empty." : JsonConvert.SerializeObject(new List<object>());
                    response.StatusCode = StatusCode.Ok;
                }
                else
                {
                    Console.WriteLine("Deck enthält Karten.");
                    response.Payload = format == "plain"
                        ? FormatDeckAsPlainText(deckCards, username)
                        : JsonConvert.SerializeObject(deckCards);
                    response.StatusCode = StatusCode.Ok;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Abrufen des Decks: {ex.Message}");
                response.Payload = $"An error occurred: {ex.Message}";
                response.StatusCode = StatusCode.InternalServerError;
            }

            return response;
        }

        // Methode zum Konfigurieren des Decks (PUT /deck)
        private Response ConfigureDeckFromPayload(string username)
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

                // Deck konfigurieren über die Database-Klasse
                db.ConfigureDeck(username, cardIds);

                response.Payload = "Deck configured successfully.";
                response.StatusCode = StatusCode.Ok;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler in ConfigureDeckFromPayload: {ex.Message}");
                response.Payload = $"An error occurred: {ex.Message}";
                response.StatusCode = StatusCode.BadRequest;
            }

            return response;
        }

        // Methode zum Anzeigen des unkonfigurierten Decks (GET /deck/unconfigured)
        private Response ShowUnconfiguredDeck(string username)
        {
            var response = new Response();

            try
            {
                Console.WriteLine($"Abrufen des unkonfigurierten Decks für Benutzer: {username}");
                var deckCards = db.ShowUnconfiguredDeck(username);

                if (deckCards == null || deckCards.Count == 0)
                {
                    Console.WriteLine("Unkonfiguriertes Deck ist leer.");
                    response.Payload = JsonConvert.SerializeObject(new List<object>()); // Leeres JSON-Array
                    response.StatusCode = StatusCode.Ok;
                }
                else
                {
                    Console.WriteLine("Unkonfiguriertes Deck enthält Karten.");
                    response.Payload = JsonConvert.SerializeObject(deckCards);
                    response.StatusCode = StatusCode.Ok;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Abrufen des unkonfigurierten Decks: {ex.Message}");
                response.Payload = $"An error occurred: {ex.Message}";
                response.StatusCode = StatusCode.InternalServerError;
            }

            return response;
        }

        // Methode zum Formatieren des Decks als Plain Text
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
