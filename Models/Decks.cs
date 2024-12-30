using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trimmel_MCTG.DB
{
    public class Decks
    {
        public int DeckId { get; set; }
        public int UserId { get; set; }
        public int Card1Id { get; set; }
        public int Card2Id { get; set; }
        public int Card3Id { get; set; }
        public int Card4Id { get; set; }

        public Users ?User { get; set; } 
        public Cards ?Card1 { get; set; } 
        public Cards ?Card2 { get; set; } 
        public Cards ?Card3 { get; set; } 
        public Cards ?Card4 { get; set; }

        public db.Database Database
        {
            get => default;
            set
            {
            }
        }
    }

}
