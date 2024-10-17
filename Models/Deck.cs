using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trimmel_MCTG.DB
{
    public class Deck
    {
        public int DeckId { get; set; }
        public int UserId { get; set; }
        public int Card1Id { get; set; }
        public int Card2Id { get; set; }
        public int Card3Id { get; set; }
        public int Card4Id { get; set; }

        public User ?User { get; set; } // Navigation property to the User entity
        public Card ?Card1 { get; set; } // Navigation property to Card 1
        public Card ?Card2 { get; set; } // Navigation property to Card 2
        public Card ?Card3 { get; set; } // Navigation property to Card 3
        public Card ?Card4 { get; set; } // Navigation property to Card 4
    }

}
