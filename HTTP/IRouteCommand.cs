using MCTG_Trimmel.HTTP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trimmel_MCTG.db;

namespace Trimmel_MCTG.HTTP
{
    // Interface IRouteCommand - definiert die grundlegenden Operationen für eine Routenanfrage
    public interface IRouteCommand
    {
        // Führt den Befehl aus und gibt eine entsprechende Antwort zurück
        Response Execute();

        // Setzt die Datenbankinstanz für die spätere Verwendung im Befehl
        void SetDatabase(Database db);
    }
}
