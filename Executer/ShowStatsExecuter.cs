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

            // Nur Username und UserStats zurückgeben
            var result = new
            {
                Username = user.Username,
                UserStats = new
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
