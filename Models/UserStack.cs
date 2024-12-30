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

        public User ?User { get; set; } 
        public Card ?Card { get; set; }

        public Deck Deck
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
