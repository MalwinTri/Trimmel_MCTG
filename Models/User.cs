using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trimmel_MCTG.DB
{
    public class User
    {
        public int UserId { get; set; }
        public string ?Username { get; set; }
        public string ?Password { get; set; }
        public int Coins { get; set; } = 20; // Default value for coins
        public string Token => $"{Username}-msgToken"; 
    }

}
