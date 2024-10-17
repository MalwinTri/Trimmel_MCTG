using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trimmel_MCTG.DB
{
    public class PackageCard
    {
        public int PackageId { get; set; }
        public int CardId { get; set; }

        public Package ?Package { get; set; } 
        public Card ?Card { get; set; } 
    }

}
