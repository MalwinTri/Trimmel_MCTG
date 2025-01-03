using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Trimmel_MCTG.db;
using Trimmel_MCTG.HTTP;
using MCTG_Trimmel.HTTP;

namespace Trimmel_MCTG.Executer
{
    internal class TradeExecuter : IRouteCommand
    {
        private readonly RequestContext requestContext;
        private Database db;

        public TradeExecuter(RequestContext requestContext)
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
                // Prüfen, ob die URL auf ein bestimmtes Handelsangebot verweist
                if (requestContext.ResourcePath.Contains("/tradings/"))
                {
                    var pathParts = requestContext.ResourcePath.Split('/');
                    if (pathParts.Length < 3 || !Guid.TryParse(pathParts[2], out var tradingId))
                    {
                        response.Payload = "Invalid trading deal ID.";
                        response.StatusCode = StatusCode.BadRequest;
                        return response;
                    }

                    // POST /tradings/{id} → Trade ausführen
                    if (requestContext.Method == Trimmel_MCTG.HTTP.HttpMethod.Post)
                    {
                        return ExecuteTrade(tradingId);
                    }
                    // DELETE /tradings/{id} → Handel löschen
                    else if (requestContext.Method == Trimmel_MCTG.HTTP.HttpMethod.Delete)
                    {
                        return DeleteTrade(tradingId);
                    }
                }
                else if (requestContext.ResourcePath == "/tradings" && requestContext.Method == Trimmel_MCTG.HTTP.HttpMethod.Post)
                {
                    // POST /tradings → Handel erstellen
                    return CreateTradingDeal();
                }

                response.Payload = "Invalid request.";
                response.StatusCode = StatusCode.BadRequest;
            }
            catch (Exception ex)
            {
                response.Payload = $"An error occurred: {ex.Message}";
                response.StatusCode = StatusCode.InternalServerError;
            }

            return response;
        }

        private Response ExecuteTrade(Guid tradingId)
        {
            var response = new Response();

            try
            {
                // Handelsangebot aus der Datenbank abrufen
                var tradingDeal = db.ExecuteQuery(
                    "SELECT userid, offered_card_id, required_type, min_damage FROM trading WHERE tradingid = @tradingId::uuid",
                    new Dictionary<string, object> { { "@tradingId", tradingId } }
                ).FirstOrDefault();

                if (tradingDeal == null)
                {
                    response.Payload = "Trading deal not found.";
                    response.StatusCode = StatusCode.NotFound;
                    return response;
                }

                // Benutzer-Informationen
                string username = ExtractUsernameFromToken(requestContext.Token);
                var user = Users.LoadFromDatabase(db, username);

                // Überprüfen, ob Benutzer mit sich selbst handelt
                if (user.UserId == Convert.ToInt32(tradingDeal["userid"]))
                {
                    response.Payload = "You cannot trade with yourself.";
                    response.StatusCode = StatusCode.BadRequest;
                    return response;
                }

                // Extrahiere die angebotene Karte aus dem Request-Body
                var offeredCardId = Guid.Parse(requestContext.Payload.Trim('"'));

                // Überprüfen, ob der Benutzer die Karte besitzt
                bool ownsCard = db.DoesUserOwnCard(username, offeredCardId);

                if (!ownsCard)
                {
                    response.Payload = "You do not own the card you are offering.";
                    response.StatusCode = StatusCode.BadRequest;
                    return response;
                }

                // Handelslogik: Tausche Karten, lösche das Handelsangebot
                db.ExecuteNonQuery(
                    "DELETE FROM trading WHERE tradingid = @tradingId::uuid",
                    new Dictionary<string, object> { { "@tradingId", tradingId } }
                );

                response.Payload = "Trade executed successfully.";
                response.StatusCode = StatusCode.Ok;
            }
            catch (Exception ex)
            {
                response.Payload = $"An error occurred: {ex.Message}";
                response.StatusCode = StatusCode.InternalServerError;
            }

            return response;
        }




        private Response DeleteTrade(Guid tradingId)
        {
            var response = new Response();

            try
            {
                int rowsAffected = db.ExecuteNonQuery(
                    "DELETE FROM trading WHERE tradingid = @tradingId",
                    new Dictionary<string, object> { { "@tradingId", tradingId } }
                );

                if (rowsAffected == 0)
                {
                    response.Payload = "Trading deal not found.";
                    response.StatusCode = StatusCode.NotFound;
                }
                else
                {
                    response.Payload = "Trading deal deleted successfully.";
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

        private Response CreateTradingDeal()
        {
            var response = new Response();

            try
            {
                // JSON-Body des Requests parsen
                var payload = JsonConvert.DeserializeObject<TradingDealPayload>(requestContext.Payload);
                if (payload == null)
                {
                    response.Payload = "Invalid payload.";
                    response.StatusCode = StatusCode.BadRequest;
                    return response;
                }

                // Validierung der Eingabedaten
                if (string.IsNullOrEmpty(payload.Id) ||
                    string.IsNullOrEmpty(payload.CardToTrade) ||
                    string.IsNullOrEmpty(payload.Type) ||
                    payload.MinimumDamage < 0)
                {
                    response.Payload = "Invalid trading deal details.";
                    response.StatusCode = StatusCode.BadRequest;
                    return response;
                }

                // Überprüfen, ob der Benutzer existiert
                string username = ExtractUsernameFromToken(requestContext.Token);
                var user = Users.LoadFromDatabase(db, username);
                if (user == null)
                {
                    response.Payload = "User not found.";
                    response.StatusCode = StatusCode.NotFound;
                    return response;
                }

                // Handelsangebot in der Datenbank erstellen
                var parameters = new Dictionary<string, object>
                {
                    { "@tradingId", Guid.Parse(payload.Id) }, // Übernimm die `Id` aus dem Request
                    { "@userId", user.UserId },
                    { "@offeredCardId", Guid.Parse(payload.CardToTrade) },
                    { "@requiredType", payload.Type },
                    { "@minDamage", payload.MinimumDamage }
                };

                db.ExecuteNonQuery(
                    "INSERT INTO trading (tradingid, userid, offered_card_id, required_type, min_damage) " +
                    "VALUES (@tradingId, @userId, @offeredCardId, @requiredType::card_type_enum, @minDamage)",
                    parameters
                );

                response.Payload = "Trading deal created successfully.";
                response.StatusCode = StatusCode.Created;
            }
            catch (Exception ex)
            {
                response.Payload = $"An error occurred: {ex.Message}";
                response.StatusCode = StatusCode.InternalServerError;
            }

            return response;
        }

        private string ExtractUsernameFromToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                throw new ArgumentException("Token cannot be null or empty.", nameof(token));

            var parts = token.Split('-');
            if (parts.Length > 0)
                return parts[0];

            throw new InvalidOperationException("Invalid token format.");
        }

        // TradingDealPayload-Klasse zur Deserialisierung des JSON-Requests
        private class TradingDealPayload
        {
            public string Id { get; set; } // Optional, wenn von der API mitgegeben
            public string CardToTrade { get; set; }
            public string Type { get; set; }
            public int MinimumDamage { get; set; }
        }
    }
}
