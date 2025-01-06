using MCTG_Trimmel.HTTP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trimmel_MCTG.db;

namespace Trimmel_MCTG.HTTP
{
    public interface IRouteCommand
    {
        Database Database { get; set; }
        Route Route { get; set; }

        Response Execute();

        void SetDatabase(Database db);
    }
}
