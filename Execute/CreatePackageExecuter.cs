using MCTG_Trimmel.HTTP;
using Newtonsoft.Json;
using Trimmel_MCTG.db;
using Trimmel_MCTG.DB;
using Trimmel_MCTG.HTTP;

public class CreatePackageExecuter : IRouteCommand
{
    private RequestContext requestContext;
    private Database db;

    public CreatePackageExecuter(RequestContext requestContext)
    {
        this.requestContext = requestContext;
    }

    public void SetDatabase(Database db)
    {
        this.db = db;
    }

    public Response Execute()
    {
        var response = new Response();

        // Überprüfung, ob der Benutzer ein Admin ist
        if (requestContext.Token != "admin-mtcgToken")
        {
            response.StatusCode = StatusCode.Unauthorized;
            response.Payload = "Only admins can create packages.";
            return response;
        }

        try
        {
            // Karten aus der Anfrage laden
            var cards = JsonConvert.DeserializeObject<List<Card>>(requestContext.Payload);

            // Überprüfen, ob genau 5 Karten übermittelt wurden
            if (cards == null || cards.Count != 5)
            {
                response.StatusCode = StatusCode.BadRequest;
                response.Payload = "A package must contain exactly 5 cards.";
                return response;
            }

            // Neues Paket erstellen
            int packageId = db.InsertPackage();

            foreach (var card in cards)
            {
                // Karteigenschaften festlegen
                card.SetElementType();
                card.SetCardType();

                // CardType ist jetzt ein string, kein Enum mehr
                if (card.CardType != "spell" && card.CardType != "monster")
                {
                    response.StatusCode = StatusCode.BadRequest;
                    response.Payload = $"Invalid card type: {card.CardType}";
                    return response;
                }

                // Karte speichern und verknüpfen
                db.InsertCard(card);
                db.LinkCardToPackage(packageId, card.CardId);
            }




            response.StatusCode = StatusCode.Created;
            response.Payload = "Package and cards successfully created.";
        }
        catch (Exception ex)
        {
            response.StatusCode = StatusCode.InternalServerError;
            response.Payload = $"An error occurred: {ex.Message}";
        }

        return response;
    }
}
