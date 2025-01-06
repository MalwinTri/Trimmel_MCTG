using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ShowStatsExecuter;

namespace Trimmel_MCTG.helperClass
{
    public class StatsResponse
    {
        public string Username { get; set; }
        public UserStatsResponse UserStats { get; set; }
    }
}
