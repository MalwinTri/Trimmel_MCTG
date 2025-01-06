using MCTG_Trimmel.HTTP;
using Newtonsoft.Json;
using Trimmel_MCTG.db;
using Trimmel_MCTG.DB;
using Trimmel_MCTG.HTTP;

namespace Trimmel_MCTG.Executer.transaction
{
    public class AcquirePackagesExecuter : IRouteCommand
    {
        private readonly RequestContext requestContext;
        private Database db;

        public AcquirePackagesExecuter(RequestContext requestContext)
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
                string[] tokenParts = requestContext.Token?.Split('-') ?? Array.Empty<string>();
                if (tokenParts.Length == 0 || string.IsNullOrEmpty(tokenParts[0]))
                {
                    response.StatusCode = StatusCode.Unauthorized;
                    response.Payload = "Authorization token is invalid or missing.";
                    return response;
                }

                string username = tokenParts[0];

                int userCoins = db.GetUserCoins(username);
                if (userCoins < 5)
                {
                    response.Payload = "Not enough coins to acquire a package.";
                    response.StatusCode = StatusCode.BadRequest; 
                    return response;
                }

                var package = db.GetNextAvailablePackage();
                if (package == null)
                {
                    response.Payload = "No packages available.";
                    response.StatusCode = StatusCode.NotFound;
                    return response;
                }

                var packageCards = db.GetCardsByPackageId(package.PackageId);
                foreach (var card in packageCards)
                {
                    db.AssignCardToUser(username, card.CardId);
                }

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
