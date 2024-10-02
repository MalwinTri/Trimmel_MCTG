using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trimmel_MCTG.DB
{
    public class Battle
    {
        public int BattleId { get; set; }
        public int User1Id { get; set; }
        public int User2Id { get; set; }
        public int WinnerId { get; set; }

        public User User1 { get; set; } // Navigation property to User 1
        public User User2 { get; set; } // Navigation property to User 2
        public User Winner { get; set; } // Navigation property to the winner
    }

}
