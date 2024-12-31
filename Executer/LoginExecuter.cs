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
        // Setze den RequestContext und prüfe, ob dieser nicht null ist
        requestContext = request ?? throw new ArgumentNullException(nameof(request), "Request context cannot be null");

        // Überprüfe, ob das Payload nicht null oder leer ist
        if (string.IsNullOrEmpty(requestContext.Payload))
        {
            throw new InvalidDataException("Payload cannot be null or empty.");
        }

        // Deserialisiere die Benutzerdaten aus dem Payload
        user = JsonConvert.DeserializeObject<Users>(requestContext.Payload) ?? throw new InvalidDataException("Invalid user data in the payload.");
    }

    public IRoute IRoute
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
            // Versuche, den Benutzer über die Datenbank anzumelden
            if (db.Logging(user))
            {
                // Wenn die Anmeldung erfolgreich war, setze die Payload und den StatusCode auf OK
                response.Payload = "User successfully logged in";
                response.StatusCode = StatusCode.Ok;
            }
            else
            {
                // Wenn die Anmeldung fehlschlägt, setze die Payload und den StatusCode auf Unauthorized
                response.Payload = "Invalid username or password";
                response.StatusCode = StatusCode.Unauthorized; // 401 Unauthorized passt besser zu einem Login-Fehler
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
