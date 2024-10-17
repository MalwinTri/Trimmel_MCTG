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

        public User ?User { get; set; } 
        public Card ?Card1 { get; set; } 
        public Card ?Card2 { get; set; } 
        public Card ?Card3 { get; set; } 
        public Card ?Card4 { get; set; } 
    }

}
