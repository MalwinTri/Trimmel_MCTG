using MCTG_Trimmel.HTTP;
using Newtonsoft.Json;
using Npgsql;
using Trimmel_MCTG.db;
using Trimmel_MCTG.HTTP;

namespace Trimmel_MCTG.Executer.user
{
    public class UpdateUserDataExecuter : IRouteCommand
    {
        private readonly RequestContext requestContext;
        private readonly string username;
        private Database db;

        public UpdateUserDataExecuter(RequestContext request, string username)
        {
            requestContext = request;
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
                string tokenUsername = ExtractUsernameFromToken(requestContext.Token);

                if (tokenUsername != username)
                {
                    response.Payload = "You are not authorized to update this user data.";
                    response.StatusCode = StatusCode.Forbidden;
                    return response;
                }

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

                user.Bio = updatedData.Bio;
                user.Image = updatedData.Image;

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

            var tokenParts = token.Split('-');
            if (tokenParts.Length > 0)
            {
                return tokenParts[0];
            }

            throw new InvalidOperationException("Invalid token format.");
        }

    }

    
}
