using MCTG_Trimmel.HTTP;
using Newtonsoft.Json;
using Trimmel_MCTG.db;
using Trimmel_MCTG.DB;
using Trimmel_MCTG.HTTP;
using System;
using System.Collections.Generic;

public class BattleExecuter : IRouteCommand
{
    private readonly RequestContext requestContext;
    private Database db;

    // Warteschlange für Spieler
    private static Queue<int> waitingPlayers = new Queue<int>();

    public BattleExecuter(RequestContext requestContext)
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
            string username = ExtractUsernameFromToken(requestContext.Token);
            var user = Users.LoadFromDatabase(db, username);

            if (user == null)
            {
                response.Payload = "User not found.";
                response.StatusCode = StatusCode.NotFound;
                return response;
            }

            // Spieler zur Warteschlange hinzufügen
            lock (waitingPlayers)
            {
                if (waitingPlayers.Count == 0)
                {
                    waitingPlayers.Enqueue(user.UserId);
                    response.Payload = "Warten auf zweiten Spieler.";
                    response.StatusCode = StatusCode.Accepted;
                    return response;
                }
                else
                {
                    // Zweiten Spieler finden
                    int opponentId = waitingPlayers.Dequeue();

                    // Gegner aus der Datenbank laden
                    var opponent = Users.LoadFromDatabase(db, opponentId);
                    StartBattle(user, opponent);

                    response.Payload = "Kampf gestartet!";
                    response.StatusCode = StatusCode.Ok;
                    return response;
                } 
            }
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


    private void StartBattle(Users player1, Users player2)
    {
        Console.WriteLine($"Kampf zwischen {player1.Username} und {player2.Username} gestartet.");

        // Beispielhafte Kampf-Logik
        var random = new Random();
        bool player1Wins = random.Next(0, 2) == 0;

        if (player1Wins)
        {
            Console.WriteLine($"{player1.Username} hat gewonnen!");
            UpdateStats(player1, true);
            UpdateStats(player2, false);
        }
        else
        {
            Console.WriteLine($"{player2.Username} hat gewonnen!");
            UpdateStats(player2, true);
            UpdateStats(player1, false);
        }
    }

    private void UpdateStats(Users player, bool won)
    {
        var stats = UserStats.LoadOrCreateStats(db, player.UserId);

        if (won)
        {
            stats.Wins++;
            stats.Elo += 10;
        }
        else
        {
            stats.Losses++;
            stats.Elo -= 5;
        }

        stats.SaveToDatabase(db);
    }
}

public class BattleResult
{
    public Users Winner { get; set; }
    public Users Loser { get; set; }
}
