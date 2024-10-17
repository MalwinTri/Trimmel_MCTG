using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trimmel_MCTG.DB
{
    public class Card
    {
        public int CardId { get; set; }
        public string ?Name { get; set; }
        public int Damage { get; set; }
        public string ?ElementType { get; set; }
        public CardType CardType { get; set; } 
    }

    public enum CardType
    {
        Spell,
        Monster
    }

}
