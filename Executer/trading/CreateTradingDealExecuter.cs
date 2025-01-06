using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Trimmel_MCTG.DB;
using Trimmel_MCTG.db;
using Trimmel_MCTG.HTTP;
using MCTG_Trimmel.HTTP;
using Trimmel_MCTG.helperClass;

namespace Trimmel_MCTG.Executer.trading
{
    internal class CreateTradingDealExecuter : IRouteCommand
    {
        private readonly RequestContext requestContext;
        private Database db;

        public CreateTradingDealExecuter(RequestContext requestContext)
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
                var payload = JsonConvert.DeserializeObject<TradingDealPayload>(requestContext.Payload);
                if (payload == null)
                {
                    response.Payload = "Invalid payload.";
                    response.StatusCode = StatusCode.BadRequest;
                    return response;
                }

                if (string.IsNullOrEmpty(payload.Id) ||
                    string.IsNullOrEmpty(payload.CardToTrade) ||
                    string.IsNullOrEmpty(payload.Type) ||
                    payload.MinimumDamage < 0)
                {
                    response.Payload = "Invalid trading deal details.";
                    response.StatusCode = StatusCode.BadRequest;
                    return response;
                }

                string username = ExtractUsernameFromToken(requestContext.Token);
                var user = Users.LoadFromDatabase(db, username);
                if (user == null)
                {
                    response.Payload = "User not found.";
                    response.StatusCode = StatusCode.NotFound;
                    return response;
                }

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
    }
}