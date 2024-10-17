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

        public User ?User1 { get; set; } 
        public User ?User2 { get; set; } 
        public User ?Winner { get; set; } 
    }

}
