using MCTG_Trimmel.HTTP;
using Newtonsoft.Json;
using Trimmel_MCTG.db;
using Trimmel_MCTG.HTTP;

namespace Trimmel_MCTG.Executer.stats
{
    public class GetUserStatsExecuter : IRouteCommand
    {
        private readonly RequestContext requestContext;
        private Database db;

        public GetUserStatsExecuter(RequestContext request)
        {
            requestContext = request;
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
                string username = ExtractUsernameFromToken(requestContext.Token);

                var parameters = new Dictionary<string, object>
                {
                    { "@username", username }
                };

                var result = db.ExecuteQuery(
                    "SELECT wins, losses, draws, elo FROM user_stats WHERE username = @username",
                    parameters
                );

                if (result.Count > 0)
                {
                    var stats = new
                    {
                        Wins = Convert.ToInt32(result[0]["wins"]),
                        Losses = Convert.ToInt32(result[0]["losses"]),
                        Draws = Convert.ToInt32(result[0]["draws"]),
                        Elo = Convert.ToInt32(result[0]["elo"])
                    };

                    response.StatusCode = StatusCode.Ok;
                    response.Payload = JsonConvert.SerializeObject(stats);
                }
                else
                {
                    response.StatusCode = StatusCode.NotFound;
                    response.Payload = "User stats not found.";
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = StatusCode.InternalServerError;
                response.Payload = $"An error occurred: {ex.Message}";
            }

            return response;
        }

        private string ExtractUsernameFromToken(string token)
        {
            return token.Split('-')[0];
        }
    }
}
