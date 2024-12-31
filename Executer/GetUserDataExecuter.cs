using MCTG_Trimmel.HTTP;
using Newtonsoft.Json;
using Trimmel_MCTG.db;
using Trimmel_MCTG.HTTP;

namespace Trimmel_MCTG.Execute
{
    public class GetUserDataExecuter : IRouteCommand
    {
        private readonly RequestContext requestContext;
        private readonly string username;
        private Database db;

        public GetUserDataExecuter(RequestContext request, string username)
        {
            this.requestContext = request;
            this.username = username;
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
                // Überprüfe Berechtigung: Nur der Benutzer selbst darf seine Daten abrufen
                string requestingUser = ExtractUsernameFromToken(requestContext.Token);
                if (requestingUser != username)
                {
                    response.Payload = "You are not authorized to access this user data.";
                    response.StatusCode = StatusCode.Forbidden;
                    return response;
                }

                // Lade Benutzer aus der Datenbank
                var user = Users.LoadFromDatabase(db, username);
                if (user == null)
                {
                    response.Payload = "User not found.";
                    response.StatusCode = StatusCode.NotFound;
                    return response;
                }

                // Erfolgreiche Antwort
                response.Payload = JsonConvert.SerializeObject(new
                {
                    Name = user.Username,
                    Bio = user.Bio,
                    Image = user.Image
                });
                response.StatusCode = StatusCode.Ok;
            }
            catch (Exception ex)
            {
                // Allgemeiner Fehlerfall
                response.Payload = $"An error occurred: {ex.Message}";
                response.StatusCode = StatusCode.InternalServerError;
            }

            return response;
        }

        private string ExtractUsernameFromToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                throw new UnauthorizedAccessException("Authorization token is missing.");

            // Extrahiere Benutzernamen aus Token
            string[] tokenParts = token.Split('-');
            if (tokenParts.Length < 2)
                throw new UnauthorizedAccessException("Invalid authorization token format.");

            return tokenParts[0];
        }
    }
}
