using System;
using System.Collections.Generic;
using Trimmel_MCTG.db;

namespace Trimmel_MCTG.Models
{
    public class Scoreboard
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int Wins { get; set; } = 0;
        public int Losses { get; set; } = 0;
        public int Elo { get; set; } = 1000;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public static List<Scoreboard> GetAll(Database db)
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

            return scoreboards;
        }

        public static Scoreboard LoadByUserId(Database db, int userId)
        {
            var parameters = new Dictionary<string, object> { { "@userid", userId } };
            var results = db.ExecuteQuery("SELECT * FROM scoreboard WHERE userid = @userid", parameters);

            if (results.Count == 0)
            {
                throw new KeyNotFoundException($"Scoreboard entry for userId {userId} not found.");
            }

            var row = results[0];
            return new Scoreboard
            {
                Id = Convert.ToInt32(row["id"]),
                UserId = Convert.ToInt32(row["userid"]),
                Wins = Convert.ToInt32(row["wins"]),
                Losses = Convert.ToInt32(row["losses"]),
                Elo = Convert.ToInt32(row["elo"]),
                CreatedAt = Convert.ToDateTime(row["created_at"]),
                UpdatedAt = Convert.ToDateTime(row["updated_at"])
            };
        }

        public void SaveToDatabase(Database db)
        {
            var parameters = new Dictionary<string, object>
            {
                { "@userid", UserId },
                { "@wins", Wins },
                { "@losses", Losses },
                { "@elo", Elo },
                { "@updated_at", DateTime.UtcNow }
            };

            db.ExecuteNonQuery(
                "UPDATE scoreboard SET wins = @wins, losses = @losses, elo = @elo, updated_at = @updated_at WHERE userid = @userid",
                parameters
            );
        }

        public static void CreateEntry(Database db, int userId)
        {
            var parameters = new Dictionary<string, object>
            {
                { "@userid", userId },
                { "@wins", 0 },
                { "@losses", 0 },
                { "@elo", 1000 },
                { "@created_at", DateTime.UtcNow },
                { "@updated_at", DateTime.UtcNow }
            };

            db.ExecuteNonQuery(
                "INSERT INTO scoreboard (userid, wins, losses, elo, created_at, updated_at) VALUES (@userid, @wins, @losses, @elo, @created_at, @updated_at)",
                parameters
            );
        }
    }
}
