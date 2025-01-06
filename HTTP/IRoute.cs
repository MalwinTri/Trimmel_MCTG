using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trimmel_MCTG.HTTP
{
    public interface IRoute
    {
        Route Route { get; set; }

        IRouteCommand? Resolve(RequestContext request);
    }
}
