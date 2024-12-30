using MCTG_Trimmel.HTTP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Npgsql;
using Trimmel_MCTG.db;
using Trimmel_MCTG.HTTP;

namespace Trimmel_MCTG.Execute
{
    public class AcquirePackage : IRouteCommand
    {
        private readonly RequestContext requestContext;
        private Database db;

        public AcquirePackage(RequestContext requestContext)
        {
            this.requestContext = requestContext;
        }

        public void SetDatabase(Database database)
        {
            db = database; 
        }

        public Response Execute()
        {
            Response response = new Response();

            try
            {
                // Extrahiere Benutzername aus Token
                string[] tokenParts = requestContext.Token.Split('-');
                string username = tokenParts[0];

                // Überprüfe, ob Benutzer genug Coins hat
                int userCoins = db.GetUserCoins(username);
                if (userCoins < 5)
                {
                    response.Payload = "Not enough coins to acquire a package.";
                    response.StatusCode = StatusCode.BadRequest;
                    return response;
                }

                // Hol das nächste verfügbare Paket
                var package = db.GetNextAvailablePackage();
                if (package == null)
                {
                    response.Payload = "No packages available.";
                    response.StatusCode = StatusCode.BadRequest;
                    return response;
                }

                // Karten des Pakets dem Benutzer zuweisen
                var packageCards = db.GetCardsByPackageId(package.PackageId);
                foreach (var card in packageCards)
                {
                    db.AssignCardToUser(username, card.CardId);
                }

                // Reduziere Benutzercoins
                db.UpdateUserCoins(username, userCoins - 5);

                // Lösche das Paket
                db.DeletePackage(package.PackageId);

                // Optional: Beste Karten in Deck einfügen
                db.DeleteDeckByUser(username);
                var bestCards = db.GetBest4Cards(username);

                // Überprüfen Sie, ob mindestens 4 Karten verfügbar sind
                if (bestCards.Count >= 4)
                {
                    db.InsertIntoDeck(
                        bestCards[0].CardId, // Erste Karte
                        bestCards[1].CardId, // Zweite Karte
                        bestCards[2].CardId, // Dritte Karte
                        bestCards[3].CardId, // Vierte Karte
                        username             // Benutzername
                    );
                }
                else
                {
                    // Fehlerfall: Weniger als 4 Karten verfügbar
                    Console.WriteLine("Not enough cards to form a deck.");
                }

                response.Payload = $"Package acquired successfully for user: {username}";
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
