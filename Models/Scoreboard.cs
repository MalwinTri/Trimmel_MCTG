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

        
    }
}
