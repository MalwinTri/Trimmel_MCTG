using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Trimmel_MCTG.DB;
using Trimmel_MCTG.db;
using Trimmel_MCTG.HTTP;
using MCTG_Trimmel.HTTP;

namespace Trimmel_MCTG.Executer.trading
{
    internal class ShowTradingDealsExecuter : IRouteCommand
    {
        private readonly RequestContext requestContext;
        private Database db;

        public ShowTradingDealsExecuter(RequestContext requestContext)
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
                var result = db.ExecuteQuery("SELECT * FROM trading", new Dictionary<string, object>());

                var tradingDeals = new List<object>();

                foreach (var row in result)
                {
                    tradingDeals.Add(new
                    {
                        TradingId = row["tradingid"].ToString(),
                        UserId = Convert.ToInt32(row["userid"]),
                        OfferedCardId = row["offered_card_id"].ToString(),
                        RequiredType = row["required_type"].ToString(),
                        MinDamage = Convert.ToInt32(row["min_damage"])
                    });
                }

                response.Payload = JsonConvert.SerializeObject(tradingDeals);
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