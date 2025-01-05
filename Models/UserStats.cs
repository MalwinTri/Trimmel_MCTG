using System;
using System.Collections.Generic;
using Trimmel_MCTG.db;

namespace Trimmel_MCTG.DB
{
    public class UserStats
    {
        public int UserId { get; set; }
        public int Wins { get; set; } = 0;
        public int Losses { get; set; } = 0;
        public int Elo { get; set; } = 1000;

        public Users? User { get; set; }

        // Lädt oder erstellt die Statistiken eines Benutzers
        public static UserStats LoadOrCreateStats(Database db, int userId)
        {
            var parameters = new Dictionary<string, object> { { "@userid", userId } };

            // Überprüfen, ob ein Eintrag in `userstats` für den Benutzer existiert
            var result = db.ExecuteQuery("SELECT * FROM userstats WHERE userid = @userid", parameters);

            if (result.Count == 0)
            {
                // Wenn keine Statistiken vorhanden sind, Eintrag erstellen
                db.ExecuteNonQuery(
                    "INSERT INTO userstats (userid, wins, losses, elo) VALUES (@userid, 0, 0, 1000)",
                    parameters
                );

                // Nach der Erstellung erneut abrufen
                result = db.ExecuteQuery("SELECT * FROM userstats WHERE userid = @userid", parameters);

                // Log optional hinzufügen
                Console.WriteLine($"Created new stats entry for userId: {userId}");
            }

            // Erwartet genau eine Zeile aus der Datenbank
            var row = result[0];
            return new UserStats
            {
                UserId = Convert.ToInt32(row["userid"]),
                Wins = Convert.ToInt32(row["wins"]),
                Losses = Convert.ToInt32(row["losses"]),
                Elo = Convert.ToInt32(row["elo"])
            };
        }




        // Aktualisieren der Statistiken
        public void SaveToDatabase(db.Database db)
        {
            var parameters = new Dictionary<string, object>
            {
                { "@userid", UserId },
                { "@wins", Wins },
                { "@losses", Losses },
                { "@elo", Elo }
            };

            db.ExecuteNonQuery(
                "UPDATE userstats SET wins = @wins, losses = @losses, elo = @elo WHERE userid = @userid",
                parameters
            );
        }
    }
}
