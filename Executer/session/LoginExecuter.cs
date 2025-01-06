using MCTG_Trimmel.HTTP;
using Newtonsoft.Json;
using Trimmel_MCTG.db;
using Trimmel_MCTG.DB;
using Trimmel_MCTG.HTTP;

public class LoginExecuter : IRouteCommand
{
    private readonly Users user;
    private readonly RequestContext requestContext;
    private Database db;

    public LoginExecuter(RequestContext request)
    {
        requestContext = request ?? throw new ArgumentNullException(nameof(request), "Request context cannot be null");

        if (string.IsNullOrEmpty(requestContext.Payload))
        {
            throw new InvalidDataException("Payload cannot be null or empty.");
        }

        user = JsonConvert.DeserializeObject<Users>(requestContext.Payload) ?? throw new InvalidDataException("Invalid user data in the payload.");
    }


    public void SetDatabase(Database db)
    {
        this.db = db ?? throw new ArgumentNullException(nameof(db), "Database cannot be null");
    }

    public Response Execute()
    {
        if (db == null)
        {
            throw new InvalidOperationException("Database connection has not been set.");
        }

        var response = new Response();
        try
        {
            string? token = db.Logging(user);

            if (!string.IsNullOrEmpty(token))
            {
                response.Payload = JsonConvert.SerializeObject(new
                {
                    Message = "User successfully logged in",
                    Token = token
                });
                response.StatusCode = StatusCode.Ok;
            }
            else
            {
                response.Payload = "Invalid username or password";
                response.StatusCode = StatusCode.Unauthorized; 
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during user login: {ex.Message}");
            response.Payload = "Internal server error";
            response.StatusCode = StatusCode.InternalServerError;
        }

        return response;
    }

}
