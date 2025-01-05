using MCTG_Trimmel.HTTP;
using Newtonsoft.Json;
using Trimmel_MCTG.db;
using Trimmel_MCTG.DB;
using Trimmel_MCTG.HTTP;

public class ShowStatsExecuter : IRouteCommand
{
    private RequestContext requestContext;
    private Database db;

    public ShowStatsExecuter(RequestContext requestContext)
    {
        this.requestContext = requestContext;
    }

    public void SetDatabase(Database database)
    {
        this.db = database; // Datenbankverbindung setzen
    }

    public Response Execute()
    {
        var response = new Response();

        try
        {
            string username = ExtractUsernameFromToken(requestContext.Token);

            var user = Users.LoadFromDatabase(db, username);
            if (user == null)
            {
                response.Payload = "User not found.";
                response.StatusCode = StatusCode.NotFound;
                return response;
            }

            var stats = UserStats.LoadOrCreateStats(db, user.UserId);

            var result = new StatsResponse
            {
                Username = user.Username,
                UserStats = new UserStatsResponse
                {
                    UserId = stats.UserId,
                    Wins = stats.Wins,
                    Losses = stats.Losses,
                    Elo = stats.Elo
                }
            };

            response.Payload = JsonConvert.SerializeObject(result);
            response.StatusCode = StatusCode.Ok;
        }
        catch (UnauthorizedAccessException ex)
        {
            response.Payload = ex.Message;
            response.StatusCode = StatusCode.Unauthorized;
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
            throw new UnauthorizedAccessException("Authorization token is missing.");

        var parts = token.Split('-');
        if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0]))
            return parts[0];

        throw new UnauthorizedAccessException("Invalid token format.");
    }

    public class StatsResponse
    {
        public string Username { get; set; }
        public UserStatsResponse UserStats { get; set; }
    }

    public class UserStatsResponse
    {
        public int UserId { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Elo { get; set; }
    }
}


