using System.Collections.Generic;

namespace Trimmel_MCTG.DB
{
    public class Package
    {
        public int PackageId { get; set; }                
        public int Price { get; set; } = 5;            
        public List<PackageCard> PackageCards { get; set; } = new List<PackageCard>();


        public Package(int packageId, int price)
        {
            PackageId = packageId;
            Price = price;
        }

        public override string ToString()
        {
            return $"Package ID: {PackageId}, Price: {Price}, Cards Count: {PackageCards.Count}";
        }
    }
}
