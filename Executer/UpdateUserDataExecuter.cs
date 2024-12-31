using MCTG_Trimmel.HTTP;
using Newtonsoft.Json;
using Npgsql;
using Trimmel_MCTG.db;
using Trimmel_MCTG.HTTP;

namespace Trimmel_MCTG.Execute
{
    public class UpdateUserDataExecuter : IRouteCommand
    {
        private readonly RequestContext requestContext;
        private readonly string username;
        private Database db;

        public UpdateUserDataExecuter(RequestContext request, string username)
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
                // Extrahiere den Benutzernamen aus dem Token
                string tokenUsername = ExtractUsernameFromToken(requestContext.Token);

                // Überprüfen, ob der Benutzer autorisiert ist
                if (tokenUsername != username)
                {
                    response.Payload = "You are not authorized to update this user data.";
                    response.StatusCode = StatusCode.Forbidden;
                    return response;
                }

                // Lese den Payload und überprüfe auf null
                if (string.IsNullOrEmpty(requestContext.Payload))
                {
                    response.Payload = "Payload is missing or empty.";
                    response.StatusCode = StatusCode.BadRequest;
                    return response;
                }

                var updatedData = JsonConvert.DeserializeObject<Users>(requestContext.Payload);

                if (updatedData == null)
                {
                    response.Payload = "Failed to deserialize user data.";
                    response.StatusCode = StatusCode.BadRequest;
                    return response;
                }

                if (!updatedData.IsValidForUpdate())
                {
                    response.Payload = "Invalid user data provided.";
                    response.StatusCode = StatusCode.BadRequest;
                    return response;
                }

                var user = Users.LoadFromDatabase(db, username);

                if (user == null)
                {
                    response.Payload = "User not found.";
                    response.StatusCode = StatusCode.NotFound;
                    return response;
                }

                // Aktualisieren der Daten
                user.Bio = updatedData.Bio;
                user.Image = updatedData.Image;

                // Speichern Sie die aktualisierten Daten
                var parameters = new Dictionary<string, object>
                {
                    { "@username", username },
                    { "@bio", user.Bio ?? string.Empty },
                    { "@image", user.Image ?? string.Empty }
                };

                db.ExecuteNonQuery(
                    "UPDATE Users SET Bio = @bio, Image = @image WHERE Username = @username",
                    new Dictionary<string, object>
                    {
                        { "@bio", updatedData.Bio ?? string.Empty },
                        { "@image", updatedData.Image ?? string.Empty },
                        { "@username", username }
                    }
                );


                response.Payload = "User data updated successfully.";
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
            {
                throw new ArgumentException("Token cannot be null or empty.", nameof(token));
            }

            // Trenne den Benutzernamen vom Suffix
            var tokenParts = token.Split('-');
            if (tokenParts.Length > 0)
            {
                return tokenParts[0]; // Der Benutzername ist der erste Teil
            }

            throw new InvalidOperationException("Invalid token format.");
        }

    }

    public class UserData
    {
        public int UserId { get; set; }   
        public string Name { get; set; }
        public string Bio { get; set; }
        public string Image { get; set; }
    }
}
