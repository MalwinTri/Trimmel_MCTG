using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trimmel_MCTG.HTTP
{
    // Interface IRoute - definiert eine Methode zur Auflösung von Anfragen in Befehle
    public interface IRoute
    {
        Route Route { get; set; }

        // Methode zur Auflösung einer Anfrage (RequestContext) in einen entsprechenden Routenbefehl (IRouteCommand)
        IRouteCommand? Resolve(RequestContext request);
    }
}
