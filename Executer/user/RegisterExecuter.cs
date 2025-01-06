using MCTG_Trimmel.HTTP;
using Newtonsoft.Json;
using Trimmel_MCTG.db;
using Trimmel_MCTG.DB;
using Trimmel_MCTG.HTTP;

public class RegisterExecuter : IRouteCommand
{
    private Users user;
    private readonly RequestContext requestContext;
    private Database db;

    public RegisterExecuter(RequestContext request)
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
            if (db.CheckAndRegister(user))
            {
                response.Payload = "User created successfully";
                response.StatusCode = StatusCode.Created;
            }
            else
            {
                response.Payload = "User could not be created";
                response.StatusCode = StatusCode.BadRequest;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during user registration: {ex.Message}");
            response.Payload = "Internal server error";
            response.StatusCode = StatusCode.InternalServerError;
        }

        return response;
    }
}
