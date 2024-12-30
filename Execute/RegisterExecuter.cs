using MCTG_Trimmel.HTTP;
using Newtonsoft.Json;
using Trimmel_MCTG.db;
using Trimmel_MCTG.DB;
using Trimmel_MCTG.HTTP;

public class RegisterExecuter : IRouteCommand
{
    private User user;
    private readonly RequestContext requestContext;
    private Database db;

    public RegisterExecuter(RequestContext request)
    {
        // Setze den RequestContext und prüfe, ob dieser nicht null ist
        requestContext = request ?? throw new ArgumentNullException(nameof(request), "Request context cannot be null");

        // Überprüfe, ob das Payload nicht null oder leer ist
        if (string.IsNullOrEmpty(requestContext.Payload))
        {
            throw new InvalidDataException("Payload cannot be null or empty.");
        }

        // Deserialisiere die Benutzerdaten aus dem Payload
        user = JsonConvert.DeserializeObject<User>(requestContext.Payload) ?? throw new InvalidDataException("Invalid user data in the payload.");
    }

    public IRoute IRoute
    {
        get => default;
        set
        {
        }
    }

    public Trimmel_MCTG.hash.PasswordHasher PasswordHasher
    {
        get => default;
        set
        {
        }
    }

    public void SetDatabase(Database db)
    {
        // Setze die Datenbankinstanz und prüfe, ob diese nicht null ist
        this.db = db ?? throw new ArgumentNullException(nameof(db), "Database cannot be null");
    }

    public Response Execute()
    {
        // Überprüfe, ob die Datenbankverbindung gesetzt wurde, bevor mit der Ausführung fortgefahren wird
        if (db == null)
        {
            throw new InvalidOperationException("Database connection has not been set.");
        }

        var response = new Response();
        try
        {
            // Versuche, den Benutzer in der Datenbank zu registrieren
            if (db.CheckAndRegister(user))
            {
                // Wenn die Registrierung erfolgreich war, setze die Payload und den StatusCode auf Created
                response.Payload = "User created successfully";
                response.StatusCode = StatusCode.Created;
            }
            else
            {
                // Wenn die Registrierung fehlschlägt, setze die Payload und den StatusCode auf BadRequest
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
