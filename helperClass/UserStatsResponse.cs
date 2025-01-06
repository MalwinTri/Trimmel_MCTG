using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trimmel_MCTG.helperClass
{
    public class UserStatsResponse
    {
        public int UserId { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Elo { get; set; }
    }
}
