using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trimmel_MCTG.DB
{
    public class Trade
    {
        public int TradeId { get; set; }
        public int UserId { get; set; }
        public int OfferedCardId { get; set; }
        public CardType RequiredType { get; set; } 
        public int MinDamage { get; set; }

        public User ?User { get; set; } 
        public Card ?OfferedCard { get; set; } 
    }

}
