using MCTG_Trimmel.HTTP;
using Trimmel_MCTG.db;
using Trimmel_MCTG.HTTP;

namespace Trimmel_MCTG.Execute
{
    public class ErrorExecuter : IRouteCommand
    {
        private readonly string errorMessage;
        private readonly StatusCode statusCode;

        public ErrorExecuter(string errorMessage, StatusCode statusCode = StatusCode.BadRequest)
        {
            this.errorMessage = errorMessage;
            this.statusCode = statusCode;
        }

        public void SetDatabase(Database database)
        {
            // Keine Datenbank erforderlich für Fehlerbehandlung
        }

        public Response Execute()
        {
            return new Response
            {
                StatusCode = statusCode,
                Payload = errorMessage
            };
        }
    }
}
