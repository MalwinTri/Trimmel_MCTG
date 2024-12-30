using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trimmel_MCTG.DB
{
    public class UserStats
    {
        public int UserId { get; set; }
        public int Wins { get; set; } = 0;
        public int Losses { get; set; } = 0;
        public int Elo { get; set; } = 1000; 

        public Users ?User { get; set; }

        public Battle Battle
        {
            get => default;
            set
            {
            }
        }

        public db.Database Database
        {
            get => default;
            set
            {
            }
        }
    }

}
