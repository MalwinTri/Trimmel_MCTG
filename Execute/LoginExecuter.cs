using MCTG_Trimmel.HTTP;
using Newtonsoft.Json;
using Trimmel_MCTG.db;
using Trimmel_MCTG.DB;
using Trimmel_MCTG.HTTP;

public class LoginExecuter : IRouteCommand
{
    private readonly User user;
    private readonly RequestContext requestContext;
    private Database db;

    public LoginExecuter(RequestContext request)
    {
        requestContext = request ?? throw new ArgumentNullException(nameof(request), "Request context cannot be null");

        if (string.IsNullOrEmpty(requestContext.Payload))
        {
            throw new InvalidDataException("Payload cannot be null or empty.");
        }

        user = JsonConvert.DeserializeObject<User>(requestContext.Payload) ?? throw new InvalidDataException("Invalid user data in the payload.");
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
            if (db.Logging(user))
            {
                response.Payload = "User successfully logged in";
                response.StatusCode = StatusCode.Ok;
            }
            else
            {
                response.Payload = "Invalid username or password";
                response.StatusCode = StatusCode.Unauthorized; // 401 Unauthorized fits better for login failure
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
