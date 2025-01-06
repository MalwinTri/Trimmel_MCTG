using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Trimmel_MCTG.db;
using Trimmel_MCTG.HTTP;
using MCTG_Trimmel.HTTP;
using Trimmel_MCTG.helperClass;

namespace Trimmel_MCTG.Executer.trading
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
                if (requestContext.ResourcePath.Contains("/tradings/"))
                {
                    var pathParts = requestContext.ResourcePath.Split('/');
                    if (pathParts.Length < 3 || !Guid.TryParse(pathParts[2], out var tradingId))
                    {
                        response.Payload = "Invalid trading deal ID.";
                        response.StatusCode = StatusCode.BadRequest;
                        return response;
                    }

                    if (requestContext.Method == HTTP.HttpMethod.Post)
                    {
                        return ExecuteTrade(tradingId);
                    }
                    else if (requestContext.Method == HTTP.HttpMethod.Delete)
                    {
                        return DeleteTrade(tradingId);
                    }
                }
                else if (requestContext.ResourcePath == "/tradings" && requestContext.Method == HTTP.HttpMethod.Post)
                {
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

                string username = ExtractUsernameFromToken(requestContext.Token);
                var user = Users.LoadFromDatabase(db, username);

                if (user.UserId == Convert.ToInt32(tradingDeal["userid"]))
                {
                    response.Payload = "You cannot trade with yourself.";
                    response.StatusCode = StatusCode.BadRequest;
                    return response;
                }

                var offeredCardId = Guid.Parse(requestContext.Payload.Trim('"'));

                bool ownsCard = db.DoesUserOwnCard(username, offeredCardId);

                if (!ownsCard)
                {
                    response.Payload = "You do not own the card you are offering.";
                    response.StatusCode = StatusCode.BadRequest;
                    return response;
                }

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
                    { "@tradingId", Guid.Parse(payload.Id) }, 
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
