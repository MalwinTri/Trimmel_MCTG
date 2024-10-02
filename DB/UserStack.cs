using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trimmel_MCTG.DB
{
    public class UserStack
    {
        public int UserId { get; set; }
        public int CardId { get; set; }
        public bool InDeck { get; set; } = false;

        public User User { get; set; } // Navigation property to the User entity
        public Card Card { get; set; } // Navigation property to the Card entity
    }

}
