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
        public int Coins { get; set; } = 20; 
        public string Token => $"{Username}-msgToken";

        public Deck Deck
        {
            get => default;
            set
            {
            }
        }

        public UserStack UserStack
        {
            get => default;
            set
            {
            }
        }

        public UserStats UserStats
        {
            get => default;
            set
            {
            }
        }

        public Trade Trade
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
