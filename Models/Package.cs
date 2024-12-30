using System.Collections.Generic;

namespace Trimmel_MCTG.DB
{
    public class Package
    {
        // Eigenschaften
        public int PackageId { get; set; }                // Eindeutige ID des Pakets
        public int Price { get; set; } = 5;              // Standardpreis des Pakets (in Coins)

        // Enthaltene Karten im Paket
        public List<PackageCard> PackageCards { get; set; } = new List<PackageCard>();

        // Konstruktoren

        public Package(int packageId, int price)
        {
            PackageId = packageId;
            Price = price;
        }

        // Methode zur Ausgabe der Paketinformationen
        public override string ToString()
        {
            return $"Package ID: {PackageId}, Price: {Price}, Cards Count: {PackageCards.Count}";
        }
    }
}
