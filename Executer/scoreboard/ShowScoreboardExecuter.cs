using MCTG_Trimmel.HTTP;
using Newtonsoft.Json;
using Trimmel_MCTG.db;
using Trimmel_MCTG.DB;
using System.Collections.Generic;
using Trimmel_MCTG.HTTP;
using Trimmel_MCTG.Models;

public class ShowScoreboardExecuter : IRouteCommand
{
    private RequestContext requestContext;
    private Database db;

    public ShowScoreboardExecuter(RequestContext requestContext)
    {
        this.requestContext = requestContext;
    }

    public void SetDatabase(Database database)
    {
        this.db = database;
    }

    public Response Execute()
    {
        var response = new Response();

        try
        {
            var results = db.ExecuteQuery("SELECT * FROM scoreboard ORDER BY elo DESC", new Dictionary<string, object>());

            var scoreboards = new List<Scoreboard>();
            foreach (var row in results)
            {
                scoreboards.Add(new Scoreboard
                {
                    Id = Convert.ToInt32(row["id"]),
                    UserId = Convert.ToInt32(row["userid"]),
                    Wins = Convert.ToInt32(row["wins"]),
                    Losses = Convert.ToInt32(row["losses"]),
                    Elo = Convert.ToInt32(row["elo"]),
                    CreatedAt = Convert.ToDateTime(row["created_at"]),
                    UpdatedAt = Convert.ToDateTime(row["updated_at"])
                });
            }

            response.Payload = JsonConvert.SerializeObject(scoreboards);
            response.StatusCode = StatusCode.Ok;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred: {ex.Message}");
            response.Payload = $"An error occurred: {ex.Message}";
            response.StatusCode = StatusCode.InternalServerError;
        }

        return response;
    }
}
