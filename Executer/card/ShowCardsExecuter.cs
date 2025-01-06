using MCTG_Trimmel.HTTP;
using Newtonsoft.Json;
using Npgsql;
using Trimmel_MCTG.db;
using Trimmel_MCTG.HTTP;

namespace Trimmel_MCTG.Executer.card
{
    public class ShowCardsExecuter : IRouteCommand
    {
        private readonly RequestContext requestContext;
        private Database db;

        public ShowCardsExecuter(RequestContext requestContext)
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

            if (string.IsNullOrEmpty(requestContext.Token))
            {
                response.StatusCode = StatusCode.Unauthorized;
                response.Payload = "Authorization token is missing.";
                return response;
            }

            try
            {
                string[] tokenParts = requestContext.Token.Split('-');
                if (tokenParts.Length == 0 || string.IsNullOrEmpty(tokenParts[0]))
                {
                    response.StatusCode = StatusCode.BadRequest;
                    response.Payload = "Invalid authorization token format.";
                    return response;
                }

                string username = tokenParts[0];

                var userCards = db.GetCardsByUsername(username);
                response.Payload = JsonConvert.SerializeObject(userCards);
                response.StatusCode = StatusCode.Ok; 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                response.StatusCode = StatusCode.InternalServerError; 
                response.Payload = $"An error occurred: {ex.Message}";
            }

            return response;
        }


    }
}
