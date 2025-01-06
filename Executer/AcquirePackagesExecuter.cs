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
using Trimmel_MCTG.DB;

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
                string[] tokenParts = requestContext.Token?.Split('-') ?? Array.Empty<string>();
                if (tokenParts.Length == 0 || string.IsNullOrEmpty(tokenParts[0]))
                {
                    response.StatusCode = StatusCode.Unauthorized;
                    response.Payload = "Authorization token is invalid or missing.";
                    return response;
                }

                string username = tokenParts[0];

                // Überprüfe, ob Benutzer genug Coins hat
                int userCoins = db.GetUserCoins(username);
                if (userCoins < 5)
                {
                    response.Payload = "Not enough coins to acquire a package.";
                    response.StatusCode = StatusCode.BadRequest; // 400 Bad Request
                    return response;
                }

                // Hol das nächste verfügbare Paket
                var package = db.GetNextAvailablePackage();
                if (package == null)
                {
                    response.Payload = "No packages available.";
                    response.StatusCode = StatusCode.NotFound; // 404 Not Found
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

                // Deck aktualisieren, falls genügend Karten vorhanden
                var bestCards = db.GetBest4Cards(username);
                if (bestCards.Count >= 4)
                {
                    db.InsertIntoDeck(
                        bestCards[0].CardId,
                        bestCards[1].CardId,
                        bestCards[2].CardId,
                        bestCards[3].CardId,
                        username
                    );
                }
                else
                {
                    Console.WriteLine("Not enough cards to form a deck.");
                }

                response.Payload = $"Package acquired successfully for user: {username}";
                response.StatusCode = StatusCode.Created; // 201 Created
            }
            catch (UnauthorizedAccessException ex)
            {
                response.Payload = $"Unauthorized access: {ex.Message}";
                response.StatusCode = StatusCode.Unauthorized; // 401 Unauthorized
            }
            catch (KeyNotFoundException ex)
            {
                response.Payload = $"Data not found: {ex.Message}";
                response.StatusCode = StatusCode.NotFound; // 404 Not Found
            }
            catch (Exception ex)
            {
                response.Payload = $"An internal error occurred: {ex.Message}";
                response.StatusCode = StatusCode.InternalServerError; // 500 Internal Server Error
            }

            return response;
        }

    }
}
