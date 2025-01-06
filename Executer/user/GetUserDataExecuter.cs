using MCTG_Trimmel.HTTP;
using Newtonsoft.Json;
using Trimmel_MCTG.db;
using Trimmel_MCTG.HTTP;

namespace Trimmel_MCTG.Executer.user
{
    public class GetUserDataExecuter : IRouteCommand
    {
        private readonly RequestContext requestContext;
        private readonly string username;
        private Database db;

        public GetUserDataExecuter(RequestContext request, string username)
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
                string requestingUser = ExtractUsernameFromToken(requestContext.Token);
                if (requestingUser != username)
                {
                    response.Payload = "You are not authorized to access this user data.";
                    response.StatusCode = StatusCode.Forbidden;
                    return response;
                }

                var user = Users.LoadFromDatabase(db, username);
                if (user == null)
                {
                    response.Payload = "User not found.";
                    response.StatusCode = StatusCode.NotFound;
                    return response;
                }

                response.Payload = JsonConvert.SerializeObject(new
                {
                    Name = user.Username,
                    user.Bio,
                    user.Image
                });
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
                throw new UnauthorizedAccessException("Authorization token is missing.");

            string[] tokenParts = token.Split('-');
            if (tokenParts.Length < 2)
                throw new UnauthorizedAccessException("Invalid authorization token format.");

            return tokenParts[0];
        }
    }
}
