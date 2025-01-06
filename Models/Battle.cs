using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trimmel_MCTG.db;

namespace Trimmel_MCTG.DB
{
    public class Battle
    {
        public int BattleId { get; set; }
        public int User1Id { get; set; }
        public int User2Id { get; set; }
        public int? WinnerId { get; set; } 

        public Users? User1 { get; set; }
        public Users? User2 { get; set; }
        public Users? Winner { get; set; }

        public Battle(int user1Id, int user2Id)
        {
            User1Id = user1Id;
            User2Id = user2Id;
        }
    }
}
