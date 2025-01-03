using System;
using System.Collections.Generic;
using MCTG_Trimmel.HTTP;
using Trimmel_MCTG.db;
using Trimmel_MCTG.HTTP;

internal class DeleteTradingDealExecuter : IRouteCommand
{
    private readonly RequestContext requestContext;
    private readonly string tradingId;
    private Database db;

    public DeleteTradingDealExecuter(RequestContext requestContext, string tradingId)
    {
        this.requestContext = requestContext;
        this.tradingId = tradingId;
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
            // tradingId überprüfen und in GUID umwandeln
            if (!Guid.TryParse(tradingId, out var parsedTradingId))
            {
                response.Payload = "Invalid trading ID (must be a valid GUID).";
                response.StatusCode = StatusCode.BadRequest;
                return response;
            }

            // Prüfen, ob der Datensatz existiert
            var parameters = new Dictionary<string, object> { { "@tradingId", parsedTradingId } };
            var result = db.ExecuteQuery("SELECT * FROM trading WHERE tradingid = @tradingId", parameters);

            if (result.Count == 0)
            {
                response.Payload = "Trading deal not found.";
                response.StatusCode = StatusCode.NotFound;
                return response;
            }

            // Löschen
            db.ExecuteNonQuery("DELETE FROM trading WHERE tradingid = @tradingId", parameters);

            response.Payload = "Trading deal deleted successfully.";
            response.StatusCode = StatusCode.Ok;
        }
        catch (Exception ex)
        {
            response.Payload = $"An error occurred: {ex.Message}";
            response.StatusCode = StatusCode.InternalServerError;
        }

        return response;
    }
}
