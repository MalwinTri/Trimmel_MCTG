using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trimmel_MCTG.db;

namespace Trimmel_MCTG.helperClass
{
    public class BattleResult
    {
        public Users Winner { get; set; }
        public Users Loser { get; set; }
        public string Logs { get; set; }
    }
}
